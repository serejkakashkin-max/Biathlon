# M15.6 initial foundation — 2026-10-15

Owner closes M15.5; M15.6 remains open. Source baseline9867b24b, branch
codex/m15-6-career-creator. ADR-0070 and the verbatim owner brief define scope.

## Implemented

Read-only Career OVR / Ski / Shooting / Physical / Technique / Mental ratings
use authored weights over the existing13 attributes. Suggested weights total100.
Integer half-even normalization maps0..100000 to1..99. No race multiplier, readiness
input, RNG draw or save mutation. Warm1000-rating projection allocates0 managed
bytes in the native test. Stats-vector and athlete projections agree.

Detached creation domain adds3 tiers,6 presets,6 age-compatible origins, absolute
allocation with weighted convex costs/refunds, OVR/stat/budget bounds and atomic
invalid edits. Tier/preset switching derives from the baseline; repeated refunds
cannot accumulate points. Confirmation goes through the existing CareerFactory
and FrontEndSession durable-write boundary. Added World Cup start reuses its real
calendar. Existing old factory call sites remain intact. New draft UI, appearance
and creation provenance persistence are still subsequent WP work; no completed
creator/build is claimed here.

## Tests and baseline correction

Native selected results: ratings10PASS; creation6PASS; session12PASS;
existing start profiles6PASS; existing foundation4PASS. These are selected counts,
not the1969-test inventory. Final evidence is copied under docs/quality/m15-6.

First rating test expected a denominator110 due to an arithmetic mistake; actual
data and implementation used100. Corrected independent expectation; retained first
red result. No test removed or disabled.

An additional existing CareerTuningLockTests run failed before any modification to
its locked resources: both historical lock files retained the pre-M13 tuck hash.
The only mismatch is tuck_provisional_v1.json. Source4514c774 (2026-10-10) added
world_hold_through_turns=true, subsequently retained in owner-approved M13–M15.5.
Its current normalized hash equals HEAD, ca179b2f8701876456d7ec333890da1d87f0511f6bd846df0085b96122277be4.
Synchronize precisely that one hash in both locks, retaining every other entry,
test assertion and the failed evidence. This does not retune race behavior.

## Sports context

The [September2026 IBU rules, §1.3](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
define U21 by birth-year table and permit younger athletes in higher classes.
The game's18–20 junior career remains an inclusive development abstraction:
an18-year-old can enter the higher junior class; this is not a full implementation
of separate U17/U19 events, qualifying points or national entry quotas. Age is
shown at December31 of the season start year. Adult tier restrictions are game
creation choices, not a claim that IBU forbids younger World Cup entrants.

## Remaining

Live3D/editor/race profile persistence, existing athlete catalog, save migration,
individual growth/persistent pool, UI history, multi-season calibration, deterministic
regressions and final native visual/build acceptance. Current balance values are
provisional until WP-I; no assertion of equal preset win rates is made.
