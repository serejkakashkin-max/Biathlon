# ADR-0083 — Exact snow-cut rows, local rim constraints and readable split gaps

2026-11-01. M16.5 remains OPEN. Owner reports sharp Sprint snow at a branch
return and asks TAB gaps over a minute to use minutes/seconds.

## Snow: repair the source of the visible steps

The native Sprint return reproduction finds a broad exposed vertical face with
area 0.14503 m². Three interacting presentation issues explain the defect:

1. Route selection retained complete native panels whose spans merely touched a
   selected route interval. A cut could therefore draw past its actual connection.
2. Natural rim smoothing rotated the cross-row at the cut independently from the
   other connected road. That misaligned two originally matching boundaries.
3. One terrain-unsafe rim row reduced the rotation limit of the entire loop. This
   retained short backwards teeth at other knot rows which could be smoothed safely.

Supply existing RouteUsage interval endpoints as extra rendering samples. Keep
every old sample and its position; the original track/visual/path resources and
accepted route stay unchanged. The two boundary rows now lie exactly at their
authored distances. This refines the prior whole-panel selection contract: no
unpaid overhang is added merely to complete an overlapping source panel.

Hold the authored cross-row at those cut samples and fade the permitted rotation
over the existing smoothing window. Open endpoints receive the same bounded fade.
The separate render rim retains row centres, heights and full widths. If terrain
clearance constrains a loop, admit each row against its complete actual incident
terrain triangles and bound its rotation locally. Preserve the exact source pair
at scale zero and the exact target pair at scale one. The original clearance,
coverage, rollback and float-face checks remain mandatory.

All construction is cold. Existing snow skirts provide the rounded banks; no new
roof overlay, renderer, texture, collider, dynamic light or per-frame geometry.
An experimental global roof-envelope approach was rejected after it produced
contact/topology failures. Its source and receipts remain local diagnostic
artifacts; none of that code or its test-oracle changes is shipped.

The original full-composite terrain oracle is retained byte-for-byte. Its
Sprint witness previously placed its negative control inside the discarded Main
overhang. Derive that witness from the real branch connection: 0.3 m beyond the
branch's half-width, then 0.3 m before/after the Main connection. Keep the same
inside/outside/upward-roof assertions and all original tolerance values.

## Gaps

Add one pure presentation formatter. Round once to tenths before choosing the
clock unit, so 59.96 becomes +1:00.0 rather than +60.0. Below a minute retain
seconds; at a minute use m:ss.f; at an hour use h:mm:ss.f. Localize the decimal
separator. Invalid negative/nonfinite inputs are rejected. Leader text remains.

Use it for the main timing line, split notice, TAB checkpoint row, broadcast and
replay TV. Remove the fixed seconds suffix from the localized timing templates.
TimingAthleteRow owns a numeric checkpoint binding so elapsed and gap columns
cannot diverge across those callers or row reuse. Formatting does not advance
the accepted clock, edit recorded splits or alter classification/scoring.

## Qualification

Require the original native return reproduction and strict full-contact tests,
all three formats, rotated Sprint/Pursuit, finish runouts, immutable source
buffers, exact cut samples, pinned tangent/fade tests and RU/EN reused gap rows.
The final receipts and fullscreen build are recorded in the M16.5 follow-up
quality report. No milestone closure is inferred from implementation.

### 2026-11-01 qualification delivered

62 distinct selected checks pass: 29 pure formatter/HUD/distance cases, 3 complete
Sprint replay/guard/allocation cases, 12 native snow cases and 18 native UI/replay
cases. The snow matrix includes Sprint/Pursuit at 0/45 degrees, both finish
runouts and Mass Start. The photographed return has zero exposed vertical faces
under the retained oracle; its original largest face was 0.145028859 m². Three
native High views were inspected. The exact-cut policy remains explicitly scoped
to Sprint; legacy Pursuit/Mass Start construction retains its existing path.

The reused TAB row now has named elapsed/gap labels. Its test reads the actual
label text against literal RU/EN examples, including 59.96 -> 1:00.0 and the
return from minutes to seconds; it also preserves the leader and accepted tick.

`Builds/M16_5_Snow_Gaps_Review/START_TEST.cmd` succeeds with zero errors. Actual
2560×1440 fullscreen startup, clean loading, personal countdown, recovery and
settings pass with owner profiles unchanged. Five core DLLs remain byte-identical
to the preceding results build. Evidence and limitations:
[follow-up report](../quality/m16-5/snow-gaps/README.md). M16.5 remains OPEN.
