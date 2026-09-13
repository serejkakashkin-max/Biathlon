# OWNER-FEEL-001 — WP147 presentation review

Issue remains OPEN. This is a presentation review, not a difficulty/balance qualification.

Before WP147, the third-person rifle stayed on its static ready axis while the dedicated camera followed Sight. WP147 maps that same composed Sight to rifle/head direction at 1:1 angular scale, then resolves the two hands. No separate oscillation or random offset is added. `ShootingSession.Sight` already includes base aim, composed breathing/heartbeat/muscular/pressure/breath-hold sway and current handling/recoil. Adding those components again would visibly double them.

The dedicated target camera already uses Sight.Ray. Target aperture dimensions, spacing, distance, field of view and player input/fine-control behavior are unchanged. Wind influences projectile resolution; an extra rifle rotation is not invented to represent ballistic wind drift. ShotAim uses the handling state before a shot; the post-tick Sight includes the new recoil, which is appropriate for the subsequent visual pose. No animation transform is used as a shot origin/direction by simulation.

The fixed grey background/red crosshair and schematic venue still make causal cues sparse. First/third-person alignment addresses a missing visual connection; it does not prove whether breathing, heartbeat, muscular fatigue or any other challenge is strong enough. The dedicated camera hides the athlete body, so better third-person poses alone cannot resolve the owner's player-feel concern. No authoritative tuning was performed.

Required followups: WP151 retains actual prone/standing target-camera and body evidence. WP229 reviews the complete owner checklist and hands-on feel; WP230 reviews difficulty/accessibility and default assists; WP232 reviews race-wide ski/shooting balance. Breathing, HR impulses, fatigue, stance, pressure, wind, trigger timing, hold/rebound, fine aim, stats and assists all remain in that mandatory checklist. None is marked resolved by this review.
