# M14 WP-186 — career dashboard

2026-10-12. WP-186 complete. WP-187 training UI next; full M14 remains open.

The career page now shows the actual 16-event production calendar, next event
and weeks until it, current season/week, saved player readiness, season points,
standing, wins and podiums. Calendar entries distinguish completed, current,
upcoming, past and ineligible events; player results/points appear only when
the saved race contains that player. Initial zero-race careers have no fabricated
ranking. Restored legacy careers keep their original calendar.

`CareerDashboardSummary` reads stable-ID saved records and Career standings.
Rendering and preference changes do not advance a week, consume RNG, change
athlete state or write a profile. Existing prepare/recovery/settings/back flow
continues with the same save boundary. Presentation and tests only; gameplay,
career formulas and graphics quality remain unchanged.

Validation:

- .NET 19 passed, 0 failed: dashboard/results models and front-end save/session.
- Native EditMode dashboard: 3 passed, 0 failed.
- Native PlayMode: result → updated dashboard → next event and
  new profile/continue/settings navigation, 2 passed.
- RU 1600×900 and EN 1024×768/130% captures inspected.
- Final Unity compile state is settled, no new Console errors after the repaired
  test import (CS0246), no exceptions.

Evidence: [delivery summary](../quality/m14/dashboard-20261012/delivery-summary.json),
[Russian dashboard](../quality/m14/dashboard-20261012/dashboard-ru.png),
[English 4:3 / 130% dashboard](../quality/m14/dashboard-20261012/dashboard-en.png).

WP-179/180/182/183/184/185/186 complete; WP-181/196/197 partly delivered;
WP-187–195 pending. [Full tracker](../M14_WP_STATUS.md).
Season history, training selection, equipment and replay remain separate WPs.
Production Pursuit composition remains a known separate issue, so the full
M14 gate remains open. No new player build.

GitHub publication of WP-185/preceding local history confirmed at
`b9672818e19e602f72e945b35003b049e342e1d4` on `codex/wp-078-ai-pack`.
WP-186 is committed and pushed separately on the same branch.
