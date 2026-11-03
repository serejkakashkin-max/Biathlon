# M16.6.9 — Russia and scenery shadow continuity

2026-11-03 owner request: add Russia, its national kit, a flag behind the shooting range and a coach with spotting scope before the firing line; fix shadows appearing/disappearing like the previously repaired trees/spectators.

## Delivered

- New production rosterv2 adds RUS. Existing v1 saves/drafts keep their original roster and are not rewritten. Same-seed generation preserves names, abilities, potential, money, ages and RNG state; new nationality metadata is cosmetic.
- Russia is selectable in the actual creator, saved and restored. National UI flag and seventh waving flag behind the actual backstop use white/blue/red horizontal bands.
- Blue suit with white upper sleeves/red forearms; same original athlete geometry, fabric and equipment. Source: [official SBR2022/23 collection](https://biathlonrus.com/news/obnarodovana-ofitsialnaya-forma-sbornoy-rossii-na-sezon-2022-23/). This is an original simplified interpretation of that documented tricolor collection, not a claim about the current DE collection. No source photos or sponsor logos were copied into the game.
- Thirteenth coach and spotting scope in the existing safe enclosure; national colors on coat and cap, Russian flag on enclosure. Exact corridor, terrain and scope alignment tests pass. Only new coach12assets were retained from baking; existing36staff meshes preserved.
- Production trees, spectators and winter dressing use independent fixed-mesh shadow submissions. Casters remain eligible outside the visible frustum and beyond visible LOD transitions, bounded by actual light projection and existing quality shadow distance. Original visible LOD/fog/frustum behavior and benchmark adapters remain intact.

Decision and compatibility: [ADR0091](../../../adr/ADR-0091-m16-6-russia-and-stable-scenery-shadows.md).

## Reproduction and qualification

Initial native shadow regressions2/2FAILED:100%of reference shadow pixels were missing for an offscreen original tree; a visible far tree submitted0shadow geometry. Receipt:native-shadow-reproduction.json. Final original reproduction and additional crowd/noallocation checksPASS in native-playmode.json. Comparison uses an ordinary native tree shadow caster and the same camera/light/receiver. Final6818reference pixels,298missing(4.37%,edge/material differences), below the original15%tolerance. shadow-0.png is no-caster baseline, shadow-1.png ordinary reference, shadow-2.png fixed explicit-camera submission. No assertion/tolerance was relaxed.

Final nativePlay81/81PASS: forest visibility/LOD continuity, old shadow geometry/benchmark contracts, spectator population, original/new shadow regressions, national-kit/bib integration, seven flags, thirteen coaches/corridor/scope validation, actual creator/continue and real race launch/pause/abandon. NativePlay runtime134.8611538s. NativeEdit3/3PASS (including the2roster tests also run on.NET). .NETRelease37/37PASS for roster compatibility, generation parity, career creation/world/session boundaries. Total119distinct cases,121executions across runtimes. No full2193case race-suite claim for this follow-up.

Inspected original native avatar front/back, Russian coach/scope/enclosure flag and range flag in the complete production venue. Additional RU/EN/large-text menu and forest continuity captures retained. Console0Error/Exception/Assert at final check. No compile errors. The fixed path deliberately submits previously omitted shadow geometry; no target-hardware minimum-FPS claim is made.

Final player:2560x1440FullScreenWindowPASS/exit0, RUScareer/roster_v2,bothlocalizedgametitles,18artresources,6captures,ownerprofilefingerprintunchanged/errors[]. This standalone probe is menu-only; the nativePlay venue captures exercise the actual environment and shadows, and nativePlay separately exercises real race startup. Stable persistence identity remains Biathlon.

Final build:Succeeded0errors/520warnings,481135291bytes,22.3727425s. Initial build also succeeded. All25ProjectSettings assets match the pre-build backup after restoring Unity's temporary High-pipeline serialization. Source/binary hashes:delivery.json. STATE history prefix preserved. Unrelated pre-existing dirty settings/packages/venue data and historical QA are excluded from publication.

Review build:Builds/M16_6_9_Russia_Shadow_Review/START_TEST.cmd.
M16.5closed;M16.6OPEN for ownerreview;M17notstarted. Next:owner reviews Russia in a new career and shadow stability/performance while skiing and rotating the camera.
