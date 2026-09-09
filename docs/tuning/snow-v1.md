# Provisional race snow conditions

SnowCatalog supplies stable profile IDs and glide/grip/snow-drag coefficients; RaceSnowConditions
binds track snow_profile_id surface IDs to actual race profiles. This permits race-time selection
without mutating course geometry or duplicating coefficients. Constructor validates every track
surface, including penalty loops, before use. No unknown-ID neutral fallback is allowed.

Resistance multiplier = snow drag / glide; applied once to WP017 snow resistance, independently
of aerodynamic drag. Grip scales WP020 lateral capability, lowering safe turning speed on ice.
Conditions are immutable for a v0.1 race. A later explicit tick condition-change event can replace
the snapshot; this task does not introduce implicit changing weather or RNG.
Fixture values are provisional; icy_fast gains glide but loses grip, fresh_soft/wet_slow add
resistance. All race participants share the same condition snapshot and formulas.
