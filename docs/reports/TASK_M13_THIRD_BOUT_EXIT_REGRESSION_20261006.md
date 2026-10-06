# M13 — stopped player at the third Mass Start range exit

The owner's screenshot showed athletes waiting behind the player after shooting.
The existing test covered the second bout. This checkpoint retains that Unity
smoke test and adds a long headless regression at the **third** bout on the
actual Woodland Mass Start. It holds the player at the shared exit for 240
ticks, requires nearby followers, records the first authoritative body block,
and requires real followers to pass without a long stationary queue. The
simulation, AI policy and production tuning are unchanged.

The final headless third-bout case passed 1/1: player stopped at exit tick
13,035 / station 115.362 m, two nearby followers passed, maximum follower
stall 0 ticks. An earlier combined .NET run passed both second and third bout
2/2. The corresponding Unity run also completed both 2/2, as recorded by its
TestResults.xml; the MCP request timed out at 300 s while Unity continued and
finished at 656 s. The final source separates the long third-bout headless
case from the Unity-native second-bout smoke case. On that final source Unity
compiled and the smoke case passed 1/1 (312 s), although its MCP request again
timed out before Unity wrote the successful XML. Editor idle; recent Error
Console empty.

An initial attempt with 40 Mass Start entrants failed at factory validation,
before any racing: this format's authored profile and range have capacity 30.
The project's general maximum of 40 still applies to other eligible formats.
The invalid fixture's TRX is retained as diagnostic evidence; it is not a
gameplay-queue failure and no production capacity was changed to bypass it.

Evidence: `docs/quality/m13/owner-exit-third-bout-20261006/`.
The observed scripted holds do not substitute for the owner's hands-on review
of the consolidated Windows build. M13/WP175–178 remain open; no intermediate
build was made.
