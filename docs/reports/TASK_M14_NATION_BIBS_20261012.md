# M14 — National colors and IBU leader bib review

2026-10-12. Owner requested yellow/red bibs, a consistent suit color per country,
and a new test build. M14 remains OPEN for owner testing and acceptance.

## Delivered

- WP-181/188: country-backed kits replace ordinal colors for career athletes.
  NOR red, SWE yellow, FIN light, GER graphite, FRA blue, ITA green. The pre-race
  screen and season table show matching country flags/color swatches.
- WP-189: verified IBU 2026 yellow overall, red current-discipline and combined
  yellow/red bib rules. Existing points/countback calculation retained; added
  a first-Mass-Start regression against wrongly inheriting Sprint red. Bib
  numbers survive all role changes; leader icons appear in season standings.
- WP-192: fixed replay omission of career appearance. Saved nationality,
  selected skis and pre-start bib roles now bind at playback creation and
  remain correct through forward/backward seeking.
- WP-196/197: native screenshots inspected; country legend, World Cup leader
  table, RU/EN pre-race and 130% text at 1024x768. Fixed clipped standings
  pagination after adding the legend; buttons now remain visible.

## Verification

Native EditMode: `CareerAppearanceTests` **6/6**, `FrontEndCareerPagesTests`
**7/7**. Native PlayMode: `CareerRaceAppearanceTests` **4/4**,
`G2AppearanceTests` **2/2**, `NationBibReviewTests` **4/4**,
`RaceReplayPlayModeTests` **1/1**. The final standings test was repeated after
the pagination fix and passed. **24 distinct focused checks passed**; no test
disabled or weakened. Tool TotalTests denotes registered suite size, not the
executed filtered count. During implementation, compilation errors were
repaired; final refresh/build and new Console error/exception checks are clean.

Coverage includes all six nations in a production 30-athlete career field,
same-nation material sharing, bib roles from different actual classifications,
unchanged save/RNG/tick after rendering, preserved numbers, nation changes and
zero allocations over 100 unchanged kit/bib applications. Replay is independent
of AI and remains unchanged in authority state during seek/render checks.
Visual fixtures use explicit synthetic standings to exercise both leaders;
they are not installed as career results or presets in the test player.

No authoritative physics, balance, AI, race definitions, save schema or command
journal format changed. Full-race balance regressions were not repeated for
this presentation correction. Original suit texture/normal/roughness sources,
LOD meshes and graphics quality settings remain intact. Seven bounded shared
kit variants include the neutral fallback; no per-athlete suit material clone
or suit property block is introduced.

## Build

`Builds/M14NationBibs20261012/START_M14_TEST.cmd`

Unity **6000.6.0f1**, Windows x64 Development / DX11. Build **Succeeded**,
**0 errors / 0 warnings**, **370466798 bytes**. Standalone startup exited
**0**; startup smoke succeeds at 1600x900. See build summary and smoke evidence
under `docs/quality/m14/nation-bibs-20261012/`.

## Sports scope and limitations

[Official September 2026 IBU rules](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
§15.7 explains yellow/red/combined; §15.8.5 defines points ties by placing
countback. These bibs belong to World Cup classifications, not a temporary
on-course lead. Junior/local careers do not receive them. New saves have no
invented defending leaders before recorded classification results exist.
Blue U23 and World Championship defending-champion bibs are outside this
yellow/red request. National kit colors are the game's consistent convention,
not an IBU mandate. Unsupported historical country codes receive neutral gray.

Next action: owner tests this build, including career/replay kit consistency,
and decides whether M14 needs another correction. Production state history is
preserved by append; Git publication receipt is recorded there after push.
