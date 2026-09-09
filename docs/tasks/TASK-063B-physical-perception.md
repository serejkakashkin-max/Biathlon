# TASK-063B — Physical perception contract

Capture coherent caller-authored corridor placements at an explicit simulation tick, with
stable-ID neighbors and the shared DraftingSystem output. Reject duplicate/invalid placements,
stale binding to race perception and insufficient output capacity. Do not synthesize placements
from Sprint route distance: dedicated paths provide unavailable physical perception until WP071
supplies authoritative pack placement. Validate shared-model equality, corridor separation,
order independence and warmed allocation cost. Complete WP063 after both sources pass Unity.
