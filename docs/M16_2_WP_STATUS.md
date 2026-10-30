# M16.2 — Live shooting broadcast

2026-10-30. M16.1 CLOSED / OWNER APPROVED. M16.2 OPEN / IN PROGRESS.
Feature branch `codex/m16-2-live-shooting` from `ab54425c`; M17 not started.
Preserve unrelated owner changes, accepted authority, motion, quality and saves.

| WP | Status | Acceptance |
| --- | --- | --- |
| A — Existing feed and design audit | COMPLETE | Existing model/HUD/tests inspected; actual target-mask versus shot-order semantics documented |
| B — Bounded live queue | COMPLETE | Native20/20 and CoreCLR20/20; stable30-athlete queue, retained completion, exact ticks, seek/duplicate cases, zero warmed model allocations |
| C — Frostline broadcast graphics | COMPLETE / QUALIFYING | 20rows at1080p; responsive720/900p, distinct bib/mat, chronological hit/miss shapes, RU/EN, reduced motion; native layout checks PASS |
| D — Native verification and footage | IN PROGRESS | Native20EditMode +2PlayMode PASS; actual30-athlete player footage captured; later player-miss scenario being recorded |
| E — Windows delivery and publication | IN PROGRESS | Windows candidate built; fresh5-race/14-week exact authority regression PASS; final player/fullscreen evidence and report pending |

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
