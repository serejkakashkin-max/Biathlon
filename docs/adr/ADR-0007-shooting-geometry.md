# ADR-0007 — Shooting coordinates and aperture boundary

Accepted WP038. Lane-local coordinates use metres: X right, Y up, Z toward the target plane.
All five aperture centres share Y=0 and Z=distance_m; presentation transforms this local frame.
Ray direction need not be normalized. Intersection must lie strictly ahead of the ray origin.
Parallel rays, behind-origin planes and unrepresentable far intersections miss. Zero rays fail.

Impact X/Y, centres and radii round to integer nanometres, ties-to-even. The circle boundary is
inclusive using exact decimal squared integer distances after a bounding-box check. No ad hoc
floating epsilon, bullet-radius expansion or hidden outcome roll. This is a geometric gameplay
encoding, not a claim of nanometre physical accuracy or ballistic certification. WP046 will
compose trajectory/wind and accepted shots with this common player/AI geometry.

The public IBU rules overview confirms five targets at 50 m and 45 mm prone / 115 mm standing
diameters: https://www.biathlonworld.com/discover-biathlon/how-it-works/rules-overview (2026-09-07).
The 0.2 m centre spacing is provisional authored configuration; certified target mechanism
layout and impact momentum are not modeled here. Each target has an independent closure bit;
repeated hits cannot create duplicate closures. Athlete stats never change these apertures.
