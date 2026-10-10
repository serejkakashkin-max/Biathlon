# ADR-0046 — Owner M13 follow-up: reached mats, reload handling, reserve and shorter course

Date: 2026-10-10. Status: implemented for owner review; M13 remains OPEN.

The owner requests reliable first-range arrival, uninterrupted assisted departure,
manual reload on every bout, harder controlled shooting, slower recovery, clear
penalty junctions, a roughly 20% shorter course and fewer performance hitches.

## Decision

Preserve the complete shooting command when AI movement rewrites ski effort.
Previously that rewrite discarded both the reload command and the manual-bolt
policy exactly when the arrival initialized a shooting session. Manual sessions
apply their handling impulse on accepted bolt cycling, rather than discharge.
The initial chamber remains empty and every later shot consumes its chamber;
shot five exits directly. Historical automatic-bolt callers retain their policy.
The manual session and movement contexts identify the changed behavior.

Plant the athlete only within the existing 0.5m position tolerance around the
assigned physical mat endpoint, with the existing bounded speed, yaw and energy
policy. A projection to the terminal station alone cannot authorize a shooting
position several metres beyond the mat. Missed mat/exit targets use bounded
bearing recovery. Assisted exits skip scripted braking for a moving athlete;
real physical contacts still exchange momentum and may change speed.

Production selects distinct versioned physiology/breathing/heartbeat/muscular
resources. Recovery power becomes 120W, anaerobic drain 2400W, fatigue gain
0.00045/s and recovery 0.00006/s. Existing endurance, capacity, recovery and
strength curves continue to modify those physical quantities for career entrants.
Breathing amplitude is 0.0012rad, heartbeat 0.00036rad, muscular sway 0.00022rad.
All effects remain deterministic; geometric 50m/115mm/45mm hit tests are unchanged.
Breath debt and high-heart-rate modifiers remain active. AI uses the same shared
physics and shooting model, but chooses tuck from the downhill decision policy;
the player's freely held tuck remains available on all terrain.

Compress the forest course in depth and height together, retaining the complete
stadium and 150m penalty circuit. Main lap becomes 960.005m from 1200m. Rebind all
route stations, format distances, shared regions, directed passages, snow-bank
windows, roadside objects, trees and terrain. Paid segment lengths round to the
nearest millimetre; unchanged affine world chords differ by at most half a
millimetre, within the existing authored geometry tolerance. No speed multiplier
or teleport shortens racing. Height compression prevents the shorter climb from
requiring an attack simply to keep moving.

After station rounding, branch endpoints are rebound to their exact main-road
samples (maximum correction 0.281mm). This preserves touching Sprint/Pursuit
portals rather than loosening their admission tolerances.

Measured overlapping corridor guards retain physical contact authority. Parallel
athletes wholly within the same straight start continuation do not conflict with
each other merely because the shared guard spans that road. The complete body
proof continues to check contact. Penalty fence feet sit outside the road and
boards overlapping another connecting road are omitted.

Reject an entirely off-camera grove before per-tree LOD/frustum processing, using
a conservative bound over the same native meshes and wind excursion. No visible
canopy or gameplay body is removed by that optimization. The shorter roadside
forest contains 334 fewer trees. Exact terrain overlays keep the ribbon above
ground without weakening runtime admission.

## Validation and limits

Evidence: `docs/quality/m13/owner-followup-20261010/`. Focused tests exercise all
30 first arrivals and the third actual manually controlled bout, sustained
recovery, career stat effects, sway/breath control, deterministic shot replay,
physical mat admission and native Unity composition/rendering. CPU timings are
diagnostics, not visible player FPS. Owner acceptance and any remaining visible
hitches are explicitly pending. No M14 work or automatic gate approval follows.
