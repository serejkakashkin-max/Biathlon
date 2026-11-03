# ADR-0091 — Russia and stable scenery shadows

2026-11-03. Owner-directed M16.6 follow-up; M16.6 remains open for owner review.

## Russia

New production careers use `career_roster_v2`, appending RUS to the original six
countries. The v1 resource remains byte-for-byte historical input. Restoring a
save selects its roster by persisted RulesId, as calendars already do. A draft
created with the old roster retains that roster when published. Nation sampling
uses the same number of RNG draws: physical abilities, potential, money, ages
and final RNG state stay identical for equal seeds. Only new nationality metadata
and the roster identity change. No existing athlete's nationality is rewritten.

RUS receives a vector UI flag, a seventh fabric flag behind the actual backstop,
and one additional coach with a spotting scope in the existing safe enclosure.
The approved original athlete and coach meshes are retained. National panel
colors/thresholds are authored once in the appearance definition and consumed by
the athlete shader and the coach bake. Existing nations default to no additional
panels and keep their palettes. Only the new coach's derivatives are delivered;
existing staff geometry is preserved.

The visual reference is the [official SBR 2022/23 presentation](https://biathlonrus.com/news/obnarodovana-ofitsialnaya-forma-sbornoy-rossii-na-sezon-2022-23/):
blue base, white upper sleeves and red forearms, without copying sponsor logos.
This is an original simplified interpretation of that documented collection,
not a claim to reproduce the newest 2026 supplier's unrevealed collection.
The [SBR supplier announcement](https://biathlonrus.com/news/kompaniya-de-stala-novym-ekipirovshchikom-sbornoy-rossii-po-biatlonu/)
reports a change to DE from July2026; it does not supply a verified new racing
palette. No inference about international eligibility is encoded in game rules.

## Shadow reproduction

The existing frustum/fog policy keeps visible geometry complete, but shadow
casters used the same selected instances and shadows stopped at far LOD.
An offscreen original tree therefore lost100%of its reference shadow pixels on
visible snow. A visible far tree reported0shadow triangles. Both original
regressions failed before the fix. This is separate from the previous visible
foliage blending work in ADR-0079.

For production complete-visibility scenes, prepare a separate bounded caster set
from all placements before visible-frustum/LOD rejection. Its conservative
influence bounds extend along the actual directional light towards receivers,
including below the caster base. The existing quality definition supplies the
shadow distance. URP performs final light-space/distance culling. Stable source
meshes use original forestLOD1, crowdLOD1 and original dressing meshes. Their
wind/motion remain synchronized; camera-dependent foliage dithering is disabled
only for the independent shadow submission. Visible meshes receive shadows but
do not cast a duplicate second shadow. Per-camera buffers are reused, batches
remain at most500instances, and no per-frame managed allocations are added.

Legacy benchmark adapters without complete visibility retain their previous
submission contracts. Production caster/triangle counts are exposed for tests
and diagnostics. This deliberately adds shadow work for offscreen/far casters
that the old code omitted; it does not increase render resolution or shadow-map
distance. Native capture timings are not a claim about minimum-end hardware.

No simulation geometry, collisions, physics, shooting outcomes, race duration,
AI tuning, save schema or recorded-command policy changes. Evidence and known
qualification limits: `docs/quality/m16-6/russia-shadows/README.md`.
