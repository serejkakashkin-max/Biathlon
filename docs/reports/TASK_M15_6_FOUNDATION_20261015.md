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

## Updated checkpoint — own-athlete wizard and v3 appearance

The earlier UI/persistence TODO is superseded for the implemented own-athlete
path: FROSTLINE now has identity/look/skills steps, a separate confirmation,
live G2 studio and read-only OVR/group instruments. A draft does not create a
profile, including on a failed durable write. Three leagues, all13 ability
allocations and6 age-filtered origins use the existing career boundary. Switching
an incompatible age uses the selected tier's authored default, retaining valid
ages and the other draft settings. Allocation never raises hidden Potential.

SaveVersion3 adds immutable versioned AppearanceProfile (stable face/skin/build
IDs or legacy identity recipe) and creation provenance/initial abilities. v1
confidence migration precedes v2→v3 migration. Old stats, money, inventory,
IDs, RNG checkpoint and recorded race payload/fingerprint remain exact. The
original foundation hash fixture is retained and checked against the original
v2 field projection; it is not replaced with a newly generated golden hash.
Further growth fields belong to the still-unreleased M15.6 schema and are pending.

Preview reuses one model/target/camera, provides full/front/side/back/face views,
drag/scroll and4 pose modes, releases owned resources and leaves RenderSettings
alone. Cosmetic application reuses the existing bounded shared materials/LODs.
The same saved profile is applied by the actual race proxy. Native UI acceptance
changed face, skin, build and league, confirmed, continued the profile and launched
a real production race; the actor retained the chosen appearance at tick0, without
bypassing the personal countdown.8 native images are included. RU normal and EN
130%/4:3 layouts are captured; existing keyboard/gamepad/focus tests remain.

Final selected verification:48 EditMode +36 PlayMode =84 unique native cases,
and44 CoreCLR Release cases PASS,0 skips/failures. This is not a full-suite run.
Includes current and legacy-v2 saved Mass Start replay/backward seek, the pinned Sprint traces and
the exact original12-row balance fixture. See quality/m15-6/verification.json.
The settled compile checkpoint is clean; fresh Error/Exception/Assert queries
after the recorded final checkpoint are empty. Earlier compilation/probe failures remain diagnostic
history and are not described as passing runs.

### Verification corrections, retained rather than hidden

Native image review corrected an overexposed first studio and its face framing.
The first parity fixture incorrectly compared pose-dependent joint corrective
weights on differently posed athletes; the corrected check compares every face/
body channel across all3 LODs, retaining joint regressions separately. The UI
probe initially submitted an unfocused button; it now uses actual focused navigation.
An old depthless UI capture painted white stencil rectangles; its target now has
depth24, and the original field contrast test confirms actual dark inputs/glyphs.

CoreCLR's first in-method assertion measurement reported24B. A no-inline measured
boundary keeps assertion boxing outside it, retaining an explicit allocation
positive control. More significantly, this Editor's Mono
GetAllocatedBytesForCurrentThread returned0 even for1000 retained arrays; the
independent native GC.Alloc probe recorded24800B/1000 events. A zero Mono counter
alone is therefore insufficient evidence. The native OVR allocation test is moved
to the engine-capable PlayMode assembly and uses GC.Alloc with a real positive
control; the engine-free CoreCLR guard remains in EditMode source. Both detect
positive allocations and report0 for the warmed rating projection. The new cached
appearance application guard also uses the native recorder. No engine references
were added to Career or its engine-free test assembly. An initial attempted native
recorder import there failed compilation and was corrected by this separation.
Older native zero-counter assertions are not retrospectively claimed validated.

The balance fixture failure was solely the newly serialized M13 tuck option in
its historical context. Its exact canonical hashes remain unchanged: the legacy
fixture explicitly reads its original false option. A new matched test compares
current=true against historical=false for all12 runs: contexts differ and every
measured sporting metric/event hash is identical. Production tuning is unchanged;
the current option remains observable and tested. Initial red TRX/JSON retained.

### Milestone limitations / next work

WP-A/B/C complete. WP-D and H remain in progress: hair/eyes, height/proportion and
accessory editing/fitting, ready-athlete mode/catalog, rival/athlete cards and OVR
history are not complete. WP-E/F/G/I still require authentic selection, roster-pack
rights contract, grouped potential/individual age response, persistent career pool,
multi-season simulations/calibration and final build/visual acceptance. No World
Cup qualification/licensed-athlete completeness, preset win balance, full native
suite, all12 requested final screens, Windows M15.6 build or milestone closure is
claimed at this checkpoint. No new third-party media/model imports or graphics
quality reductions were introduced. M16 has not started.
