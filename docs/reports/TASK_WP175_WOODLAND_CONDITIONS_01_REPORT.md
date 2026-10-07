# TASK WP175 Woodland conditions 01 — physical component verification

Date: 2026-10-05. Focused physical component verification PASS; WP175 native weather views, full condition races/replays and performance acceptance remain OPEN.

The fixture binds every actual Woodland surface to each of four existing canonical profiles: packed_neutral, fresh_soft, icy_fast and wet_slow. All36 loops and1,291 segments are sampled at interiors and exact boundaries:5,164 samples per profile. No numerical tuning coefficients are copied or changed. Missing surfaces and unknown profiles fail before stepping.

An actual main technical bend demonstrates the existing glide/grip trade-off through the shared SnowConditionSystem, SkiForceSystem and CorneringSystem. Ice has lower resistance and lower safe cornering speed; soft/wet snow have greater resistance. Four1,200-tick/20Hz force/cornering probes repeat exactly. Probe power/stats are explicit test inputs, not new production tuning. These probes finish no lap or race, contain no AI and do not qualify complete race/replay balance.

Release .NET10/10 PASS; native Unity EditMode10/10 PASS. [Portable component evidence](../quality/m13/wp175-condition-probes-20261005/component-proof.json) and [native results](../quality/m13/wp175-condition-probes-20261005/native-tests.json). The native discovery count1,057 is not the executed count. Full core acceptance still retains the separately recorded legacy allocation failure; these focused cases do not supersede it.

Observed profile distinction: main segments labelled descent_technical currently have zero grade; main edge_027/descent_fast has negative grade and zero curvature. The test therefore describes a technical bend, not a downhill turn. Actual downhill visual/physics review must sample real grade and geometry; semantic labels alone are insufficient. This observation is carried into WP175 readability and WP177 course/profile review without silently changing accepted race physics.

Next: fresh weather environment initialization and ordinary native views for all five visual rows; actual Sprint40-to-qualified-Pursuit40 and Mass30 condition flows with exact replay; appropriate available CPU/GC/render evidence. Hidden standalone rendering remains insufficient for sustained GPU/FPS qualification.

## 2026-10-06 follow-up: native bindings and complete solo races

The five authored Woodland weather/render combinations now pass a cold initialization and lease-restoration PlayMode check in Unity: clear, overcast and snowfall on High, plus snowfall on Balanced and Low. The [native XML](../quality/m13/wp175-conditions-20261006/native-weather-five.xml) records 5 executed / 5 passed. This verifies the environment bindings and their cleanup; it does not establish image quality or sustained frame rate.

Four headless full Woodland Sprint races now finish under the canonical packed_neutral, fresh_soft, icy_fast and wet_slow snow profiles. Each race records actual input commands, restores a serialized tape, replays to an identical race result and final athlete frame, and rejects playback against a different authoritative snow profile before tick one. The new long integration fixture uses the authored `snow_test_packed` surface ID; an earlier fixture accidentally replaced this ID and failed preflight as intended. Its [diagnostic TRX](../quality/m13/wp175-conditions-20261006/invalid-surface-fixture.trx) is retained. The corrected [TRX](../quality/m13/wp175-conditions-20261006/four-snow-full-sprint.trx) is 4 executed / 4 passed. Race lengths were 12,546, 13,236, 13,179 and 12,810 ticks in the profile order above. The [matrix-plan tests](../quality/m13/wp175-conditions-20261006/matrix-plan-tests.txt) passed 11/11.

No snow coefficients, race logic or production venue definitions changed. These are one-athlete full-race proofs; they do not qualify the required 40-athlete Sprint, qualified 40-athlete Pursuit or 30-athlete Mass Start condition matrix. Ordinary native visual captures, race-balance assessment and suitable CPU/GC/render evidence remain open. WP-175 and M13 therefore remain OPEN; no interim Windows build is produced under the owner batch directive.

## 2026-10-06 follow-up: full-field condition witnesses

The actual 30-athlete Woodland Mass Start finished on icy_fast snow in 21,858 ticks; the [headless TRX](../quality/m13/wp175-conditions-20261006/wp175-mass30-ice.trx) passed 1/1. A 40-athlete Sprint finished on fresh_soft snow in 36,794 ticks. Its actual completed result then qualified 40 athletes for a same-condition Pursuit, which finished in 21,407 ticks; the combined [Sprint→Pursuit TRX](../quality/m13/wp175-conditions-20261006/wp175-sprint-pursuit40-soft.trx) passed 1/1. The [isolated Sprint TRX](../quality/m13/wp175-conditions-20261006/wp175-sprint40-soft.trx) passed 1/1 before the qualification check was added. Each result contains all expected finish rows. The tests use the authored Woodland field capacities and a constant-memory event audit; they do not fabricate pursuit gaps.

An initial simultaneous two-runner exact-repeat test was stopped before completion when its testhost reached roughly 8 GB. The final full-field witnesses use one runner at a time; the combined Sprint→Pursuit test still peaked around 8.3 GB of process working set, so test-runner memory and WP-178 performance profiling deserve attention. This is not a frame-rate or GPU measurement. The cancelled attempt supplies no pass claim. The completed cases do not yet provide persisted full-field command replay, all four snow profiles for each format, ordinary native weather/view screenshots, or valid sustained performance acceptance. WP-175 remains OPEN.

A separate [native clear-weather start frame](../quality/m13/wp175-conditions-20261006/native-clear-start.png) was captured from the ordinary Woodland Mass Start Game View with the player stationary. It is a single baseline view, not a five-weather visual matrix or a frame-rate measurement. The large dark start gantry and pale rectangular roadside panel are visually prominent in this view; their final event treatment/readability deserves review with the rest of the venue presentation.

## 2026-10-07 additional full-field canonical conditions

The remaining selected condition extremes were run as separate hidden
Release .NET tests, with a discarded completed Sprint runner collected
after its authoritative result had qualified Pursuit. This changes only
test-process memory hygiene, never the race simulation. The first combined
attempt was stopped without a result when testhost working set exceeded
13 GB alongside the already memory-heavy Editor. It is not counted PASS.

[Packed-neutral Sprint 40→qualified Pursuit 40](../quality/m13/wp175-conditions-20261006/wp175-packed40-qualified-pursuit.trx)
passed 1/1: Sprint 36,438 field ticks; Pursuit 18,949 ticks. The
[wet-slow Mass Start 30](../quality/m13/wp175-conditions-20261006/wp175-wet-mass30.trx)
passed 1/1 at 21,986 ticks. Both contain all expected finish rows, and
their fixed-seed event hashes are retained in the TRX. Combined with the
previous fresh-soft Sprint/Pursuit 40 and icy-fast Mass Start 30 witnesses,
all four canonical snow profiles now have at least one completed
production-size race flow. This is **coverage across profiles**, not every
format under every profile.

The original single-athlete exact replay still covers all four snow profiles.
Full-field command replay, player-controlled traffic feel, all five weather
views and sustained rendering performance remain open. WP-175 is not PASS.

## 2026-10-07 persisted full-field Mass Start replay

A complete 30-athlete packed-neutral Woodland Mass Start was stepped from
production AI commands into a separate authoritative field. The command
tape was serialized to JSON, parsed into a new tape, and replayed against a
third independently constructed field **without running AI**. All three
fields completed; source/recorded/replayed result JSON matched. Recorded
versus replayed field event hashes matched exactly, as did final frames and
physical loop/local station for every athlete. The tape covered 21,011
fixed ticks and compressed to 187,948 command runs. Its in-memory JSON was
436,819,909 characters; it was validated but deliberately not committed as
a large binary/text artifact. The bounded [Release .NET receipt](../quality/m13/wp175-conditions-20261006/wp175-mass30-persisted-replay.trx)
passed 1/1 in 2m59s. Unity AssetDatabase refresh succeeded, the Editor was
not compiling, and the recent Error Console was empty. The full replay is
headless-only because it executes three production-size race fields.

This closes the specific missing full-field persisted-command replay witness
for one Mass Start/snow combination. The full format-by-condition matrix,
player-controlled traffic feel, five actual weather-view captures, visual
quality and sustained rendering/LOD performance remain open. WP-175 and M13
are not PASS; no Windows review build has been issued.
