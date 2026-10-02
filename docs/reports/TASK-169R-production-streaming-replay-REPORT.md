# TASK169R — persisted production command replay

2026-10-02. The bounded VenueLab persistence/replay contract passes its nine Release tests. Woodland Basin's actual 30-athlete Mass Start completes in 57,832 ticks, passes every-tick exact replay without AI, and produces an identical independently recorded command tape, context and final result. Sprint60 remains incomplete and requires further traffic diagnosis. WP169 and M13 remain IN_PROGRESS.

## Change and verification

`dotnet/Biathlon.VenueLab/ProductionReplayHarness.cs` adds streaming accepted-command persistence and reconstruction from exact resource snapshots, qualification, seed, field context and initial state. The CLI exposes `record` and `replay`; `race` accepts an actual completed Sprint result for Pursuit and excludes finished athletes from stationary diagnostics. A separate VenueLab.Tests project covers all command fields, repeated deterministic persistence, semantic corruption with a valid checksum, context corruption, record/decompression bounds, output collision preservation, capacity prefixes and a complete tiny Sprint-to-Pursuit factory integration. All nine tests pass. Core gameplay, physics, AI tuning, player controls and shooting geometry are unchanged by the harness.

Replay advances only the reconstructed field with accepted commands. It compares exact serialized public state and ordered authoritative events at every tick, then exact completion and final result. The six excluded AI event types follow the existing AI-free replay convention and are named in the contract. Every production tape and context is bounded and checksummed; exhaustion produces an honest replayable prefix. The factory's large cold context and CLI state serialization are diagnostic costs, not runtime frame-rate or zero-allocation claims.

See [persistence contract](../quality/m13/wp169r-streaming-replay-contract.md) and [nine-test/prefix evidence](../quality/m13/wp169r-streaming-replay-verification.json). These are the earlier bounded contract checkpoint; full-race evidence follows separately.

## Actual full Mass Start

Both independent AI runs use the imported O34 production resources and frozen N/P assemblies. All30 athletes complete the actual15km/four-bout race at tick57,832. Maximum live on-course stationary run is311ticks; there are680,897 authoritative field events. Full replay compares every tick, all athletes, exact ordered events and the final result with zero AI steps. Independent streamed SHA-256 verification confirms identical1,573,337,595-byte tapes and identical cold contexts, as well as every recording property except file paths and wall time. Sources and required resources remained unchanged. See [full Mass30 report](../quality/m13/wp169r-full-mass30-repeat.md) and [independent proof](../quality/m13/wp169r-full-mass30-repeat-verification.json).

## Sprint finding and next task

The real60-athlete Sprint recording stops at tick108,385 under the explicit4GiB compressed capacity. It has no final result, and its maximum live stationary run is72,420ticks. This is a traffic/liveness finding; increasing tape bounds would not resolve the observed wait. The original recording is retained, its actual tape hash checked, and a prefix replay is underway. No incomplete result is used to qualify Pursuit. [Diagnostic evidence](../quality/m13/wp169r-sprint60-incomplete-diagnostic.json) records this limit.

Next: identify and correct the actual Sprint blocking state, verify the correction with deterministic regression tests, then repeat full production Sprint60 and qualified Pursuit60 with persisted exact replay. Unity presentation, snow shoulders, visibility, delivery and the remaining WP169 acceptance are separate work. No new Windows build or WP169/M13 completion is claimed.
