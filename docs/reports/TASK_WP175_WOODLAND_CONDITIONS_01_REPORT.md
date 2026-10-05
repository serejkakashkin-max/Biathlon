# TASK WP175 Woodland conditions 01 — physical component verification

Date: 2026-10-05. Focused physical component verification PASS; WP175 native weather views, full condition races/replays and performance acceptance remain OPEN.

The fixture binds every actual Woodland surface to each of four existing canonical profiles: packed_neutral, fresh_soft, icy_fast and wet_slow. All36 loops and1,291 segments are sampled at interiors and exact boundaries:5,164 samples per profile. No numerical tuning coefficients are copied or changed. Missing surfaces and unknown profiles fail before stepping.

An actual main technical bend demonstrates the existing glide/grip trade-off through the shared SnowConditionSystem, SkiForceSystem and CorneringSystem. Ice has lower resistance and lower safe cornering speed; soft/wet snow have greater resistance. Four1,200-tick/20Hz force/cornering probes repeat exactly. Probe power/stats are explicit test inputs, not new production tuning. These probes finish no lap or race, contain no AI and do not qualify complete race/replay balance.

Release .NET10/10 PASS; native Unity EditMode10/10 PASS. [Portable component evidence](../quality/m13/wp175-condition-probes-20261005/component-proof.json) and [native results](../quality/m13/wp175-condition-probes-20261005/native-tests.json). The native discovery count1,057 is not the executed count. Full core acceptance still retains the separately recorded legacy allocation failure; these focused cases do not supersede it.

Observed profile distinction: main segments labelled descent_technical currently have zero grade; main edge_027/descent_fast has negative grade and zero curvature. The test therefore describes a technical bend, not a downhill turn. Actual downhill visual/physics review must sample real grade and geometry; semantic labels alone are insufficient. This observation is carried into WP175 readability and WP177 course/profile review without silently changing accepted race physics.

Next: fresh weather environment initialization and ordinary native views for all five visual rows; actual Sprint40-to-qualified-Pursuit40 and Mass30 condition flows with exact replay; appropriate available CPU/GC/render evidence. Hidden standalone rendering remains insufficient for sustained GPU/FPS qualification.
