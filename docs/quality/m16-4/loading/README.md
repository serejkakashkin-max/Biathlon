# M16.4 WP-D — Frostline race loading

2026-10-31. Test build: `Builds/M16_4_Loading_Review/START_TEST.cmd`.
The previous shooting-contact review binary is preserved in its original folder.

After **Start Race**, an illustrated loading screen immediately replaces the
pre-race page. It uses the existing Woodland venue artwork, Frostline Deep/Ink/
Accent, Oswald/Roboto and five small target circles. A truthful caption describes
the actual preparation stage. No made-up percentage, technical resource dump,
new bitmap, racing camera or background racing-tick work. Reduced motion keeps
the activity signature static; RU/EN and large4:3 are checked.

`CareerRacePresentation` yields two ordinary layout/render frames before race
composition, athlete presentation, environment construction and final handoff.
The host remains disabled at field tick0, with personal countdown unarmed. The
world gets a frame behind the loader before the existing BeginRace/countdown
sequence takes over. Delayed-start waiting/skip behavior remains unchanged.
No minimum fake-loading duration or time-based invented progress is introduced.
Unity construction is still synchronous: the scheduled dots cannot animate
while the main thread is blocked, but the illustration and stage remain visible.
This change provides feedback; it does not claim asynchronous initialization or
shorter loading times. Back/repeat launch cannot interrupt partial composition.
Failure cleans up the partial race and returns to the ordinary menu/error without
saving the prepared career week.

`FrontEndRaceLoading` builds the dedicated view in the existing top-order menu
UIDocument, above world/HUD initialization. Its activity schedule is paused when
the page is rebuilt/removed. Existing image and fonts are reused. `PlayerBuildM16_4`
adds a separate loading review folder and review notes. The existing opt-in
`M15_6PlayerReview` can record actual painted loading stages with
`-biathlonM16Review -biathlonLoadingReview`; ordinary play creates no QA instance.

Verification:

| Receipt | Result | Scope |
| --- | --- | --- |
| `native-final.json` | 4 PASS | Immediate view, all stages, duplicate/back blocking, held tick/deadline, failure cleanup/save preservation, existing full production environment launch/pause/abandon, RU/EN/large/reduced-motion layout |
| `editmode-final.json` | 21 PASS | Existing career session and personal start/countdown contracts |
| `player/loading-review.json` | 4 painted stages | Compiled real launch, immediate view, host/tick held and countdown unarmed; four actual native panel captures |
| `player/review.json` | PASS/exit0 | Ordinary desktop fullscreen, actual load and personal3–2–1/start, owner profiles unchanged, creator/Continue/recovery/settings/checkpoint/export |

`views/loading-*` are inspected layout fixtures, explicitly frozen before
composition. `player/07d-loading-*` capture the real staged pipeline without
freezing or editing the race. The field remains tick0 until the normal countdown
handoff. Menu layout checks do not advance career/race state.

Unity6000.6.0f1 Windows build Succeeded,0errors,385364547bytes,20.37s. Categorized
520warnings:24unchanged CS0618 call sites,460UnityAIInference `ConvGeneric` compute
variants and36Sentis pixel-shader variants, retained in `build-messages.tsv` and
classified in `build-warning-audit.json`. The generic helper still uses legacy
`m15_5_visual_review` receipt ID; `milestone.json` identifies M16.4. Actual final
Console is clean, compilation and Play Mode idle. No new full-race, career-soak
or minimum-hardware performance claim.

The first compiled stage receipt passed its state flags, but visual inspection
found that the old explicit QA camera request overwrote the menu's texture once
the host existed. Those intermediate frames are rejected as visual proof
(`interim-capture-audit.json`). Final QA skips that world request during loading
and requires rendered title pixels in every stage capture. Ordinary screen-space
menu rendering was not affected by that offscreen QA bug.

All670 baseline core/resource files remain byte-exact, including every Domain,
Simulation, AI, Career and Replay source. No seed, balance, race timing, physical
setup duration, replay/event contract or save-schema change. Previous PROJECT_STATE
history is retained as a complete prefix and new dated records are appended.
Unrelated owner/Unity changes remain untouched and excluded from this feature.

Recheck with `python scripts/quality/report_m164_loading.py`. Historical M16.4
shooting-contact receipts remain in the parent folder and correspond to47f33589;
this follow-up has its own source/build fingerprints. WP-D awaits owner testing;
M16.3 remains owner-approved, M16.4 remains open.
