# WP-176 — Woodland career calendar and production race boundary

Date: 2026-10-07. Status: implementation and headless integration verified;
native long-run validation and product career UI selection remain open.

The new `woodland_career_calendar_v1` defines a 52-week season with 16 events
on the single owner-selected production venue: six Sprint→Pursuit pairs and
four Mass Starts. Events are uniquely ordered, Pursuits reference an earlier
same-venue Sprint, and training/rest weeks remain between race blocks. The
legacy six-event `career_calendar_v1` and its save ID are unchanged. Career
definitions select the new calendar explicitly, so old saves cannot be
silently loaded under different events.

`CareerRaceProvider` now selects the authored Woodland format profile, route,
range/start geometry and neutral snow condition through
`ProductionVenueRaceFactory`. Entrant stats, equipment, form and mental
pressure are supplied for every career athlete through the same runner. The
saved authoritative Sprint result and SHA-256 supply Pursuit qualification;
Mass Start uses the authored production start. The old synthetic provider
remains available for its existing regression and save format.

Release .NET evidence: calendar/legacy/weekly regression 8/8 PASS
([TRX](../quality/m13/wp176-woodland-career-20261007/regression.trx));
production Sprint→saved qualified Pursuit 1/1 PASS
([TRX](../quality/m13/wp176-woodland-career-20261007/qualified-pursuit.trx));
production Mass Start 1/1 PASS
([TRX](../quality/m13/wp176-woodland-career-20261007/mass-start.trx));
post-API cleanup fast regression 3/3 PASS
([TRX](../quality/m13/wp176-woodland-career-20261007/final-fast.trx)).
The standalone career Sprint finished in 30,450 ticks, the qualified Pursuit
in 21,300 ticks and the Mass Start in 21,651 ticks, all with 30 entrants.
The 52-week accounting test saved and restored each week and archived exactly
16 results. It used an explicit result boundary fixture; it is not a claim
that all 16 production races have been run in one continuous season.

Unity 6 Editor MCP compiled the first code revision, reported no compilation
errors, and ran the new calendar identity EditMode test 1/1 PASS. The final
constructor cleanup was .NET-compiled but awaits Unity's next settled refresh.
The full native class run exceeded the MCP HTTP timeout and remained active
in the Editor; it has no completion receipt and is not counted as PASS.
Subsequent Console errors concern MCP's timed-out/concurrent test requests,
not a C# compile diagnostic. No build
or visual/performance result follows from these headless tests.

Next: obtain bounded native completion, run the full production season (or
equivalent chained authoritative event proof), and connect explicit production
calendar selection to the later career UI. WP-175, WP-177, WP-178 and the
manual-steering/traffic batch remain open, so M13 is not complete.
