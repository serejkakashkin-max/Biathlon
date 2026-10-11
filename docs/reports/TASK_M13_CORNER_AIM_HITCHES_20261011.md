# M13 — corner contact cost, active sight pacing and snow edges

Owner feedback after FramePacing: setup/prone animation is smoother, but the
first bend and active aiming still hitch. Cover the sharp course edges with
snow mounds and move roadside banners toward the fence. Preserve High graphics.
M13 remains OPEN pending owner testing; M14 has not started.

## Implemented causes and fixes

- Owned actor contacts ran exact rectangle SAT even for distant pairs. A
  conservative containing-circle exclusion now skips guaranteed distant pairs;
  nearby pairs retain the same SAT, tolerance, ordering and force resolution.
- At an actual penalty-edge witness, the refined body cover made 64 exact disk
  queries. Seeding the same grid with its corner leaves lets the existing lower
  bounds skip irrelevant interiors: 6 queries, the exact same minimum
  `-0.050649348103644076`. Widths, body, clearances and cells are unchanged.
- Repeated contact subdivisions rebuilt the same local polygon and its
  quadratic simplicity check. Cache the immutable authored polygon by its
  first/last edge indices. Re-select the complete tube window, including phase
  incoming edges, every preview. Tests compare cached/fresh proofs across
  travel, range, penalty and return, and verify no warm allocations.
- Active sight camera and bolt rendering returned early between 20 Hz ticks.
  They now render every frame, using bounded one-tick prediction of procedural
  sway/handling only. Current accepted player aim is never delayed or predicted;
  accepted shots use their exact pre-ballistic ray. Simulation, shot resolution,
  breath, heart-rate and recoil rules are unchanged. UI strings/bank state remain
  cached per accepted tick. Real aiming tests failed before and pass after.

## Visual changes

The versioned snow policy covers the full 960.005 m main ribbon. Static 4 m
shoulders have varying 0.65 m crests at 19 m spacing, outside the ski surface.
Crests are rejected over adjoining paid surfaces/shared apron. Apron feather
width remains independent of bank width. Materials and renderer count stay the
same. Open/closed joins, actual first-bend crests and apron exclusion pass native
geometry checks. Course length and authoritative snow boundaries are unchanged.

Rebound 39 roadside sign groups using the complete imported footprint; all 36
instantiated stela/billboard footprints clear the paid road edge by at least
4.3 m. Grounding uses native terrain under the complete footprint. The descent
banner and two billboards also needed height/nearby fitting corrections.
Texture, mesh, shadow and High quality settings are not reduced; no extra forest
removal was justified by this capture. Native corner, descent and sight screenshots
are retained in the evidence directory.

## Measurements and validation

Headless fixed-seed Mass30 through first bouts and 30 seconds of player departure,
3,837 ticks: mean simulation cost **5.235 → 3.948 ms** (24.6% lower), worst
**195.005 → 60.592 ms** (68.9% lower). Terminal player state and all available
200-tick motion-interval/setup/bout/allocation checkpoints match exactly.
Allocated bytes are unchanged at 45,543,496. First-bend window ticks 401–800
mean **4.7085 → 3.9983 ms**; detailed per-tick maxima for that window are unavailable.
These are .NET CPU measurements, not player FPS or portable timing thresholds.

Native bounded aim capture measured about 2 ms CPU and 0.04–0.06 ms GPU; freezing
simulation did not remove the discrete sight update. Corner capture had roughly
532 draws, 4.8 million triangles and 1.5–2.1 ms GPU time. It used an editor-driven
all-AI probe with a different scheduler; it is not an exact replay of the owner's
first-bend run. EditorLoop/startup/tool overhead is not attributed to the game.
Deep profiling was not used for these results.

- .NET contacts/differential checks: 64 PASS, including 28,640 SAT comparisons.
- .NET ribbon/catalog captures: 35 PASS. Two historical geometry witnesses now
  use the exact old venue snapshot outside Assets; their numeric assertions
  remain intact after later course shortening/start relocation.
- .NET polygon cache: 2 PASS; CPU profile: 1 PASS.
- Signed command-tape suite: 12 PASS, including complete MassStart/Sprint replay;
  one existing Pursuit composition failure before simulation, detailed below.
- Native EditMode: contacts 13, ribbon 19, cache 2 PASS.
- Native PlayMode: real aim 3, terrain/contact 13, snow banks 3,
  complete roadside footprint placement 1 PASS.
- Python dressing checks: 14 PASS. Unity compilation settled; final Console
  error/exception queries are empty. Native visual views were inspected.

## Build and practical limits

Windows owner build:
`Builds/WoodlandM13CornerAim-20261011/START_WOODLAND_REVIEW.cmd`.
Succeeded, zero errors/warnings, 355,755,516 bytes, High default, maximum 30.
Hidden standalone Mass30/Sprint30 startup, paid 400-tick controls and real scene
transition passed with exit 0. Hidden-window rendering was suspended; this smoke
provides no FPS/graphics evidence. Controls and suggested owner checks are in
the build's `READ_ME_RU.txt`.

Remaining CPU peak is about 61 ms. At the sampled slow period there are six
physical subdivisions rather than the usual one; attributing their remaining
cost needs separate stage timing. Do not claim every full-race hitch is gone.
Prediction may make a small correction when the next observation arrives;
raw mouse input still enters the fixed 20 Hz simulation.

Two unrelated existing failures are preserved, not disabled or weakened:
Pursuit factory passes a non-null startContinuation into a directed-passage
constructor that rejects that composition; the owner build exposes MassStart
and Sprint. The broad native dressing fixture reaches unchanged grandstand_01
and fails its base-embedding assertion (0.767 m vs 0.025 m). Its position and
terrain samples match HEAD; moved signs have a separate passing native test.
These are recorded follow-up defects, not hidden green results. M13 is not closed.

Evidence: `docs/quality/m13/corner-aim-hitches-20261011/`.
Next: owner first-bend/active-shooting test in the standalone build; address any
remaining measured contact-subdivision peak and the separate existing defects
before claiming full milestone completion.
