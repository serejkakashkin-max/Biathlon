# M16.2 — Live shooting broadcast · owner review

2026-10-30. M16.1 CLOSED / OWNER APPROVED. M16.2 implementation is ready for owner
review; closure remains the owner's decision. M17 not started. Feature branch
`codex/m16-2-live-shooting` from `ab54425c`. Queue feature `eaa1ad21`, final release
and small-screen tests/legibility `d68e1afd`. No main merge.

Run `Builds/M16_2_Live_Shooting_Review/START_TEST.cmd` — the ordinary career game
in desktop borderless fullscreen. [Build receipt](build-summary.json),
[M16.2 milestone](milestone.json), [exact executable/assembly hashes](build-fingerprint.json).
The common builder retains its historical `m15_5_visual_review` receipt ID; the
milestone file identifies this M16.2 delivery. This is a Development Windows64
build with existing careers supported. No profile migration or new game required.

## What changed

The existing `RangeBroadcastModel`, `GameplayRangeBroadcast` and
`range_broadcast_v1.json` are updated in place. No parallel shooting model.

* Fixed bounded rows for each athlete/visit, actual observed arrival then mat
  order. No player-first rule, miss priority or rotating pages. Twenty rows at
  1920×1080; adaptive capacity and readable typography at 900p/720p and large text.
* All30athletes stay in the model. A visible active row cannot be evicted for a new
  athlete. Overflow has a count. Completed rows hold3accepted seconds, fade0.4s,
  then admit the next queued entry. A completed queued result receives its own hold
  at admission. Stored result data remains after the row leaves the screen.
* Bib is read from the existing athlete bib presentation binding. Mat is the
  separate accepted lane ordinal. Career names/nations use the existing roster.
  Player styling is only a thin cyan rule, with equal queue/retention treatment.
* Five chronological accepted-shot marks: outlined pending, filled light hit,
  crossed red miss. Score and active/completed symbols. Slide/fade entry, local hit
  reaction, decaying miss accent and completed-row highlight. No whole-board flash.
* The accepted feed updates the board immediately; general telemetry keeps its
  existing cadence. Fixed UI elements and cached strings avoid rebuilding rows.
  Native warmed model observation/composition allocates0managed bytes. This is not
  a claim that the whole game/UI allocates zero or a race FPS benchmark.
* The existing readiness card moves beside the bottom indicators during coverage,
  then returns to its original position, avoiding the live board. TAB replaces the
  panel outside shooting. The panel and children ignore gameplay input. Reduced
  motion removes sliding/scaling; typography and high contrast remain supported.

The original250m canonical route entry/exit context remains; penalty transit holds
it open. No Euclidean stadium-radius approximation or gameplay clock modification.

## Accepted data, replay and honest limits

The physical `ClosedTargetsMask` does **not** encode shot order. The observer stores
the accepted shot-count/closed-count deltas, including final shot and session
release in the same snapshot. Reverse target selection does not corrupt the marks.
It never raycasts, aims, rolls RNG or resolves a hit/miss. All timers use accepted
tick/frequency with bounded render interpolation.

Duplicates do nothing. Rewind, skipped ticks or clock changes rebase transient
state without hit/miss/completion pulses or resurrected historical completed rows.
An arbitrary seek into a mixed past series cannot recover its chronological
sequence from an aggregate mask. Those past slots use an explicit dash/unknown
mark and exact accepted score, never fabricated hits or misses. All-hit/all-miss
prefixes can be identified without guessing. First-observed arrival order after
seek uses that observed tick/mat, not a claimed recovered historical timestamp.

At capacity, the player follows the same overflow rule as everyone else; no
reserved player slot is introduced. At smaller resolutions fewer rows are visible,
but all active/queued data remain accessible to the observer. Existing accepted
formats are Sprint, Pursuit and MassStart. **Individual and Relay are not yet
implemented competition formats in the project.** This presentation stage does not
create pretend versions or change simulation to add them. Future relay extra rounds
need a real accepted-data contract beyond the present five-shot magazine.

## Native visuals and videos

[Open the gallery](review.html).

* Main: [31-second1080p video](live-shooting-main.mp4),620original native frames at
  20fps. Real production career MassStart,30athletes, all controlled by the existing
  AI for repeatable QA. All30were displayed;30simultaneously active observed. Shows
  player in natural order, hit/miss updates, clean5/5, hold/fade and replacement.
* Player miss: [15-second1080p video](live-shooting-miss.mp4),300native frames at
  20fps. Same career pipeline/field; ordinary player effort, aim, trigger and bolt
  commands deliberately point away from the targets. Existing AI steering supplies
  reproducible movement using the same physical solver. **Only input is scripted;
  no outcome, mask, athlete pose, stat or shooting rule is overwritten.** The
  accepted geometric miss appears in the player's actual chronological slot.
* Real world plus native UI targets are composited in GPU only for these explicit
  dev-player probes. One accepted20Hz tick per exported video frame; PNGs are not
  generated UI mockups or retouched. Screenshot/layout warmup pauses are excluded
  from video frames. Videos are silent QA captures, not human play or timing/FPS
  measurements. [Frame hashes, video hashes and full decode checks](video-evidence.json).
* Actual native screenshots cover1920×1080,1600×900,1280×720 in RU/EN; existing
  HUD tests additionally verify large text and input transparency. The screenshots
  around arrival can show partway entry opacity, correctly reflecting that tick.
* [Main player protocol](player-main/review.json),
  [geometric player-miss protocol](player-miss/review.json) retain accepted ticks,
  masks, chronological marks, visible slots, overflow and distinct displayed count.
  Dedicated review entry points/cameras/shader/PNG allocation run only with explicit
  development flags; ordinary gameplay adds no MonoBehaviour, camera or raycast.

Earlier native recordings failed the **evidence scenario** because the actual AI
player shot5/5 and provided no own-player miss during the captured sequence. They
had no runtime errors and are retained as incomplete evidence, not reported as
passing acceptance. The separate ordinary-input miss probe supplies that scenario.
Two earlier responsive-overlap failures and the Pursuit fixture missing its required
qualification were corrected; no assertion was removed or failing test disabled.

## Verification

* Native23EditMode PASS: [final expanded run](model-expanded-final-native.json).
  All30stable/overflow/arrival ordering, every possible miss position, reverse
  physical target order, five misses, exact final-shot hit/miss with same-state
  release,3s hold/0.4s fade, duplicates, seek, bout isolation and warmed allocations.
  Pursuit binds an actual qualifying Sprint result, not a made-up qualification.
* Native3unique PlayMode PASS: [HUD/readability run](hud-legibility-final-native.json),
  [final range/layout capture](hud-clear-final-native.json),
  [capture clearing regression](capture-clear-green-native.json).
  Existing retained HUD and actual production career MassStart test updated in
  place. Panel bounds,20-row1080p capacity, adaptive smaller capacity, flags,
  pulse/strip state, unchanged authoritative tick and existing help/TAB behavior.
  The final capture regression reproduces removed UI content on a real native
  RenderTexture: [red before fix](capture-clear-red-native.json), green after fix.
  The QA-only panel now clears transparent before every redraw. The ordinary
  camera-cleared screen panel is unchanged. This prevents stale retired rows and
  captions in the videos; both clips were recaptured and inspected through their
  middle and last frames. [Diagnostic before](capture-stale-before.png),
  [same accepted frame after](capture-clear-after.png).
  API contract: [Unity6000.6 PanelSettings.clearColor](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/uielements/panelsettings/clearcolor).
* Separate CoreCLR23/23PASS: [TRX](coreclr-model-final.trx). Same cases on another
  runtime, not23extra unique native cases. No claim of a green full legacy suite.
* Fresh [five complete30-athlete authority regressions](race-regression/summary.json):
  2Sprint/2Pursuit/1MassStart,121817ticks/2400geometric shots and14actual weekly
  save/reload boundaries. Full race payloads, event hashes, tick/shot counts and
  saved-career boundary hashes match approved M16 byte-for-byte. Final save
  `5b9c15db8923236a520ab5888bba925bcf63e68671501716243dfcb466bcfad7`.
  The existing lab receipt keeps its historical `m16_1_visual_authority_regression_v1`
  ID; this directory contains the newly executed M16.2 invocation and its manifest.
* [655-file authority source guard](authority-source-final.json): starting-workspace
  Domain/Simulation/AI/Career/Replay C# and non-UI Biathlon JSON unchanged. No
  ShootingStateMachine/Session/Handling, AI, geometry, mats, rules, penalties, game
  timers, career/save or authoritative replay edit. Existing unrelated local edits
  were retained rather than staged or reverted.
* Both compiled-player scenario probes PASS with zero runtime Error/Exception/Assert.
  Final ordinary fullscreen acceptance verifies actual creator/save/Continue,
  backup recovery, settings/bindings reload, checkpoint, diagnostic export,
  personal countdown and accepted race ticks at2560×1440; owner profile directory unchanged:
  [receipt](fullscreen-acceptance/review.json).
  Selected final fullscreen screenshots: [personal countdown](fullscreen-acceptance/08-countdown.png),
  [ordinary race](fullscreen-acceptance/09-race.png). Other listed raw startup
  captures remain locally in `artifacts/m16-2/fullscreen-final`.
* Build0errors;24API deprecation warnings in existing/new review adapters:
  [build messages](build-messages-final.txt). Native Editor Console is clean after
  qualification: [final check](console-final.json). Native
  probes retain the existing URP active-render-texture warning at shutdown, not a
  runtime exception. No graphics quality reduction. Final shader support, Console
  and build evidence are retained separately; no zero-warning claim.

Next: owner tests `START_TEST.cmd`, checks live shooting in their career and decides
whether to close M16.2. M17 is not started and main is not merged.
