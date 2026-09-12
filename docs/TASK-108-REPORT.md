# TASK-108 — Training sessions and weekly plans

WP-108 complete. Six authored session types cover endurance, intensity, technique, prone, standing and recovery. Every session declares all 13 adaptation targets, fatigue cost, recovery need/gain, health-risk foundation and time cost. These definitions do not apply injury outcomes.

An immutable weekly plan validates kinds and the calendar's available time before use. Race weeks have fewer slots; an empty plan permits rest. Non-recovery training cannot have zero fatigue cost. Invalid/duplicate session definitions fail explicitly.

Validation: .NET career8/8; Unity training2/2. Tests cover targeted adaptation, explicit costs, race-week limits, defensive ownership and malformed definitions. No race output or M8 tuning changed. Actual adaptation and weekly fatigue/form application follow in WP109–111; they are not claimed here.

Next: WP109 potential/age/quality/recovery/diminishing-return adaptation.
