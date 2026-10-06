# M13 manual directional device input — 2026-10-06

Keyboard A/D and gamepad left-stick now have a dedicated fixed-tick path into
`ManualSkiCommand` v2. It uses the existing sampled device curve and effort/action
edges, but encodes steering as signed yaw rather than an absolute lateral-line
target. Neutral/released input requests zero yaw; focus loss also releases it.
The v1 host and its existing `BuildDirectional` path have not changed.

Focused Release .NET input/command tests: 15/15 PASS. Native Unity PlayMode
`PlayerDeviceInputTests`: 6/6 PASS. AssetDatabase refreshed, scripts compiled,
Editor idle, recent Error Console empty. Receipts:
`docs/quality/m13/owner-manual-input-20261006/`.

No race balance, authoritative motion or replay was altered. The new method is
not invoked by the playable host. Full manual control still requires a safe
world/route transaction, course/range/penalty transitions, successive contacts,
event-log, v2 replay and host wiring. M13 and WP-175–178 remain OPEN; the owner
batch directive still forbids an intermediate Windows build.

## Persisted intent follow-up

Added a distinct `ManualSkiCommandTape` v2 with stable recording ID, exact
context and athlete binding, consecutive ticks and explicit capacity. It
serializes/restores all signed steering and action fields, rejects v1 tapes and
does not allocate during warm per-tick append. Focused .NET device/steering/tape
selection 29/29 PASS; native Unity EditMode tape class 3/3 PASS. Receipts:
`m13-manual-intent-tape.trx` and `native-manual-intent-tape.xml` in the evidence
directory. Unity compiled, Editor idle, recent Error Console empty.

This tape is only an input artifact. It cannot yet step a race, restore full
world state, replay AI or reproduce race events; host integration remains open.
