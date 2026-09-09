# Player command recording and replay

PlayerTimeTrialSession owns the solo race and control adapters. Effort preview, posture, steering
and technique selection validate before the race commit; then effort/lateral state commit. Exact
raw PlayerCommand, applied line/cost and full race frame are observable. Recorded overtake pulses
report no target in this solo harness; they do not create phantom competitors.

PlayerCommandTape preallocates fixed capacity, rejects missing/duplicate/out-of-order/foreign-athlete
commands, and checks capacity before advancing a recorded session. Replay context includes every
model/input/athlete/track/route/condition definition, settings, ski ID, seed and frequency. Exact
context comparison rejects changed data under an unchanged ID. Cold JSON stores canonical command
JSON strings plus context; loaded streams validate every row. No implicit hold-last gaps on replay.

The solo control corridor conservatively uses the route's minimum width throughout, so a future
narrowing cannot invalidate an earlier lateral position or force a teleport. This intentionally
leaves extra space unused on wider segments; local taper planning belongs to later track/field work.
Line movement reduces useful propulsion through TimeTrialInput.MovementEfficiency, default 1.
CSV v2 appends that explicit input. Original v1 columns remain independently checked against M2
hashes; v2 hashes pin the added column. Existing no-steering course results are unchanged.
