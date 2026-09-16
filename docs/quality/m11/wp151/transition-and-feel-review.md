# M11 transition and OWNER-FEEL-001 review

2026-09-15. Presentation review; no authoritative shooting tuning.

| Coverage | Evidence | Result |
|---|---|---|
| Idle, flat, climb, acceleration, sprint, tuck, technical descent | matrix/Locomotion.png; locomotion tests | Covered by data-driven poses and blending |
| Signed corners, fresh/tired motion, recovery fixture | matrix/ExertionAndCorners.png; corner/exertion tests | Covered; race finish visibility limitation remains below |
| Carry/draw/lower/ready/exit/carry, prone and standing | matrix/ProneSequence.png and StandingSequence.png;400 samples per integrated stance transition | Continuous sampled head motion, finite contact, rigid pole length and carry restoration pass |
| Actual phase deadlines and composed aim | real-bout PlayMode test; shooting/Prone.json and Standing.json | Snapshot clocks retained, rifle axis follows Sight1:1; shooting forces full LOD |
| Dedicated shooting camera | shooting/Prone-camera.png, Standing-camera.png | Actual neutral-input first shots at ticks2091/6031; closed target and post-shot sight differ according to existing recoil |
| Player chase | chase/tick-000000 through tick-002500 | Approved chase composition retained; state/commands/hash accompany captures |
|30 athletes and distance behavior | ../wp150/ near/far screenshots and timings | All30 visible, original and reduced mesh, measured zero-allocation pose path |

The matrix uses synthetic poses on a flat fixture; its bolt column supplies a known sight offset to inspect alignment. It is not a recording of an authoritative shot or a separate physical bolt animation. Actual shooting camera evidence is captured independently from the running Sprint host. State screenshots satisfy WP151's screenshots/clip alternative; no video clip is claimed.

## Missing transitions and retained visual actions

- **M11-VIS-001 — finish staging:** recovery pose exists, but AthleteProxyPresentation currently hides finished entrants. A usable finish-area sequence needs visible staging and a presentation clock after the authoritative race stops. Keep this explicit through WP160/WP164 rather than claim it is in the playable sequence.
- **M11-VIS-002 — character fidelity:** original rigid skin, segmented shoulders/elbows, simple head/gloves and no articulated finger/bolt action remain development quality. Reassess in the M12 representative close-up benchmark before G2. Current images do not establish final modern character quality.
- **M11-VIS-003 — ground contact envelope:** local plane and bounded limb reach do not solve arbitrary terrain crests/obstacles or world-locked pole friction. Validate against the finished terrain/snow benchmark at WP154/WP164.
- **M11-VIS-004 — distance transitions:** two tiers without geometry crossfade and distance-only selection; zoomed broadcast shots need priority/projected-size handling in WP163/WP164. Existing hysteresis protects ordinary threshold crossings.
- **M11-VIS-005 — scene readability:** bare route ribbons, empty environment and schematic range lack material, lighting, weather and atmospheric context. These are M12/WP152–166 work, not final visual acceptance.

No external paid/account/licensing action has been established as necessary. Therefore no asset owner gate is raised on speculation. Re-evaluate concrete asset need if M12 quality evidence demonstrates that the original approach cannot meet the target.

## OWNER-FEEL-001 remains OPEN

Actual samples show HR177/182 and low muscular fatigue in these scripted entries; they are not a difficulty test or proof that the fatigue system is too weak. Neutral aim plus a trigger on every tick is automated input, not owner hands-on evidence. M11 corrected third-person aim alignment, preserved geometric shot resolution, and added no hidden RNG. Exertion animation bypasses shooting; it does not add a second sway oscillator.

The dedicated view still uses a sparse grey background, simple targets and little bodily context. Better skiing/third-person animation cannot establish adequate aiming difficulty. All owner items remain mandatory at WP229/WP230/WP232: breathing, heartbeat/HR, muscular fatigue, stance, pressure/composure, wind/ballistics, trigger timing, breath hold/rebound, fine control, shooting stats, difficulty/accessibility assists, and greybox presentation versus simulation telemetry. No numeric authoritative tuning or closure is inferred from this presentation review.
