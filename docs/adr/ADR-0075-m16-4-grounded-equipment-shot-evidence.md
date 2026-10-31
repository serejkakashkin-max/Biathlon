# ADR-0075 — M16.4 grounded shooting equipment and accepted shot evidence

2026-10-31. Owner closes M16.3 and requests laid equipment and persistent shot traces.

The upright equipment is the staged pole pair: rifle transfer stows its handles
at 1.36m, and late ground contact forces the shafts upright. Use stance-specific
handle locations and surface-parallel pole shafts during the existing transfer.
Prone poles rest alongside the right side; standing poles lie between the skis.
The visual shaft/basket clearance is authored once in the contact recipe. Model
feet/ski directions still use the same surface normal. Setup, exit, rifle aim,
authoritative position and every race/handling duration remain unchanged.

IBU ECR8.3.2.1 prohibits removing skis on the mat. The real authored physical mat
is flush with the packed snow (its centre is minus half its thickness). Visual
qualification must use this frame; an initially raised diagnostic cube incorrectly
occluded ski bodies and was corrected to the production mat's actual top plane.
Use the shipped Hero rig for review, plus independent original-rig contact tests.

Shot traces consume exact accepted `ShotImpact` / `ShotOutcome` observations from
every `RacePresentationFeed.Published` tick. The production host already preserves
adjacent accepted states on catch-up frames, and the positive minimum exit duration
retains the fifth accepted shot before mat release. No sight interpolation, closure
mask inference, new target resolver, Unity physics or random outcome is involved.
Bounded pure presentation storage records all five current-bout slots per athlete;
skipped shots stay unknown. Duplicate and stale observations cannot add a trace.
An explicit feed reset clears race-local evidence even at the same destination tick.

Each bank has one preallocated, initially disabled marks mesh and one shared
material. Actual authored front-plate mesh bounds determine placement limits.
Keep at most32 front-plate marks per bank; oldest marks are replaced. Aperture
interiors and white closure flaps clip damage in the shader. A centre hit passing
through the aperture does not drill a new hole in the white flap. A rim graze can
chip visible front-plate paint. Far misses do not place floating holes outside the
bank. Meshes update only on actual plate strikes/reset, not every render frame.

A compact last-shot diagram in the existing shooting card displays the accepted
point and nominal5.6mm footprint. Edge hit and edge miss labels use the accepted
outcome; their proximity band is purely descriptive. Existing ADR-0007's inclusive
nanometre-canonical circle remains the sole scoring boundary. No bullet-radius
expansion, hidden roll, difficulty bonus, new event/hash format or save migration.
Real mechanical targets use release impulse (AnnexA3.2.1.13), including half-bullet
strikes and allowed mechanism tolerances; this game does not claim to model that
mechanism. Preserve its agreed geometric encoding and expose the result honestly.

Qualification keeps the accepted HUD area limits (under10% normal /17% large4:3),
checks both final shots, bounded storage, exact wind-adjusted points, warm zero
model allocations, surface contact, reversible equipment and unchanged deadlines.
Native rendering must produce actual pixels; a black unsubmitted capture is not
visual proof. Verify the D3D11 shader and compiled ordinary fullscreen player.

Primary references consulted 2026-10-31:

- [IBU September2026 ECR,8.3.2.1](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
- [IBU AnnexA,3.1.7.1 and3.2.1.13](https://assets.ctfassets.net/cz0vl36hcq0x/2iDuPyE16smAkjo3DoobKs/fba5ec5c25b846c07b6e5e39b4295761/ECR_Annexes_2026_cap04.pdf)
- [US Biathlon coach education, winter range procedure](https://minnesotabiathlon.com/wordpress/wp-content/uploads/2020/11/CoachesEducation.pdf)

## 2026-10-31 — owner follow-up: visible race loading

The previous launch only relabeled its button before synchronous composition.
Use the existing menu's top-order UIDocument for a dedicated Frostline screen:
the existing Woodland venue illustration, Deep/Ink/Accent palette, Oswald/Roboto,
the actual format/venue and a five-target indeterminate activity signature.
No fake percentage or technical resource descriptions. Reduced-motion uses a
static indicator. No new bitmap, scene, world camera or racing-tick work.

Expose four presentation stages: prepare the race, create athletes/presentation,
initialize the venue, then ready. Let two ordinary frames run before each
synchronous phase so layout and a render submission can complete. Unity world
construction still runs synchronously; scheduled animation cannot run while that
main-thread work blocks. The truthful status/illustration remain visible then.
This is a loading-feedback change, not an async-loading/performance claim.

Keep the host disabled, field tick0 and personal countdown unarmed throughout
loading. After the prepared world has rendered behind the loader, perform the
existing BeginRace/ArmRaceStartCountdown handoff. Delayed Sprint/Pursuit starts
retain their waiting and skip behavior. No race duration, seed, checkpoint/save,
physical preparation or personal countdown length changes. Loading blocks repeat
launch/back input. Failure cleans up the partial race and restores the menu/error.
Native tests cover launch/stages, readonly clock, failure and unchanged saves;
layout fixtures freeze the real screen before composition. Opt-in development
player QA can capture all four actual painted stages, separate from frozen views.
