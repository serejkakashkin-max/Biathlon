# M16.6.6 — Winter sports menu photography

Owner request, 2026-11-02: generate beautiful training, rifle and ski images,
research contemporary menus online, integrate a modern presentation, publish
GitHub changes and deliver a fullscreen review build.

## References and decision

- [Forza Motorsport car selection](https://forums.forza.net/t/menu-car/626882):
  large product imagery with comparable specifications and clear categories.
- [EA FC 25 career navigation](https://www.ea.com/games/ea-sports-fc/fc-25/news/pitch-notes-fc-25-career-mode-deep-dive):
  explicit tasks and direct navigation in a dense career interface.
- [Forza accessibility options](https://support.forza.net/hc/en-us/articles/20964254277267-Forza-Motorsport-Accessibility-Support):
  readable text, contrast and configurable presentation remain requirements.

These are layout references, not copied assets. All eighteen images were made
with the built-in OpenAI image_gen tool. Exact final prompts and resource IDs:
[generation-prompts.json](generation-prompts.json).

The visual direction is a Nordic training camp and winter equipment catalog.
Retain the established FROSTLINE family: deep navy #0B1928, slate #132A3B,
snow-white #E8F5F8, ice accent #85D5DF, warm gold #D6AA69; add the glacial
photography backdrop #CDE7F5. Oswald remains the restrained display face,
Roboto the body/data face. No new font licensing or loading dependency.

Signature: full-width photographic card tops, with all live text and controls
below the photograph. Training gets six distinct real sporting activities;
eight ski colors match the existing appearance catalog; four rifles have
individual materials and silhouettes. No baked-in text, commercial logos or
fictional gameplay benefits. Illustrations are menu artwork, not exact renders
of the existing in-race meshes or the player's chosen athlete.

```
Career / plan rail | category + existing forecast / page navigation
                  | photograph       photograph       photograph
                  | name / ownership / real comparison / action
```

Training uses two columns, the shop three; compact layouts use one column.
Photo height follows its aspect ratio; products have bounded height and fit
without cutting equipment. Rifle UV framing removes only empty studio
headroom. Training preserves the whole composition. Narrow/large-text views
retain vertical scrolling and focus-driven control visibility. Shop pagination
is above the cards so another product can be reached immediately.

Selected cards and keyboard/gamepad focus use a complete border; high contrast
retains white outlines and black text surfaces. Photography ignores pointer
picking and never becomes an extra focus stop. No new animation.

An initial rounded overflow clip produced opaque white quads on the existing
UI renderer despite passing resource-binding checks. Remove that unnecessary
clip; explicitly bound image width and add a rendered-pixel contrast assertion
in native equipment captures. Inspect actual screenshots, not only hierarchy.

## Authority and delivery

This owner-directed raster presentation supersedes ADR-0087's code-native
illustration choice only. Existing six training kinds, preview formulas,
equipment prices/stats, inventory, saves, RNG, race and replay are unchanged.
Original v1 artwork is retained. New resources are under
`Assets/Resources/Biathlon/UI/Art/CareerV2`, with committed Unity metadata,
1024-pixel import bound, no CPU-readable copy and no mipmaps.

The optional `-biathlonMenuArtReview` development-player mode reuses the existing
isolated review harness; it validates all eighteen resource bindings and saves
three actual menu captures. Its receipt explicitly identifies a menu-only test,
not a full race qualification. Normal startup does not instantiate the probe.

M16.6 stays open for owner review. No M17 work.
