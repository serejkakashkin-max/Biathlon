# TASK169R — bounded persisted production replay

The VenueLab CLI can now persist accepted production commands and replay them through a freshly reconstructed actual factory field. The current production evidence covers 1,000 ticks each for Woodland Basin O34 Mass Start 30 and Sprint 60. Both pass exact public state and ordered field event comparison. This is prefix acceptance; complete production Mass Start 30, Sprint 60 and qualified Pursuit 60 remain pending. The small integration fixture completed Sprint in 1,849 ticks and its genuinely qualified Pursuit in 3,613 ticks, including exact final result replay.

The implementation is confined to `dotnet/Biathlon.VenueLab` and a new `dotnet/Biathlon.VenueLab.Tests` project. It does not change authoritative physics, AI policy, venue geometry, Unity presentation or imported data. The existing `race` command also accepts a completed Sprint source for Pursuit and excludes finished athletes from its stationary counter.

## Commands

From the repository root, use the frozen Release binary under `artifacts/wp169r/dotnet/bin/Biathlon.VenueLab/release/Biathlon.VenueLab.dll`:

```powershell
dotnet artifacts/wp169r/dotnet/bin/Biathlon.VenueLab/release/Biathlon.VenueLab.dll record Assets/Resources Assets/Resources Biathlon/Venues/woodland_basin_v1/manifest Biathlon/Venues/woodland_basin_v1/sprint_profile Biathlon/Venues/woodland_basin_v1/sprint_route Biathlon/race_snow_neutral_v1 60 120000 artifacts/wp169r/full-sprint60.commands.gz artifacts/wp169r/full-sprint60-record.json --max-tape-mib 4096 --max-raw-mib 65536
dotnet artifacts/wp169r/dotnet/bin/Biathlon.VenueLab/release/Biathlon.VenueLab.dll replay artifacts/wp169r/full-sprint60.commands.gz artifacts/wp169r/full-sprint60-replay.json
```

These commands are examples for future full validation, not evidence that those races have completed. Mass Start uses `mass_start_profile`, `mass_start_route`, and count 30. Once the real Sprint 60 source is complete, use the corresponding `pursuit_profile` and `pursuit_route`, count 60, and append `--sprint-result artifacts/wp169r/full-sprint60-record.json` to `record` or `race`. The source can be the direct persisted `SprintResultSnapshot` JSON or a completed CLI report's `result_json`. Qualification uses the exact result roster and rejects a different requested count; a prefix report cannot qualify a Pursuit.

## Persistence and comparison contract

The versioned kind is `production_venue_stream_commands_v1`. Recording writes two necessary files: a gzip binary command tape and `<tape>.context.json.gz`. Keep both with the frozen assemblies. The context contains the exact resource JSON snapshots actually requested by the production factory, the qualification snapshot when present, seed 123, frequency 20, ordered field context and initial public state/events. Original resource byte hashes, compiled assembly hashes and source hashes accompany the evidence. Replay loads only the persisted resource reader and rejects a different assembly set. It constructs the same production factory field and validates its exact context, roster and initial state; the newly created AI runner is never advanced.

Each sequential tick contains all accepted ski, aim, wind, pressure, trigger, breath and lateral command fields, canonical public state JSON and ordered field event JSON, protected by a SHA-256 record checksum. Replay calls `field.Step(commands, lateral)` and compares exact serialized bytes at every tick. A known valid command corruption with a recomputed checksum still fails at its first actual state mismatch. The public comparison includes all `LastFrame` physiology/shooting outputs, `PhysiologyState`, motion and route distance, physical loop/local distance/length/closed status, lateral and draft state, junction tickets, lifecycle and finish components, range path/lane, shooting masks and state, range arrival counts/receipts, reservation ownership/change/history, and every checkpoint/split/timing entry. Finish times use exact rational components without evaluating an invalid initial display value.

The filter matches the existing AI-free replay convention: only `AiIntentChanged`, `AiShootingPolicyChanged`, `AiPursuitTacticsChanged`, `OvertakeStarted`, `OvertakeCompleted`, and `OvertakeAborted` are excluded. All authoritative field events retain their original order. The footer compares completion, exact serialized final result and cumulative field event count, and rejects trailing bytes. `PASS_COMPLETE_EXACT_REPLAY` requires actual completion and final result parity; `PASS_PREFIX_EXACT_REPLAY` never means full race acceptance.

## Capacity and measured evidence

The recorder streams one tick at a time rather than keeping a race-sized command array. It permits 1–60 athletes and 0–120,000 ticks, with a hard 8 MiB tick record and 16,384 retained events per tick. Defaults cap compressed commands at 256 MiB and uncompressed tick records at 4 GiB. Explicit options allow at most 4 GiB compressed and 64 GiB raw. The recorder reserves room for a complete maximum-sized batch and footer, stops with `FILE_CAPACITY_PREFIX` before advancing another command, and leaves a valid replayable prefix. Final results also have an 8 MiB cap. State snapshots dominate disk volume, so a full run can reach its capacity even with command compression.

The complete cold context is separately capped at 16 MiB compressed and 128 MiB expanded, with bounded streaming serialization/decompression. Mass Start 30's exact factory context is 80,774,424 bytes expanded and 1,545,691 bytes compressed; Sprint 60's is 17,215,141 and 451,626 bytes respectively. The initial 32 MiB context limit rejected the legitimate Mass Start snapshot and was replaced by this measured, bounded format. Core factory construction still creates its large one-time context string; this CLI does not claim zero cold allocation or a bound smaller than that factory context.

| Production prefix | Ticks | Field events | Compressed tape | Raw tick records | Largest tick record | Maximum stationary |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Mass Start 30 | 1,000 | 8,130 | 12,523,327 B | 157,037,471 B | 161,454 B | 2 ticks |
| Sprint 60 | 1,000 | 336 | 7,228,692 B | 338,223,215 B | 338,802 B | 1 tick |

Both prefix replays have zero AI steps and exact state/event parity. Nine Release tests pass: all command fields, deterministic repeated persistence, valid command mutation, context mutation, oversized record rejection, decompression capacity, output collision preservation, zero/capacity prefixes, and completed tiny Sprint → actual-result qualified Pursuit. They are integration/contract tests; the tiny venue is not a production traffic or capacity proof. Evidence and hashes are in `wp169r-streaming-replay-verification.json` and `artifacts/wp169r/results/streaming-replay-tests.trx`.

Full production completion/liveness, full results and full real Sprint 60 → Pursuit 60 replay remain the next validation. Runtime/visual acceptance and WP169 completion are not inferred from these headless prefixes.
