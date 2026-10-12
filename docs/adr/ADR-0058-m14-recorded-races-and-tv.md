# ADR-0058 — Recorded race commands and observation-only TV

Date: 2026-10-12. Status: accepted implementation for M14 owner review.

M14 needs compatible saved replays, camera selection, highlights and TV graphics without running tactical AI again or changing a completed career race.

Accepted manual field commands are observed only after the transaction commits. The binary journal contains ordered athlete inputs, steering and brakes at each fixed tick, together with schema, tick frequency and hashes of the initial field, physical world and movement policy. The writer streams to disk with gzip compression and bounded buffers; it does not store scene objects or frame transforms. A failed optional recording does not undo or fault the committed race. Career saves remain independent of replay files.

A replay restores the saved pre-race career snapshot, venue and seed into a fresh manual field. It reads commands directly; it never asks AI for decisions or writes career progress. Backward seeking rebuilds the field against the newly restored venue instance. Forward work is bounded by both ticks and wall time per frame. File hashes, initial context hashes and the final authoritative result hash detect corruption or incompatible definitions. Old incompatible recordings show a readable error rather than being silently reinterpreted.

Highlights derive from actual race events. Sprint close finishes use registered elapsed times, not simultaneous physical arrivals. Broadcast shots bind to stable athlete IDs and are presentation only. TV standings explicitly identify checkpoint timing; unavailable ranks/gaps remain unknown. The live optional F2 panel and replay shooting board consume accepted snapshots. Text size, contrast and target symbols alter presentation only.

The existing independent start approach and Pursuit directed passages can coexist. An obsolete constructor exclusion prevented their composition and was removed. The headless production career provider now binds the same manual movement composition already used by the playable host; synthetic career fixtures keep their existing path. This intentionally aligns headless production results with live race physics. Live power, steering, contact, shooting and AI tuning are unchanged.

Verification includes a complete recorded/replayed 30-athlete Sprint with byte-identical career result, a saved Sprint-to-Pursuit career transition, stream corruption/context tests and native replay seek/navigation checks. M14 closure remains the owner's decision after testing the build.
