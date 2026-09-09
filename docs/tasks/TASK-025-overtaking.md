# TASK-025 — WP-025 overtaking mechanics

Dependencies WP024 PASS. Fixed-tick overtake controller selects nearest forward target with stable
ties, checks left/right space, moves out, passes, returns only after longitudinal clearance.
Effort surcharge and propulsion coordination loss price lateral movement; no displacement into
occupied space. Bounded timeout/cancellation safely returns instead of indefinitely holding a lane.
Important start/completion/abort events use IRaceEventSink. Test phases, no-space/no-overtake zones,
blocked return, costs, event determinism, invalid-input atomicity and allocations via .NET/Unity.
