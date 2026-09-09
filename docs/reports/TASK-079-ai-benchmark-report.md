# TASK-079 — AI decision and performance benchmark

2026-09-09. WP-079 implementation; final verification recorded below.

## Scope and reproduction

The standalone BalanceLab `ai` command runs four complete 30-athlete fields: flat/rolling ×
expert/developing, all seed123, alternating attacker/patient personalities and identical physical
capabilities. Synthetic Sprint: 3000m base distance, two bouts, 150m per miss, 20Hz, interval2s.
These deliberately dense fixtures preserve the WP078 composition and policies.

```powershell
& "$env:LOCALAPPDATA/Biathlon/dotnet/dotnet.exe" run --project dotnet/Biathlon.BalanceLab -c Release -- ai Assets/Resources/Biathlon tests/fixtures/tracks artifacts/physiology/ai-benchmark-wp079
& "$env:LOCALAPPDATA/Biathlon/dotnet/dotnet.exe" run --project dotnet/Biathlon.BalanceLab -c Release -- ai-queues Assets/Resources/Biathlon tests/fixtures/tracks artifacts/physiology/ai-queues-wp079
```

`AiDecisionCounts` is a bounded, allocation-free observer of published tactical/shooting samples.
It counts all eight intents and effort0..1000, excluding shooting samples from the tactical
histogram. The CLI records per-athlete elapsed finishes, penalties, accepted shots, final resource
state, intent switches, sprint starts, phase durations and on-course intent durations. Phase uses
tick-start state; the finishing tick counts whole. Stationary means exactly unchanged authoritative
travel, not an inferred deadlock. Staging, shooting and finished actors are excluded from the
stationary-on-course classification. Accounting checks cover every athlete tick and decision.

Timing wraps only `PackAiRaceRunner.Step`, including decision counting and retained event logging;
it excludes setup, output and observer sampling. First100 ticks are excluded. Later bout/controller
initialization is included in the measured allocation total. Percentiles use nearest rank. Runtime:
.NET8.0.30, Windows10.0.26200.0, 32 logical processors. Background Editor/test activity can affect
wall time. No rendering or Unity frame-rate claim is made.

## Measured distributions

| Case | Finish median / p95 (s) | Mean misses / 10 | Stationary ticks / all athlete ticks | Step median / p95 (ms) | Headless realtime factor | Sprint starts |
|---|---:|---:|---:|---:|---:|---:|
| Flat expert | 1986.24 / 2047.35 | 0.83 | 69.02% | 0.900 / 1.548 | 53.27× | 5084 |
| Flat developing | 2776.41 / 2845.67 | 4.27 | 74.41% | 0.865 / 1.375 | 57.51× | 2655 |
| Rolling expert | 1844.32 / 1902.74 | 0.87 | 63.47% | 0.858 / 1.048 | 62.78× | 6899 |
| Rolling developing | 2769.16 / 2833.61 | 4.13 | 72.20% | 0.843 / 1.012 | 62.08× | 2777 |

All120 athletes finish; each has10 accepted shots. Every case has zero precision-fallback ticks.
Flat expert result SHA256 remains
`ed8473687eecee9a3a1c970c57ee9bdfffc1c638c6cce15ee5301c13bd334c27`, exactly WP078.
The complete machine report includes every actor, full sparse effort histograms, intent counts,
result/context hashes and min/median/p95/max/mean distributions: `docs/quality/wp079/benchmark.json`.

Expert/developing shooting differences arise under the existing allowed control changes. A single
matched seed and two synthetic tracks do not estimate population balance. Tactical counts must
be normalized by their own totals: expert samples at5Hz and developing at2Hz. Follow is never
selected here; it is not proven generally unreachable. Dense blocked traffic dominates decisions,
so these are not calibrated free-skiing tactical frequencies. Measured Step allocation is about
42.93MB per complete field, including non-steady-state work; it is not a zero-GC full-race claim.

## Queue diagnosis and M6 gate

A reproducible `ai-queues` command varies only field size and then start interval, holding flat
track, expert difficulty, seed and capabilities fixed. The retained CLI reproduction gives (`docs/quality/wp079/queues.json`):

| Athletes / interval | Stationary on-course ticks | Moving on-course ticks | Sprint starts |
|---|---:|---:|---:|
| 1 / 2s | 0 | 6716 | 1 |
| 2 / 2s | 380 | 13744 | 2 |
| 6 / 2s | 10688 | 43975 | 6 |
| 30 / 2s | 834788 | 322781 | 5084 |
| 30 / 30s | 428220 | 260732 | 3856 |

For two athletes, every stopped tick lies in main840..850m, immediately before the firing line.
For six,10590/10688 stopped ticks lie there;98 lie in890..900m. For30 at2s,
800807/834788 (95.93%) lie in810..850m. The30-second interval does not remove the queue. Thus
this is not merely an overly dense2-second start. ADR0013 already documents the serialized shared
shooting footprint. These probes localize the stationary symptom; they do not prove the entire
sprint-toggle causal chain. The late-race queue overlaps the configured final200m decision window.

**M6 NOT PASS.** Completing all races safely is established; believable field throughput and
stable final-sprint behavior are not. WP079 delivers the benchmark/report, not a waiver of this
gate. Do not label M7 unlocked solely because WP079 is done. Next technical task: isolate the
queue/final-sprint interaction, provide physically represented range-lane access/bypass consistent
with the GDD, then repeat benchmark and deterministic/continuous-occupancy regression. Preserve
these baseline distributions rather than retuning around a serialized stadium bottleneck.

No hidden physical multipliers, race policy, result schema, geometry or gameplay tuning changed
in this task. No owner approval or scope expansion is requested for ordinary technical remediation.

## Verification

Final verification is recorded in PROJECT_STATE and `docs/quality/wp079/verification.json`.
A complete single-athlete observer/no-observer replay compares exact result JSON and every event;
counts agree with runner totals. A separate test checks every histogram bucket against captured
samples and strict zero allocation after warming the counting sink. Unity MCP executes both.

The first full .NET run passed442/443: the unchanged Foundation allocation test again measured
2472 bytes. Its immediate isolated rerun passed1/1. Warming a complete10000-step workload on a
separate world did not resolve it (second full run442/443,1880 bytes); that attempted change was
reverted. A controlled full run with tiered compilation disabled is recorded in verification.json.
Both failure TRX files are retained. No test is removed, skipped or given a nonzero tolerance.
Unity MCP also ran all14 Foundation tests during this investigation; final source preserves the
original Foundation test. Runtime/JIT isolation is a test-environment control, not a gameplay fix. The controlled full run passed443/443 in6m10s. The test project now sets TieredCompilation=false in its generated runtimeconfig; BalanceLab/game settings are unchanged. This isolates the observed runtime-mode sensitivity without claiming an allocation-stack root cause.

PlayMode/screenshot acceptance is unchanged: this task adds headless tooling and a pure observer.
The previous WP078 PlayMode7/7 remains the latest runtime integration baseline, not a fresh run.
