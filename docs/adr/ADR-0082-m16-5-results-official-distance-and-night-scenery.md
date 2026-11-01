# ADR-0082 — M16.5 results density, nominal kilometres and lit scenery

2026-11-01. Owner explicitly APPROVES/CLOSES M16.4 and starts M16.5 with five
corrections: modern results/10rows, official discipline distance, evening crowd
and adverts lit, no TIMING wording, cumulative kilometre labels at every gate.

## Results

Keep approved Frostline identity: navy/ice/pale blue, Oswald display numerals and
Roboto identities. Compact the result hero/shooting summary and use a predictable
ten-row classification. Restrained blue row separators, podium gold, flag/name,
aligned time/gap/points and the player accent carry hierarchy. Pagination follows
the player's row at first open, then respects explicit navigation. No extra text,
new background generation or giant decorative cards. Layout lives in one versioned
UI recipe, with normal/large text and small-screen bounds checked. An original
native test reproduces5rows; ten-row/pagination/clipping checks replace that limit.

## Official distance is a presentation coordinate

The owner previously approved short physical race lengths. Do not lengthen them,
increase race duration, alter speed/power or change accepted route authority.
Pure `RaceDistanceView` maps that route to existing canonical discipline profiles
(`*_standard_men_v1`/`*_standard_women_v1`). Test-category fixtures retain their
authored distance. There are no copied distance constants in production code.
Existing category selection is retained; no new age/league eligibility is implied.

Map each authored lap separately to its official share, including first/final
start/finish branches. A first attempt scaling the whole route fails the new
lap-boundary tests (Mass Start3093m instead of3000m). Nominal Men Mass Start
boundaries must be3/6/9/12/15km; actual route sections and timestamps stay intact.
Penalty/shooting time does not add nominal race distance. Clamp prestart/finished
progress, validate finite values, keep warmed numeric conversion allocation-free.

Preparation, last-split notice/top timing and TAB history share the same view.
Timing gantries precompute every actual CheckpointLayout occurrence at binding.
Choose the occurrence from accepted progress, keeping a passed value until the
camera has cleared its gantry (authored20physical metres), then show the next
passage. Rewind chooses its original value again. No per-frame mesh generation,
ground queries or string formatting. Reuse the stadium frame with one numeric
label at its existing header position; start label stays at the separate launch.

CheckpointLayout deliberately requires the exact track snapshot. Re-reading its
serialized double values and serializing again tripped that guard in the first
integration run. Bind the original immutable venue track instead of weakening
the authority identity check. All three format tests retain that strict guard.

## Night scenery

The M16.4 static illumination field was applied to snow/athletes/coaches, but
crowd shaders and ordinary URP Lit venue props did not sample it. Extend both
crowd shaders. Cold-clone only supplied static scenery materials, retaining
shared originals, all textures/colours/keywords and the full existing URP Lit
property/pass implementation. The new derivative shader wraps only forward PBR
to add the same fixed diffuse field; shadow/depth/normal/texture paths are kept.
It retains the Unity Companion License notice/source attribution. Targets and
their dedicated lighting are independent. Static batching still follows cloning.
No new realtime lamps, render camera, shadow pass or physics collider. Daytime
skips the field sample. Materials are owned/released with the environment.

Native side-by-side lamps-off/on captures use the actual roadside crowd; measured
torso-region luminance must increase. Ad material texture/colour preservation is
checked against its source. Actual compiled evening views supplement these tests.

Reference: [IBU ECR September2026](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
1.4.1/1.4.2 for Men/Women discipline lengths. No calibrated-lux or complete
multiple-shadow transport claim; existing M16.4 static-light approximation remains.
