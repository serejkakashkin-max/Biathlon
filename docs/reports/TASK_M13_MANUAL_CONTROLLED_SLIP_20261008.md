# M13 — signed steering with independent physical slip

2026-10-08. Intermediate implementation under ADR-0037/0038 and the owner's
fully manual direction directive. M13 and the final review build remain OPEN.

## Change

`ManualSkiSteeringSystem.GetRequest` exposes the existing speed, posture, grip,
yaw-rate and angular-response limits without assigning them to physical motion.
The old heading solver uses the same calculation. Its definition and defaults
are unchanged; raw post-contact spin above the command limit is accepted by the
new force path rather than being clamped to that limit.

`ManualSkiSteeredSnowSystem` uses a frozen two-support approximation. The rear
constraint follows the achieved ski heading; the front constraint is deflected
by `atan(2 * support arm * requested yaw / signed forward speed)`. The existing
minimum grip speed bounds the denominator. Reverse motion reverses deflection
for the same requested world yaw. This is a ski-contact approximation, not an
animation assignment or an exact model of skating biomechanics.

The two impulses minimize total translational and rotational kinetic energy
inside equal front/rear grip budgets. The positive determinant is evaluated
algebraically without cancellation. Both impulses are scaled together if their
angular response exceeds the existing acceleration/deceleration limit. Scaling
a dissipative impulse towards zero remains dissipative. Initial velocity and
spin remain independent of heading; neither is rebuilt along the route tangent.
The solver cannot create a pivot at rest or steer with zero grip. Propulsive
movement still comes from the shared ski force model. Turning costs existing
travel energy and exposes that loss, both impulses, request, deflection and
response scale for telemetry. No new hidden power/speed/stat multiplier exists.

`ManualSkiControlledDriveSystem` combines this one joint snow budget with the
existing actual-yaw cornering capability/braking and the unchanged longitudinal
power, gravity, drag, snow, stop and speed-cap calculation. The legacy contact
drive retains its passive API and context. Its common implementation now accepts
an internally computed frozen snow preview. The new controlled path has a
distinct versioned context binding the existing definitions and physical policy.

The resulting `SkiDrivenContactTrajectory` works through the existing common
contact-force coupler and owned contact/boundary scheduler. Every source retains
its original motion at fraction zero; prefix queries retain the same frozen
preview. Actual whole-body/course/phase checks and a fully paid clock remain
mandatory. A force preview does not authorize a rotating pair's movement.

## Verification

Fifteen new tests cover:

- parity of the request with the existing heading solver over 135 combinations;
- signed, mirrored and reverse steering, mass-scaled support forces, rest/ice,
  posture/speed/grip response, neutral local damping and retained contact spin;
- 1,620 slip/spin/heading/speed/input/grip combinations with shared impulse
  bounds, angular-response bounds and non-increasing total kinetic energy;
- unchanged neutral aligned shared drive, raw source and frozen slices;
- actual reached-pair force coupling in both roster orders, a deterministic
  100-step turn/release model rollout, and a fully paid prepared session/world
  tick with signed steering through the common force/event/publication path;
- input/context validation and zero warm managed allocation over 1,000 queries.

Initial executed run: 12/13 PASS. The neutral rotated-velocity fixture expected
bitwise zero after projecting encoded sin/cos vectors; its residual is ordinary
floating-point roundoff. The assertion now checks individual components within
2e-15, while the energy and impulse bounds remain unchanged. The failed receipt
is retained. No velocity snapping, tuning change or production tolerance increase
was introduced. Two test-source member-name compile errors were fixed before
execution; no tests were disabled or deleted.

Final all-manual plus Sprint/Pack/Player replay .NET selection: **431/431 PASS**,
including the allocation families in the previously qualified nonconcurrent
test host profile. Production/Unity GC settings were not changed.
Native Unity: new controlled class **15/15**, old steering **11/11**, old contact
drive **14/14** — **40 executed PASS**. Final engine/Console receipts are recorded
in `docs/quality/m13/controlled-slip-20261008`. Discovery total 1,515 is not an
execution count.

## Limits / next step

This is a deterministic frozen-interval contact model. Snow is not an exact
continuous Coulomb/skating solver. Heading/pose use the existing encoded state
contract; input raw motion is preserved within each proposed trajectory. The
100-step rollout qualifies the control model, not a full collision-admitted race.
The reached-pair fixture qualifies force coupling, not rotating whole-body/course
clearance or removal of owner-observed queues in the playable host.

The production host still uses legacy v1 movement. The complete common-clock
runner, all-field steering/seams, changing mat ownership, stationary shooting,
finished actors, v2 race replay and host/presentation remain to be connected and
qualified. Physical velocities beyond the shared scalar force envelope remain
an explicit unsupported case. Production 40-actor work/frame/performance and
WP-175/177/178 remain OPEN; WP-176 PASS. No intermediate Windows build, owner
gate request, shooting retuning, venue-count change or Master Plan rewrite.
