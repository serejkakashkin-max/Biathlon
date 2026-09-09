# Provisional final sprint

FinalSprintSystem is an explicit requested effort preset in the configured final-distance zone.
It requests the same 0..1000 workload as ordinary controls; it cannot add watts, speed or reserve.
Existing M1 reserve depletion/fatigue reduce sustained capability. A depleted athlete cannot obtain
fresh sprint power by toggling the preset. Controllers may still choose any ordinary effort outside
the zone; this convenience preset is not a prohibition on attacking earlier.

FinishTracker observes consecutive accepted distance endpoints, records RaceFinished once and
retains canonical FinishTime. ADR0004 defines within-tick linear interpolation/rounding and its
physical precision limits. Do not read Time.Seconds before HasFinished; invalid default stamps reject
conversion. Ranking compares integer whole/fractional ticks under the same race clock.
