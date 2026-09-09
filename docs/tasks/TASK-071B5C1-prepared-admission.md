# TASK-071B5C1 — Prepared admission transaction

Split existing fixed-branch admission into Prepare, ValidatePreview and Commit. Preparation
must leave authoritative positions, ownership, admitted flags, clock and events unchanged.
Return a session-bound, revision-bound value handle and expose accepted sweeps, endpoints,
admission decisions and resulting owners as value copies. Reject foreign/default/replaced/
consumed handles. An unsuccessful replacement must invalidate old scratch handles before any
validation, so partial scratch overwrite can never be committed. Preserve Step as Prepare+Commit.
Verify direct/staged event and position parity, caller-input mutation isolation, invalidation
and warmed allocation. This is a prerequisite for the eventual coordinated race transaction;
it does not itself implement multi-session atomicity or branch transitions.
