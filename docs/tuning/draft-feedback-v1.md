# Draft feedback presentation contract

DraftFeedback is a read-only observation: status, stable leader ID, strength, gap, signed lateral
wake offset, drag reduction and estimated mechanical watts saved. Supply unsheltered SkiForceOutput
at the same start-state/wind/posture/snow as the actual tick, with only drafting excluded. This
reuses authoritative drag instead of duplicating coefficients or computing a second gameplay model.
Watts saved = positive unsheltered drag × draft reduction × speed; tailwind assistance is not a saving.
It is an instantaneous mechanical estimate, not a promised metabolic refund or future finish gain.
No-draft zones suppress stale leader feedback. UI may round for display; never feed display values
back into the simulation. Later M3 harness verifies the composed caller supplies matching force inputs.
