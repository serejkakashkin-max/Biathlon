# TASK169Z actual production High forest review

Five normal GameView captures from an initialized Woodland Basin Mass Start host, frozen before stepping at tick0:30athletes,30banks,26,441authored trees and99terrain tiles. All images are2560×1440 and High. Root captured through the normal camera loop and `UnityEditor.GameView.m_RenderTexture`; no `Camera.Render` fixture is used. Each JSON contains11source and8resource SHA256 bindings, pose/FOV/clip planes, camera/save frames and the PNG hash.

| View | Before Z submitted trees | Actual Z submitted trees | Independent prediction |
|---|---:|---:|---:|
| [Stadium overview](stadium-overview.png) | 1 | 3507 | 3507 |
| [Side entry](side-entry.png) | 11 | 3758 | 3758 |
| [Climb forest](climb-forest.png) | 219 | 6988 | 6988 |
| [Highland chase](highland-chase.png) | 301 | 6201 | 6201 |
| [Snow edge](snow-edge.png) | 246 | 7370 | 7370 |

The saved PNG hashes/dimensions and all current source/resource bindings match. Camera positions/look targets differ by less than0.00001m from the earlier `venue01-captures-shoulder-review` poses; FOV/near/far planes match. Earlier T views were tick1030; these views are a different initialized host at tick0. The simulation is frozen in each set, but pixel identity, the same athlete state and a moving race observation are not claimed.

Inspection of all five PNGs confirms the production High visibility correction: the stadium and side-entry horizon contain the authored forest, and distant trees continue through the climb/highland/snow views. Tree placement, density, species, terrain, native art and lighting/URP assets are unchanged. Omitted policy and Low/Balanced remain covered by native regression tests. The snow-edge image retains the previously diagnosed blue original wall at a multi-format ribbon overlap; this is outside Z and keeps T2/WP169 visual acceptance OPEN.

Z focused9/9 and full219/219PlayMode tests pass with zero failed/skipped/Error logs. These captures establish the bounded visibility result only. Their metadata explicitly has `performance_qualification=false`: GPU, FPS, general quality/balance and overall WP169 completion are not qualified. More distant submitted geometry is an intentional cost.

The comparison rerun at **2026-10-02T21:56:52Z** verifies the captured frozen revision. Its exact source/resource hashes remain historical evidence if T2 changes presentation afterward; a comparison against another source revision is a separate result. The post-capture Console contains 0 errors, 5 retained warnings and 5 logs, with compilation_failed=false and the Editor idle.

[Z machine proof](../wp169z-production-high-forest-visibility.json) binds the exact five-view comparison, tests and source hashes. The replayable metadata comparison is `artifacts/wp169z/compare_captures.py`; its saved result is `actual-five-capture-comparison.json`. [Report](../../../reports/TASK-169Z-production-high-forest-visibility-REPORT.md).
