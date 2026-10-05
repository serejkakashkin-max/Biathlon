# Actual race schedule witnesses

`woodland_sprint40_traffic_source_v1.json` is the exact `result_json` from the completed
Sprint40 diagnostic record at41410ticks, retained under
`artifacts/owner-traffic-guidance-20261005/sprint40-delivery-record.json`.
File SHA256: `f13f731a8f3dd3983568fa3495021ebc731bca4a04e4ff9ead656e8da47ee6f3`.

This input preserves the actual qualification/start schedule that reproduced a circular
Main/penalty wait in `RangeWorldTrafficTests`. It contains no generated replacement
schedule or Unity references. It is a historical reproduction fixture, not acceptance
for the current Sprint, Pursuit, or Windows build. Current delivery runs qualify Pursuit
from their own fresh Sprint record and prove persisted replay separately.
