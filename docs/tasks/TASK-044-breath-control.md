# TASK-044 — Breath control cost

Fixed-tick holding reduces breathing while building bounded debt. Discomfort erodes its
benefit; release produces rebound until debt recovers. Exhaustion forces release and requires
button release plus recovery before another hold. Skill reduces debt rate, stance affects
recovery, never removes cost. Emit hold transition events; verify sustained hold cannot
suppress sway indefinitely, exact replay, invalid input atomicity and zero allocation.
