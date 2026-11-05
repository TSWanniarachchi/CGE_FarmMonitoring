#!/usr/bin/env python3
"""
Farm FMS PreToolUse guard hook (configured in .claude/settings.json).

Reads the PreToolUse JSON payload from stdin and answers with a permission decision on stdout:
    {"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny" | "ask", "permissionDecisionReason": "..."}}
No output (exit 0) means "no opinion": the normal permission rules apply.

Shell tools (Bash, PowerShell) - every session and agent:
  deny  git commit / push / history rewrites / destructive resets and cleans; printing user-secrets, secrets.json or
        environment variables; database commands that mention production.
  ask   database-changing commands (dotnet ef database ..., migration bundles, sqlcmd, Invoke-Sqlcmd, sqlpackage, ...)
        and user-secrets changes.
  code-reviewer: inspection commands only.

File tools (Read, Edit, Write, NotebookEdit):
  deny  any access to secrets.json or the user-secrets store.
  deny  writes by a project agent outside its documented paths (agent_type from the payload).

LIMITATIONS - this is a guard rail, not a security boundary: it matches command text with patterns, so commands
hidden in scripts, aliases, encodings or other interpreters may not be recognised. Permission rules in
settings.json are a second layer. Reasons never echo the command (it could contain a secret).
On an internal error the hook answers "ask" so a human decides.
"""

from __future__ import annotations

import fnmatch
import json
import os
import re
import sys
from pathlib import PurePosixPath

# --------------------------------------------------------------------------------------------------------------
# Shell command rules
# --------------------------------------------------------------------------------------------------------------

GIT = r"\bgit(?:\s+-(?:C|c)\s+\S+|\s+--?[\w-]+(?:=\S+)?)*\s+"

DENY_COMMAND_RULES: list[tuple[re.Pattern[str], str]] = [
    (re.compile(GIT + r"commit\b", re.I), "Agents never run 'git commit'; only the user commits."),
    (re.compile(GIT + r"push\b", re.I), "'git push' is never executed automatically; only the user pushes."),
    (re.compile(GIT + r"(?:filter-branch|filter-repo|replace)\b", re.I), "Git history rewriting is not allowed."),
    (re.compile(GIT + r"rebase\b", re.I), "'git rebase' rewrites history and is not allowed."),
    (re.compile(GIT + r"reset\b.*--hard\b", re.I), "'git reset --hard' discards work and is not allowed."),
    (re.compile(GIT + r"clean\b", re.I), "'git clean' deletes files and is not allowed."),
    (re.compile(GIT + r"(?:checkout|restore)\b(?!.*--staged).*\s(?:--\s+)?\.(?:\s|$)", re.I), "Discarding working-tree changes is not allowed."),
    (re.compile(GIT + r"stash\s+(?:drop|clear)\b", re.I), "Dropping stashes is not allowed."),
    (re.compile(GIT + r"branch\s+.*-D\b", re.I), "Force-deleting branches is not allowed."),
    (re.compile(GIT + r"(?:update-ref\s+-d|reflog\s+(?:expire|delete)|gc\b.*--prune)", re.I), "Destructive Git maintenance is not allowed."),
    (re.compile(r"\buser-secrets\s+list\b", re.I), "Listing user-secrets would print secrets."),
    (re.compile(r"secrets\.json", re.I), "Local secrets.json files must not be read or changed."),
    (re.compile(r"Microsoft[\\/]+UserSecrets", re.I), "The user-secrets store must not be accessed."),
    (re.compile(r"(?:^|[;&|(]\s*)(?:printenv|env|export\s+-p|set)\s*(?:$|[;&|)])", re.I), "Printing environment variables may expose secrets."),
    (re.compile(r"\b(?:Get-ChildItem|gci|dir|ls|Get-Item)\s+Env:", re.I), "Printing environment variables may expose secrets."),
    (re.compile(r"GetEnvironmentVariables\s*\(", re.I), "Printing environment variables may expose secrets."),
    (re.compile(r"(?:\$env:|\$\{?|%)\s*\w*(?:ConnectionStrings|Password|Secret|ApiKey|Token)\w*", re.I), "Reading secret-bearing environment variables is not allowed."),
]

DATABASE_COMMAND = re.compile(
    r"\bdotnet\s+ef\s+(?:database\b|migrations\s+(?:bundle|remove)\b)"
    r"|\befbundle\b|\bsqlcmd\b|\bosql\b|\bbcp\b|\bsqlpackage\b|\bInvoke-Sqlcmd\b|\bInvoke-DbaQuery\b",
    re.I,
)
PRODUCTION_MARKER = re.compile(r"\bprod(?:uction)?\b|--environment\s+Production|ASPNETCORE_ENVIRONMENT\s*=\s*Production", re.I)
USER_SECRETS_CHANGE = re.compile(r"\buser-secrets\s+(?:set|remove|clear|init)\b", re.I)

# The code-reviewer may only inspect.
REVIEWER_ALLOWED = re.compile(
    r"^\s*(?:git\s+(?:status|diff|log|show|blame|ls-files|rev-parse|branch\s*$|branch\s+--(?:list|show-current))\b"
    r"|dotnet\s+(?:build|test|list)\b"
    r"|python3?\s+\S*run_gates\.py\b"
    r"|(?:ls|pwd|wc|head|tail|cat|grep|rg|find|sort|uniq)\b)",
    re.I,
)
SEGMENT_SPLIT = re.compile(r"&&|\|\||;|\||\n")

# --------------------------------------------------------------------------------------------------------------
# File rules
# --------------------------------------------------------------------------------------------------------------

SECRET_PATH = re.compile(r"(?:^|/)secrets\.json$|Microsoft/UserSecrets/", re.I)

# Write boundaries per project agent (repo-relative, POSIX, glob patterns). Main session and other agents: no limit.
AGENT_WRITE_ALLOW: dict[str, list[str]] = {
    "business-analyst": ["Documentation/Requirements/**", "Documentation/Business_Design/**"],
    "solution-architect": ["Documentation/Specs/**"],
    "database-engineer": [
        "Backend/src/FarmFMS.Infrastructure/Persistence/**",
        "Database/StoredProcedures/**",
        "Backend/src/FarmFMS.Infrastructure/Data/StoredProcedureErrors.cs",
        "Backend/src/FarmFMS.Infrastructure/Repositories/*/*Procedures.cs",
    ],
    "backend-developer": ["Backend/src/**", "Backend/tests/FarmFMS.UnitTests/**"],
    "frontend-developer": ["Frontend/**"],
    "qa-engineer": ["Backend/tests/**", "Frontend/tests/**", "Frontend/**/*.Tests/**"],
    "code-reviewer": [],
}
AGENT_WRITE_DENY: dict[str, list[str]] = {
    "backend-developer": [
        "Backend/src/FarmFMS.Infrastructure/Persistence/Configurations/**",
        "Backend/src/FarmFMS.Infrastructure/Persistence/Migrations/**",
        "Backend/src/FarmFMS.Infrastructure/Data/StoredProcedureErrors.cs",
        "Backend/src/FarmFMS.Infrastructure/Repositories/*/*Procedures.cs",
    ],
}
WRITE_TOOLS = {"Edit", "Write", "NotebookEdit", "MultiEdit"}
READ_TOOLS = {"Read"}


def decision(kind: str, reason: str) -> dict:
    return {"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": kind,
                                   "permissionDecisionReason": f"Farm FMS guard: {reason}"}}


def check_command(command: str, agent_type: str) -> dict | None:
    for pattern, reason in DENY_COMMAND_RULES:
        if pattern.search(command):
            return decision("deny", reason)

    if DATABASE_COMMAND.search(command):
        if PRODUCTION_MARKER.search(command):
            return decision("deny", "Production database operations are not allowed.")
        if agent_type:
            return decision("deny", "Agents never run database-changing commands; ask the user.")
        return decision("ask", "Database-changing command: confirm the target is a non-production database and the change is approved.")

    if USER_SECRETS_CHANGE.search(command):
        return decision("ask", "user-secrets change: the value must not appear in the transcript.")

    if agent_type == "code-reviewer":
        segments = [s.strip() for s in SEGMENT_SPLIT.split(command) if s.strip()]
        if not segments or not all(REVIEWER_ALLOWED.match(s) for s in segments):
            return decision("deny", "The code-reviewer is read-only; only inspection commands are allowed.")
    return None


def to_repo_relative(file_path: str, project_dir: str) -> str | None:
    """Repo-relative POSIX path, or None when the path is outside the project."""
    absolute = os.path.normcase(os.path.abspath(os.path.join(project_dir, file_path)))
    root = os.path.normcase(os.path.abspath(project_dir))
    if absolute != root and not absolute.startswith(root.rstrip("\\/") + os.sep):
        return None
    relative = os.path.relpath(absolute, root).replace("\\", "/")
    # normcase lower-cases on Windows; map back to real casing for matching against our (case-insensitive) globs.
    return relative


def matches(relative: str, patterns: list[str]) -> bool:
    candidate = relative.lower()
    for pattern in patterns:
        p = pattern.lower()
        if fnmatch.fnmatchcase(candidate, p) or PurePosixPath(candidate).match(p):
            return True
        if p.endswith("/**") and (candidate == p[:-3] or candidate.startswith(p[:-2])):
            return True
    return False


def check_file(tool_name: str, tool_input: dict, agent_type: str, project_dir: str, scratchpad: str) -> dict | None:
    file_path = str(tool_input.get("file_path") or tool_input.get("notebook_path") or tool_input.get("path") or "")
    if not file_path:
        return None
    normalized = file_path.replace("\\", "/")
    if SECRET_PATH.search(normalized):
        return decision("deny", "secrets.json / user-secrets files must not be read or changed.")

    if tool_name not in WRITE_TOOLS or agent_type not in AGENT_WRITE_ALLOW:
        return None

    if scratchpad and os.path.normcase(os.path.abspath(file_path)).startswith(os.path.normcase(os.path.abspath(scratchpad))):
        return None
    relative = to_repo_relative(file_path, project_dir)
    if relative is None:
        return decision("deny", f"{agent_type} may not write outside the project.")
    if matches(relative, AGENT_WRITE_DENY.get(agent_type, [])) or not matches(relative, AGENT_WRITE_ALLOW[agent_type]):
        return decision("deny", f"{agent_type} may not write to '{relative}' (outside its documented paths).")
    return None


def main() -> int:
    try:
        payload = json.load(sys.stdin)
        tool_name = str(payload.get("tool_name") or "")
        tool_input = payload.get("tool_input") or {}
        agent_type = str(payload.get("agent_type") or "")
        project_dir = os.environ.get("CLAUDE_PROJECT_DIR") or str(payload.get("cwd") or os.getcwd())
        scratchpad = str(payload.get("scratchpad_dir") or "")

        if tool_name in {"Bash", "PowerShell"}:
            result = check_command(str(tool_input.get("command") or ""), agent_type)
        elif tool_name in WRITE_TOOLS | READ_TOOLS:
            result = check_file(tool_name, tool_input, agent_type, project_dir, scratchpad)
        else:
            result = None
    except Exception:  # noqa: BLE001 - fail to a human decision, never silently allow
        result = decision("ask", "the guard hook could not evaluate this call; review it manually.")

    if result:
        sys.stdout.write(json.dumps(result))
    return 0


if __name__ == "__main__":
    sys.exit(main())
