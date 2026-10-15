# M16 — Reliability test delivery

2026-10-15. WP206–215 implementation COMPLETE; M16 OPEN / OWNER REVIEW.
M15.6 remains owner approved. M17 has not started. Owner testing decides closure.

Test build: `Builds/M16_Reliability_Review/START_TEST.cmd`.
Source commit `dadbc6df2261d88861ce2ca632e0d34becdb15f8`, branch
`codex/m16-reliability`. Unity6000.6.0f1 Windows64 Development build succeeded,
0errors/0warnings,384736623bytes. Approved animation, graphics quality, manual
shooting and effort controls are retained.

## Delivered systems

- Career schema1/version3 with explicit v1/v2/v3 migrations and pinned fixtures.
  Verified atomic writes, compare-and-swap, same-career backup/checkpoint recovery,
  future-version preservation, quarantine on explicit repair and exact pre-race
  checkpoints. Repeating the same prepared start reuses identical verified bytes.
- Separate versioned preferences with legacy migration and atomic backup/recovery.
  Actual keyboard/mouse and gamepad rebinding, conflict/cancel/reset, sensitivity
  and inversion. Failed writes preserve both live settings and visible controls.
- Focused FROSTLINE settings pages, readable recovery errors, compact/130% text
  layouts, extensible translation catalogs and295 migrated approved label pairs.
  RU/EN remain the supported product locales; other locales can be added to data.
- Explicit local anonymous diagnostic ZIP: finite issue codes, public configuration
  and save structure. No raw logs, complete saves, names, IDs, paths or secrets;
  nothing is uploaded automatically.
- Two observed AI reliability fixes: paid effort/recovery after an uphill stall,
  and achieved-yaw tracking on penalty loops. Shared physics, athlete abilities,
  shooting geometry, body/course guards and resource costs are unchanged.

## Verification

124 unique selected native Unity cases PASS,0skipped, across latest applicable
per-case checkpoints (81EditMode/43PlayMode). This is not a single full historical
assembly run. Counts are actual executed cases, not MCP inventory totals.
Coverage includes33 persistence,15 migration,9 pure settings,7 actual settings
input/UI,4 recovery/support UI,10 existing menu interaction,8 skiing input,
4 shooting input,10 audio,5 localization,3 diagnostics,5 uphill restart,
1 repeated full MassStart witness,5 penalty policy/race/guard cases and5 strict
historical/current replay cases. Native failed-publication RED→GREEN and initial
serialization/layout/runtime-oracle failures remain retained as diagnosis evidence.

Focused CoreCLR persistence68/68 PASS; settings/migration53/53 PASS; final penalty
5/5 PASS. These overlap native contracts and are not added to the124-case count.

A fresh complete three-season production simulation passed156 weekly/save/reload
boundaries and48 newly executed30-athlete races:18Sprints,18Pursuits,12MassStarts,
1199866ticks and23400geometric shots. It includes4 rejected writes/retries,
3 abandon/retry sequences, exactly-once results, annual history/world checks and
final corrupt-primary recovery/repair. An independent validator checks all48
payload hashes, complete fields/bouts/penalties and the156-week save chain.
This automated core study uses AI control for all entrants; it is not48 rendered
native races or three human-played seasons. Full evidence: `docs/quality/m16/soak/`.

Final compiled Windows-player acceptance PASS at2560×1440 borderless fullscreen:
real creator → save → Continue → backup recovery/repair → actual settings file
reload → applied host binding → anonymous report → exact pre-race checkpoint →
personal countdown → production tick6 → abandon. Owner profiles unchanged;
no player Error/Exception/Assert. Native PNGs were inspected, including compact
settings, recovery notice and fullscreen support/control pages. The explicit
review render target is a QA surface; its scene capture does not claim a full
ordinary HUD screenshot. Existing menu/HUD/input tests provide separate checks.

Final Console inspection:0Error/Exception. Build compilation:0errors.
`scripts/Verify-M16-Evidence.py` verifies selected native case counts, the retained
production input manifest and the append-only PROJECT_STATE historical prefix.

## Determinism, impact and limitations

Race results can change where the two new AI decisions prevent a stall. There
are no hidden speed/power/stat bonuses. Effective commands still use the replay
journal; repeated race authority is exact within each runtime. CoreCLR and Mono
historical trajectories differ: separate strict observed runtime oracles are
retained, with the same all-runtime invalid-source/no-clock guard. No test is
excluded to obtain a passing result.

Eight pre-existing standalone AI-test failures were independently reproduced at
bbd01e62 with identical names/symptoms; see penalty-yaw baseline evidence. The
selected M16 cases pass, but a green complete historical suite is not claimed.
Runtime logs also contain existing URP warnings for stripped unsupported optional
post-processing shaders. They are warnings, not errors; this milestone does not
reduce graphics quality to silence them or claim an FPS benchmark.

No mid-race resume is advertised: an interrupted race returns to the confirmed
pre-race career. Diagnostics intentionally exclude raw user data. Unrelated
owner workspace changes are preserved and are not included in M16 commits.

Next step: owner tests the build, especially Continue, rebind/save/restart and
complete Sprint/Pursuit/MassStart races. M17 starts after that review.
