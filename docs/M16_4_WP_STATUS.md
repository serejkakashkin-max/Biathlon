# M16.4 — shooting equipment and shot evidence

Owner directive 2026-10-31: M16.3 is APPROVED / CLOSED; start M16.4.
Prior published entries retain their original M16.2 labels. This record reflects
the owner's current milestone numbering without rewriting that history.

| WP | Status | Acceptance |
| --- | --- | --- |
| A — Lay shooting equipment on the surface | COMPLETE / OWNER TEST PENDING | Poles lie beside the prone athlete / between standing skis, skis remain on the surface; same setup and exit clock; continuous return to skiing; native Hero views checked |
| B — Accepted shot marks and edge evidence | COMPLETE / OWNER TEST PENDING | Exact accepted impact coordinates,32mark bank cap, front-plate clipping and last-shot diagram; both fifth shots and normal/large HUD checked; scoring unchanged |
| C — Qualification and fullscreen test build | COMPLETE / OWNER TEST PENDING | 40EditMode/33PlayMode PASS; supported/error-free native shader; Windows0build errors, ordinary2560x1440 fullscreen startup and actual30-athlete range probe PASS; append-only state and scoped feature branch |
| D — Frostline race loading screen | COMPLETE / OWNER TEST PENDING | Immediate illustrated screen, actual preparation stages, RU/EN/large/reduced-motion;21EditMode/4PlayMode PASS; all4actual stages painted and inspected in2560x1440 player;0build/runtime errors; tick/countdown held until handoff;670core/resource files unchanged |
| E — Coaches' enclosure | COMPLETE / OWNER TEST PENDING | Raised fenced deck,12 distinct athlete-derived older coaches,12 scopes aimed at actual banks; all-format/private-path clearance; static3LOD/shared-material art;34PlayMode/1EditMode PASS, native views and2560x1440 startup,0build/runtime errors, five core DLLs identical; fullscreen Coaches Review build |
| F — Live vector minimap | COMPLETE / OWNER TEST PENDING | Compact left-side actual-format route, player heading/position and moving rivals; cached course/actor layers, no extra world camera;14PlayMode/13EditMode PASS, native RU/large EN coverage and2560x1440 player;671protected files and five core DLLs unchanged; fullscreen MiniMap Review build |
| G — Continuous distant scenery | COMPLETE / OWNER TEST PENDING | Audience/forest/dressing retained to existing fog/frustum on all tiers; cached crowd transforms, additive136-triangle distant people, original close assets intact;41PlayMode+1legacy crowd/1EditMode PASS, six matched final-player cases/2880samples and12native world views; fullscreen Visibility Review build |
| H — Smooth forest LOD | COMPLETE / OWNER TEST PENDING | Native2mm approach reproduced37.56% canopy replacement; complementary opaque blends across bounded bands preserve original close assets, visibility, instancing/cache and zero-allocation preparation; both species/start-middle-end native pixel regression PASS; six matched standalone cases/2880samples, no tree loss; fullscreen Forest Hud Review build |
| I — Quiet race and career UI | COMPLETE / OWNER TEST PENDING | Removed map title/legend/count, permanent key/help captions, empty space, navigation footers and implementation explanations; retained useful race state, accessible RU/EN presentation, split history and deliberate controls/help;79unique native cases across H/I PASS,2560×1440 startup and five core DLLs unchanged |

References: IBU September2026 ECR8.3.2.1 (skis stay on), AnnexA3.2.1.13
(mechanical release impulse), AnnexA3.1.7.1 (5.6mm ammunition), existing ADR-0007
(inclusive deterministic aperture boundary). The game's geometric contract is
preserved; the UI must not claim a full mechanical impulse simulation.
