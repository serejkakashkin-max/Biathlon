# TASK-107 — Season standings and result history

WP-107 complete. Versioned race records retain season/event/athlete identities, format, seed, authority fingerprint, ordered finish times, penalties and overtakes. Season saves preserve records; old saves without results retain their original encoding.

Authored fictional points apply to Sprint, Pursuit and Mass Start. Overall and format-specific tables order by points, wins, podiums, total elapsed time and stable ID. Duplicate events, unknown athletes, malformed/unordered finishes and mismatched seasons fail before mutation. Standings are derived from records, avoiding divergent saved totals.

Validation: .NET career tests6/6; Unity MCP standings2/2. Tests cover three formats, persistence, scoring, duplicate/invalid imports and unchanged state after rejection. Race physics and the M8 tuning lock are unchanged.

Limitations: the result importer expects authority-ordered classified finishes; calendar scheduling and real simulation integration arrive in WP121. This fictional points table does not claim official federation rules. Next: WP108 training session definitions and bounded weekly planning.
