# TASK-078 — Repeated 30-athlete AI soak

Status: DONE, 2026-09-09. Standalone441/441; Unity441 verified across full/targeted runs; PlayMode7/7; Console0 errors.

Three authored test combinations each run twice: flat/expert/seed123, rolling/developing/seed456,
flat/developing/seed789. Each field contains30 identical-capability athletes with alternating
attacker/patient personalities. Fixed tick20Hz, two real shooting bouts, actual penalties and shared
occupancy/drafting. Acceptance: finish within120000ticks, exact travel, no endpoint overlap, no
continuous-validation failures, no precision fallback, no >2400tick individual skiing stall and
no eight rapid lateral reversals in a chain (adjacent reversal gaps <=20ticks). No thresholds were
loosened after failures.

The initial rolling run failed at tick57457 when athlete29 had waited2401ticks at the penalty exit.
The main route progress tie-break could starve a penalty-exit contender. PackSprintFieldSession
now prepares/commits FIFO approach tickets after incumbent priority; physical safe-path validation
remains mandatory. Context adds junction_fifo_after_incumbent_v1; GetJunctionWaitSince exposes wait
state. The same seed then finished twice at58719ticks with identical results and streaming digest;
maximum stall1148ticks, maximum rapid-reversal chain1. See wp078-diagnosis.trx (failure retained),
wp078-fairness.trx, ADR0013. No simulation speed/power/target bonuses were added.

This ordering correction changes four earlier shared-pack golden results. Preflight passed their
geometric, distance, bout and completion assertions before comparison to the old SHA; expected
digests were then revised for the new context. Previous digests remain in TASK071C evidence.

| Fixture | New result SHA256 |
|---|---|
| flat12 interval0.5 | 164f496e078824383039bcf9ca6ac747f407c95b358f21bc2b26ea82a81a9bb7 |
| flat30 interval2 | e55374aadb1053b48b012c74413b319db0cd2bfb4db3dd05b5826b2f88871594 |
| rolling6 interval1 | 1f03aa6fdc5c237f53b666b2e01524cd0ae49bcb601836b9036a74820fd90895 |
| technical6 interval1 | c1f89ea3d39d903aa5a7c0d522e374781ed7226ef1a315178436f033e09b1d3e |

Evidence: wp078-standalone.trx, wp078-editmode-verification.json, wp078-cooperative-unity-results.xml, wp078-console-final.json and wp078-playmode.json.
artifacts/physiology/ai-soak contains exact context/result JSON, schema1 audit JSON and a CSV tail
for each combination. The 64-row tail explicitly reports dropped older rows. A streaming FNV64
regression digest includes every decision, sampled trajectory state and event identity/payload;
SHA256 covers results. Repeat assertions compare context, result, digest, tail and intent counts.
FNV is a regression fingerprint, not a cryptographic integrity mechanism.

Preflight also saw one2472-byte Foundation GC assertion failure; unchanged focused rerun passed.
The final full suite subsequently passed unchanged. No failing test was removed or disabled.

Limits: finite fixed-seed synthetic proof, no claim of universal liveness or balanced believable AI.
Shared synthetic shooting footprints serialize access, so race duration is not a stadium throughput
benchmark. Frequent resource-driven sprint activation can require further tuning. WP079 remains
the next task and M6 is not declared passed here. No new presentation or Inspector setup required.

Standalone final: 441/441, zero skipped, 4m36s. Six complete races, 180 athlete finishes.

| Thirty-athlete case | Ticks | Max stationary skiing ticks | Trace FNV64 | Result SHA256 |
|---|---:|---:|---|---|
| Flat expert seed123 | 42153 | 612 | 61758b3207d023b1 | ed8473687eecee9a3a1c970c57ee9bdfffc1c638c6cce15ee5301c13bd334c27 |
| Rolling developing seed456 | 58719 | 1148 | 79a899df81436769 | 767c76cc3192fa04893db68a7cc432a79e64fc521730412961b0300dfcee6a53 |
| Flat developing seed789 | 57840 | 1148 | 161cdb0a5530b411 | 12b8608ce270d4a6233421e380c28c8da047a3a007c8406cee31ee2611ca2159 |

Both repetitions of each case matched. Maximum rapid reversal chain1 for every case. Maximum
simultaneous physical drafts6/9/7. Foundation allocation test passed in the unchanged final suite.

Unity transport returned a timeout, but its persisted NUnit XML recovered the actual outcome:
438 passed; the three repeated-soak cases alone exceeded Unity's default180000ms wall-clock
per-test timeout. Each executed both full races with the same hashes as .NET. Original XML is
retained as wp078-interrupted-unity-results.xml. Set Timeout(1200000) specifically for these
long tests; simulated liveness limits are unchanged. Editor-only UnityTest wrappers execute the
pure workload on a cancellable worker and yield frames, preserving Editor responsiveness and
propagating assertion failures back to the test runner. Final cooperative Unity rerun:9/9 passed in20m22s. All three fingerprints match .NET; all441 EditMode cases reconcile successfully.


Final PlayMode:7/7, Console0 errors and1 Editor warning; plugin timeout restored to10000ms. The transport response timeout is retained; successful NUnit XML and MCP collector status provide the final evidence. Published source checkpoint 9697564949ee883ec839dc2c5ecafb846475bc9d on codex/wp-078-ai-pack; see PROJECT_STATE.md for continued publication tracking.

Publication verification: a clean archive of the staged Git tree restored, built and passed441/441 tests in3m25s (wp078-publish-snapshot.trx). This includes the unchanged initial-template LFS/icon inconsistency; the owner icon/package/editor changes were not staged.

