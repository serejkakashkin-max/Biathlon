# ADR-0040 — Practical M13 snow shoulders

2026-10-10. Accepted under the owner's repeated instruction to relax M13
acceptance and finish the single-venue milestone. Supplements ADR-0038;
IBU lane selection in ADR-0039 remains unchanged.

The groomed ribbon lies on continuous snow. Its edge should not behave like an
invisible rigid fence when a ski tip crosses it by a few centimetres. Production
therefore selects a bounded 2.0 m soft snow shoulder in
`manual_gameplay_tolerance_v1.json`. Connected edge tubes include that shoulder
in their continuous whole-body checks. It is separate from the numerical sweep
reserve and does not shrink the athlete footprint or alter athlete contacts.
Historical strict policies default to zero shoulder and remain strict.

Production numerical policy uses 0.01 m/s terminal rest, 0.01 rad/s terminal
spin, 0.04 m clearance allowance, 0.05 m sweep tolerance, 0.005 m closed portal
position tolerance, a 0.20 m ordinary seam band, and a connected window factor
of four. Athlete contact skin is 0.05 m, with 0.005 m/s gradual support recovery. Mat foot placement uses a bounded 0.50 m longitudinal band, selected explicitly by the owner practical policy.
These are explicit gameplay tolerances, not claims of exact rigid ski physics.

Ordinary chart handoffs accept a position within a 4 m local window, capped by
half the successor corridor width. This applies only after the source ordinary
endpoint was observed and admitted. The new chart starts at zero credit; the
next paid interval observes the existing position continuously. It resolves
wide mat exit geometry without forcing the athlete back to the centre anchor.
Firing, penalty and finish phase ownership is not granted by this window.
Integer scoped endpoints use the catalog's exact nanometre coordinate. Passive snow-edge support arrests outward spin/acceleration while retaining rotation that increases clearance. Arresting every rotation had trapped two actual Pursuit athletes near the last corner; the complete production field is the regression witness.

The shared fixed clock, actual X/Z/heading, travel integration, selected path,
physical portal crossing and race phases are unchanged. There is no teleport,
paid standing fallback, free route credit, hidden shooting RNG or automatic
player course steering. The encoded route knot fix separately retains a two
nanometre representation reserve when choosing a connected edge window.

Validation retains strict legacy tests and adds regressions for the actual
Pursuit knot and Mass Start corner. Full production fields and Unity compilation
must still be checked. Imperfect visual contact and later balance polish are
acceptable; crashes and a race that cannot finish are not declared complete.

Production allows two metres of continuous snow beside each groomed edge. The earlier 0.50 m trial still behaved like a rigid course wall for ordinary AI lines. This final owner-practical choice keeps side snow traversable without disabling physical athlete contacts or granting phase/progress shortcuts.

Ordinary AI supplies bounded bearing recovery when its owned target is behind; private mat hairpins retain forward pursuit. The final-lap finish fork leaves Main at 1130 m before the folded range-return corner. This removes that unsuitable final-racing maneuver through authored geometry, with separate finish/runout and unchanged earlier-lap shooting/penalty ownership. Exact contexts and data hashes bind both choices for replay.
