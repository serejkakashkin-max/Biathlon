# TASK-169T2 actual Game View review

These are fresh 2560×1440 High-quality captures from native Unity's normal `GameView.m_RenderTexture`. Each adjacent JSON binds the actual host, tick, context, resolved paid route occurrence, camera, source/resource hashes and PNG hash. Hosts are frozen at tick 0; these images do not qualify a moving race, completed replay, Windows build or GPU/frame rate.

| Image | Actual field and purpose | Root's visual observation |
|---|---|---|
| [stadium](stadium.png) | Mass30 stadium baseline | Raised private U outlines and pale rectangular regions are visible. |
| [private paths hidden](stadium-private-hidden-diagnostic.png) | Same Mass30 host/context/tick/camera; only typed private mat-loop surface/skirt renderer enabled states changed transiently | Crisp raised U outlines disappear; pale terrain regions remain. |
| [private paths restored](stadium-restored.png) | Same Mass30 host; exact previous renderer enabled states restored | Original sharp private U appearance returns. |
| [Mass Main568m](mass-main568.png) | Actual Mass30 paid Main568m | The old inactive Sprint-branch wall is absent. |
| [Sprint course568m](sprint-paid-extension568.png) | Actual Sprint60; paid `sprint_extension` source18.952m | Its actual paid branch retains a source-height/terrain curtain, requiring later content-quality review. |
| [Pursuit Main568m](pursuit-main568.png) | Actual Pursuit60, unchanged real historical completed-Sprint qualification | Same paid Main camera as Mass; the inactive Sprint-branch wall is absent. |
| [Pursuit terrain baseline](pursuit-stadium-terrain-baseline.png) | Actual Pursuit60 stadium; all paid paths visible | Baseline for the separate owned-terrain material probe. |
| [terrain blend suppressed](pursuit-stadium-terrain-suppressed.png) | Same Pursuit60 host/context/tick/camera; temporary `_TerrainBlend=0` via native MPBs | Pale/U regions remain; their edges look crisper. This probe does not establish that terrain blend alone causes the white patches or fixes them. |
| [terrain blend restored](pursuit-stadium-terrain-restored.png) | Same Pursuit60 host; complete retained native MPBs restored | Root inspected the restored view: presentation returns to baseline, with the white-patch issue unchanged. |

The first six capture bindings are independently checked in `artifacts/wp169t2/stadium-isolation-verification.json` and `artifacts/wp169t2/format-snow-capture-verification.json`. The terrain probe's four native sidecars and three image/metadata pairs pass `artifacts/wp169t2/capture-support/terrain-blend-independent-native-verification.json`. All 198 original renderer-wide/material-slot MPBs were empty; all 198 return to exact empty absence. Complete opaque MPB copying is distinct from enumerable emptiness/`_TerrainBlend` summaries; no digest of arbitrary unenumerated MPB properties is claimed.

Pursuit's native raw context SHA is `764114305ab8a8d99021c3bc4fde84fa9383639fbb1297b56ee0aaec779ffbd1`. Historical .NET raw context SHA `00c5353737c1cba6f4100f4a151fb5ae37d9cfdceb9dc493eaebafa712b27568` differs in decimal serialization lexemes. A complete read-only token proof verifies exact ordered structures, all integers and every binary64 decimal bit pattern; a separate audit finds zero duplicate keys. Only the transient native frozen-host capture uses the verified exact native SHA. Authoritative context/replay equality is unchanged.

The accepted historical qualification remains byte-identical at SHA `7f2675246b4d63255e880364ed35302d8db501dc67588a8b56eb2076b3b1a529`, with all 60 qualifiers and 60 completed Sprint result rows. Existing Mass30/Sprint60/Pursuit60 capacities are recorded honestly. No image represents the requested future max-40 field or compact 2.75–3m range.

T2 is DONE/PASS for bounded route selection and native contact behavior. Final focused tests pass 5+11+8+2; full native PlayMode passes 224/224. Fresh Console after capture/restoration reports 0 errors, 0 warnings, 0 logs and no compilation failure; nine earlier known transient helper errors remain preserved separately. The private-path/terrain white-patch issue and active Sprint curtain remain content work; these diagnostics are not production fixes or a blanket venue visual-quality PASS. WP-169 remains in progress.
