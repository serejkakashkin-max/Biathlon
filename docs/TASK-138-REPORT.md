# TASK-138 — Playable Mass Start

WP138 complete. Assets/Scenes/PlayableMassStart.unity launches player mass_0 and29 AI with the shared simultaneous-start grid. PlayableSprintHost has an explicit Mass Start initializer/mode; input, cameras, target presentation and final HUD reuse the same authority path. Authored mass_stadium_visual_v1 adds the open30m approach, ending at the existing main-loop start. Runtime mass fixtures are packaged from tests/fixtures/tracks. The scene includes its generated approach mesh.

Validation: Unity PlayMode full30-athlete host race1/1 (23 discovered),82.3 seconds: simultaneous scheduled starts, all30 finishers, four player bouts and final Mass Start HUD. .NET grid/results7/7; Unity EditMode grid5/5. Game View early pack screenshot inspected (docs/quality/m10/wp138-grid.png), tick157 with a temporary overview camera for diagnosis. Final Console0 errors/1 warning, compilation_failed=false.

No balance/physics changes. The screenshot shows early race pack, not a frozen pre-gun grid. Automated completion uses scripted player commands; human feel remains G1. Venue and approach are schematic, and per-tick presentation capture still allocates; runtime performance/visual polish require later work. Next: WP139 screenshot/playtest harness with state evidence.
