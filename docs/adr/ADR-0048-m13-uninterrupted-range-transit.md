# ADR-0048 — Uninterrupted automatic range transit and bounded presentation work

2026-10-10. Implemented for owner testing; M13 remains OPEN.

The owner explicitly requires automatic range approach/departure to continue
through congestion without speed loss, permitting forceful pushing. This overrides
the GDD's ordinary hard-contact rule only during these automatic manoeuvres.

The versioned manual AI policy selects soft contacts on RangeApproach/RangeExit
occurrences for AI and the assisted player alike. These actors keep ordinary ski
power, fatigue, steering, snow, course, resource and phase admission. Pair impact
and continuing-contact forces do not stop them. Presentation separates overlapping
models with a bounded, smoothed offset; this is a visual yield rather than an
energy-conserving impulse. Stationary shooters retain their reached position.
After assistance, ordinary contacts resume as soon as actual oriented bodies are
clear. Manual steering elsewhere keeps ordinary collision authority. The selected
policy participates in the existing AI replay context. Each complete athlete
command records the selected transit flag; the field validates its range scope
before applying it. Replay therefore reproduces the exception without running AI.
Optional JSON fields default to false in older tapes. Pack/manual tapes also retain
manual bolt intent. Snapshots expose yield state. This explicit assisted-transit
exception must not become an AI speed bonus.

The mat plant radius increases from 0.5m to 0.8m within its existing speed/yaw limits.
The reproduced endpoint failure occurred when a folded mat path observed its end
about 0.62m from the centre; the old radius could commit terminal progress while
leaving motion nonzero, causing MovingFiringArrival. Foot placement retains the
reached pose and paid travel and records the dissipated energy. No teleport or
lane reservation bypass is introduced.

Bend braking now uses the distance-dependent braking envelope for ordinary bends
as well as private mats. The previous comparison with the distant bend's final
safe speed suppressed acceleration too early. Private departures use the shortest
heading error with bounded yaw feedback, retaining the shared snow/grip solver.

A conservative circumscribed-body/translation bound rejects distant contact pairs
before detailed swept tests. Constant-velocity contact odometers use the exact
linear distance instead of 64 quadrature samples. Near crossing/rotating/arriving
pairs retain the original detailed checks and explicit contact semantics.

Penalty width becomes 6m from 4m, retaining 150m distance. Terrain clearance and its
fence follow the new width. Additional main/penalty snow-bank windows use the
existing clipped skirt builder and safeguards. Cold authored mounds restore snow
at selected fences; 36 trees in front of the stands are removed. High retains its
name/default selection, with native resolution and 2048px/two-cascade shadows to
reduce excess rendering cost. The fixed 20Hz race clock is unchanged.

The owner build remains a review candidate. Local timing evidence is bounded and
does not establish hitch-free performance on every machine or an entire race.
