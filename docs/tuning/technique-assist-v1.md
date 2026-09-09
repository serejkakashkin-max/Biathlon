# Technique assistance

Automatic mode uses WP018's best available efficiency with no additional multiplier. Manual mode
requires a stable technique ID and applies its actual terrain/stat/fatigue response. Output preserves
requested/applied/best IDs, override flag and efficiency loss. Automatic validates supplied IDs even
when overriding them, so stale or malformed recorded commands cannot silently become valid.
The mode is an explicit session accessibility setting; WP035 must record it with other input tuning.
