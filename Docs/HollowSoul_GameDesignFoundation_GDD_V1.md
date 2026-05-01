# Hollow Soul: Game Design Foundation + GDD V1

Date: 2026-05-01  
Audience: Internal team alignment for Martin, Rafal, and future implementation/design passes  
Status: Working foundation, not a public pitch deck

## 1. Executive Summary

**Hollow Soul** is an alien-souls roguelite about a lost humanoid execution-prototype exploring shattered realities to survive, consume souls, and rebuild its derelict spaceship. The player is not a heroic space marine and not a human knight. The player is **The Prototype**: neutral, purpose-built, dangerous, memory-damaged, and searching for meaning after waking inside a reality collapse caused by **The Hollow Star**.

The game combines Isaac-like room-to-room randomness, Dark-Souls-inspired danger and recovery, Doom-3-like dark sci-fi mood, and the adventure energy of older action platformers. It should feel tragic, adventurous, strange, and sometimes unexpectedly funny.

The current prototype already contains many systems: branches, hubs, bosses, chests, coins, items, weapons, rooms, challenges, and world framing. M68 reframes those systems around one clear direction: **Survive, Consume, Rebuild**.

## 2. Elevator Pitch

### One-Sentence Pitch

**Hollow Soul is a modern 3D alien-souls roguelite where a lost execution-prototype clears shattered dimensions, consumes souls for survival, and rebuilds its forgotten spaceship one dangerous run at a time.**

### Short Team Pitch

The player wakes as The Prototype, a neutral alien humanoid made to execute orders it can no longer remember. Reality has been torn apart by The Hollow Star, mixing prehistoric worlds, ancient tombs, fallen futures, medieval terror, abyssal spaces, and heaven-hell collisions into unstable branch-runs. Each run sends the Prototype through portals to collect souls, weapons, resources, memories, and technology. Between runs, the Derelict Sanctuary spaceship slowly reopens as meta-progression restores modules, challenges, NPCs, secrets, and access to deeper worlds.

### Steam-Style Pitch Draft

Slash, shoot, guard, and consume your way through broken realities in **Hollow Soul**, a 3D roguelite about an alien prototype searching for its forgotten purpose. Every run reshuffles rooms, bosses, items, chests, portals, and strange worlds. Spend souls to rebuild your derelict spaceship, unlock new systems, and push deeper into the collapse.

### Internal Development Pitch

Hollow Soul is not just a dungeon prototype. It is a run-based action RPG about an alien machine-being using souls as fuel to survive and reclaim its home. The game should be built around satisfying moment-to-moment combat, readable room progression, rare meaningful rewards, and a meta-structure where the spaceship becomes the long-term home for portals, challenges, upgrades, secrets, NPCs, and memory recovery.

### Art-Direction Pitch For Rafal

The world should feel stylized, modern, cosmic, and dangerous. No toy-diorama rule. No full grimdark realism. Think readable 3D shapes, strong silhouettes, bold materials, beautiful dead worlds, glowing alien energy, rusty ship machinery, cosmic portals, dead civilizations, and enemies that are fun to look at even when they are threatening. Priority art targets are the player, floors, walls, chests, coins, portals, doors, basic enemies, basic bosses, and core pickups.

## 3. Core Fantasy

The player is **The Prototype**.

The Prototype is:

- A lost alien humanoid execution-unit.
- Neutral rather than good or evil.
- Built to follow orders, but missing the orders.
- Dangerous, practical, and strangely innocent.
- Sustained by souls, biomass, technology, or unknown fuel.
- Slowly discovering that the Derelict Sanctuary spaceship belongs to it.

The player goal is not fully understood at the start. Early goals are survival, power, resources, and repair. Later goals can include memory recovery, ship restoration, recruitment, secret endings, and deciding what The Prototype was originally made to do.

## 4. World Premise

**The Hollow Star** is a cosmic anomaly somewhere between collapsed star, black-hole incident, high-tech disaster, and impossible reality wound. It consumed worlds, histories, myths, timelines, creatures, and machines, then spat them back out as unstable mixed realities.

The current M50 world identity catalog remains valid, but should now serve the new core fantasy:

- The Broken Meridian: mixed threshold and shattered timelines.
- Before Teeth: prehistoric hunger before language.
- The Sunken Cartouche: ancient Egypt drowned into impossible afterlife water.
- The Black Keep: medieval terror, iron, siege smoke, failed prayer.
- The Rust Choir: fallen future machines still singing after death.
- The Choir Below: hell and heaven collided and kept singing.
- The Last Hour: the end of times looped until endings are tired.
- The Blind Deep: abyssal pressure, memory, and no horizon.

Future world selection should be weighted by depth. World 1 should not normally throw the most hellish or end-of-time content immediately. Later worlds should pull from stranger, darker, more hostile pools.

## 5. Design Pillars

### Pillar 1: Survive, Consume, Rebuild

Souls are necessary fuel. They are not simply coins, and they are not automatically evil. The Prototype consumes souls to survive, power itself, repair technology, and reopen the Derelict Sanctuary.

Design rule: soul rewards should feel valuable, slightly uneasy, and strategically important.

### Pillar 2: Every Run Is A Broken Reality

Every run should feel different through rooms, bosses, items, chests, shops, secrets, biomes, enemies, and branch choices.

Design rule: randomness must create new situations, not just shuffle numbers.

### Pillar 3: The Ship Remembers What The Prototype Forgot

The Derelict Sanctuary is the long-term meta-progression home. It should eventually contain portals, challenge terminals, upgrade modules, NPCs, memory recovery, ship systems, secrets, and access to deeper realities.

Design rule: permanent progression should mostly unlock systems, access, context, and options, not simple permanent stat inflation.

### Pillar 4: Violence Must Feel Good Before It Gets Complex

Combat must feel satisfying at the basic level: slashing, shooting, guarding, collecting souls, opening chests, beating a boss, and escaping a dangerous room. Complexity should come after feel.

Design rule: melee should be a major part of combat. Ranged attacks complement melee rather than replacing it.

### Pillar 5: Beauty In Dead Worlds

The game should be beautiful-but-dead, cosmic, dreamlike, dangerous, and dark at times. It should be stylized and readable, not photorealistic. It should be darker than Spyro, not as oppressive as Diablo, and more modern than toy-diorama greybox.

Design rule: each biome needs distinct materials, palette, lighting, silhouettes, and hazard language.

## 6. Target Player Experience

The player should remember:

- The alien prototype silhouette.
- The derelict spaceship slowly reopening.
- Cosmic worlds and portals.
- Souls as a visible, desirable, uneasy resource.
- Strange creatures, dangerous bosses, and satisfying action.
- The feeling of becoming powerful through rare items and good decisions.
- Occasional odd humor inside a tragic universe.

The game should support both serious atmosphere and weird adventure moments. Examples: freeing an alien beast from a meteorite, meeting a drunk NPC who only wants beer, finding a machine that speaks like a tired priest, or discovering a harmless creature inside a terrifying room.

## 7. Core Loops

### Moment Loop

Move, aim, slash, shoot, guard, dodge later, collect souls or coins, survive.

### Room Loop

Enter a room, read threats, clear enemies, collect reward/chest/soul, choose a door.

### Branch Loop

Survive rooms, gather resources, defeat the boss, return through a temporary reality hub.

### Run Loop

Launch from the ship, enter a prologue, clear world branches, defeat bosses, extract or die, preserve meta-progression.

### Meta Loop

Spend souls and resources to repair ship systems, unlock access, reveal memories, open challenges, expand item pools, recruit entities, and reach deeper worlds.

## 8. Run Structure Direction

The current branch structure is mostly correct:

- Start from the ship.
- Enter a prologue branch.
- Reach a temporary between-realities hub.
- Choose among three branch portals.
- Clear branches and bosses.
- Advance to deeper worlds.
- Extract or die.

The missing long-term layer is the Derelict Sanctuary. The ship should become the main structure that absorbs the current menu functions over time: New Run, Continue, Challenges, Developer Lab, portals, upgrades, and memory recovery.

Branch choice should eventually use portal visuals to hint at:

- Biome or world flavor.
- Potential reward category.
- Danger level or anomaly type.
- Secret possibility.

A full successful run should target at least 30 minutes once content density supports it.

## 9. Combat Identity

Combat should feel like a mix of Isaac, Dark Souls, and Doom 3:

- Isaac: room pressure, build randomness, projectiles, item chaos.
- Dark Souls: danger, commitment, stamina, boss respect, recovery.
- Doom 3: dark sci-fi tension, hostile spaces, alien technology.

Combat priorities:

- Melee-forward action with ranged support.
- Satisfying slash impact and visible weapon use.
- Ranged weapons as complements, not total replacements.
- Stamina as a soft pacing tool for now.
- Guard/parry as a readable defensive layer.
- Dodging/dashing/rolling later.
- Fast-paced bursts without becoming unreadable chaos.

Enemy design should vary by behavior:

- Cornering enemies.
- Cowardly enemies that flee or reposition.
- Terrifying enemies that create pressure through presence.
- Small enemies that are satisfying to destroy.
- Bosses that test different skills depending on identity.

## 10. Items, Rewards, And Risk

Items should be rare and meaningful. The current M51/M52 direction of sparse ordinary room rewards is aligned with the vision.

Reward identity:

- Ordinary rooms: mostly coins, HP refill, chest, or nothing.
- Treasure rooms: primary item source.
- Boss rewards: meaningful power/reward moments.
- Shops: medium importance, useful but not mandatory.
- Souls: strategic resource for survival, ship systems, and access.

Risk identity:

- Bad items should exist.
- Bad or cursed items should be mostly optional risk, not random punishment.
- Unknown or cursed rewards can appear in secrets, shrines, strange chests, risky shops, anomalies, or special events.
- Synergies should feel hidden and rewarding.

The M54 projectile passive items fit the build-power direction, but future item expansion should pause until the basic combat/reward loop feels good.

## 11. Characters And The Prototype

Characters should become more than minor stat differences, but the first design foundation should stay focused on The Prototype.

Long-term character/remnant direction:

- Anonymous remnants rather than traditional named heroes.
- Different starting weapons, stats, items, and rules.
- Compatible with shared weapons and armor.
- Death erases the run but does not erase ship/meta progress.
- Permanent progression unlocks more systems, worlds, bosses, characters, items, and endings.

## 12. Spaceship Meta-Progression

The Derelict Sanctuary should eventually become the emotional and structural heart of the game.

Initial ship fantasy:

- A derelict alien vessel trapped behind reality-anomaly walls.
- Sections are hidden by unknown reality structures.
- Clearing bosses and anomalies restores power, doors, terminals, and modules.
- The ship feels safe but eerie, not cozy.
- It can host portals, challenge devices, item archives, NPCs, repair terminals, memory fragments, and upgrade modules.

Meta-progression should prioritize:

- Access to new ship sections.
- New portals and world pools.
- New challenge terminals.
- New item/weapon/enemy/boss unlocks.
- New NPCs or recruited entities.
- Lore and memory recovery.
- Secret paths/endings.

Meta-progression should avoid becoming mostly permanent stat grinding.

## 13. Art Direction Foundation

The old dark toy-diorama direction is no longer valid.

New art direction:

- Stylized modern 3D.
- Cosmic, dangerous, beautiful, and readable.
- Dark at times but not constantly oppressive.
- Simple enough to produce reliably.
- Strong silhouettes and materials before fine detail.
- Biome-specific floors, walls, props, lighting, and color language.
- HDR/glow/portals/soul energy as major identity tools.

First ArtPass priorities:

- Player / The Prototype.
- Floor and walls.
- Doors and portals.
- Chests and coins.
- Rocks and basic obstacles.
- Basic enemies.
- First bosses.
- Core pickups and soul visuals.

Asset pipeline rule:

- ArtPass visuals stay visual-only.
- Gameplay remains authored by runtime data/controllers.
- Every visible prefab must be scale-calibrated, bottom-aligned, material-complete, and inspectable in Developer Lab.

## 14. Feature Priority Tiers

### Beta Core

- Melee/ranged combat feel.
- Souls, coins, chests, and sparse rewards.
- Boss clears and boss rewards.
- Branch portals and temporary hubs.
- First Derelict Sanctuary ship frame.
- Run persistence and extraction/death flow.
- Readable HUD and pickup clarity.
- Basic ArtPass for player, floors, walls, chests, coins, enemies, portals, doors, and bosses.

### Near-Term

- Spaceship rooms and modules.
- Challenge terminals.
- Upgrade/repair terminals.
- NPC and recruitment hooks.
- Biome-weighted worlds.
- Better enemy and boss polish.
- Soul consumption UX.
- Secrets and optional-risk cursed content.

### Later

- Many characters/remnants.
- Large item pool.
- Secret endings.
- Deeper quests.
- Dashing/rolling.
- Advanced synergies.
- More worlds.
- Ship expansion systems.
- Larger boss and enemy rosters.

### Deferred Or Risky

- Too many modifiers before combat feels good.
- Too many items before reward pacing is readable.
- Full NPC systems before ship structure works.
- Heavy permanent stat grinding.
- Overly realistic art that slows production.
- Feature expansion that hides weak basic combat.

## 15. Current Prototype Alignment

Current systems that align:

- M50 Hollow Star world identity catalog.
- M53 boss roster and boss framework.
- M54 item catalogue and projectile passives.
- M52 chests and coin denominations.
- M51 sparse reward rebalance.
- M42 pickup clarity and build HUD direction.
- Developer Lab and ArtPass validation direction.

Current systems that need reframing:

- Menus and challenge access should eventually become ship systems.
- Current hubs should become temporary between-reality spaces, not the true safe base.
- Souls should gain stronger fiction and UX as fuel.
- ArtPass should move away from placeholder primitives into stylized modern cosmic assets.

Current systems to avoid expanding too fast:

- Modifier complexity.
- Item volume.
- Character roster.
- Deep NPC systems.
- Full biome filtering before basic visual identity exists.

## 16. Next 10 Design-Aligned Milestones

### M69: Spaceship Meta Hub Greybox V1

Create the first playable Derelict Sanctuary greybox. Move or mirror New Run, Continue, Challenge, Developer Lab, and portal access into a simple ship-space structure. Keep menus available as fallback if needed.

### M70: Combat Feel + Melee-First Rebalance

Make melee feel like the standard combat mode. Improve slash timing, impact feedback, hit visuals, enemy reactions, attack commitment, stamina pacing, and ranged support behavior.

### M71: Soul Economy + Consumption UX

Make souls visible and meaningful. Add clearer soul drops, pickup timing, consumption feedback, boss soul moments, and ship-fuel framing without changing all meta systems yet.

### M72: Art Direction Bible + Asset Scale Contract

Create a practical art bible and asset delivery rules: scale, pivots, materials, texture expectations, prefab wrapping, ArtPass roles, Developer Lab inspection, and acceptance states.

### M73: First Biome Identity Pass

Choose 2-3 beta biomes from the M50 catalog and give them distinct floors, walls, lighting, portals, materials, room dressing, and enemy color language.

### M74: Enemy Identity + Readability Pass

Make basic enemies visually and behaviorally distinct. Prioritize silhouettes, windups, hit reactions, death feedback, and clear player learning.

### M75: Boss Beta Subset Polish

Pick Stone Warden plus 2-3 bosses from M53 and polish them deeply: arenas, windups, boss bar states, attacks, death moment, reward clarity, and ArtPass silhouettes.

### M76: Ship Upgrade/Module Unlocks V1

Add the first real ship repair loop. Spend souls to unlock ship rooms/modules such as Challenge Terminal, Portal Engine, Item Archive, Memory Chamber, or Workshop.

### M77: Secrets, Cursed Items, And Optional Risk V1

Add a small optional-risk layer: cursed chests, anomaly shrines, unknown pickups, or secret rooms with tempting rewards and possible downsides.

### M78: Vertical Slice Vision Lock + Team Handoff

Lock a beta slice that demonstrates the new vision: ship start, portal run, combat/reward loop, boss clear, soul return, ship repair, readable ArtPass, and team QA/PDF handoff.

## 17. Design Guardrails

If a new feature does not strengthen one of these, it should wait:

- Combat satisfaction.
- Soul economy meaning.
- Run randomness and replayability.
- Ship/meta progression.
- World identity.
- Art readability.

If a feature makes the prototype harder to understand, harder to test, or harder to art-pass before the core loop feels good, it should be delayed.

## 18. Open Questions For Later

- What exactly was The Prototype originally ordered to do?
- Is The Hollow Star natural, artificial, divine, or caused by the ship?
- Are souls conscious, fuel, data, biomass, or all of these?
- Who built the Derelict Sanctuary?
- Can recruited entities live on the ship?
- What is the first secret ending?
- Can The Prototype become more human, more machine, more alien, or something else?

These questions should remain open until the core game loop earns them.
