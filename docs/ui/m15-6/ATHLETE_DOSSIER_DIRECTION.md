# Athlete dossier — M15.6

The career page is a sporting dossier: identify the athlete, understand permanent
ability, then inspect the evidence of development. It uses the existing FROSTLINE
materials rather than introducing a second visual identity.

Palette: Deep ice `#0B1928`, Night blue `#132A3B`, Snow ink `#E8F5F8`, Cold cyan
`#85D5DF`, Medal gold `#D6AA69`, Warning coral `#F3988D`. Oswald carries ratings
and restrained display headings; Roboto carries labels, controls and explanations.

The signature is a career track: an actual chronological OVR line, with individual
observations and a gold peak marker. A single observation stays a single dot.
No fake historical curve, projected future ceiling or animated presentation timer.

The dossier has four deliberate views: profile, 13 abilities, development,
milestones.
Navigation and identity occupy the left column; one native G2 preview occupies
the middle; the selected view occupies the right. The existing creator studio,
private camera and RenderTexture are reused across tabs, including the saved
national kit, appearance and selected skis. Back returns to the exact originating
career page and releases the studio when leaving the dossier.

Ability rails are cyan, observed positive seasonal deltas gold, negative deltas
coral. Deltas have signs, so color is never the only signal. Form and fatigue are
explicitly separate from OVR. Development assessment describes observed changes;
the exact hidden potential is not exposed. Lifetime races/wins/earnings come from
actual saved counters. Shared podium encounters never become lifetime podiums.

The world roster has active-field, persistent-world and rivalry views, country,
circuit and name filters, and separate OVR/points/age/name sorts. Each athlete opens their
own dossier, preserving stable identity. Current points are absent for off-field
athletes because aggregate development does not invent simulated race results.
Retired athletes use a separate bounded archive page; capacity archival is labelled
separately from retirement. Details use bounded pages rather than text walls.
Standings rows expose a visible focus border and keyboard submission to dossiers.

Milestones read actual persisted race/win counters and observed/current peak OVR.
Their six authored targets and localized labels live in career_achievements_v1.
They grant no rewards or capability bonuses and do not invent podiums, titles or
historical race results. Three concise medal cards appear per page. Training
displays the real plan's OVR forecast alongside form/fatigue; small actual ability
changes do not imply an immediate increase in rounded OVR.

Native QA covers Russian at1600×900, English at1024×768 with130% text, explicit
keyboard submission, Back/return state, 13 real abilities, unchanged save/RNG and
unchanged scene lighting/quality. Screenshots come from the live Unity UI.
