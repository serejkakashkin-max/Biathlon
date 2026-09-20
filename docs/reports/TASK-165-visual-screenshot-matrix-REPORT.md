# TASK-165 вЂ” Visual screenshot matrix

2026-09-20. Capture/regression task PASS. Final visual quality FAIL recommendation; owner G2 remains pending after WP166.

Captured27 combinations at2560Г—1440: low/balanced/high Г— clear/overcast/snowfall Г— approach/athlete/targets, all at explicit8s. Every case retains its actual camera pose/projection, quality/weather/lighting,10 fixture athletes, source snapshot tick400, visible forest/crowd counts and hardware/API. Manifest records source-input and PNG SHA256 hashes and the dirty working tree based on8f73b5f. HTML gallery and three contact sheets provide review access; original full-resolution captures remain the evidence.

Three additional snowfall rail captures at2в†’6в†’2s change3388042 pixels with motion and0 on rewind (maximum channel difference0). `Verify-VisualMatrix.py` verifies all30 image sizes, hashes, case metadata, exact27-cell coverage and pixel rewind; PASS. Unity Console0 errors/0 warnings, compilation_failed=false. Runtime C# and tuning did not change in this WP; WP164 full93/93 and Windows smoke remain applicable. No duplicate test suite was run for capture/report-only changes.

Reviewed all27 cells on tier contact sheets plus full-resolution athlete and target frames. Targets remain visibly distinguishable but snowfall markedly lowers target-face contrast, requiring future presentation/readability review against gameplay weather. The simple anatomy/materials, geometric trees and sparse stadium prevent the required clear generational improvement. `quality/m12/wp165/visual-comparison.md` records source-linked qualitative comparison with official NGL stills, owner references, limitations and prioritized actions. No claims about competitor simulation/performance/animation are inferred from stills; no third-party art imported or branding copied.

This technical PASS does not promote WP164 art to final quality or approve G2. OWNER-FEEL-001 remains OPEN; no shooting balance changes. Next: WP166 CPU/GPU timing and draw/batch budgets, then concrete owner G2 review.

WP166 clarification (2026-09-20): the earlier Windows smoke PASS covered scene/simulation/quality initialization, not actual frame rendering. Actual render coverage is now supplied by WP166 nine offscreen checks and PNGs; see WP-166-PROFILING-REPORT.md.
