# ADR-0094 — Private mat approach margin and authored moving-pass zones

2026-11-03. Follow-up qualification of ADR-0093. Preserve immutable v2/v3 AI
resources and all physical admission rules; publish a new decision policy v4.

The complete default Release run found one failure among 2205 executed tests:
the original sixteen-event career season. The first Sprint and Pursuit completed,
then the first Mass Start rejected a moving firing arrival at tick11770,
athlete21, mat30. A captured pre-event career snapshot reproduces it in about
22 seconds. The actor crossed the terminal plane0.915m from the mat centre,
outside the existing0.8m plant circle. Speed1.584m/s and spin1.383rad/s were
already within the unchanged capture limits. A projection is not permission
to shoot. No tolerance expansion, pose snap, free stop or assertion change.

Diagnostics excluded wrong mat assignment: the session and catalog both selected
mat30. A shorter private-approach target made no difference because steering
was already saturated. Fixed speed caps on the shared approach caused different
traffic failures and were rejected. The successful local change applies the
existing data-driven bend_speed_safety_factor0.85 to the private approach curve,
including its existing pre-fork braking envelope. Previously that curve used
the exact yaw/grip limit with no decision reserve. Departure limits are unchanged.
All deceleration remains an ordinary paid brake/effort command under the common
force solver. The captured first Mass Start then completes.

The subsequent season run completed five events and exposed a dense contact/
edge scheduling failure in the next Mass Start. Inspection found the new moving
pass selector did not consult authored overtake_allowed flags. v4 checks the
whole existing ordered lookahead for those flags before starting a moving pass.
It retains the full-body corridor, speed/reserve gates and physical admission.
An already established pass finishes through existing clearance rules; the
policy does not abruptly merge an athlete into a neighbour. Stationary obstacle
avoidance remains available. An athlete can wait through a forbidden bend and
pass on an allowed straight. This is AI route judgment, not a hidden speed cap.

New races bind moving_overtake_version2 / manual_ai_steering_v4. Absent/zero
metadata still binds immutable v2, version1 immutable v3. The same selector is
used by career headless execution, Unity host and replay reconstruction/backward
seek. The movement context changes only for the new policy. Save schema,
statistics, RNG, physics, shooting geometry and race distances stay unchanged;
new decisions intentionally may change race order and times.

Regression fixtures under Assets/Tests/Fixtures/M16 contain actual developed
career snapshots and seeds, with authoritative earlier results. They are not
fabricated placements. The first snapshot comes from original full-run v3;
the second was captured before the winter Mass Start during the margin
qualification sequence. The unchanged original full-season test remains the
end-to-end acceptance gate. Per-curve tests check earlier braking, unchanged
departure/straight behavior, authored permission across the horizon, legacy
defaults, actual physical moving passes, command replay and all three replay
policy versions. Final receipts are in docs/quality/m16-6/traffic-contact.
