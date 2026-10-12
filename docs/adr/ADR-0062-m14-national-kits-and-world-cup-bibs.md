# ADR-0062 — National kits and World Cup leader bibs

2026-10-12. Owner-requested presentation correction. M14 acceptance stays open.

The race should make an athlete's country recognizable from the suit. The
previous presentation cycled suit colors by bib ordinal, so teammates could
have different colors and renumbering could change an athlete's kit.

Bind the saved `CareerAthlete.Nation` to a versioned national palette. The six
current roster nations have distinct colors: NOR red, SWE yellow, FIN light,
GER graphite, FRA blue, ITA green. One shared material variant per palette
entry preserves the original fabric textures, normal maps and shader settings.
Unknown historical country codes receive a neutral kit, never a fabricated
country. Diagnostic athletes without nationality retain ordinal kit variants.
This palette is an owner-requested game convention; IBU does not prescribe
these exact country colors.

The independent bib material keeps its number and applies the existing
pre-start World Cup classifications: yellow overall, red current discipline,
split yellow/red when both roles apply. Points and placing countback come from
registered season results. Junior/local circuits and careers without results
cannot invent World Cup leaders. The assignment stays fixed throughout the
race; current on-course rank does not repaint bibs. No scoring or simulation
behavior changes in this correction.

Replay presentation now binds nationality, selected skis and leader roles from
the saved pre-race career snapshot. Seeking resets authority but retains those
bindings. Presentation never writes the save or race command stream.

Compact country swatches appear before the start and in season standings.
Leader icons use the exact same bib palette resource as athlete meshes.
Standings pagination accommodates the added legend. Palette/material setup
runs at composition time; unchanged kit/bib application allocates no memory.

## Official source

[IBU September 2026 Event and Competition Rules](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf),
§15.7.1–15.7.2 (leader bibs), §15.8.4–15.8.5 (scores/countback), checked via
[IBU downloads](https://www.biathlonworld.com/inside-ibu/downloads).

The current calendar supports Sprint, Pursuit and Mass Start in separate career
circuits. Blue U23 bibs, World Championship defending-champion bibs and missing
historical discipline classifications are not introduced by this scoped change.
No fabricated preseason defending champion is created.

Validation/build evidence: [review report](../reports/TASK_M14_NATION_BIBS_20261012.md).
