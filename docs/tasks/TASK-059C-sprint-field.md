# TASK-059C — Headless Sprint field

Compose a stable entrant roster, interval scheduler, complete athlete sessions, checkpoint
timing board and final SprintResultBook. Validate all input rows before advancing the field.
Finished sessions remain frozen while remaining athletes continue; result publication occurs
once per finisher. Test multi-athlete completion, late start ordering, invalid field command
atomicity, repeat events/results and AI-driven bouts.

M5 uses dedicated synthetic logical lanes (one per entrant) and independent ski paths;
no physical lane-layout claim or hidden speed modifier. Pack interactions and occupied-range
queues are M6/M7 work. Keep this explicit in the report. WP060 adds complete-race replay,
WP061 full regression fixtures, WP062 the 60-athlete benchmark/M5 gate.
