# TASK-021 — WP-021 snow conditions

Dependencies WP020 PASS. Versioned snow profile catalog and race-condition surface bindings resolve
track snow IDs into glide, grip and snow drag. Immutable snapshots are explicit race inputs; unknown
surface/profile references fail before stepping. Apply resistance to forces and grip to cornering.
No weather RNG or equipment yet. Test profile identity/serialization, neutral compatibility, slow snow,
low-grip technical limits, invalid references and repeated allocation-free integration in .NET/Unity.
