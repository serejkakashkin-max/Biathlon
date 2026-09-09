# TASK-030 — WP-030 player input command schema

Dependencies M2 PASS. Immutable versioned command with stable schema/athlete IDs and positive integer
target tick; analog effort, normalized line intent, held tuck, tactical preset, overtake/cancel pulses
and optional explicit technique ID. Strict JSON required fields/enums/ranges, no engine/frame time.
Schema expresses intent only; effort/steering/assist adapters follow WP031..033 and recording WP035.
Test serialization/rejection, exact tick/numeric encoding and allocation-free command construction.
