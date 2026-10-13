# Contextual shooting rivals — owner follow-up

2026-10-13. M14 WP-182/183 presentation follow-up COMPLETE. Latest source
`e2095e94` on `codex/m15-audio`; M15 audio remains delivered, M16 not started.
Test launcher: `Builds/Range_Broadcast_Review/START_TEST.cmd`.

## Behaviour

The Frostline shooting board automatically opens 250 route metres before the
authored entry and remains through shooting, penalty transit and until 250 metres
beyond the authored exit. Canonical route distance crosses lap seams and avoids
false stadium-radius activation on another stretch of the track. It hides at
prestart/finish and temporarily yields to the TAB split table.

Rows contain mat ordinal, career flag/name and five target circles: actual closed
targets are white, open targets dark. A separate small red dot is added to the left
for each accepted miss. The row pulses red briefly after a new miss; reduced motion
uses steady highlighting. Accepted results remain visible after releasing the mat.
The board reports the current shooting visit and stance, without inventing a rank.

Eight retained rows fit above the readiness/pulse panels, preserving the optical
center. The current athlete and fresh misses are pinned; other athletes rotate
automatically every five accepted seconds, with an athlete count/page footer.
All eligible rivals are accessible through these pages. Long names truncate with
full tooltip text. Elements ignore input and never block steering or shooting.

Files: `RangeBroadcastModel.cs`, `GameplayRangeBroadcast.cs`, the versioned
`range_broadcast_v1.json`, host HUD wiring, two read-only snapshot fields, native
and headless tests, build script. ADR-0068 documents policy and constraints.

## Verification

23 distinct selected tests PASS: 21 .NET core (8 new board tests plus 13 existing HUD
model checks) and 2 native PlayMode (actual career Mass30 and existing retained HUD).
The initial eight model checks also passed before pagination was added; the final
21-test receipt is the latest result. No test was removed or disabled.

New checks cover exact activation boundaries, prestart/finish hiding, target/miss
geometry counts, flash expiry, completed-series persistence, duplicate observation,
rewind rebasing and read-only authority. A real deterministic thirty-athlete career
Mass Start adapter run reached tick 4,344: board activation, an actual accepted miss,
career flags, all-rival pagination, completed series and later automatic closure
passed. Repeated UI refresh did not advance authority. This field run uses the
production route/roster with AI control for the observed athlete and omits environment
dressing to isolate the HUD; it is not a complete manually played race.

RU normal Full HD and EN 130% 1024×768 captures were inspected. Tests bound the board
to the right half, below the help hint, above pulse/readiness cards and inside the
viewport. Input picking is ignored. Existing compact-HUD coverage checks pass.
These do not claim the temporary board preserves the default HUD's 90% empty-area
budget, which was established for normal racing without contextual overlays.

Win64 Development build succeeds: 0 errors / 0 build warnings. Ordinary startup
exits 0. Native standalone venue/start/range review succeeds with 12 captures,
14 navigation actions, no collected errors and owner-profile hashes preserved.
The standalone review uses its existing one-athlete shooting probe; the dense
thirty-athlete board is verified by the separate PlayMode run. Its results page is
the explicitly isolated recorded-result boundary fixture. The screenshot with
real aiming camera/venue was inspected. Known offscreen URP DOF/Panini warnings
remain baseline. Final Unity Console contains no errors or exceptions.

Evidence: `docs/quality/range-broadcast/`; build identity records source and player
assembly hashes. Tests/additive snapshot observations never change race equations,
shot resolution, input commands, saves, replay serialization, AI or balancing.
No full-race balance regression was needed for this observer-only change.

## Performance and next step

No new snapshots, cameras, per-athlete scripts or physics queries. Fixed row/order
arrays, bounded retained UI and the existing 10 Hz HUD refresh constrain work.
The observer receives existing accepted snapshots to avoid missing a shot between
UI refreshes. This is an architectural work bound, not a measured full-game FPS
or zero-allocation claim. Paused/rewound observations do not invent flash events.

Next: owner tests the automatic panel during approach, shooting and departure,
particularly crowded Mass Start pages and miss feedback. The owner's screenshot
guided the information arrangement; visual-quality acceptance remains theirs.
Previous builds and unrelated local modifications were preserved.
