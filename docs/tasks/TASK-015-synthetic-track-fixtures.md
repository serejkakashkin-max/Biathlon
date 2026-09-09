# TASK-015 — WP-015 synthetic track generator

Dependency WP-014 PASS. Add deterministic .NET authoring command that transforms a validated track
template using versioned recipe parameters to generate flat/climb/rolling/technical descent/range-approach
fixtures. Keep a shared flat range tail, relocate range/penalty/checkpoints by exact distances and validate
all outputs with Domain before writing. No production venue/geometric closure claim.
Tests: generated profile identities/grades/curvature/metadata/anchors in shared .NET/Unity tests;
repeat CLI generation byte-for-byte; invalid recipe fails before output mutation. Check Console.
Generator is development-only .NET tooling using built-in JSON nodes; Unity consumes validated output.
