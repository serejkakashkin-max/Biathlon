# M16.4 WP-J вЂ” leader jerseys in TAB and simpler country identity

2026-10-31. Owner requests yellow/red leader jersey icons in the TAB split table
and removal of the suit-colour swatch beside country flags. Ready for owner test;
M16.4 remains open. Fullscreen candidate:
`Builds/M16_4_Leader_Hud_Review/START_TEST.cmd`.

The split row reads the exact pre-start `CareerLeaderBibs` already bound to its
athlete proxy, by stable athlete ID. A yellow jersey means overall leader; red
means current discipline leader; combined yellow/red means both. Current split
place never determines the badge. An unbound/ordinary/junior athlete has no badge.
Existing circuit eligibility, ties and pre-start classification stay unchanged.
This uses ADR-0062/0064's approved behavior, including development overall-only
and historical series rules; no new rules interpretation or scoring change.

Each retained row owns one compact vector jersey and updates its role rather
than allocating another element on refresh. Rebinding the row clears a previous
athlete's badge. History, locale changes and hidden/reopened tables continue to
use the same start classification. Palette comes from the same versioned resource
as the actual bib fabric. Jersey silhouette/white edge differentiates the badge
from a national flag. The national chip now consists of its flag and country code;
remove the colour swatch and its colour-explanation tooltip in both the pre-race
identity and standings legend. Actual national suit colours are unchanged.

## Verification

26native cases PASS:12PlayMode +14EditMode.

- New checkpoint-leader tests:5, covering no/yellow/red/combined roles, row reuse,
  invalid role rejection and the actual30-athlete World Cup career field. All
  scheduled Sprint starters pass an actual first split; the first split table is
  compared against the pre-start classification and actual rig bibs. Its overall
  leader is15th and discipline leader28th on this split, proving badges do not
  follow current placing. Saved career and accepted tick do not change while
  inspecting the UI. Native RU1600Г—900 and large EN1024Г—768 views inspected;
  icons and the table fit their rows/screen. Unit cases cover the combined icon
  binding; separate native views show the yellow and red jerseys.
- Existing nation/bib/country UI and checkpoint history/cursor:7. All six actual
  national suits and the existing four bib roles remain supported. Added explicit
  absence checks for `kit_swatch` on country legends and pre-race identity.
- Career appearance, circuit rules and Mass Start seeding/journal compatibility:
  14. Includes ranked and historical120tick source/replay journal checks.

No tests disabled/deleted. The new test's missing namespace import was corrected
before qualification. Native Console before build0errors; final Console audited
after build. Windows Unity6000.6.0f1 development build: Succeeded,0errors,
520existing warnings,426987631bytes,30.334s. The shared builder's historical
receipt ID remains unchanged; milestone.json identifies M16.4.

Native data: `bib_native.json`, `bib_regression.json`, `bib_edit.json`.
Views: `tab-ru.png`, `tab-en.png`, `country-en-large.png`, `pre-race-ru.png`.
Authority:356core source files unchanged and all five
Domain/Simulation/AI/Career/Replay DLLs byte-identical to the preceding Forest Hud
Review. No tuning, national palette, scoring, input, save schema or gameplay logic
changes. There is no new world pass, camera, generated texture or per-tick logic.

The fullscreen startup receipt and final Console state are archived alongside
this report. QA uses isolated profiles; no owner career files are changed.
Next: owner checks the new icons and country identity in the candidate.

Ordinary startup:2560×1440 FullScreenWindow, randomized Sprint ordinal/bib22,
wait skip and personal3–2–1–0, accepted tick12605 after scheduled12601. All4loader
stages painted with clock/countdown held. Isolated-profile checks PASS and owner
profiles unchanged; no captured runtime errors. Final actual Console0errors/
0warnings, idle Editor. Optional advanced navigation probe is not enabled/claimed.

Reproduce: `python scripts/quality/report_m164_leader_hud.py`.
