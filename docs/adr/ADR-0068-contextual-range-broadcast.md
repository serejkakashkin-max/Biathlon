# ADR-0068 — contextual rival shooting broadcast

2026-10-13. Owner-requested follow-up to M14 WP-182/183; uses the Frostline HUD.

The panel observes the accepted race feed and opens from 250 route metres before
the authored range entry to 250 metres beyond the authored exit for each shooting
visit. These are canonical route distances, crossing lap seams; they are not a
Euclidean radius around the stadium. Penalty transit holds canonical progress, so
the panel remains available throughout that transit. Prestart/finished views hide it.

Snapshot adds read-only canonical route distance and the last completed target mask.
No shot outcomes, lane assignment, physics, timers, rules, AI or saves are changed.
The observer keeps a fixed row per athlete. Five circles show actual target closures;
misses equal accepted shots minus closed targets and appear as separate small red
dots to the left. An accepted increase in misses highlights the row for one second,
with two gentle pulses per second; reduced-motion mode uses steady highlighting.
Duplicate observations, rewinds and large tick gaps rebase without false flashes.
Completed series remain visible after the athlete releases the mat.

Eight retained rows fit at the right edge above the pulse/readiness cards. The
current athlete and fresh misses are pinned; remaining rows automatically page
every five accepted seconds. A count/page footer makes this explicit. Active
shooters precede finished series; displayed mat ordinals are distinct from race
classification. Flags/names use the existing career roster. No new rank is invented.

The definition has schema/id and contains distances, timing, row and width budgets.
The Unity-free model owns fixed arrays; insertion sorting occurs at the existing
HUD cadence. There are no additional snapshots, cameras, render passes, raycasts
or per-athlete MonoBehaviours. Elements ignore input; TAB temporarily covers the
panel with the split table. UI scope is presentation only, not a new milestone.

Verification: route-boundary and final-series/seek tests, actual thirty-athlete career
Mass Start adapter run, flags, miss flash, automatic pagination, retained results,
read-only authority and native RU/EN large-text bounds. See the delivery report.
