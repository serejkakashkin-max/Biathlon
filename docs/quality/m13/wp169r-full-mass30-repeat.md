# TASK169R — full Mass30 replay and independent AI repeat

The actual imported Woodland Basin O34 Mass Start 30 completes at tick 57,832. Its full persisted command replay passes exact public state and ordered field event comparison at every tick, with no AI replay steps, and reproduces the exact final result. A separately executed AI recording using the same frozen Release binary produces byte-identical full tape and context hashes. This accepts this full Mass30 replay and repeat evidence; it does not declare WP169 complete.

The independent verifier streamed SHA-256 over both actual 1,573,337,595-byte tape files and both context files, checked the hashes against their reports, compared every recording property except actual output paths and elapsed wall time, and verified all captured source and required resource hashes against the current files. Exact tick count, final result JSON, field/AI event totals, record sizes, stationary diagnostic and all inventories agree. The independently identical input bytes allow reuse of the first completed replay proof.

| Full production measurement | Value |
| --- | ---: |
| Athletes | 30 |
| Completed ticks at 20 Hz | 57,832 |
| Authoritative field events | 680,897 |
| Maximum live on-course stationary run | 311 ticks |
| Compressed command tape | 1,573,337,595 B |
| Uncompressed tick records | 17,377,466,028 B |
| Compressed cold context | 1,545,691 B |
| Expanded cold context | 80,774,424 B |

The common tape SHA-256 is `d3a9a04e9d30962d003fb8cc54ba2c008563adff1f191db89cfbe259d0ff5014`; the context SHA-256 is `2f8ef2ee616f0aa617b5b7cacffa8465dc28be96aabe82caa5d8ff4cd1ab3be7`. Exact final result JSON has SHA-256 `0eb91f3e2cc943272357a10733a754b82f1dfd81e092b9c4ad2908da508e905d`.

See `wp169r-full-mass30-repeat-verification.json` for report/file/source/assembly/resource hashes and all deterministic comparisons. Original reports are `artifacts/wp169r/full-mass30-record.json`, `full-mass30-replay.json` and `full-mass30-repeat-record.json`. The independent verifier is `artifacts/wp169r/verify_mass30_repeat.py`. The CLI source and binary remained frozen throughout these runs; no rebuild, authoritative code/data mutation, Unity action, PROJECT_STATE edit or Git mutation was performed for the repeat.

Full Sprint60 repeat/replay, actual full Sprint60 result → Pursuit60 qualification/completion/replay, and remaining Unity/runtime/visual acceptance are still separate pending work. The earlier prefix contract/proof files remain historical evidence for their recorded snapshot.
