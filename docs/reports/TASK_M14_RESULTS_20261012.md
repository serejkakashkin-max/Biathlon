# M14 WP-185 — race results

2026-10-12. WP-185 complete; WP-186 next/in progress. Full M14 remains open.

The results screen now shows the whole field with the player highlighted,
elapsed time, classification gap, per-bout misses and points. The player summary
adds actual target closures/stances, penalty loops, personal checkpoint/sector
times, points gained, total points and season ranking before/after. Results and
career continuation use the saved race boundary. RU/EN and 130% text at 4:3 were
visually inspected; narrow protocol rows were corrected after the first capture.

Changed: Career result observations/validation, AI completion collection,
Presentation results summary/session publication and Unity results view/text.
Optional details preserve old saves. No race law, scoring or graphics change.

Validation:

- .NET: 19 passed, 0 failed (results summary, session, standings, complete production race).
- Native EditMode: 5 results cases + 11 session cases passed.
- Native PlayMode: results/profile/splits/navigation capture passed.
- Production Sprint30: 31,136 ticks, repeatable result collection, persisted/restored
  shot masks and checkpoint observations. Authority SHA-256 remains
  `701cabf4c0e01295abd425d7f1f2afb00611c20f421b6372e8cb7ef2eb21ff15`,
  identical to the previous M14 completed-race evidence.
- Unity compiled; no product Console errors/exceptions. One MCP test-run-busy
  response was retried after completion and passed; it is not a game exception.

Evidence: [delivery summary](../quality/m14/results-20261012/delivery-summary.json),
[Russian screen](../quality/m14/results-20261012/results-ru.png),
[English 4:3 / 130% screen](../quality/m14/results-20261012/results-en.png).

Limits: absent legacy shot/split observations show no invented data. Detailed
results remain current-season records, with expanded history/replay in later WPs.
The separately recorded production Pursuit composition issue remains unresolved;
this report does not claim that full Sprint → Pursuit gameplay is accepted.
No new build. Next: WP-186 career calendar/current standing, then remaining UI/replay WPs.
