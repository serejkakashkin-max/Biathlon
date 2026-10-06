# TASK M13 — bounded manual route progress proposal

Date: 2026-10-06. Intermediate M13 integration; WP-175–178 and manual steering remain OPEN.

`ManualRouteProgression` consumes only group-safe world/route frames from the existing joint sweep. It keeps the last observed station separate from the furthest credited station for each athlete on a selected route occurrence. Going backward does not reduce earned progress, and retracing the same ground does not award it again. The owning race phase supplies a maximum allowed station; a firing line, finish, section seam or other unadmitted limit rejects the whole proposal. A foreign/duplicate athlete, unmatched route section or missing projection cannot publish a partial roster. The contract is event-free, warm allocation-free and binds the exact route definition in `manual_same_section_high_water_credit_v1` context.

The code does not commit athlete state, attach odometer distance, produce lap/range/penalty events or authorize crossing a section. It intentionally rejects adjacent-section movement until topology admission and range/penalty world geometry are implemented. The v1 playable host and its automatic route following have **not** changed. No race outputs, balance or production tuning changed.

Validation: Release .NET focused manual-route/joint/AI/force regression 43/43 PASS ([TRX](../quality/m13/wp175-manual-route-progress/manual-route-regression.trx)); native Unity EditMode `ManualRouteJointSweepTests` 12/12 PASS ([XML](../quality/m13/wp175-manual-route-progress/unity-manual-route-joint-editmode.xml)). Unity AssetDatabase refreshed, compilation idle; recent Error Console empty. The initial fully qualified Unity test-class filter returned no tests because this MCP runner expects the short class name; the corrected filter executed all 12, and the earlier tool error is not a project compile/test failure.

Next: explicit section-seam admission and range/penalty path cursors, then one authoritative world/route/physiology/lifecycle transaction and exact full-field v2 replay. Only after those pass can the playable host consume `BuildManual` and the owner test one consolidated Windows build.
