# TASK-009 — WP-009 athlete stats to physical parameters

Dependency WP-008 PASS. Physiology stats Endurance/Anaerobic Capacity/Recovery/Strength [0,100]
map through required versioned JSON piecewise-linear modifier curves to the immutable base definition.
Use base values once; curves are dimensionless modifiers, not duplicated physical defaults.
Validate complete endpoints, strictly increasing stat knots, finite positive factors and monotonic direction.
Tests: endpoints/interpolation/full stat sweep, invalid JSON/stats, round-trip, template immutability,
Recovery40/80 and Anaerobic40/80 integrated comparison. Run .NET/Unity and Console.
No career progression or final balance changes. Rollback new contracts/curves/test/documents only.
