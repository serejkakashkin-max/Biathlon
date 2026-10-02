# Venue01 — bounded shoulder verification, visual findings

Actual Unity6000.6.0f1 GameView,2560×1440, High tier, frozen live Mass Start30 at tick1030. These five views qualify the reviewed geometry checkpoint, not final WP169 visual acceptance or player/GPU performance. Normal camera rendering includes the instanced forest/crowd; no forced Camera.Render was used. Each JSON binds the exact camera, source/resource hashes and PNG hash.

| View | Visible trees | Finding |
| --- | ---: | --- |
| [Stadium](stadium-overview.png) | 1 | Distant authored forest is culled well before the fog starts; presentation correction required. |
| [Side entry](side-entry.png) | 11 | Thirty mats and the rear distribution corridor are present; distant forest remains absent. This still image does not prove dense admission. |
| [Climb](climb-forest.png) | 219 | Straight shoulders and nearby forest render; conservative retained joins remain at branch overlaps. |
| [Highland](highland-chase.png) | 301 | Nearby trees/shadows render; distant continuation ends at the old cull boundary. |
| [Snow edge](snow-edge.png) | 246 | The Main568 west edge retains a visible vertical step at an overlapping Sprint branch. |

Source97f01ad3 passed native Contact11/11, Course8/8 and complete PlayMode210/210, with zero failures/skips/Error logs. Every paid source ribbon/terrain height remains unchanged. Whole-panel fallback prevents new overlap, buried caps and incomplete terrain contact; it does not make every resulting edge visually acceptable.

At the snow-edge witness, a full outside shoulder genuinely overlaps an original Sprint branch; even shrinking that width cannot remove the overlapping upper step at its origin. The geometry gate must remain strict. A separately verified format/route presentation policy is required before this witness can be accepted. Forest visibility is handled by TASK169Z; gameplay, body/guard dimensions, shooting and race output stay unchanged.

Historical before-shoulder views are retained in ../venue01-captures-after-snow-fix. Camera poses match, while live ticks, fog and rendered sources differ; no pixel-identical comparison is claimed. Console after these views:0errors,5retained warnings,compilation_failed=false. No new Windows delivery or final WP169 PASS.
