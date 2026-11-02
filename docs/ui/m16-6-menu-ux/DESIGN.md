# M16.6.7 — Career navigation and next action

Owner request, 2026-11-02: research other companies and modernize the menu with intuitive navigation. Build and GitHub delivery required.

## Reference analysis

- [EA FC career guide](https://help.ea.com/en/articles/ea-sports-fc/career-mode/) and [FC25 deep dive](https://www.ea.com/games/ea-sports-fc/fc-25/news/pitch-notes-fc-25-career-mode-deep-dive): a central task list surfaces pressing career actions while retaining free navigation. Adaptation: explain the current week, selected training capacity and next action using actual career state.
- [Xbox XAG112](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112): Forza examples connect focus order to the visual arrangement; Sea of Thieves demonstrates consistent navigation and input prompts. Adaptation: stable career destinations, explicit current-page marker, persistent control hint, visible overflow and preservation of native field interaction.

These are information-architecture references, not copied layouts or artwork. The research does not establish that every menu in these games is optimal.

## Design decision

Retain the approved winter-sport FROSTLINE identity: navy #0B1928, slate #132A3B, snow #E8F5F8, ice #85D5DF and gold #D6AA69. Oswald remains the restrained sports display face; Roboto handles navigation, explanatory copy and figures. Existing original training and product photography stays in use.

The signature is a career cockpit: the season navigation remains on the left; the current week and its next action lead the overview. Preparation and season records are grouped separately. The gold page marker persists when keyboard focus moves elsewhere; ice focus identifies the action ready to submit. Paths use live labels, never icon-only guesses.

```
BIATHLON / circuit        | NOW / WEEK n
athlete / season / week   | training capacity + effect of next action
Season overview           | [End week / Prepare race]
PREPARATION               | athlete / next event / readiness / calendar
  Training                |
  Equipment               | Training: forecast + week plan + activity photos
MY SEASON                 | Equipment: category + comparisons + product photos
  Standings / History     |
  Rivals / Replays        |
  Back                    |
--------------------------+------------------------------------------------
Navigation / Select / Back
```

Review of the initial layout found that adding destinations above the old training plan buried its controls in the side rail. The plan therefore moves next to its forecast in the main content area. The same critique moved shop categories and money above the product grid, where they remain immediately visible at both acceptance sizes. Slim visible scrollbars expose overflow without introducing horizontal scrolling. On Home, an existing profile makes Continue the first primary action; a fresh profile still starts with New Career and a plain-language explanation.

## Behavioral boundaries

The Unity adapter composes existing Back/OpenCareerPage transitions for direct sibling navigation. No core session contract, save schema, preparation, RNG, training formula, economy or race behavior changes. Navigating preserves the in-memory training plan. Only the existing explicit week completion action applies it. No speculative or fake task completion is displayed.

Visible scrollers consume directional events in Unity; button navigation is intercepted before ScrollView handles it, while native controls/fields retain their own handlers. Existing remembered focus survives returning to a page. Current page is a noninteractive highlighted label, not a disabled action.

Supersedes only the hidden-scrollbar presentation choice in ADR-0066. No architectural or gameplay ADR change is needed.

## Acceptance

Native PlayMode navigation, save/plan preservation, week preparation, real gamepad and keyboard; RU1600x900 and EN1024x768 at130% high contrast; native EditMode resource/panel validation; .NET career/session regression; fullscreen standalone with isolated profile store. See ../../quality/m16-6/menu-ux for receipts and captures. M16.6 remains OPEN for owner review; M17 is not started.
