# M14 owner-review revision — 2026-10-12

Implementation complete; M14 remains pending owner acceptance.

## Delivered

- Preserved the original menu biathlete. Replaced dense career pages with a snow/ink/teal card system, paged calendar, graphic training forecast, ski comparisons, actual course map and compact result protocol. Default result page contains the player. Keyboard/controller focus and RU/EN text scaling remain supported.
- Woodland now has six accepted timing lines per main loop. Crossing shows provisional rank/gap; Tab opens the last crossed line's field table. Sprint compares individual elapsed times; simultaneous formats use global race time. Athletes who have not crossed that line are excluded.
- Keyboard A/D reaches full input in 100 ms. Shared yaw response is quicker; releasing steering retains the released heading. S is explicit physical braking. Paid pole-assisted stepping enables faster low-speed recovery without teleporting or a hidden player advantage.
- A pulse band appears within 160 m of a scheduled range and during shooting. Its green/amber/red guidance uses the actual physiology and breathing definitions, with words and numbers as well as color. It explains existing continuous sway; it grants no accuracy bonus.
- After the player's finish, watch unfinished competitors with follow, overhead or side cameras. Use [ / ] to switch athletes, C to switch camera, and Enter / Complete race to simulate the remaining field. Every authority tick, event and replay command is retained; results save only after the real field completes.

Timing reference: [IBU September 2026 rules, section 9.2.7](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf) places intermediate timing after the penalty loop following shooting. Woodland's extra forest/climb/descent lines are an authored gameplay choice. Presentation reference: [official Datacenter](https://biathlonresults.com/?RaceId=BT1718SWRLCP02SWSP).

## Verification

- .NET focused controls, paid pivot, checkpoints and HR checks: **76 passed**.
- Full production Sprint30, AI-free command replay, exact protocol and durable career transition: **passed**, 31,602 ticks; result SHA-256 `509e232de65dbbfbb174da7ad2835d5eef12d700adf33bbef0ee1ac8ad512c24`.
- Native PlayMode menu/navigation: **10 passed**; device input **8 passed**; HUD **1 passed**; review cards and live post-finish authority **2 passed**. Final focused card and result layout checks also passed. Native EditMode review core: **6 passed**.
- Screenshots inspected at 1600x900 RU and 1024x768 EN with 130% text. Pagination controls are checked against the actual page viewport; all calendar/protocol entries remain available.
- Unity Console: no current errors. Native UI capture and camera-state assertions cover the post-finish interface; this is not a frame-rate benchmark or a manual full race.

Evidence: `docs/quality/m14/review-20261012/`. Implementation decisions: [ADR-0059](../adr/ADR-0059-m14-owner-review-controls-and-live-finish.md).

## Build and compatibility

Code source: `4d522bdccbeb2188ca1ab6195148b70e82dfa895` (implementation `5e04835`, final result viewport fix `4d522bd`).
Test package: `Builds/M14Revision20261012/START_M14_TEST.cmd`. Build/startup receipts are recorded in the qualification summary beside this report's screenshots.

Steering physics and checkpoint context intentionally change race output. Player and AI share the same physical operator; existing career profiles remain valid. Older incompatible command replays are rejected with the existing compatibility message. Texture, shadow, resolution and quality presets were not reduced. Pre-existing local project/rendering/package changes were preserved and excluded from these commits.

All WP-179 through WP-197 remain implemented (19/19); this revision addresses owner feedback on WP-179, 181, 182, 184, 185, 186-189 and 194-197 plus shared controls. No M15 work started. Next action is owner testing of this package and the owner's M14 acceptance/correction decision.
