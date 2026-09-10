# TASK-084 — Pursuit regression suite

Status: DONE — 2026-09-10. Next WP085 Mass Start grid/launch.

Added a cooperative Unity/standalone fixed-seed matrix that repeats each complete shared AI
Pursuit twice. Scenarios: three tied starters/expert123; six starters with2s gaps/developing456;
six starters with30s gaps/expert789. Reused the source result contract to build exact start gaps.
No gameplay code or tuning changed. The test factory now accepts seed/difficulty explicitly.

Every tick checks repeated athlete frames and physical body separation. Per-actor skiing stalls
retain the existing120-second bound, excluding shooting/staging. Starts match source gaps,
four bouts/twenty shots complete per athlete, all penalty-loop events match real travelled
5000m +150m per miss, and exact global arrival ordering survives result reload. Events and result
JSON repeat completely. Dense scenario must include an actual completed overtake. Existing
Pursuit start/tie/overflow and close-finish/display-tie tests remain part of the acceptance suite.

| Athletes | Gap s | Seed / AI | Ticks | Max skiing stall ticks | Penalties | Completed overtakes |
|---|---|---|---|---|---|---|
|3|0|123 expert|14449|0|3|0|
|6|2|456 developing|18986|9|45|3|
|6|30|789 expert|16624|8|3|0|

.NET Pursuit suite17/17 in6s. Unity MCP matrix1/1 (three cases, each repeated,21.7s), close
finishes2/2, starts3/3:6/6. Console0 errors/4 existing MCP warnings. Current discovery467;
prior full463/463 and WP083 focused32/32 remain baselines, not a new full467 suite run.

All three .NET/Unity result hashes match exactly:
- tied:02ea6e3a27fe9b0118892a9e0b6dc337fa15b0058b791f24e99e596ce9366c8f
- dense:dba9d5f4c691d624c7dd3d2d28e8ba86b9f38b1a51ce0a420ad85b88e9e66236
- separated:c4ffab38731bb2f209b865b769af4379fb8ca79150c94ecea1d6a563480c1af0

Evidence: artifacts/physiology/wp084-final.trx, wp084-unity-final.json, wp084-unity-results.xml,
wp084-unity-close-finishes.json, wp084-unity-starts.json, wp084-console.json; hashes and metrics
under docs/quality/wp084. Limit: synthetic six-athlete Pursuit coverage, not30-athlete Mass Start
launch/traffic or final venue balance. No new PlayMode/visual claim. Complete-finisher snapshot
limitation remains documented in ADR0015. M7 requires the remaining Mass Start/championship WPs.
