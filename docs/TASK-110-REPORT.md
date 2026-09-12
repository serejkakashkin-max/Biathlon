# TASK-110 — Career fatigue carryover

WP-110 complete. Training, recovery sessions, race duration and weekly rest update persisted fatigue using authored bounded coefficients. Recovery skill affects rest effectiveness. Structured frames expose before/after state and nominal load/recovery. Counter overflow and invalid duration/slots fail before mutation.

Validation: .NET career12/12, Unity fatigue2/2. Repeated intense weeks accumulate fatigue across save/load; full rest restores it; fatigue reduces subsequent adaptation. Invalid race/rest operations preserve state. No injury punishment, permanent skill loss or automatic weekly scheduling is introduced.

The career-to-race adapter will consume this readiness in WP121; current race constructors still retain their established default state. Next: WP111 temporary form, separate from permanent skills.
