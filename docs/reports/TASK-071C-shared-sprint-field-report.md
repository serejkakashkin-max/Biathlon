# TASK-071C / WP-071 — Shared Sprint field

Date: 2026-09-09. TASK-071C and WP-071 DONE. No owner/blocker gate applies.

## Implementation

Added `PackSprintFieldSession`: one tick-start physical snapshot, periodic shared drafting,
staged real physiology/terrain forces, bounded lane motion with configured work cost, continuous
relative body sweeps and forward-prefix/follower replanning. Directed penalty seams preserve
incoming bodies and exact traveled distance. Whole-tick junction exclusion and stable priority
prevent sidecar/branch changes from bypassing occupancy. Start staging and finish retirement are
explicit; route progress and physical loop identity remain distinct across laps.

Extended SprintAthleteSession preparation with draft/propulsion/work inputs and prepared-range
inspection; added all-handles validation before SprintFieldSession's batch commit. Normal solo
Step and old independent Sprint field semantics remain unchanged. Added ISprintFieldView and
PackPerceptionSource so existing cadence/terrain/pace/draft/overtake policies read coherent race
and actual local periodic placements before submitting one shared batch. No UnityEngine reference
or Unity physics enters the authoritative assemblies. ADR-0012 records the orchestration policy.

Added PackSprintFieldTests, PackAiIntegrationTests and PackSprintPlayModeTests. They exercise full
real bouts/penalties/results, exact repeated frames/events/results, solo equivalence, dense lane
changes, staged enrollment, malformed input atomicity, zero warm movement allocation, lateral
work cost, actual AI pass requests and draft selection, and rejection of repeated open loops.
Four result SHA-256 baselines are asserted in both runtimes. Every dense finisher must account
for exactly 3000 m plus 150 m per geometric miss and complete both bouts.

## Corrections found during integration

Nanometre prefix search caused microscopic stop/restart fragments and frequent shared-time
precision fallback in the 30-athlete stress fixture. Millimetre-resolution blocked-prefix search
preserves strict continuous safety and unblocked nanometre physics, and removes all fallback
ticks in the four regression fixtures. The 30-athlete test improved from approximately 210 to
28 seconds standalone. A separate reproduced test caught false ContactStarted events for a
stationary solo athlete; contact now derives from actual pack replanning instead of zero speed.

The first Unity run of the superseded nanometre implementation exceeded the MCP timeout and
continued occupying Editor. It was aborted by restarting only this project's test Editor; that
run is not acceptance evidence. The optimized complete suite is rerun through Unity MCP with a
temporarily increased transport timeout, and the original timeout is restored after verification.

## Verification and impact

Final standalone: 416/416 PASS (about 39 seconds). Unity MCP EditMode: 416/416 PASS
(2:50.39). Unity MCP PlayMode: 7/7 PASS, including the new complete shared Sprint.
Final Console: zero errors, one upstream warning, compilation_failed=false; Editor is out of
PlayMode and not compiling/updating. MCP timeout restored to 10000 ms in memory and on disk.
Artifacts: `artifacts/physiology/wp071-standalone.trx`, `wp071-editmode.json`,
`wp071-playmode.json`, `wp071-console.json`, and `wp071-pack-regression.json`.
The machine summary contains scenario sizes, tick counts, events, draft/staging frames and
result hashes. Canonical M5 60-athlete result/event hash assertions remain green in the full suite.

The corrected reflection call used `typeName`, matching the installed tool's actual serializer;
its earlier malformed-argument errors were archived before Console reset. A later empty PlayMode
discovery result was not accepted: RequestScriptReload restored discovery and all seven named
tests passed again. Final PlayMode NUnit XML is also saved. Test-UnityCore.ps1 now accepts a
configurable HTTP timeout (900 seconds by default), since the full suite exceeds two minutes;
its PowerShell syntax was validated. Long synchronous tests may also need a larger MCP plugin
timeout. No failed test was disabled or removed.

Flat 30-athlete changing-lane Sprint completes in 45,039 ticks (2251.95 simulated seconds);
flat 12-athlete dense starts in 16,092 ticks; rolling 6 in 14,004; technical closed variant 6 in
12,365. All have zero precision fallback. AI command integration completes in 12,250 ticks,
observing 49 draft selections and 6 pass requests, and reproduces every field frame/event/result.
These are synthetic fixtures, not a claim of final athlete balance. Shooting remains geometric.

Allocation claims apply to warmed measured .NET movement/snapshot loops, not entire races:
construction, shooting-session initialization, results and event-log growth allocate. Unity's
allocation counter was previously shown unsupported, so it is not used as independent GC proof.

## Limits and next package

Width uses the conservative minimum across authored loops. Junctions reserve the full tick;
incoming seam proxies conservatively retain a full body and lock lateral movement until clear.
The prefix solver is safe and deterministic but does not promise optimal throughput or fairness
for arbitrary topology. Draft is sampled at tick start, with no within-tick wake recomputation.
The physical snapshot radius is at most half a loop. Consumers must observe exposed athlete
sessions rather than advance them outside the field; event sinks must be nonthrowing/nonreentrant.

Closed repeated-loop Sprint topology and explicit coincident seams are supported. Arbitrary open
connectors require authored paths; synthetic logical firing assignments remain inherited from M5.
Real range-lane assignment is M7 work. No new visual presentation was added; PlayMode checks
runtime composition, and screenshots are not an acceptance criterion for this headless package.
WP-078 repeated 30-athlete soak and WP-079 believable-competitor milestone are not declared done.

Next: WP-072 Pre-range recovery tactics. Existing unrelated repository edits are preserved;
no staging, commit, push, package upgrade or owner approval is part of this task.
