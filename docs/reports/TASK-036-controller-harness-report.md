# TASK-036 — Controller tuning harness

Status: PASS, 2026-09-07. WP036 completes M3.

Added DeviceControlDefinition and device_control_v1.json, DeviceCommandBuilder,
PlayerDeviceInput (Unity Input System assembly references), DeviceCommandTests and
PlayerDeviceInputTests. Keyboard virtual effort advances only on simulation ticks;
raw gamepad axes use one configured deadzone/curve. Held state and latched action edges
are separate. Cancellation wins conflicting pending actions. Invalid target ticks do
not consume input. Tick generation allocates zero managed bytes in the measured loop.

Validation: 198/198 standalone and Unity EditMode; 4/4 Unity PlayMode, zero skips/failures.
Evidence: artifacts/physiology/wp036-standalone.trx, wp036-final-editmode.json,
wp036-final-console.json, wp036-final-playmode.json. Console: zero errors, four existing
upstream reload/disposal warnings at the EditMode capture. Complete device-generated
course replays identical motion, lateral position, physiology, finish and all events.
Existing Foundation and both ski CSV versions remain passing.

Bindings: keyboard W/S change virtual effort, A/D line target, Shift attack, Ctrl recovery,
Space tuck, E overtake pulse, Q cancel pulse. Gamepad RT effort, left stick X line, LT recovery,
South tuck, West overtake, East cancel. Initialize(athlete, frequency) configures the adapter;
UseGamepad is an explicit host selection, default keyboard, with no Inspector wiring required.
Capture is also callable by the harness. The race host calls Build once per target tick.

Limits: sampled held input is not a reconstruction of arbitrary sub-frame hardware events.
Pulses coalesce until a tick. There is no shipping rebinding UI or final visual skier here;
those belong to later presentation packages. Device tuning changes future commands, while
replay stores already encoded commands and does not depend on device tuning. No balance
constants in physics changed. No commit made: baseline includes unrelated owner changes.
Next: WP037 shooting lifecycle.
