# MILESTONE M4 — Shooting

Status: PASS — 2026-09-08. Master Plan technical gate: complete headless five-shot bouts
without hidden player hit RNG. WP037–052 implement lifecycle/lane contracts, five independent
apertures, aim, breathing/heartbeat/muscle, stance, costly hold, wind, shot resolution,
handling, pressure, shared AI control, complete training/replay and statistical lab.

Validation: 262/262 .NET and Unity EditMode; 5/5 Unity PlayMode, including Resources-based
complete shooting session and serialized replay. Console after PlayMode: zero errors, one
upstream warning. Exact CLI/.NET/Unity training CSV/events and all 1800 fixed-seed balance
outcomes match pinned SHA fixtures. Perfect geometric aim closes every target at all tested
handling skill levels; no resolution RNG exists. HR/fatigue comparisons and hold/debt/rebound
checks pass. Earlier Foundation, ski and player command regressions remain passing.

100,000 active composed ticks (5,000 simulated seconds): .NET 70.1878 ms, Unity 86.0193 ms,
zero managed bytes measured, exact final sight angles. Benchmark excludes construction,
recording export and graphics; it is an active aiming loop, not a complete race throughput
claim. Evidence: docs/quality/m4-benchmark-{dotnet,unity}.json and wp052 MCP captures.

![Shooting comparisons](../quality/m4-shooting-comparisons.png)

Provisional limitations retained for M8: rested AI saturates accuracy; easy profile has a
narrow four-hit distribution; wind estimator is absent. The large wind penalty is observed,
not tuned away. Training receives external HR/fatigue; M5 integrates race recovery. Visual
rifle/athlete animation and production control UI are later milestones. This PASS is technical,
not final balance or owner visual approval. No owner/blocker gate currently applies.

Next: M5 WP053 competition profiles, followed by complete headless Sprint composition.
