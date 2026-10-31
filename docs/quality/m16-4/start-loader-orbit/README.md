# M16.4 WP-K/L/M — personal start, loader HUD and ski camera

2026-10-31. READY FOR OWNER TEST; M16.4 remains OPEN.
Fullscreen candidate: `Builds/M16_4_Start_Camera_Review/START_TEST.cmd`.

## Delivered behavior

- K: a delayed player is hidden until the existing personal3second countdown.
  Keep the accepted waiting pivot for camera tracking, but hide the avatar,
  equipment and pose/VFX work. At the exact boundary show the athlete without
  moving a physical body or changing start gaps. Immediate starters retain their
  local3–2–1 hold. Skip executes the same intervening ticks/journal commands.
- L: the independently sorted GameplayHud document remains detached throughout
  Venue/Ready loading. Show it only after the menu loader hands over to the race.
  Extend the compiled loader probe with `hud_hidden`; normal HUD remains available
  in the personal waiting/countdown view after loading.
- M: hold LMB and move the mouse while skiing to orbit the existing camera through
  rear, side and front views. Release blends back to the approved follow pose.
  Bounded pitch/final elevation prevents pole flips on steep grades; existing
  terrain/obstruction probes remain. Pause/focus loss, tables/help, prestart,
  shooting, replay/spectator takeover cancel the gesture. A hold begun over a UI
  table cannot become a drag after closing it. On entering shooting with a look
  drag held, require trigger release before a fresh LMB press can fire normally.
  Ordinary aiming, bolt and breath remain independent. New instructions are in
  deliberate F1 help; no permanent control captions are added to the HUD.

Camera deltas are consumed once per input frame, not per simulation tick or
multiplied by dt. Data lives in versioned `camera_orbit_v1`. Camera math/input
application is allocation-free after warmup. No added camera, production render
target, render pass, tree collider or physics query. Angles do not enter race
commands, saves or replays; normal replay/spectator cameras retain their behavior.

## Root causes and red evidence

`red_start_loader.json` retains the two original native failures: selected
prestart athlete active during long waiting, and HUD enabled/attached over the
real career loader. Earlier tests explicitly approved the old early avatar;
update that expectation to the owner's new instruction and add boundary tests.
No test was removed/disabled and no assertion tolerance was relaxed.

A rapid orbit probe exposed a pre-existing render-order error: environment
submission used the previous camera,1244trees, when the final frame required1210.
`red_render_order.json` retains that failure. Submit environment after camera
composition (orders200/100), making normal automatic culling match this frame's
final view exactly. This removes a source of visual gaps on quick turns without
changing forest detail or visibility policy. ADR-0080 records the decision.

The added Sprint test originally looked up a Pursuit-specific fixture ID; correct
it to use the actual stable player ID. The gallery initially attempted AI stepping
after binding manual player control; supply normal manual commands instead. Both
test-fixture defects were corrected before final qualification.

## Verification

38unique native cases PASS:29PlayMode and9EditMode. Final raw receipts:

- `camera_start_final.json`:13 — real Mouse state/gating, UI/focus cancellation,
  actual range takeover and trigger release, rear/side/front native gallery,
  current-frame automatic forest culling, wrapped yaw/clamped pitch/steep-grade
  protection/damped return/0warmed allocations; original chase collision/grade,
  all four start/pace presentation cases and shooting mouse/gamepad input cases.
- `start_loader_regression.json`:12 — all Sprint/Pursuit/Mass Start personal wait
  cases, exact one-tick-before/at3seconds, skip/ordinary journal equality, complete
  loading stages/failure/menu recovery/locale layout, checkpoint history/cursor.
- `broadcast_regression.json`:4 — dedicated camera ownership/restoration,
  deterministic rail/track cuts, zoom/culling and malformed-data rejection.
- `start_edit.json`:9 — personal prep clocks, delayed/early starts, skip boundary,
  invalid definitions and allocation-free observation.

Actual gallery: `orbit-0.png`, `orbit-90.png`, `orbit-180.png`, two native athletes
and the actual woodland environment, after100normal player ski commands. Camera
changes do not alter accepted tick/world pose. These are native offscreen visual
checks, not a whole-game FPS certification. The initial temporary start-pillar
gallery was replaced by the clear side/front course captures before publication.

Windows Unity6000.6.0f1 development build Succeeded,0errors/520existing warnings,
430640903bytes/28.981s. Shared builder retains its historical receipt ID;
milestone.json identifies M16.4. Compiled fullscreen startup, all four real loader
captures and final Console are archived here after qualification.

Compiled `player-qualified/review.json`: success,2560×1440 FullScreenWindow,
randomized Sprint start/bib29, personal wait skipped,3–2–1–0 observed, owner
profiles unchanged and no captured errors. `loading-review.json` confirms all
four stages, held clock/unarmed countdown and `hud_hidden:true`; all four actual
loader captures are retained. Waiting screenshot may contain an earlier starter;
the selected bib29 is hidden until its own countdown screenshot. Optional
advanced navigation qualification was not enabled and its default false fields
are not claimed as results. Final native Console:0errors/0warnings, idle, after
inspecting0errors/75existing build warning entries and clearing them.

1050task-entry core/pure-presentation/resource files remain byte-identical; only
new orbit data is added. Five Domain/Simulation/AI/Career/Replay DLLs are unchanged
against the prior Leader Hud Review. No scoring, start scheduling, balance,
physiology, AI, shot geometry, replay schema or save schema change. The only new
live input boundary prevents a camera hold becoming a shot. Owner profiles are
preserved; PROJECT_STATE keeps its full task-entry history prefix.

Next: owner tests delayed Pursuit/Sprint, loading, and held-LMB views in the
fullscreen candidate. No owner approval or milestone closure is inferred.
Recheck archived qualification with `python scripts/quality/report_m164_start_camera.py`.
