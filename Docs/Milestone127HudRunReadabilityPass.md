# M127: HUD + Run Readability Pass

M127 locks the beta HUD readability pass after M126's design lock.

## Decisions

- Enlarged minimap and map interaction are deferred beyond M127.
- The top-right minimap uses a plain dark rectangular background instead of the previous cosmic frame.
- The current location label sits below the minimap.
- The bottom-right Debug Spawn button is hidden from normal screen UI.
- `F10` opens the Developer Spawn Menu only in editor/development builds.

## Acceptance

- Health, souls, coins, optional key/boss-key, active item, consumable card, pickup reveal, and minimap surfaces remain present.
- Normal gameplay has no always-visible developer/debug spawn button.
- Developer/debug surfaces remain available only through explicit debug routes.
- Location labels resolve Spaceship, Developer Lab, inter-branch hubs, and world branch names.
