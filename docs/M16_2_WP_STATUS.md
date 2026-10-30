# M16.2 — Live shooting broadcast

2026-10-30. M16.1 CLOSED / OWNER APPROVED. M16.2 OPEN / IN PROGRESS.
Feature branch `codex/m16-2-live-shooting` from `ab54425c`; M17 not started.
Preserve unrelated owner changes, accepted authority, motion, quality and saves.

| WP | Status | Acceptance |
| --- | --- | --- |
| A — Existing feed and design audit | COMPLETE | Existing model/HUD/tests inspected; actual target-mask versus shot-order semantics documented |
| B — Bounded live queue | IN PROGRESS | Stable arrival/lane order, 30 athletes, retained completion, tick-based fades, repeat/seek handling |
| C — Frostline broadcast graphics | PENDING | 20 rows at1080p, adaptive smaller layouts, bib/mat distinction, per-shot symbols, RU/EN, motion/accessibility |
| D — Native verification and footage | PENDING | Existing tests updated, overflow/lifecycle/rewind, true MassStart gameplay and3resolutions, video |
| E — Windows delivery and publication | PENDING | Build, fullscreen acceptance, authority regression, report, state/GitHub |

Design: existing Deep #0B1928 / Surface #132A3B / Ink #E8F5F8 / Muted #B0C4D0 /
Accent #85D5DF / Danger #F3988D. Oswald for names and compact broadcast numerals;
Roboto for utility captions. A narrow, quiet vertical board of fixed-height rails:
`bib | mat | flag | athlete | ○ ○ ○ ○ ○ | score/status`. Signature is the five
accepted-shot capsules: outlined pending, filled light hit, red cross miss. A thin
cyan player rule retains equal queue treatment. No separate player block, page
rotation, large prose, whole-panel flashing or new camera in ordinary gameplay.

Tick semantics: completion/retention/fade use accepted race tick/frequency. Missing
chronological evidence after seek is explicitly unknown, never inferred from target
bit positions. The existing feed/snapshot is the sole shooting source. Unsupported
race formats must be stated honestly; do not add simulation to satisfy a UI test.
