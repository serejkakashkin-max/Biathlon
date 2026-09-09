# Provisional technique response

Authority: ski_technique_provisional_v1.json. Ski Technique maps linearly from the configured
minimum at stat 0 to unity at 100. Positive grade ramps climbing affinity to full_climb_grade;
Climbing stat reduces the uphill technique loss only. Each profile linearly blends flat/climb
affinity. Automatic selection maximizes efficiency; ties use serialized profile order, independent
of power, RNG or athlete identity. Explicit profile ID is available for later technique assist.

Muscular fatigue reduces coordination linearly to exhausted_technique_modifier. This distinct
coordination loss follows M1's reduced physical output; do not apply form or power fatigue again.
Resource expenditure uses original delivered watts; multiply once by technique efficiency for
SkiForceSystem. Higher skill converts the same exertion to more motion, not free physiology power.
Profiles are provisional mathematical skating modes, not an animation taxonomy or validated
sport biomechanics. Downhill uses flat affinity until explicit tuck/technical constraints arrive.
Outputs expose every factor and stable selected ID for runner telemetry and replay integration.
