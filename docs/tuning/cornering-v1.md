# Provisional cornering contract

Safe speed = sqrt(lateral capability / absolute curvature), bounded by the numerical ski envelope.
Lateral capability interpolates Cornering stat then multiplies line quality, fatigue, segment risk
and downhill-specific skill factors. Straight segments impose no artificial cornering loss.
Line quality is an explicit 0..1 simulation input; the later line/steering system supplies it.

Entering faster than the safe speed causes deterministic braking proportional to excess speed,
capped by maximum_braking_mps2. SkiForceSystem integrates that resistance with every other force;
there is no instantaneous speed clamp, chance-based slip or stat override of player input.
This reactive controller may retain a small steady excess under downhill gravity; overspeed remains
observable. Future tactical lookahead can brake before the curve. Sustained propulsion while braking
still costs full physiological effort. Posture eligibility remains independently constrained.

All coefficients are provisional in cornering_provisional_v1.json. The runner owns overspeed-entry
events; evaluator reports lateral capability, safe/excess speed and actual braking force each tick.
