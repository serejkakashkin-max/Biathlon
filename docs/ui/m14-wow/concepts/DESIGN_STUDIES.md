# M14 native design studies — 2026-10-13

These are alternative visual compositions rendered by Unity UI Toolkit, not
screenshots of a completed redesign. Every frame carries `DESIGN STUDY` and
`SAMPLE DATA · NOT A LIVE CAREER`. Sample speeds, time, position and pulse only
exercise hierarchy; they must never replace live model bindings in production.
Existing generated biathlete and Woodland Basin art is reused. The Alpine Studio
map reads the actual `main` loop from the authored venue's `visual.json`.

## A — Frostline Broadcast

Palette: deep navy `#0B1928`, ice `#E8F5F8`, glacier `#85D5DF`, medal gold
`#D6AA69`, quiet steel `#B6CAD4`. Oswald display, Roboto body/data. A restrained
timing-line motif ties menu edge, action and race telemetry together.

Composition: an editorial left-hand menu anchors a large biathlete hero; the
next race has one low right-hand invitation. The race HUD is split across four
small corners, keeping the centre entirely open. Gold represents the elevated
pulse/split emphasis in this specimen; actual status colours must be semantic
and also have a readable label.

```
MENU                         RACE
brand |                      lap / time           split
headline | athlete
continue |                   unobstructed play
navigation | next race       speed / pace         pulse
```

This is the recommended direction: it fits skiing's cold material palette,
feels like a dedicated biathlon broadcast, and preserves the owner's favoured
background. It gives the career a strong identity without making the gameplay
view into a dashboard.

## B — Alpine Studio

Palette: glacial paper `#EAF0F2`, white `#F8FBFC`, dark ink `#0B1928`, sporting
red `#D85144`, slate `#51727D`. Oswald headings, Roboto labels. A persistent
light sidebar makes the career a equipment-room planning surface. The broad
landscape and genuine course map form a two-part venue dossier. HUD panels are
light with one sporting red timing edge, asymmetrically distributed.

```
MENU                         RACE
sidebar | day of race        format / lap / time    split
        | venue | course
        | kit   | start      unobstructed play
                             pulse                 speed
```

Tradeoff: exceptionally clear planning, but more light panel area competes
with snow during a race and the hero is less prominent.

## C — Night Pursuit

Palette: midnight blue-violet `#11162C`, ice white `#E8F5F8`, muted lavender
`#CDD1E0`, warm amber `#EAC47F`. Oswald cinematic display, Roboto utility. Full
hero with a quiet framed career invitation and centred bottom navigation.
Race time/lap/split are a short top ribbon; motion telemetry shares one bottom
instrument strip.

```
MENU                         RACE
brand          night          lap / time / split
invitation  | hero
continue    |                 unobstructed play
       bottom navigation         speed / pace / pulse
```

Tradeoff: a memorable controller-friendly silhouette, but the middle bottom
instrument is closer to the athlete and its low stance animation.

## Rendering and critique

Run `Biathlon.PlayModeTests.FrostlineConceptTests` in Unity PlayMode. Its single
coroutine writes `a-menu.png`, `a-hud.png`, `b-menu.png`, `b-hud.png`, `c-menu.png`,
`c-hud.png` here at 1280×720 using the project's real Oswald and Roboto fonts.
Captures require visual review before selecting the production direction.
The fixture never changes the race, saves, menu bindings, textures or quality
settings. Production will still require keyboard/controller focus, responsive
1024×768 and enlarged Cyrillic text checks, plus reduced-motion behavior.
