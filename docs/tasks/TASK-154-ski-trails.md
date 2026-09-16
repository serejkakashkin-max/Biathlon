# TASK-154 — Bounded ski and pole contact marks

IN PROGRESS after WP153. Render subtle ski trails and pole impressions from observed presentation contact. Use fixed-capacity buffers, distance culling/fade and bounded lifetime; break strips on loss of contact, teleport, scene transition and replay rewind. Typed rig sockets/contact adapters supply positions; no scene-name identifiers, colliders or authoritative simulation edits.

Verify capacity, expiry, contact gaps, rewind and distant culling with unit tests and real-host integration; test warmed allocation and record representative snow/contact captures via Unity MCP. Keep the local-plane limitation visible until finished-terrain integration at WP164. No persistent physical snow deformation is implied. Update state/report and push after verification, then WP155 snow interaction VFX.
