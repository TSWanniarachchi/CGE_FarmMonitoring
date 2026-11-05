#!/usr/bin/env python3
"""
Farm FMS quality-gate runner (G1 build, G2 unit tests, G3 architecture tests).

Runs only the approved Release commands:
    dotnet build <solution> -c Release -nologo
    dotnet test  <solution> -c Release --no-build -nologo
and prints a concise report.

It never runs migrations, database scripts or user-secrets commands, never prints environment variables, and
masks anything that looks like a credential in the output it echoes. It does not edit project files; `dotnet build`
writes only build output (bin/ and obj/).

Usage (from anywhere inside the repository):
    python .claude/skills/farmfms-quality-gates/run_gates.py              # Backend
    python .claude/skills/farmfms-quality-gates/run_gates.py --frontend   # also build the Frontend solution
    python .claude/skills/farmfms-quality-gates/run_gates.py --build-only

Exit code: 0 when every executed gate passed, 1 when a gate failed, 2 on usage / environment errors.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
BACKEND_SLN = REPO_ROOT / "Backend" / "FarmFMS.sln"
FRONTEND_SLN = REPO_ROOT / "Frontend" / "FarmFMS.Web.sln"

BUILD_TIMEOUT_SECONDS = 900
TEST_TIMEOUT_SECONDS = 1200
MAX_LISTED_ISSUES = 15

# Credential-looking fragments are masked before anything is printed.
KEY_VALUE_SECRET = re.compile(
    r"(?i)\b(password|pwd|user\s*id|uid|secret|api[_-]?key|token|accountkey|sharedaccesskey)(\s*=\s*)[^;\s\"']+")
BEARER_SECRET = re.compile(r"(?i)\b(bearer\s+)[A-Za-z0-9\-._~+/]+=*")

BUILD_SUMMARY = re.compile(r"^\s*(\d+)\s+(Warning|Error)\(s\)", re.MULTILINE)
DIAGNOSTIC = re.compile(r":\s*(warning|error)\s+([A-Z]+\d+)\s*:", re.IGNORECASE)
TEST_SUMMARY = re.compile(
    r"(?P<outcome>Passed|Failed)!\s+-\s+Failed:\s+(?P<failed>\d+),\s+Passed:\s+(?P<passed>\d+),"
    r"\s+Skipped:\s+(?P<skipped>\d+),\s+Total:\s+(?P<total>\d+).*?-\s+(?P<assembly>[\w.]+\.dll)"
)
FAILED_TEST = re.compile(r"^\s*Failed\s+(?P<name>[\w.<>`,\[\]() ]+?)\s+\[", re.MULTILINE)


def mask(text: str) -> str:
    text = KEY_VALUE_SECRET.sub(r"\1\2<masked>", text)
    return BEARER_SECRET.sub(r"\1<masked>", text)


def run(command: list[str], timeout: int) -> tuple[int, str]:
    try:
        completed = subprocess.run(
            command,
            cwd=REPO_ROOT,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=timeout,
        )
        return completed.returncode, (completed.stdout or "") + (completed.stderr or "")
    except FileNotFoundError:
        return 127, f"Command not found: {command[0]}"
    except subprocess.TimeoutExpired:
        return 124, f"Timed out after {timeout} s: {' '.join(command)}"


def build_gate(solution: Path) -> dict:
    exit_code, output = run(["dotnet", "build", str(solution), "-c", "Release", "-nologo"], BUILD_TIMEOUT_SECONDS)
    counts = {kind.lower(): int(n) for n, kind in BUILD_SUMMARY.findall(output)}
    issues = sorted({mask(line.strip()) for line in output.splitlines() if DIAGNOSTIC.search(line)})
    locked = "MSB3027" in output or "MSB3021" in output
    warnings, errors = counts.get("warning"), counts.get("error")
    passed = exit_code == 0 and warnings == 0 and errors == 0
    return {
        "name": f"G1 Build ({solution.relative_to(REPO_ROOT).as_posix()}, Release)",
        "passed": passed,
        "exit_code": exit_code,
        "detail": f"{warnings if warnings is not None else '?'} warning(s), {errors if errors is not None else '?'} error(s)",
        "issues": issues,
        "note": "Output files are locked by another process (stop the running API / Visual Studio debug session)." if locked else
                ("" if warnings is not None else mask(output.strip().splitlines()[-1]) if output.strip() else "No build output."),
    }


def test_gates(solution: Path) -> list[dict]:
    exit_code, output = run(["dotnet", "test", str(solution), "-c", "Release", "--no-build", "-nologo"], TEST_TIMEOUT_SECONDS)
    summaries = [m.groupdict() for m in TEST_SUMMARY.finditer(output)]
    failed_names = sorted({mask(m.group("name").strip()) for m in FAILED_TEST.finditer(output)})

    def gate(name: str, predicate) -> dict:
        rows = [s for s in summaries if predicate(s["assembly"])]
        if not rows:
            return {"name": name, "passed": False, "exit_code": exit_code, "detail": "no test results found",
                    "issues": [], "note": "Test assembly not found or not built (run the build gate first)."}
        failed = sum(int(r["failed"]) for r in rows)
        detail = "; ".join(
            f"{r['assembly']}: {r['passed']} passed, {r['failed']} failed, {r['skipped']} skipped (total {r['total']})" for r in rows)
        return {"name": name, "passed": failed == 0, "exit_code": exit_code, "detail": detail,
                "issues": [n for n in failed_names] if failed else [], "note": ""}

    gates = [
        gate("G2 Unit tests", lambda a: a.endswith("UnitTests.dll")),
        gate("G3 Architecture tests", lambda a: a.endswith("ArchitectureTests.dll")),
    ]
    others = [s for s in summaries if not s["assembly"].endswith(("UnitTests.dll", "ArchitectureTests.dll"))]
    if others:
        gates.append(gate("Other test assemblies", lambda a: not a.endswith(("UnitTests.dll", "ArchitectureTests.dll"))))
    if exit_code != 0 and all(g["passed"] for g in gates):
        gates.append({"name": "dotnet test exit code", "passed": False, "exit_code": exit_code,
                      "detail": "non-zero exit code without failed tests", "issues": [],
                      "note": mask(output.strip().splitlines()[-1]) if output.strip() else ""})
    return gates


def print_report(results: list[dict]) -> None:
    print("Farm FMS quality gates (Release)")
    print("=" * 32)
    for result in results:
        status = "PASS" if result["passed"] else "FAIL"
        print(f"[{status}] {result['name']}: {result['detail']} (exit code {result['exit_code']})")
        for issue in result["issues"][:MAX_LISTED_ISSUES]:
            print(f"        - {issue}")
        if len(result["issues"]) > MAX_LISTED_ISSUES:
            print(f"        ... {len(result['issues']) - MAX_LISTED_ISSUES} more")
        if result["note"]:
            print(f"        note: {result['note']}")
    overall = all(r["passed"] for r in results)
    print("-" * 32)
    print(f"Overall: {'PASS' if overall else 'FAIL'} (G0, G4-G8 are manual / review gates; see SKILL.md)")


def main() -> int:
    parser = argparse.ArgumentParser(description="Run Farm FMS quality gates G1-G3 (Release).")
    parser.add_argument("--frontend", action="store_true", help="also build the Frontend solution (G1)")
    parser.add_argument("--build-only", action="store_true", help="run G1 only")
    args = parser.parse_args()

    if not BACKEND_SLN.is_file():
        print(f"Backend solution not found: {BACKEND_SLN}", file=sys.stderr)
        return 2

    results = [build_gate(BACKEND_SLN)]
    if args.frontend:
        if FRONTEND_SLN.is_file():
            results.append(build_gate(FRONTEND_SLN))
        else:
            print(f"Frontend solution not found: {FRONTEND_SLN}", file=sys.stderr)
            return 2
    if not args.build_only:
        if results[0]["passed"] or results[0]["exit_code"] == 0:
            results.extend(test_gates(BACKEND_SLN))
        else:
            results.append({"name": "G2/G3 Tests", "passed": False, "exit_code": -1,
                            "detail": "skipped: the Backend build failed", "issues": [], "note": ""})

    print_report(results)
    return 0 if all(r["passed"] for r in results) else 1


if __name__ == "__main__":
    sys.exit(main())
