# TASK-017 — WP-017 ski physical force model

Dependencies M1 and WP-013..016 PASS. Pure fixed-tick force model: propulsion from delivered watts,
gravity via grade, baseline snow friction and signed relative-air drag. Data-driven mass/coefficients,
zero-speed regularization/force ceiling/numeric speed envelope. Observable forces, speed and position.
Use integer nanometre position accumulation to preserve sub-millimetre motion, millimetre track sampling.
Tests: force identities, flat terminal behavior, uphill/downhill, head/tail wind, coast/stall/no reverse,
stop-inside-tick distance, invalid inputs/overflow, deterministic integrated physiology and zero allocation.
No technique/tuck/dynamic snow/equipment/drafting yet; those remain WP-018+. Run .NET/Unity/Console.
