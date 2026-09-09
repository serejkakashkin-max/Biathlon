# TASK-071B2 — Constrained forward-prefix commit

Allow a prepared moving step to retain a nonnegative prefix of its existing timed path and
stop at that endpoint. Never accept arbitrary route/penalty coordinates, extra distance or
increased velocity. Preserve full prepared physiological cost; use retained path for actual
checkpoint/range/lap/finish accounting. Return a new revision and invalidate the old handle.

This is a trajectory-commit primitive, not a collision solver: caller must prove continuous
safety of the prefix. Contact timing/branch junction coordination remain TASK071B3 and beyond.
Test zero-motion constraints, immutable physiology, no premature mat/finish/penalty completion,
partial penalty branch accounting, original-path acceptance and invalid caps before mutation.
