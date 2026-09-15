# TASK-149 — Observed fatigue/exhaustion presentation

2026-09-15. WP149 PASS. M11 remains in progress; next WP150 crowd/LOD performance.

The locomotion driver now consumes detached muscular fatigue, anaerobic reserve and HR observations. Versioned presentation data maps them to a bounded intensity, with0.8 s smoothing, greater body lean, a slightly lower hip and reduced arm/stride amplitude. A small visual breathing movement belongs to skiing only. Accepted tuck retains its pose; shooting bypasses the added motion so the composed authoritative Sight is not duplicated. A finish-recovery pose is available to animation consumers and the validation fixture.

Full Unity PlayMode **44/44 PASS**, including bounded/monotonic intensity, gradual recovery without root drift, visibly different tired/finish poses, preserved contact and both-stance rifle/grip checks. Real-bout observation integration and existing input/camera/full-race tests pass. Console **0 errors/0 warnings**, compilation_failed=false. Source and asset changes are presentation-only; no authoritative or balance input changed, and OWNER-FEEL-001 remains OPEN.

Evidence: `docs/quality/m11/wp149/` contains the final full test result/XML, Console and fixed fresh/tired/finish comparison. Reproduce with `scripts/Capture-AthleteExertion.ps1` in the running animation validation scene. The initial recovery pose was insufficiently distinct; increasing only its visual lean/hip drop corrected that failure. A test-runner Unknown/zero-test result after reload was discarded; the persisted final qualification has44 executed passes.

Limits: synthetic maximum fatigue comparison is an animation fixture, not a claim that a real race reaches that state. The current race presentation hides finished entrants, so finish recovery is presently visible in the fixture only; finish-area staging needs integration before claiming an in-race finish sequence. Original skinning remains developmental. No new Windows executable or final visual approval is claimed. WP151 must carry this visibility limitation into its missing-transition review.
