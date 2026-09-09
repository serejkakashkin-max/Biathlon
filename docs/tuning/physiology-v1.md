# Provisional physiology v1

Authority: `Assets/Resources/Biathlon/physiology_provisional_v1.json` (schema 1).
No C# fallback defaults. Values are hypotheses for automated M1 scenarios, not owner-approved final balance.

Units: HR/drift bpm; power and anaerobic drain/recovery scales watts (joules/second);
anaerobic capacity joules; tau/exposure seconds; gain/recovery inverse seconds;
effort/load/exponents/modifiers/efficiency/terrain multiplier dimensionless.

Anaerobic scales divide by capacity to yield normalized resource change per second.
This keeps capacity meaningful: larger reserves last longer and refill more slowly for the same watts.
Fatigue efficiency interpolates from 1 fresh to configured minimum at fatigue 1.
Requested control [0,1000] will map to [0,maximum_relative_effort] in WP-003.
HR rise/recovery and exponents follow GDD initial hypotheses. Remaining values are explicitly provisional.

WP-003: sustainable watts = base watts × form × fatigue efficiency. Control maps relative to that
current capability. At form zero watts and relative effort are zero (no 0/0). Requested and delivered
power are separate telemetry. Above-threshold peak effort interpolates from threshold at empty reserve
to maximum relative effort at full reserve; this provisional coupling prevents resource-free sprinting.
WP-006 adds reserve dynamics; WP-003 only reads the resource. Physiological load must use delivered effort.

WP-005: high-load exposure grows in seconds above drift_threshold; at/below the threshold it decays
by RecoveryModifier seconds per second. Clamp exposure at drift_saturation_s. This explicit provisional
recovery rule prevents permanently retained drift; fatigue and exposure jointly scale drift strength.

WP-007: muscular fatigue gain follows GDD effort exponent/terrain/susceptibility. Recovery fraction
is max(0,(threshold-delivered effort)/threshold), multiplied by fatigue recovery rate, RecoveryModifier
and (1-fatigue×recovery penalty). Net gain/recovery saturates [0,1]; resource change is observable.

WP-009: physiology_stat_curves_v1.json is the authority for dimensionless stat curves. Endurance
scales sustainable watts; Anaerobic Capacity scales joules; Recovery scales recovery modifier;
Strength reduces fatigue susceptibility. Curves interpolate ordered stat knots, preserve base definitions,
and produce versioned derived identities. No player/AI or difficulty factors exist. HR rest/max remain
individual template parameters; stat gains do not arbitrarily raise maximum heart rate.

All fields are required. Schema rejects unknown versions, missing/null/nonfinite/out-of-domain values and
inconsistent HR/effort bounds. Additive unknown fields are ignored. External invalid resources are rejected;
only system updates explicitly saturate valid computed results. ADR-0002 defines precision and qualification.
