# Farm FMS – Architecture Decisions (Superseded)

| | |
|---|---|
| Status | **Superseded** on 2026-11-04 |
| Replaced by | [`FarmFMS_Architecture_Decisions_v2.md`](FarmFMS_Architecture_Decisions_v2.md), the single authoritative architecture decision record |

This file is kept only so existing links resolve. It contains no decisions.

Its non-conflicting content has been reconciled into v2:
- implementation status of AD-04, AD-05, AD-07, AD-14, AD-15, AD-16 and AD-23;
- the implemented error mapping in AD-09;
- AD-24 (dbo schema).

Its earlier naming rules (PMS prefixes `Cpk`/`Cun`/`Cck`/`I{u|n}{c|n}`, the abbreviation list without `Rol`, and the 15-character identifier limit) are **withdrawn**. They were replaced by v2 AD-08: the approved `PK`/`UQ`/`IX`/`DF`/`CK` patterns with descriptive names and no custom length limit.
