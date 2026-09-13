# TASK-148 — Ski and pole surface contact

2026-09-13. WP148 PASS; M11 remains in progress. Next WP149 fatigue/exhaustion presentation.

Added a read-only rendered-route grade to the animation frame and a final ground-contact layer. Foot IK aligns skis to a bounded local plane while preserving limb lengths, ski yaw and recovery lift. The original ski sockets now seat the ski/binding beneath the boot instead of leaving a 27 mm gap. Poles preserve their 1.36 m shaft length, touch the plane during push and lift smoothly during recovery; staged shooting poles also meet the surface. Versioned presentation settings own sampling, slope envelope and pole clearance. No authoritative movement, shooting, camera or balance parameters changed.

Unity full PlayMode **42/42 PASS**, including three new tests for authored geometric grade, flat/uphill/downhill equipment contact, recovery clearance, rigid pole length, unchanged root, and preserved shooting hands/rifle alignment. Existing race, input, camera, both-stance and 30-athlete integration tests pass. Console **0 errors/0 warnings**, compilation_failed=false after runtime captures. Evidence and XML: `docs/quality/m11/wp148/`.

Inspected fixed -12%/0/+12% slopes and actual two-athlete scripted Sprint chase at ticks0/400. `scripts/Capture-AthleteContact.ps1` recreates the contact fixture in the running animation validation scene. It resets the locomotion clock before capture so previous preview states cannot contaminate the selected pose. This fixture is separate from actual-race evidence.

Limits: local-plane approximation, grade bounded to35%; it cannot conform a long ski to a sharp crest or arbitrary obstacle. Reach clamping is retained (under25 mm residual in the sampled motion/slope envelope). Pole push contact is procedural plane contact, not a world-locked friction simulation. The original development skin remains simple; dense-field performance is WP150 and full transition evidence WP151. No new Windows executable or final visual approval is claimed. OWNER-FEEL-001 stays OPEN without shooting retuning.
