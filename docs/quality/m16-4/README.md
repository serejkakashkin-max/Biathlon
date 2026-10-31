# M16.4 — shooting equipment and accepted impact evidence

2026-10-31. IMPLEMENTED / OWNER REVIEW. Owner closes M16.3; M16.4 awaits testing.

## Delivered behavior

Poles are laid along the prone athlete's right side or between standing skis.
They settle onto the same local snow/mat plane and return continuously to the
hands during the existing rifle-transfer progress. The late contact pass no
longer makes stowed shafts vertical. Skis keep their original surface-aligned
footing. Actual production mats remain flush with packed snow. Rifle aim, root
placement, setup/exit durations and the authoritative shooting clock are unchanged.

Every published accepted shot contributes its exact wind-adjusted impact and
outcome to a bounded presentation ledger. This observes catch-up ticks before
rendering and retains both fifth shots, rather than guessing from the current
sight or the closure mask. Duplicate/stale/rejected shots cannot invent marks.
Feed reset explicitly clears race-local evidence.

One preallocated mesh per bank retains at most32 front-plate marks. A shared
material clips the real aperture interiors and closed white flaps. A centre hit
goes through the aperture and does not drill the replacement flap; a rim hit can
scrape paint. Misses beyond the actual authored plate bounds do not hang damage
in empty space. Older marks are replaced when capacity is reached. Geometry
updates only when a real plate strike is recorded or the race is reset. There
are no per-shot GameObjects, material clones or physics queries.

The existing shooting card includes a compact last-shot diagram: nominal5.6mm
footprint, exact accepted centre, edge hit / edge miss and distance from the rim.
It shows accepted evidence only, not the live point of aim. Normal HUD coverage
stays below10%; large4:3 below17% (native samples about8.7%,8.3%,16.5%). The first
large4:3 layout measured17.4%; the final version was compacted without relaxing
the existing limit. `layout-edge-hit` / `layout-edge-miss` are explicitly synthetic
presentation layout fixtures; the `hud-*` captures use a real accepted shot.

## Scoring contract

ADR-0007 remains unchanged: the exact aperture boundary counts, one nanometre
outside misses. Player and AI share the resolver; no bullet-radius expansion or
hidden random roll is introduced. The visual near-edge band uses half the nominal
5.6mm bullet diameter to describe the accepted result. This is not a full model
of the mechanical target's release impulse/tolerances. IBU ECR8.3.2.1, AnnexA
3.1.7.1/3.2.1.13 and US Biathlon's winter range procedure were consulted; direct
primary links and the decision are retained in ADR-0075.

## Verification

| Receipt | Result | Scope |
| --- | --- | --- |
| `equipment-red-native.json` | 4 red on old code | Both stances and slope cases reproduce upright poles |
| `final-editmode.json` | 40 PASS | Impact ledger, exact edge controls, zero warm model allocations, resolver, shooting state/session and accepted-shot transition |
| `final-playmode.json` | 33 PASS | Equipment, reversible lay/pickup, both stance deadlines, rifle/sight alignment, exact hit/miss traces including fifths and no-render catch-up, bounded bank marks, production environment lifecycle, range/HUD compatibility, native views |
| `final-visual.json` | 2 PASS, included above | Latest Hero/flush-mat and front-plate views, accepted HUD plus edge-label layout controls |
| `shader-final.json` | Supported /0errors | Native D3D11 shader after actual render |

The first offscreen fixture omitted the active render-pipeline setup and saved
black textures; it was corrected and a non-black-pixel assertion added. Its
initial raised mat cube also occluded skis; it now matches the real flush-top mat
and uses the shipped Hero rig. Native D3D11 compilation found the reserved `point`
identifier in the new shader; it was renamed and actual rendering rerun. Retained
red receipts are diagnostics, not final pass evidence. No failing test or contact,
scoring, timing or HUD area tolerance was removed to get a pass.

`views/` contains inspected High Hero prone/standing/top views, actual plate
scuffs with open/closed apertures, and RU/EN compact HUD captures. Isolated world
fixtures and UI-only captures are labeled and are not full-race performance tests.

## Windows build

[Launch M16.4 fullscreen](../../../../Builds/M16_4_Shooting_Contact_Review/START_TEST.cmd).
New folder; previous M16.2/owner build is preserved. Unity6000.6.0f1 build
Succeeded,0errors,389004027bytes,31.98s. The shared helper's legacy receipt ID
remains `m15_5_visual_review`; `milestone.json` identifies M16.4. All520 warnings
are categorized:24 unchanged CS0618 call sites,460 UnityAIInference `ConvGeneric`
compute variants,36 Sentis pixel-shader variants. New impact shader has no
warnings/errors. Source/binary SHA256 values are in `fingerprints.json`.

Compiled ordinary-player startup PASS/exit0,2560x1440 FullScreenWindow matching
the desktop, zero captured runtime errors. Creator/appearance, Continue/recovery,
bindings/settings/checkpoint/export pass, owner profiles unchanged. Sprint drew
bib6, skipped the wait, displayed3–2–1, scheduled3001/accepted3005. Optional cursor/
TAB history review was not rerun; default navigation fields are not new claims.
`player/` retains the receipt, countdown and actual world/HUD startup screenshot.

Additional compiled production-range probe PASS/exit0 at tick2835: actual career
MassStart with30athletes, all5player hits and118other accepted shots. The inspected
world/UI image shows the last-shot diagram after shot5 and laid poles on the real
range. This reuses the existing opt-in live-shooting/audio probe with its in-memory
career store and ordinary AI commands, not edited impacts, poses or results.
`player-shooting/` retains receipt, log audit and world/HUD image. Its legacy M16.2
audio receipt ID is intentional; default live-board/frame fields are unexecuted
in that probe mode and are not new board/navigation verification claims. There
are no new gameplay review cameras or diagnostic loops in normal play.

Actual Console before build:0errors,2 existing Editor analyzer warnings; build
warning audit retained. Final actual Console0errors/0warnings, compilation and
Play Mode idle. This qualification does not claim a new full-career soak, full
race hash run, minimum-spec FPS or complete target-impulse physics model.

## Scope and reproducibility

All Domain/Simulation/AI/Career/Replay CS files remain byte-exact. Of669 baseline
protected core/resource files, only the two authorized presentation recipes
changed (`athlete_rifle_v1` schema4, `athlete_ground_contact_v1` schema2). A new
impact visual recipe supplies the bounded budget and nominal bullet appearance.
No race tuning, event/hash contract, balance, target size or save schema changed.
The complete published PROJECT_STATE prefix at8eb90857 is preserved, with dated
append-only approval/progress/delivery records. Preexisting owner/Unity changes
are preserved and excluded from this feature branch.

Run `python scripts/quality/report_m164.py` from the project to verify retained
test, shader, build, source, authority and history evidence. Owner art acceptance
remains the next task. M16.3 closed per owner; M16.4 stays open for this test.
