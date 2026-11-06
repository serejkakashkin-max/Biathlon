# Rejected M16.7 source fixtures

These are project-authored poses of CC0 source anatomy retained as negative
regressions. They are never installed as the active athlete. Original source
and licenses: ../MakeHuman/Natural/provenance.json and ../MakeHuman/provenance.json.

- rejected_thumb.blend: rejected early finger/thumb contact.
- rejected_pinching_end.blend: upper-end fingertip pinch, palm away from handle;
  verify_hand_contacts.py must fail (exit 2).
- rejected_claw_grip.blend: proximal contact with distal palmar pads away from
  the handle; verify_distal_grip.py must fail (exit 2). This stripped source
  retains original rejected glove/strap/rounded-handle/contact geometry and
  armature, without body textures or unrelated equipment.
- rejected_working_grip.blend: the owner-rejected ordinary grip, with the
  handle in front of the fingers. All ten Skate palmar patches fail the same
  verify_distal_grip.py test that now covers both Skate and Tuck (exit 2).

Run with the current unwarped Natural anatomy as --anatomy. Actual red and
green receipts: docs/quality/m16-7-wrist/receipts. These tests do not certify art.
