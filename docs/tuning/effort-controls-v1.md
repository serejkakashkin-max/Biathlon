# Effort controls

Versioned effort_control_provisional_v1.json owns analog deadzone/saturation, ordered tactical values
and optional assistance rates. Analog range between thresholds maps monotonically to 0..1000 with
ties-to-even quantization. Assisted response rate-limits a continuous nine-decimal control state,
then quantizes the applied integer; fractional progress survives tick frequencies such as 30/144 Hz.
Unassisted response directly applies the target. This is input assistance, not extra physical power.

FinalSprint preset uses ordinary race effort plus an explicit final-sprint request; existing WP027
zone logic decides when sprint effort applies. The later composed input adapter must preserve that
ordering, held posture and raw command in telemetry. Device bindings/keyboard increments follow WP036.
