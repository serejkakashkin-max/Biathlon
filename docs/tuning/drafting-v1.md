# Provisional drafting field

Participants supply stable athlete ID, a common directed physical corridor ID, longitudinal
nanometres and lateral metres. The caller must use the same distance origin for a corridor and
must distinguish disconnected/parallel routes. Do not derive these IDs from GameObject names.
WP024/route composition supplies the roster and placement; this field has no collision authority.

Eligible leaders are ahead within the configured gap range and segment allows drafting. Smooth
distance attenuation, lateral wake overlap, speed and exposed headwind form a bounded strength.
Crosswind shifts the wake laterally; follower can align with that displaced wake. Strong tailwind
with nonpositive relative flow supplies no drag saving. Strongest leader wins, ordinal ID breaks
ties; effects do not stack with field size. Slow skiers get less benefit than fast skiers.

Apply output DragAreaMultiplier with tuck's factor to WP017 drag. There is no physiology refund:
same effort accelerates more, or a pace controller may explicitly lower effort for the same speed.
All wind/width/distance/reduction values are provisional JSON. Runner records active leader/strength.
