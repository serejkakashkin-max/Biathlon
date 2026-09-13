# TASK-143 — Cornering presentation

2026-09-13. WP143 PASS. Next WP144 rifle carry; M11 remains in progress.

The humanoid leans into signed turns with a speed-dependent, capped visual angle and exponential response. Hips/torso lean while analytic legs preserve ski alignment and limb lengths; the placement/camera pivot remains unchanged. Versioned presentation settings define angle cap, response, gravity reference and route sampling span. These are visual values, not authoritative cornering tuning.

The greybox visual route differs from physical curvature metadata. `VisualRouteCurvature` measures the rendered route's signed tangent change over a bounded distance window; it wraps geometrically closed seams and clamps open endpoints. `AthleteAnimationFrame` carries explicit presentation curvature separately from untouched simulation observations. Direct consumers without visual geometry retain the simulation curvature fallback. No race/input/core files changed.

Unity full PlayMode **33/33 PASS**, including three corner tests, locomotion/rig/input tests and complete Sprint/Pursuit/Mass Start. Tests check sign, speed response, saturation, stop, reversal continuity, transformed roots, ski alignment, the actual greybox left turn and closed seam. Native Console **0 errors/0 warnings**, compilation_failed=false. Evidence/XML in `docs/quality/m11/wp143/`.

Inspected fixed left/straight/right comparison (12 m/s, grade -0.06, effort 0.7, curvature -0.06/0/+0.06, t=0) and chase ticks565/575/590 around the visible 200 m corner. Fixed camera: (0,2.8,7) looking at (0,0.8,0), orthographic size2, three rigs spaced2 m. Body lean is readable; pole/terrain contact remains WP148.

Limits: greybox corners remain piecewise-linear direction changes and venue ribbons/shadows remain crude. This package smooths body banking; it does not claim smooth production route geometry. No updated Windows build or final humanoid quality claim. OWNER-FEEL-001 remains OPEN; shooting geometry/numbers unchanged.
