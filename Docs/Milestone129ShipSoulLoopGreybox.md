# M129: Ship-Soul Loop Greybox

M129 is a runtime greybox and lock-artifact milestone. It makes the ship the beta loop's practical center without rebuilding the whole hub.

## Decisions

- The ship is a clean roguelite hub first: readable, useful, and fast to navigate.
- The Departures launcher is reframed as the `Portal Engine`.
- The Portal Engine is immediately usable once a valid profile is selected.
- The Technology Lab presents existing soul upgrades as ship tech modules.
- Ship module costs use `Banked Souls` copy.
- A persistent `Ship Log` panel explains the loop in plain operational language.
- Normal world-loop souls stay at risk through inter-branch hubs.
- The final world-loop endpoint is `Return to Ship`; this banks souls and returns to ship arrival.
- Normal-run death returns to ship arrival with zero souls banked.
- Arrivals quarantine remains the post-run reset beat.

## Runtime Copy

- Portal launch terminal: `Portal Engine`
- Ship log panel: `Ship Log`
- Ship log message: `Portal Engine online. Banked Souls are safe aboard ship. Souls collected during a run bank only after final return.`
- Final endpoint: `Return to Ship`
- Module names: `Vitals Module`, `Stamina Module`, `Reactor Module`, `Combat Module`

## Deferrals

- No save schema changes.
- No reward or economy schema changes.
- No biomass runtime behavior.
- No Black Orb or generic-resource runtime behavior.
- No runtime run-HUD rename to `Unbanked Souls` in M129.

## Acceptance

- The player can start at the ship, use the Portal Engine, enter a normal run, move through branch hubs without banking, and bank souls only through final Return to Ship.
- The player can spend Banked Souls on ship tech modules.
- The ship includes a visible Ship Log surface that explains the banking rule.
- Menus remain fallback access, while the ship communicates the beta loop.
