# G2-VR014 — fabric, reflections and distant forest

2026-09-26. Intermediate remediation checkpoint, **not G2 approval**. M13/WP167 remain blocked. Owner requested continued work through G2; hero/animation review continues after this checkpoint.

- Fixed a presentation defect: AthleteBib did not consume its authored normal map. It now uses tangent-space normals and mip-filtered fabric detail instead of an aliased sine weave.
- Suit/bib/cap now consume audited CC0 Poly Haven roughness maps; import is linear, mipmapped, repeat-wrapped. Import recipe and provenance hashes updated. No additional third-party branding.
- Benchmark adds a 256px HDR venue reflection probe at a fixed capture position, refreshed on weather/lighting selection changes only. This is a local environmental approximation, not screen-space reflection or per-athlete ray tracing. Camera-specific instanced vegetation/crowd is not included in probe captures.
- Distant forest increases from 512 to 3,584 placed far-LOD trees, with irregular strata and scale. Root height now interpolates actual Cartesian triangle planes rather than polar cell fractions. Ground pivot accounts for the source mesh bounds. Existing route and gameplay colliders are unchanged.

## Verification

Unity MCP full PlayMode: **161/161 PASS**, 142.330s, no skipped tests. [Raw result](../quality/g2/remediation-014/playmode.json). New tests exercise rendered normal-map response, imported texture channels, and exact height against every landscape triangle. Existing allocation, motion, camera, rewind and race-observation contracts remain passing.

The first material checks exposed stale generated material bindings and overexposure in the isolated test fixture. Assets were regenerated after compilation and fixture illumination corrected; neither failing test was removed. An intervening TestRunner run reported Unknown/0: not accepted as validation. The fresh Editor run above executed the whole assembly.

[Hero close-up](../quality/g2/remediation-014/hero-face.png) and [venue](../quality/g2/remediation-014/approach.png) inspected. The hero face and garment silhouette still need improvement. No claim of visual acceptance or GPU/frame-rate qualification. Windows package will be rebuilt after the next hero pass, so the existing player remains checkpoint013 at this checkpoint.

Authoritative simulation, player shooting geometry, input/replay format and race balance are unchanged. Full .NET regression from013 remains historical evidence, not rerun for this presentation-only change.

Next: improve the hero face using the already audited local character sources, then motion and final owner review build. G2 remains owner-only.
