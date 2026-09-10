# TASK-087 — Mass Start range order and physical lane binding

Objective: first bout by bib/grid ordinal; later bouts sequentially by actual physical entry.
Source: IBU ECR November2025 Article8.2.1, linked in the report. Preserve fixed-lane baseline.
Use a versioned opt-in policy, preallocated actor/lane projections and per-bout arrival counters.
The physical range fork is the synthetic admission line. Exclusive entry makes accepted arrivals
strictly ordered; denied previews never reserve lanes. Commit logical and physical assignments
with accepted movement, retaining occupied branches and tail checks. No body removal/teleport.
Tests: malformed policy, complete AI field with per-bout arrival ordering, physical/logical lane
agreement, event counts, complete results/repeatability, prior PPSS baseline. Unity MCP + Console.
Risk: cancellation/precision fallback must cancel speculative assignments; later-bout assignments
must wait for existing physical occupants even if they belong to another bout.
