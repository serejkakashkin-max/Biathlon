# TASK-028 — WP-028 headless time-trial runner

Dependencies WP013..027 PASS. Compose route, physiology, technique, posture, cornering, snow,
equipment, final effort and finish in one fixed-tick solo session. Preview full transition before
commit; invalid input cannot partly change physiology/events. Record posture/technique/overspeed/
sprint/lap/finish transitions. Export full inputs, forces/resources, splits and deterministic summary
from .NET Balance Lab. Single-athlete time trial has no draft leader or overtaking target; pack
mechanics retain separate regression tests. Test full finishes, input replay, atomicity, GC and MCP.
