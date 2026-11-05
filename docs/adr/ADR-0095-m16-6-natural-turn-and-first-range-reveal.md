# ADR-0095 вЂ” Balanced ski-turn pose and prepared first range overlay

2026-11-05, owner follow-up M16.6.11. Presentation-only repair of the reported
sideways folding and first live-shooting overlay hitch. GDD29.2 requires separate
left/right-corner presentation selected from observed simulation state. Keep all
race physics, steering, costs, aim, shooting geometry, AI and journal rules.

## Turn balance

A red real-hero-rig test reproduces both signs at7m/s and curvature0.16/m.
Previously the whole bounded28degree bank was applied at the hips while leg IK
kept both skis on their authored paths; the pelvis had only the small cyclic
skating shift. That makes the torso fold sideways above stationary hips. The
same defect is visible without any contact impulse, excluding impact overlay as
its cause. Before/after rear-quarter captures use the actual shipped hero rig,
rifle, ground/pole fitting and grip corrections.

New versioned athlete_corner_pose_v1 sets a16degree visual bank ceiling,0.08m
maximum inward pelvis transfer and0.015m compression. Share bank between hips,
spine and chest(0.65/0.25/0.10) before analytic leg/arm IK. Existing temporal
response follows the observed turn, including reversals, stop/seek resets and
unchanged forward propulsion/tuck profiles. Apply the same helper in both the
hybrid and legacy procedural driver. Rig/model roots, ski support paths,
physical world heading and accepted input remain unchanged. Impact recovery is
still driven separately by real contact impulses.

The old corner test required hip.up lateral component>0.3, enforcing the exact
excessive single-joint bend the owner rejected. Its angular assertion now checks
the new data-defined hip share numerically; its continuity, foot alignment,
root placement, signed response and stopping checks remain. New tests measure
actual pelvis transfer and upper-body bank in both directions. Existing full
hybrid technique, anatomical elbows, ground/grip, exertion/stance and impact
checks remain acceptance gates.

## First range overlay

The board existed from startup but DisplayStyle.None prevented text/layout
preparation. The first real window started with zero measured height and built
its header, visit label and footer during skiing. A red lifecycle test records
that zero-height condition. In the baseline Development player on9950X3D/RTX5080,
first reveal at tick1712/510.791m cost0.5828ms for snapshot/publication/HUD versus
0.1540ms on the preceding frame; UI layout0.3179ms versus0.1490ms. The whole
capped frame was16.348ms: no visible full-frame stall reproduced on this machine.
Do not turn this local cold-work finding into an all-hardware hitch-free claim.

Keep the absolute board in layout with Visibility.Hidden while it is outside
its original contextual window. Its header and first visit/footer are measured
before skiing; reveal uses visibility rather than activating a new layout tree.
Precompute localized visit strings during HUD construction. Row capacity still
uses actual target height, scale and condition-card clearance. Geometry changes
request layout with an enabled/document guard. Only an actual readable-capacity
change recomposes the retained rows after a resize; ordinary geometry events
no longer recompose and repaint the entire accepted shot board. Rows remain retained and update from
the accepted feed. No guessed shots, changed arrivals, input interception,
extra camera, profile instance or diagnostic loop is added to ordinary gameplay.

Unity6.6 primary documentation confirms Hidden participates in layout and does
not render, whereas Display removes an element from layout:
https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/uielements/visibility/hidden
Profiler reference:
https://docs.unity.com/en-us/engine/6000.6/manual/uitoolkits/uielements/uie-test-ui/profile-ui/uie-profiler-markers

The opt-in RuntimeRangeRevealReview uses an actual30-athlete production field,
accepted snapshots, first window and explicit1280x720camera target. It records
600frames after a440m pre-roll, with no per-frame image/file writes. It is absent
in ordinary play unless the explicit Development-player flag is supplied. UI
markers unavailable on the backend are reported as-1, never invented as zero.

The existing first-bout HUD integration test had a pre-existing dependency on an
AI seed producing a displayed miss: it failed identically against the original
HUD source at tick6110 before and after this repair. Use ordinary off-target
player aim/trigger/bolt commands, the existing bounded steering assistance and
real geometric outcomes to supply a deterministic accepted miss. Keep every
original queue/flag/flash/retention/read-only/resolution assertion and additionally
require the accepted completed series to contain a miss. No shot result injection
or gameplay rule modification. Final receipts and limits are in
../quality/m16-6/turn-board/README.md.
