# CODEX M11 HANDOFF

Use this only **after the owner explicitly approves OWNER GATE G1**.

Read and obey in this order:

1. `AGENTS.md`
2. `docs/BIATHLON_MASTER_PLAN_FINAL_v2.0.md`
3. `docs/BIATHLON_GDD_TDD_v0.1.md`
4. approved ADRs / current verified `PROJECT_STATE.md`
5. `docs/owner/OWNER_M11_VISUAL_ADDENDUM_v1.md`
6. `docs/owner/M11_ASSET_MANIFEST_v1.md`
7. `docs/owner/M11_REFERENCE_INDEX_v1.md`
8. `docs/art/reference/m11/`

Owner G1 feedback to preserve:

- Core greybox direction is approved to proceed into M11.
- Current greybox is too technical for final visual judgment; do not treat that as a visual rejection.
- Player shooting currently feels too easy. Record this as gameplay-feel/tuning feedback. Do not add hidden RNG and do not silently retune authoritative shooting during animation work.

Proceed from WP-141 through WP-151 according to the Master Plan.

Key constraints:

- Primary ski gameplay remains third-person chase camera.
- Range may use the dedicated shooting camera.
- Presentation/animation must never become authoritative movement.
- Do not redesign G1-approved controls.
- Do not start M12/M13 mass visual-content production early.
- Do not reproduce real or pseudo-real branding visible in generated owner references.
- Do not import external third-party assets without source/license/status documentation.
- If a paid/licensed humanoid or animation package becomes materially necessary, stop only for the owner asset decision and provide a shortlist with recommendation.
- Use Unity MCP screenshots frequently.
- Validate motion in Game View and through repeatable validation scenes.
- Preserve tests, determinism and 30-athlete performance.

Before declaring M11 complete, provide:
- WP reports;
- representative fixed-camera screenshots;
- player chase-camera captures;
- shooting transition captures;
- dense athlete/performance validation;
- list of remaining visual limitations that intentionally belong to M12.
