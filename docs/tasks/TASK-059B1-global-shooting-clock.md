# TASK-059B1 — Global-clock shooting composition

Allow a complete shooting session and AI controller to start at a nonzero race tick, with
all private subsystem deadlines using that clock. Preserve existing zero-origin context and
trace fixtures. Extend command tape using explicit versioned nonzero origin; reject mismatched
origins and replay correct global event ticks. Accept the athlete's actual physiology
definition for HR validation/oscillator normalization and include it in context.

Test shifted versus unshifted complete frames (except clock fields), AI completion, tape
round-trip and origin/physiology mismatch atomicity. No physical state reset is introduced:
the future orchestrator continues physiology while supplying shooting inputs.
