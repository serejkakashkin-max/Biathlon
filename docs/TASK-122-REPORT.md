# TASK-122 — Fifteen-season headless soak

WP122 PASS. The standalone CareerSoak executable ran15 seasons/780 weeks/90 authoritative races, 1,823,050 ticks. Every week save/load roundtripped; a restored season8 Pursuit was rerun and produced an identical save. All race shooting counts, standings order, calendar boundaries, money/stat bounds, population and ID invariants passed.

Population stayed30;21 retiring AI athletes were replaced, producing51 unique IDs without reuse. Maximum serialized save size167,395 bytes. Final SHA256: b3cc3cbc7f1d1b6c4eb75314128386e1dffde4a7e95a0db0c4caf1354080c694. Machine evidence is under docs/quality/m9/soak-*. The final save and runtime outputs remain under artifacts/balance/career-soak.

Run: dotnet run --project dotnet/Biathlon.CareerSoak -c Release -- <absolute-project-root>. Uses authored30-athlete roster, six-event calendar, fixed seed71000 and a reproducible shared purchasing/training policy. All outcomes use PackAiRaceRunner, including geometric shooting. Unity actual-season parity and PlayMode7/7 were verified in WP121; the full15-season soak is standalone, not claimed as a Unity run.

One seed and synthetic venue establish structural viability; broader balance sampling remains future calibration. Next: WP123 balance interpretation.
