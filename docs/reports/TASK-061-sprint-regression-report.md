# TASK-061 — Sprint regression fixtures and Balance Lab

PASS, 2026-09-08. Six fixed-seed scenarios now run through the complete field/session and
shared shooting geometry: perfect, intentional one-miss, aggressive, conservative, wet snow
and glide skis. The reference controller uses bounded aim commands and explicit tuning data;
it cannot set target outcomes. It is a regression harness, not a gameplay difficulty preset.

324/324 standalone and Unity EditMode pass; Console zero errors/four upstream warnings.
Evidence: artifacts/physiology/wp061-{standalone.trx,editmode.json,console.json}.
Run-SprintLab.ps1 exports context, all tick CSV, events, results and command tape for every
scenario; every exported tape is replayed before success. Two CLI runs match trace hashes.
Full .NET/Unity trace/event SHA-256 values are pinned in tests/fixtures/sprint-traces-v1.sha256.
Summary retained in docs/quality/m5-sprint-regression.json.

Measured finishes: perfect 379.8613 s / zero penalties; one-miss 394.4263 s / one 150 m circuit;
aggressive 362.2675 s; conservative 409.1460 s; wet snow 391.6917 s; glide skis 378.2995 s.
Aggressive pacing finishes with reserve 0.268 versus conservative 1.0 and more fatigue.
One-miss total time difference also includes changed aiming duration, not a fixed penalty
surcharge. Physical distances are exactly 3000/3150 m as appropriate. All prior traces pass.

Initial schema field warning was fixed by making the serializer-owned member a private
property; no compiler warning suppression. No gameplay tuning changes or commit.
Next: WP062 60-athlete capacity benchmark and M5 gate. No owner gate.
