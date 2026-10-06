# M13 AI passing-line yaw intent — 2026-10-06

The manual AI route controller now accepts a bounded desired lateral target.
It converts that line into signed yaw using the route lookahead. Opposite
passing lines produce opposite steering requests while the athlete's world
pose stays unchanged until the common physical solver accepts movement.
The caller remains responsible for choosing a side and proving body/course
clearance; this method alone cannot eliminate owner-observed queues.

Changed `ManualAiSteeringController` and its EditMode tests; ADR-0028 records
the decision. Release .NET 5/5 PASS, hidden Unity 6 EditMode 5/5 PASS,
process exit 0. Deterministic repeated requests and zero per-call allocation
are covered. Evidence: `docs/quality/m13/manual-ai-pass-line.trx` and
`docs/quality/m13/manual-ai-pass-line-unity.xml`. Unity MCP was unavailable;
the hidden batch log reports test completion with no C# compilation error.

No live AI/host/replay switch, balance change or Windows build was made.
Full shared-world pass planning and contact replan remain open.
