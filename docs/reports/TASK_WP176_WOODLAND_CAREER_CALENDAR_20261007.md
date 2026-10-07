# WP-176 — Woodland career calendar and production race boundary

Date: 2026-10-07. Status: WP-176 production calendar content PASS.
The later career UI belongs to M14; the long native class has no completion
receipt and is not counted as a native PASS.

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

The above was the initial implementation checkpoint. The subsequent closure
evidence below supersedes its open full-season and selection items. Product
career UI remains scheduled for M14.

## Closure evidence, 2026-10-07

The named `CareerDefinitions.ForNewProductionCareer` entry point now selects
the 16-event Woodland calendar for new careers. `ForSave` selects the exact
legacy or Woodland definition from the persisted `calendar_id` and rejects
unknown IDs or mismatched resource IDs. The historical constructor retains
legacy behavior. A new EditMode test checks both save versions and the
rejection case. Final Release .NET selection/season-accounting regression
passed 3/3; three separately targeted Unity MCP EditMode methods passed 3/3,
including the post-change selector. Unity compiled the final C# revision.

The complete [production season TRX](../quality/m13/wp176-woodland-career-20261007/wp176-full-production-season.trx)
passed 1/1 in an isolated Release .NET process: 52 saved/restored weeks,
all 16 actual Woodland races, six real Sprint→saved-result Pursuit pairs and
four Mass Starts, followed by the archived 16-race season. Every race result
had 30 entrants, a matching SHA-256 authority payload and a positive
completion tick. Race durations by completion tick were 19,473–30,588;
the fixed-seed event hashes and per-event ticks are in the TRX. The run took
13m45s. A first unbounded attempt was stopped without a result when the
testhost working set rose in the already memory-heavy Editor session. The
completed test explicitly collected obsolete test-runner objects *between*
saved races. Its retained managed heap after each event was 7.1–7.7 MB;
this is test-process hygiene, not a gameplay rule or a player benchmark.

The [final selector TRX](../quality/m13/wp176-woodland-career-20261007/wp176-final-selection.trx)
is 3/3 PASS. The Unity HTTP timeout on the earlier entire fixture remains
an incomplete run, not hidden evidence. Product UI must call the named
production entry point in M14. No change was made to race physics, shooting
geometry, AI speed, conditions, or old save contents. WP-176 is complete;
WP-175, WP-177, WP-178, owner gameplay corrections and M13 remain OPEN.
