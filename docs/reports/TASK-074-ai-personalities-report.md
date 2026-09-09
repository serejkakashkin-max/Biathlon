# TASK-074 — Persistent AI personalities

Added AiPersonalityDefinition with all eight GDD traits, three versioned provisional profiles
(neutral, attacker, patient), and immutable AiPersonalityPolicy binding a profile to an AthleteId.
Policy context contains exact trait JSON, actor and projection policy version. Traits survive
JSON round-trip; assignment is explicit and never rerolled on tactical ticks. Career-generated
roster assignment remains later career work.

Preferences feed existing utility contributions: patience favors Recover/Hold, draft preference
favors Follow/Draft, overtake confidence favors Overtake, aggression favors Attack, final-lap
aggression replaces Attack preference on the final lap and favors FinalSprint. Shooting risk
inversely influences PreRangeRecovery preference. Physical eligibility remains outside utility
and cannot be overridden by any profile.

Shooting strategy uses an equal-weight mean of observed time urgency, shooting risk and shooting
tempo. Pressure increases tactical stability need in proportion to one minus pressure resistance;
the greater of observed need and pressure caution is supplied to WP073. This is decision behavior,
not a modifier to the common body's sway or existing shooting-athlete composure physics. No
speed, power, accuracy, tracking error, target size or RNG outcome multiplier is introduced.
The normalized projections and version tag are explicit; authored trait magnitudes remain data.

Tests cover exact profile round trips, all trait range failures, neutral utility preferences,
different decisions at a marginal opportunity, eligibility overriding aggressive preference,
final-lap preferences, pressure response, actor/context identity, invalid observations and zero
warmed allocation. Two complete identical-capability races use different persistent profiles,
cadenced utility decisions and normal pace inputs; each race is reproduced frame/event/result
exactly through a second shared-physics field. Attack decisions: 1151 for attacker, 387 for patient;
race ticks: 9878 and 9978 respectively. These are fixture outcomes, not general balance targets.
Neutral means neutral utility preference; it does not disable the explicit shooting-policy blend.

Verified 426/426 standalone and Unity EditMode; 7/7 Unity PlayMode. Evidence artifacts/physiology/wp074-*.
No visual work, manual Inspector setup or remote changes. Next: WP075 Difficulty model.

