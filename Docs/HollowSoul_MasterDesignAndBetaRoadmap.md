# Hollow Soul Master Design And Beta Roadmap

Date: 2026-05-19  
Status: Canonical planning draft for review before runtime implementation  
Source: `/Users/martinjedrzejewski/Downloads/Hollow Soul _ Game Development.pdf`  
Primary beta north star: Ship-Soul Loop  
Scope policy: preserve the full vision, but keep beta ruthless

## 1. Purpose

This document absorbs the May 19 game-development PDF into one English canonical design package for Hollow Soul. It is not an implementation changelist. It is the decision layer that should be reviewed before new Unity runtime updates are committed.

The PDF contains a mix of game pillars, UI notes, currencies, item ideas, worlds, rooms, NPCs, special enemies, challenge rules, production tasks, story fragments, and pipeline notes. This master document sorts those ideas into design sectors, evaluates each named idea, and proposes the next beta roadmap from M126 to M135.

Related existing sources:

- `Docs/HollowSoul_GameDesignFoundation_GDD_V1.md`
- `Docs/HollowSoul_6_12_Month_Roadmap_Team_Capacity_Plan.md`
- `Docs/Milestone58BetaRewardEconomyChestBalance.md`
- `Docs/Milestone63BetaContentSelectionLock.md`
- `Docs/Milestone64VerticalSliceBetaLockGate.md`
- `Assets/_Hollow/Scripts/Hollow.Editor/Build/WholeGameAuditRunner.cs`

## 2. Executive Direction

Hollow Soul should be built around one readable beta promise:

> Wake as The Prototype, survive broken portal worlds, collect souls, return to the ship, repair or unlock something meaningful, and choose the next risk.

The full game can support many strange worlds, NPCs, optional-risk chests, achievements, companions, alternate timelines, cursed variants, and secret endings. The beta slice should prove the loop, not exhaust the content.

Design priority order:

1. Combat satisfaction.
2. Soul economy meaning.
3. Ship/meta-progression clarity.
4. Readable rewards and chests.
5. World identity.
6. Optional-risk variety.
7. Full-system expansion.

## 3. Evaluation Rubric

Every source idea is evaluated using the same fields.

| Field | Meaning |
|---|---|
| Current Status | How close the current Unity project/docs already are. |
| Beta Value | Low, Medium, High, or Critical value for the beta slice. |
| Full-Game Value | Low, Medium, High, or Signature value for the long-term game. |
| Cost | Low, Medium, High, or Very High production/implementation cost. |
| Risk | Main design, scope, readability, or production risk. |
| Recommendation | Beta Core, Beta Update, Post-Beta Backlog, Prototype Later, or Cut/Defer. |
| Target | Proposed milestone, backlog bucket, or deferred note. |

Recommendation definitions:

| Recommendation | Meaning |
|---|---|
| Beta Core | Required to make the beta slice coherent. |
| Beta Update | Existing or partial feature that should be polished for beta. |
| Post-Beta Backlog | Good full-game idea, not needed for beta. |
| Prototype Later | Interesting but risky; build only as a controlled experiment after beta fundamentals. |
| Cut/Defer | Placeholder, duplicate, off-tone, or too expensive for current direction. |

## 4. Current Repo Alignment

Current systems already supporting the PDF vision:

- Room-to-room branch structure, hubs, boss clears, portals, and run persistence.
- Run souls, banked souls, coins, chests, shops, rewards, items, cards, weapons, armor, challenge records, and save snapshots.
- Sparse reward direction from M51/M58.
- Normal and golden chest foundations from M52.
- World framing and biome identity catalogues from M39/M50.
- Boss roster framework from M53.
- Challenge mode foundations from M35/M47.
- ArtPass wrapper, Developer Lab, visual safety, and Meshy integration paths.
- Advanced combat/AI foundations through M70-M115.
- Whole-game audit scaffold for M116-M125.

Current systems needing reframing for the PDF vision:

- Souls need stronger fiction, pickup timing, extraction, and ship-fuel meaning.
- Biomass is a strong concept but should not become a second fully deep economy before souls are clear.
- The ship should become the true meta-home; temporary hubs should remain between-reality choices.
- UI should show only useful player-facing information during normal play, hiding debug text.
- Room variety should expand selectively, not all at once.
- Cursed/demonic/portal chest ideas should enter as optional-risk prototypes, not random punishment.

Current systems to avoid expanding too fast:

- Large item/card volume.
- Full companion AI.
- Deep NPC quest chains.
- Many biomes and bosses.
- Complex alternate-world rules.
- Heavy permanent stat progression.
- Full vertical platforming before combat and rooms feel good.

## 5. Canonical Design Sectors

### 5.1 Core Fantasy

The player is The Prototype: a memory-damaged alien/bio-synthetic execution shell travelling through broken worlds. The Prototype is not a standard heroic knight, not a realistic space marine, and not a purely evil monster. It survives by fighting, consuming, extracting, and repairing systems it only half understands.

Canonical beta loop:

1. Start at or return to the Derelict Sanctuary.
2. Enter a portal run.
3. Clear rooms and survive a boss.
4. Collect souls, coins, items, and temporary resources.
5. Return or extract.
6. Spend souls to repair/unlock ship systems.
7. Open a new portal, challenge, archive, memory, or route.

### 5.2 Narrative And World Premise

The source PDF begins with a red cracked-land amnesia scene, crater, survival equipment, portal, tropical world, hidden technology, a system that recognizes the player, cockpit sleep, and documents revealing an old identity tied to a disturbing mission.

Canonical direction:

- Keep amnesia, portal discovery, hidden identity, ship recognition, and documents revealing partial truth.
- Replace the joke placeholder "Agent Tampon I / Federation XYZ" with the current canon of The Prototype, Derelict Sanctuary, and The Hollow Star.
- Preserve the reverse-character-arc idea: "Who am I?" becomes "Who was I?" and later "Do I want to be that?"
- Use false/incomplete memory as a mystery tool, not exposition overload.
- Use ship clues, repeating symbols, and recovered documents to foreshadow NPCs, companions, and boss/world revelations.

### 5.3 Player Controls

The source PDF proposes movement, aim, dodge, interact, melee, ranged, reusable item, card slot, pause, map enlarge, jump, and consume.

Canonical beta action set:

- Move.
- Aim/face.
- Melee attack.
- Ranged attack.
- Guard/parry where already supported.
- Interact.
- Use active item.
- Use card/consumable.
- Pause.
- Map enlarge.
- Consume, only if biomass/soul-consumption UX enters beta.
- Roll/dodge only if current M111/M112 work remains stable.
- Jump is deferred unless a very small vertical test is needed for enemies/projectiles, because platforming would reshape room design.

### 5.4 HUD And UI

The PDF calls for health, currencies, biomass, souls, minimap, active items, reusable items, keys, consumables, and card deck.

Canonical beta HUD should show:

- Health as hearts or a readable heart-like array.
- Run souls and banked soul context only where it matters.
- Coins or current run spend currency.
- Active item slot.
- Card/consumable slot.
- Key count only after keys become beta-active.
- Minimap with current room, seen rooms, rewards, boss/shop/secret hints where safe.
- Enlarged map view as a later or beta-polish interaction.
- Debug data hidden by default outside Developer Lab or explicit debug switches.

### 5.5 Combat

The PDF asks whether the game is shooter, swords, magic, or Isaac-like. Existing docs already answer: melee-forward 3D roguelite with ranged support, Isaac-like rooms, Dark Souls-inspired commitment, and Doom-like dark sci-fi tension.

Canonical combat priorities:

- Melee must feel good before adding many item effects.
- Ranged attacks complement melee.
- Souls-as-ammo is a strong optional risk, but cannot undermine the core soul economy.
- Shields should be readable equipment with durability or reliability only after guard/parry feel is stable.
- Light/medium/heavy attack tiers should map to timing, stamina, knockback, recovery, and shield pressure only if they remain understandable.
- Enemy attacks should keep active windows, readable windups, recovery, and no passive contact damage except explicit hazard cases.

### 5.6 Currencies

The PDF proposes biomass, souls, Black Orb, and resources.

Canonical economy roles:

- Coins: short-term run/shop currency.
- Souls: strategic resource for ship repair, unlocks, extraction choices, and long-term progression.
- Biomass: optional run-local consumption resource from bodies, best introduced after souls have meaning.
- Black Orb: completion currency for successful full runs, post-beta.
- Generic resources: cut/defer until a specific ship or crafting role exists.

### 5.7 Items, Cards, And Rewards

The PDF divides items into consumables, cards, single-use, multi-use, always-active, special items, falling stars, weapons, armor, and rarities.

Canonical reward structure:

- Ordinary rooms stay sparse.
- Treasure rooms, bosses, shops, special encounters, and risky chests are the item sources.
- Rarity is useful for the full game, but beta should use a small pool with clear value.
- Cards are consumable tactical events, not a huge deckbuilder system yet.
- Always-active items should be passive build modifiers.
- Active/reusable items should be few and readable.
- Special items should connect to NPCs, secrets, and unlocks.

### 5.8 Chests And Optional Risk

The PDF has a rich chest set: normal, golden, mimic, biomass, souls, demonic, corrupted, and portal chests.

Canonical beta subset:

- Normal chest.
- Golden chest.
- Mimic prototype only if visual readability is strong.
- One optional-risk chest type, preferably cursed/corrupted, with explicit player consent.
- Soul chest only after soul risk/extraction is clear.

Post-beta chest expansion:

- Biomass chest.
- Demonic chest.
- Portal chest.
- More mimic variants.

### 5.9 Rooms, Levels, Hubs, And Worlds

The PDF structures gameplay as rooms inside levels, levels inside worlds, hubs between choices, and a larger world progression.

Canonical beta structure:

- Ship base is the long-term home.
- Temporary hub or between-reality hub offers branch/level choices.
- Rooms remain the moment-to-moment gameplay unit.
- Levels/branches end in boss clear and return/extraction.
- Worlds are themed pools with weighted content.
- Dark/alternate worlds are long-term optional-risk variants.

### 5.10 Biomes

The PDF proposes many worlds: ancient Egypt, prehistoric, fallen future, end times, abyss, collapsed star interior, time loop, wreck field, hive planet, zero gravity, empty metropolis, machine city, radioactive sea, dungeons, tribute planets, loop world, heaven-hell, historical eras, and weather/temperature worlds.

Canonical beta biome rule:

- Choose 2-3 beta biomes only.
- Use the existing M50 catalog where possible.
- Each beta biome needs floor, walls, portal language, lighting/materials, enemy color/silhouette hints, and room dressing.
- Most worlds should become full-vision backlog until beta proves the core loop.

### 5.11 NPCs And Companions

The PDF proposes important NPCs, support NPCs, encounter NPCs, a Drunk NPC, Alien in meteor, Soul Eater, Dark Soul, fleeing creature, and companions.

Canonical beta rule:

- Add at most 1-2 special encounters before beta.
- Prefer encounters that prove reward/risk/ship-soul identity.
- Companions are signature full-game potential, but full companion AI should wait.
- NPCs should be memorable hooks, not a full quest network yet.

### 5.12 Enemies And Bosses

The PDF proposes Swallower, Mimic, Chameleon, Escapist, Dark Soul, Soul Eater, spiders, special bosses, demonic enemies, and world-specific threats.

Canonical beta rule:

- Polish a small roster instead of expanding count.
- Use current enemy AI foundations to make enemies readable and distinct.
- Mimic/Escapist/Soul Eater are the strongest beta candidates.
- Swallower and Chameleon are high-concept post-beta ideas.
- Bosses should focus on a beta subset, not the full roster.

### 5.13 Challenges And Achievements

The PDF proposes many challenge run rules and achievement unlocks.

Canonical beta rule:

- Keep challenge mode as curated fixed seeds/rules.
- Add only rules that use existing systems safely.
- Achievements can be designed now but implemented later unless they unlock beta-critical content.
- Challenge rewards should unlock items/world variants after the base loop is stable.

### 5.14 Art And Pipeline

The PDF lists tools, art asset tasks, spatial/model dimension guides, and a need for asset sharing.

Canonical production rule:

- The current project is Unity-based, even if earlier PDF notes mention RealityKit.
- Rafal's asset pipeline should remain Blender/Zbrush/Substance -> Unity ArtPass wrapper -> Developer Lab inspection.
- Generated AI assets can be concept/reference or controlled Meshy imports, never gameplay-authoritative logic.
- Every visual asset must pass scale, pivot, material, bounds, wrapper, and no-gameplay-script checks.

## 6. Idea Evaluation Catalogue

### 6.1 Core Fantasy And Narrative

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Amnesiac protagonist wakes in hostile red cracked land | Aligned with GDD V1 but not implemented as intro | Medium | High | Medium | Intro production can distract from playable loop | Post-Beta Backlog | Story intro backlog |
| Crater implies fall from height | Missing | Low | Medium | Low | Mostly cinematic/environmental | Post-Beta Backlog | Intro scene backlog |
| Finds survival equipment before first portal | Partially overlaps weapon/start loadout | Medium | High | Medium | Needs tutorial/onboarding design | Prototype Later | Onboarding prototype |
| First mysterious portal feels familiar | Existing portal systems; story layer missing | High | High | Medium | Needs visual/narrative clarity | Beta Update | M129 |
| Portal leads to tropical green world | Existing world framing can support but not beta-selected | Low | Medium | High | Adds biome scope | Post-Beta Backlog | Biome backlog |
| Hidden technological structure recognizes the player | Ship recognition aligns strongly | High | Signature | Medium | Needs ship scene | Beta Core | M129 |
| Cockpit/safe rest area | Aligned with Derelict Sanctuary | High | High | Medium | Can become too menu-like | Beta Core | M129 |
| Documents reveal old identity and mission | Aligned with memory fragments | Medium | Signature | Low | Over-explaining too early | Beta Update | M126/M129 |
| Placeholder identity "Agent Tampon I / Federation XYZ" | Conflicts with current tone | Low | Low | Low | Joke name breaks canon | Cut/Defer | Replace with The Prototype canon |
| Reverse character arc | Documented as compatible | Medium | Signature | Low | Needs restraint | Post-Beta Backlog | Narrative bible |
| False or manipulated memory | Compatible with Hollow Star mystery | Medium | High | Medium | Can confuse beta | Post-Beta Backlog | Narrative bible |
| Ship clues foreshadow team/NPCs | Missing but aligned | Medium | High | Low | Too many dangling hooks | Beta Update | M129 |
| Repeating symbols revealed over time | Missing | Low | High | Medium | Art/narrative overhead | Post-Beta Backlog | World identity backlog |
| Black hole/Hollow Star as central conflict | Existing GDD direction | Critical | Signature | Low | Needs consistent naming | Beta Core | M126 |
| Machine/laser/ship system powered by essence | Aligned with ship-soul loop | Critical | Signature | Medium | Can become abstract if not visual | Beta Core | M129 |
| Save all worlds vs save one central world | Not decided | Low | High | High | Branching story scope | Post-Beta Backlog | Narrative choice backlog |
| Black hole reacts/fights back | Not implemented | Medium | High | High | Scope creep if systemic | Prototype Later | Post-beta anomaly systems |

### 6.2 HUD, UI, And Controls

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Main HUD array with health and currencies | Partial/in progress through HUD systems | Critical | High | Medium | Can become cluttered | Beta Core | M127 |
| Hearts or healthbar | Existing health systems; visual pass in progress | Critical | High | Medium | Must scale for max HP changes | Beta Core | M127 |
| HP near/top of board | Partial/in progress | High | High | Low | Platform-specific readability | Beta Update | M127 |
| Biomass display | Biomass missing as economy | Medium | High | Medium | Premature second economy | Prototype Later | M128 after soul clarity |
| Souls display for current run | Existing run souls | Critical | High | Low | Must separate run vs banked | Beta Core | M128 |
| Minimap | Existing and being polished | Critical | High | Medium | Must avoid debug text | Beta Core | M127 |
| Enlarge/lift minimap | Missing or partial | Medium | High | Medium | Input/platform ambiguity | Beta Update | M127 or post-beta polish |
| Active item/reusable preview | Existing active item/card model partial | High | High | Medium | HUD clutter | Beta Core | M127 |
| Bombs as reusable/consumable | Missing specific item | Low | High | Medium | Needs destructible/secret rules | Post-Beta Backlog | Chest/secret backlog |
| Potions | Not canonical yet | Low | Medium | Low | Generic reward noise | Post-Beta Backlog | Item backlog |
| Keys | Partial through boss/locks; not broad key economy | Medium | High | Medium | Can overcomplicate rooms | Prototype Later | M130/M131 |
| One-use upgrades | Partial via rewards/cards | Medium | High | Medium | Needs clear category | Beta Update | M130 |
| Card deck near player/right side | Cards exist; hand UI missing | Medium | High | High | Grabbing/throwing implies new interaction model | Prototype Later | Post-beta card UX |
| Movement | Existing | Critical | Critical | Low | Must remain reliable | Beta Core | M134 |
| Aiming | Existing, M111 work | Critical | Critical | Medium | Mouse/gamepad/visionOS differences | Beta Core | M134 |
| Dodge/roll | Partial, M112 tests exist | High | High | Medium | Invulnerability readability | Beta Update | M134 |
| Interact | Existing | Critical | High | Low | Prompt clarity | Beta Core | M134 |
| Melee attack | Existing | Critical | Critical | Medium | Must feel primary | Beta Core | M134 |
| Long-distance attack | Existing | High | High | Medium | Must not replace melee | Beta Update | M134 |
| Reusable item slot on L1 | Existing concept/input partial | High | High | Medium | Controller mapping conflicts | Beta Update | M127/M134 |
| Card slot on L2 | Existing concept/input partial | High | High | Medium | Controller mapping conflicts | Beta Update | M127/M134 |
| Pause menu | Existing | High | High | Low | None significant | Beta Update | M135 |
| Map enlarge on touchpad press | Missing | Medium | Medium | Medium | Platform input variance | Prototype Later | Post-beta UI polish |
| Jump | Not core | Low | Medium | High | Requires vertical room design | Cut/Defer for beta | Post-beta movement prototype |
| Consume action | Missing/partial fiction | High if biomass enters | High | Medium | Needs corpses, timing, UI | Prototype Later | M128 only if scoped |

### 6.3 Combat, Weapons, Shields, And Feel

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Ranged weapon consumes souls instead of ammo | Souls exist; ammo concept not soul-bound | Medium | High | Medium | Undermines soul economy if always-on | Prototype Later | M128/M134 experiment |
| Breakable/fixable shields | Guard/shield systems exist | Medium | High | High | Durability adds repair economy | Post-Beta Backlog | Equipment backlog |
| Light/medium/heavy attack types | Existing light/heavy; medium not central | Medium | High | Medium | Complexity before feel | Beta Update for light/heavy only | M134 |
| Attack tiers with knockback | Knockback systems exist | High | High | Medium | Tuning readability | Beta Update | M134 |
| Attack tiers with shield reliability | Shield behavior exists | Medium | High | Medium | Hard to communicate | Prototype Later | Post-beta shield pass |
| Two weapon types: melee and shooting | Existing | Critical | Critical | Low | Needs visual clarity | Beta Core | M134 |
| Less pure magic/fire, more guns/organic/projectiles | Partially aligned | Medium | High | Medium | Avoid losing cosmic fantasy | Beta Update | M132/M134 |
| Magic as possible combat flavor | Existing enemy magic/soul enemies | Low | Medium | Medium | Too broad for beta | Post-Beta Backlog | Enemy/action backlog |
| Dark Souls challenge and soul tone | Existing combat direction | Critical | Signature | Medium | Avoid slow/clunky combat | Beta Core | M134 |
| Isaac-like room pressure | Existing branch/room design | Critical | Signature | Medium | Avoid copying instead of owning | Beta Core | M131 |
| Doom 3 dark sci-fi tension | Art/narrative direction | Medium | High | Medium | Too dark can hurt readability | Beta Update | M132 |
| Vertical play area with stairs/ladders/platforms | Mostly missing | Low | High | Very High | Changes navigation, camera, rooms | Cut/Defer for beta | Post-beta movement/room prototype |
| Vertical projectiles | Partially feasible with current projectiles | Medium | Medium | Medium | Needs line-of-effect clarity | Prototype Later | Combat lab |

### 6.4 Currencies And Economy

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Coins/run money | Existing | High | High | Low | Needs pacing | Beta Core | M130 |
| Biomass from bosses and corpses | Missing | Medium | Signature | High | Competes with souls and needs corpse lifetime | Prototype Later | M128 |
| Biomass usable in hub shop during run | Missing | Medium | High | High | Requires hub economy split | Prototype Later | M128/M129 |
| Corpses consumable for 5 seconds | Missing | Medium | Signature | High | Corpses, timer, animation, UI | Prototype Later | M128 |
| Souls for permanent unlocks | Existing partial banked souls | Critical | Signature | Medium | Must avoid stat grind | Beta Core | M128/M129 |
| Souls unlock worlds, bosses, items, skills, characters, modes, collectibles | Partial concept | Medium | Signature | High | Massive unlock matrix | Post-Beta Backlog | Meta backlog |
| Souls unlock 3D model viewer | Missing | Low | Medium | High | Non-core beta feature | Post-Beta Backlog | Archive module backlog |
| Extract souls in hub and end run/death choice | Missing but aligned | High | Signature | Medium | Needs clear consequence | Beta Update | M128/M129 |
| Risk continuing run with souls | Partial through run persistence | Critical | Signature | Medium | Must be fair | Beta Core | M128 |
| Black Orb per successful run | Missing | Low | Medium | Medium | Adds another currency too early | Post-Beta Backlog | Completion economy |
| Generic resources from monsters | Missing | Low | Medium | Medium | Vague, likely clutter | Cut/Defer | Revisit only with crafting need |
| Souls sent to heaven/unlocked content | Narrative-compatible | Medium | High | Medium | Tone may become too moralized | Prototype Later | Narrative/economy backlog |

### 6.5 Rarity, Items, Cards, And Rewards

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Common rarity | Partial reward definitions | Medium | High | Medium | Needs consistent tuning | Beta Update | M130 |
| Uncommon rarity | Partial | Medium | High | Medium | Needs content volume | Beta Update | M130 |
| Rare rarity | Partial | Medium | High | Medium | Needs content volume | Beta Update | M130 |
| Epic rarity | Partial | Low | Medium | Medium | Too little beta content | Post-Beta Backlog | Item expansion |
| Legendary rarity | Partial | Low | Medium | Medium | Beta power spikes | Post-Beta Backlog | Item expansion |
| World-based rarity bands | Missing explicit policy | Medium | High | Medium | Needs many rewards to matter | Beta Update as design only | M130/M132 |
| Cursed world rarity upgrade | Missing | Medium | High | Medium | Needs cursed world rules | Post-Beta Backlog | Optional-risk backlog |
| Keys: normal/gold/rusty | Partial lock concepts | Medium | High | Medium | Key economy scope | Prototype Later | M130/M131 |
| Cards as single-use or run-permanent modifiers | Existing cards partial | High | High | Medium | Category confusion | Beta Update | M130 |
| Card transforms room | Missing | Low | High | High | Big room-state mutation | Prototype Later | Post-beta card set |
| Card transforms enemies | Missing | Low | High | High | Encounter balance risk | Prototype Later | Post-beta card set |
| Card transforms whole level | Missing | Low | High | Very High | Too much scope | Cut/Defer for beta | Full-game anomaly backlog |
| Card returns player to hub | Missing but aligned | Medium | High | Medium | Can bypass risk if free | Prototype Later | Emergency extraction design |
| Card heals player | Existing-like Mend Card | High | High | Low | Must be scarce | Beta Core | M130 |
| Card kills all monsters | Missing | Low | Medium | Medium | Undercuts combat | Cut/Defer for beta | Maybe late-game rare card |
| Card reveals map | Missing | Medium | Medium | Medium | Good utility if minimap ready | Post-Beta Backlog | Map item backlog |
| Beer as single-use item | Missing | Low | Medium | Low | Tone must fit | Post-Beta Backlog | Drunk NPC chain |
| Beer heals 3 hearts | Missing | Medium | Medium | Low | Duplicates healing unless tied to NPC | Prototype Later | Drunk NPC prototype |
| Beer can be given to Drunk NPC | Missing | Medium | High | Medium | Needs NPC encounter | Prototype Later | M133 candidate |
| Magic Bomb | Missing | Medium | High | Medium | Needs destruction/secret rules | Post-Beta Backlog | Secret room backlog |
| Shield as multi-use item | Partial shield systems | Medium | High | Medium | Duplicates equipment | Prototype Later | Shield item backlog |
| HP regen multi-use item | Partial via mending charm | Medium | High | Low | Balance risk | Beta Update | M130 |
| Biomass Blocker: corpses do not vanish | Missing | Low | High | Medium | Requires corpse/biomass system | Post-Beta Backlog | Biomass item backlog |
| HP Up always-active | Existing-like reward effects | High | High | Low | Simple and readable | Beta Update | M130 |
| Falling Stars mini-meteors grant abilities | Missing | Medium | High | High | Needs meteor event and ability pool | Prototype Later | Post-beta anomaly rewards |
| Placeholder X item slots | Placeholders only | Low | Low | Low | No design content | Cut/Defer | Fill only after beta |

### 6.6 Chests, Risk, And Rewards

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Normal chest | Existing | Critical | High | Low | Needs pacing | Beta Core | M130 |
| Golden chest | Existing | High | High | Low | Needs reward distinction | Beta Core | M130 |
| Mimic chest | Missing or not beta-visible | Medium | Signature | Medium | Must be readable/fair | Prototype Later | M133 candidate |
| Cards as rewards | Existing partial | High | High | Medium | Too many effects | Beta Update | M130 |
| HP regen/heart rewards | Existing partial | High | High | Low | Healing economy | Beta Core | M130 |
| Shield reward | Partial | Medium | Medium | Medium | Category clarity | Prototype Later | M130 backlog |
| Biomass Chest | Missing | Low | High | Medium | Wait for biomass economy | Post-Beta Backlog | Biomass backlog |
| Souls Chest with consequences | Missing | High | Signature | Medium | Needs explicit risk consent | Beta Update | M130 if soul risk ready |
| Demonic Chest: item plus enemy | Missing | Medium | High | Medium | Can feel punitive | Prototype Later | Optional-risk backlog |
| Corrupted Chest: great item plus gameplay curse | Missing | High | Signature | High | Needs curse clarity | Beta Update small subset | M130 |
| Portal Chest to secret level | Missing | Low | High | High | Requires secret level pipeline | Post-Beta Backlog | Secret content |
| Double reward after double boss | Missing | Low | Medium | Medium | Needs double boss balance | Post-Beta Backlog | Boss variant backlog |
| Second treasure room on double-boss trigger | Missing | Low | Medium | Medium | System coupling | Post-Beta Backlog | Boss variant backlog |

### 6.7 Rooms, Levels, Hubs, And Progression

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Safe start room | Existing concept | Critical | High | Low | Must stay readable | Beta Core | M131 |
| Normal combat room | Existing | Critical | Critical | Low | Needs content quality | Beta Core | M131 |
| Treasure room | Existing concept | High | High | Medium | Reward pacing | Beta Core | M131 |
| Boss room | Existing | Critical | Critical | Medium | Boss polish | Beta Core | M131 |
| Shop as room or hub object | Existing hub shop direction | High | High | Medium | Decide ship/hub role | Beta Update | M129/M131 |
| Survival room with endless spiders for a timer | Missing | Medium | High | Medium | Spawn pacing | Prototype Later | M131 candidate if cheap |
| Defend object room | Missing | Low | High | High | AI targeting/object health scope | Post-Beta Backlog | Room goal backlog |
| Wave room | Partial through encounters | Medium | High | Medium | Could drag pacing | Prototype Later | M131 candidate |
| Lever room | Missing | Medium | High | Medium | Needs interactables | Prototype Later | Room goal backlog |
| Trap traversal room | Partial hazards | Medium | High | Medium | Needs readable hazards | Prototype Later | Room goal backlog |
| Find/destroy mimic disguised as rock | Missing | Low | High | High | Requires disguise readability | Post-Beta Backlog | Chameleon/mimic backlog |
| Timed kill for unique reward | Missing | Medium | High | Medium | Good special encounter | Prototype Later | M133 candidate |
| Double-boss room | Boss framework exists | Low | Medium | High | Very high balance risk | Post-Beta Backlog | Boss variant backlog |
| Secret cave via bombed wall | Missing | Medium | Signature | High | Needs destructible walls/map rules | Post-Beta Backlog | Secret room backlog |
| Bonus room via platform up/down | Missing | Low | Medium | High | Vertical transition scope | Post-Beta Backlog | Vertical backlog |
| Life/Death Room 50/50 | Missing | Medium | Signature | High | Random punishment risk | Prototype Later | Optional-risk backlog |
| Levels made of rooms | Existing branch abstraction | Critical | Critical | Low | Naming consistency | Beta Core | M131 |
| Doors lock until room goal complete | Existing core | Critical | Critical | Low | Needs visual clarity | Beta Core | M127/M131 |
| Hub offers 3 levels/portals | Existing direction | Critical | High | Medium | Ship vs temporary hub distinction | Beta Core | M129 |
| Hub shop | Existing direction | High | High | Medium | Currency clarity | Beta Update | M129 |
| Return to hub after starter level and bosses | Existing/partial | Critical | High | Medium | Must feel intentional | Beta Core | M129 |
| Emergency return via card/ability | Missing | Medium | High | Medium | Can trivialize risk | Prototype Later | Extraction backlog |
| World contains starter level, hub, 3 levels, next-world unlock | Partial direction | High | High | High | Full structure may exceed beta | Beta Update as design target | M129/M132 |
| Optional fourth secret level | Missing | Low | High | High | Extra content | Post-Beta Backlog | Secret world backlog |
| Dark worlds via special item | Missing | Medium | Signature | High | Needs alternate content | Post-Beta Backlog | Dark-world backlog |
| Alternate worlds via soul shop | Missing | Medium | Signature | High | Meta progression scope | Post-Beta Backlog | Alternate-world backlog |

### 6.8 Biomes And Worlds

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Ancient Egypt / Sunken Cartouche | Existing M50-aligned | High | High | High | Asset needs | Beta candidate | M132 |
| Prehistoric Times / Before Teeth | Existing M50-aligned | High | High | High | Enemy/prop identity | Beta candidate | M132 |
| Fallen Future / Rust Choir | Existing M50-aligned | High | High | High | Strong with mechanical enemies | Beta candidate | M132 |
| End of Times / Last Hour | Existing M50-aligned | Low | High | High | Too late-game for beta | Post-Beta Backlog | World backlog |
| The Abyss / Blind Deep | Existing M50-aligned | Medium | High | High | Needs strong visuals | Post-Beta Backlog unless chosen | M132/backlog |
| Interior of collapsed star/moon craters | Concept aligns with Hollow Star | Medium | Signature | High | Needs unique art language | Prototype Later | Biome prototype |
| Time loop world | Missing | Low | High | Very High | Systems/narrative heavy | Post-Beta Backlog | World anomaly backlog |
| Wreck field / spaceship graveyard | Partial via sci-fi art direction | Medium | High | High | Asset heavy | Post-Beta Backlog | Biome backlog |
| Hive planet that reacts | Missing | Medium | Signature | Very High | Dynamic environment/boss scope | Prototype Later | Boss/biome prototype |
| Zero gravity/upside-down world | Missing | Low | Signature | Very High | Movement/navigation rewrite | Cut/Defer for beta | Long-term experiment |
| Empty metropolis | Missing | Low | High | Very High | Large asset scope | Post-Beta Backlog | Biome backlog |
| Machine city / steampunk | Partially overlaps mechanical enemies | Medium | High | High | Visual identity clash with sci-fi | Post-Beta Backlog | Biome backlog |
| Radioactive sea | Missing | Low | Medium | High | Hazard/art scope | Post-Beta Backlog | Biome backlog |
| Dungeons with levers/events | Missing | Medium | High | Medium | Interactables needed | Prototype Later | Room goal backlog |
| Tribute planets to games/films | Missing | Low | Low | Medium | IP/tone risk | Cut/Defer | Avoid direct references |
| Loop portal world with one true portal | Missing | Medium | Signature | High | Can frustrate players | Prototype Later | Anomaly world backlog |
| Heaven-Hell world | Existing Choir Below direction | Medium | High | High | Easy to overdo visually | Post-Beta Backlog unless selected | M132/backlog |
| Feudal Japan | Missing | Low | Medium | High | Full biome content | Post-Beta Backlog | Historical biome backlog |
| Medieval Europe | Partially Black Keep-aligned | Medium | High | High | Needs visual subset | Beta candidate only if chosen | M132 |
| Roman Empire | Missing | Low | Medium | High | Extra historical biome | Post-Beta Backlog | Historical biome backlog |
| Ice tundra | Missing | Low | Medium | High | Terrain/hazard scope | Post-Beta Backlog | Weather biome backlog |
| Sandstorm desert | Missing but red land/ancient world compatible | Medium | Medium | Medium | Visibility risk | Prototype Later | Biome FX backlog |
| Eternal storm | Missing | Low | Medium | High | Lighting/hazard scope | Post-Beta Backlog | Weather biome backlog |
| Volcanic land | Missing | Low | Medium | High | Lava/hazard scope | Post-Beta Backlog | Weather biome backlog |
| Placeholder world slots X | Placeholder only | Low | Low | Low | No design content | Cut/Defer | Fill later |

### 6.9 NPCs, Companions, And Special Encounters

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Companions rescued and following/helping | Missing | Medium | Signature | Very High | Full AI, UI, balance | Post-Beta Backlog | Companion system |
| Four-legged/minion-only play idea | Missing | Low | High | Very High | Requires companion system | Post-Beta Backlog | Challenge backlog |
| Escaping creature gives rare reward if killed fast | Missing | High | High | Medium | Good readable special encounter | Prototype Later | M133 candidate |
| Dark Soul haunts after cursed chest | Partial enemy/fantasy overlap | Medium | High | High | Can feel unfair | Prototype Later | M133 or post-beta |
| Drunk NPC asks for beer | Missing | Medium | High | Medium | Tone must fit | Prototype Later | M133 candidate |
| Drunk gives great reward or not | Missing | Medium | High | Medium | Randomness must feel fair | Prototype Later | M133 candidate |
| Drunk house/key secret | Missing | Low | Medium | High | Quest/secret chain scope | Post-Beta Backlog | NPC quest backlog |
| Alien hidden in meteor | Missing | Medium | High | High | Requires meteor/secret logic | Prototype Later | Post-beta special encounter |
| Meteor can be hit, bounces, needs strong weapon | Missing | Low | Medium | Medium | Obscure without clues | Post-Beta Backlog | Alien encounter |
| Soul Eater sells items for souls | Enemy exists; NPC/shop role missing | High | Signature | Medium | Soul economy clarity required | Beta candidate | M133/M129 |
| Special NPC takes souls/money until max, becomes beast to kill, unlocks world/character | Missing | Low | Signature | Very High | Big quest/boss/unlock chain | Post-Beta Backlog | Major NPC arc |
| Important NPC category | Placeholder | Low | Medium | Low | Needs actual roles | Cut/Defer until named | NPC bible |
| Support NPC category | Placeholder | Low | Medium | Low | Needs actual roles | Cut/Defer until named | NPC bible |
| Encounter NPC category | Partially filled | Medium | High | Medium | Keep tiny for beta | Beta Update | M133 |
| Special NPC category | Partially filled | Medium | High | Medium | Keep tiny for beta | Beta Update | M133 |

### 6.10 Enemies And Bosses

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Swallower cocoon eats player | Missing | Low | Signature | Very High | Instant kill/secret transport fairness | Post-Beta Backlog | Special enemy backlog |
| Swallower kills below 8 hearts | Missing | Low | Medium | Medium | Hard threshold feels arbitrary | Cut/Defer current rule | Redesign if used |
| Swallower transports above 8 hearts to underworld | Missing | Low | Signature | Very High | Requires secret world | Post-Beta Backlog | Underworld concept |
| Mimic enemy/chest | Missing/partial concept | Medium | Signature | Medium | Fair visual tell needed | Prototype Later | M133 candidate |
| Chameleon disguises as environment | Missing | Low | High | High | Detection/readability risk | Post-Beta Backlog | Special enemy backlog |
| Escapist enemy | Missing | High | High | Medium | Good beta special if scoped | Prototype Later | M133 candidate |
| Dark Soul enemy/event | Partial through ghost/soul enemies | Medium | High | High | Can punish cursed chest too much | Prototype Later | Optional-risk backlog |
| Soul Eater enemy | Existing enemy archetype | Medium | High | Medium | NPC vs enemy identity | Beta Update | M133 |
| Demonic enemy from chest | Missing | Medium | High | Medium | Needs optional-risk clarity | Prototype Later | M130/M133 |
| Spiders as room pressure | Existing small enemy foundations likely | Medium | Medium | Low | Avoid overuse | Beta Update | M131 |
| Boss placeholder list X | Boss framework exists but PDF unnamed | Low | Low | Low | No content to implement | Cut/Defer | Use M53 boss roster |
| Portals with demon/wave challenges | Missing/partial challenge direction | Medium | High | Medium | Must not distract from branch loop | Prototype Later | M131/M133 |

### 6.11 Challenges, Achievements, And Unlocks

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Challenges as special runs with modified rules | Existing challenge mode | High | High | Medium | Too many variants | Beta Update | M135 |
| Strzelec: bow only | Existing challenge style feasible | Medium | High | Low | Needs bow balance | Beta Update | M135 |
| One life, no regeneration | Existing rule feasible | Medium | High | Low | Hard but clear | Beta Update | M135 |
| Every room mini-boss, boss room has two bosses | Missing | Low | Medium | High | Balance chaos | Post-Beta Backlog | Challenge backlog |
| Fog of war, hidden minimap, 2x level size | Missing | Medium | High | Medium | UI/map work | Prototype Later | Challenge backlog |
| Speedrun, time 2x | Partial challenge feasible | Medium | Medium | Medium | Time systems | Prototype Later | Challenge backlog |
| Tank: 2x HP/damage, 2x slower | Feasible from stats | Medium | Medium | Low | Movement feel can suffer | Prototype Later | Challenge backlog |
| Four Legged Friends: minions only | Missing | Low | High | Very High | Needs companions | Post-Beta Backlog | Companion challenge |
| Armageddon meteor falls | Missing | Low | High | High | Needs meteor hazard | Post-Beta Backlog | Anomaly challenge |
| Clean Expedition: no items/upgrades/drops | Feasible | Medium | Medium | Low | Can feel empty | Prototype Later | Challenge backlog |
| Big world, small player | Missing | Low | Medium | High | Camera/collision/rooms | Post-Beta Backlog | Challenge backlog |
| Arachnophobia: all spiders | Feasible if spider roster stable | Medium | Medium | Low | Repetitive | Prototype Later | Challenge backlog |
| Short life: half HP, +1 after boss | Feasible | Medium | Medium | Low | Good simple rule | Prototype Later | Challenge backlog |
| Boss-related achievements | Missing | Medium | High | Medium | Needs achievement infra | Post-Beta Backlog | Achievement backlog |
| Chmielowy Sen: give beer 10 times | Missing | Low | Medium | Medium | Needs Drunk NPC | Post-Beta Backlog | Drunk chain |
| Unlock passed-out drunk | Missing | Low | Medium | Medium | NPC variant scope | Post-Beta Backlog | Drunk chain |
| Unlock non-drinker who gives beer | Missing | Low | Medium | Medium | NPC variant scope | Post-Beta Backlog | Drunk chain |
| Rescue 5 aliens | Missing | Low | High | High | Needs Alien encounter | Post-Beta Backlog | Alien chain |
| Unlock Falling Stars | Missing | Low | High | High | Needs meteor rewards | Post-Beta Backlog | Alien chain |
| Cleaner: kill Dark Soul with special item | Missing | Low | High | High | Needs Dark Soul/special item | Post-Beta Backlog | Dark Soul chain |
| Unlock dark energy projectile weapon | Missing | Low | High | Medium | Needs weapon/content | Post-Beta Backlog | Dark Soul chain |
| Wet Socks: hit by projectile while jumping over pod | Missing | Low | Low | High | Requires jump and odd trigger | Cut/Defer | Revisit if jump exists |
| You Suck: lose 20 games in row | Missing | Low | Medium | Low | Negative achievement tone | Post-Beta Backlog | Achievement backlog |
| Worst Behind Me: escape Death Room | Missing | Low | Medium | Medium | Needs Death Room | Post-Beta Backlog | Life/Death backlog |
| Live Happily: find three Life Rooms in one run | Missing | Low | Medium | Medium | Needs Life Rooms | Post-Beta Backlog | Life/Death backlog |
| Characters unlocked by puzzle pieces, one per run/random room | Missing | Low | High | High | Meta collection grind | Post-Beta Backlog | Character unlocks |
| Items unlocked by achievements | Partial concept | Medium | High | Medium | Needs achievement infra | Post-Beta Backlog | Unlock matrix |
| Alternate worlds unlocked by tasks/bosses | Missing | Medium | High | High | Scope heavy | Post-Beta Backlog | Alternate worlds |

### 6.12 Art, Pipeline, Production, And Differentiation

| Idea | Current Status | Beta Value | Full-Game Value | Cost | Risk | Recommendation | Target |
|---|---|---:|---:|---:|---|---|---|
| Distinguish from Isaac via 3D, story, art, melee/shooting, hubs, body consumption | Aligned with GDD | Critical | Signature | Medium | Must be visible, not just written | Beta Core | M126-M135 |
| Distinguish from Diablo via stylized arcade/alien direction | Aligned | High | High | Medium | Visual indecision | Beta Update | M132 |
| Less realistic, more readable stylized art | Aligned | High | High | Medium | Asset consistency | Beta Update | M132 |
| Decide realism/fantasy/horror/blocks/pixel-art | GDD chooses stylized cosmic 3D | Critical | High | Low | Avoid reopening settled direction | Beta Core | M126 |
| Rafal delivers 1-3 UV mapped blocks | Historical task, likely superseded by ArtPass pipeline | Medium | Medium | Low | Needs current asset standards | Beta Update | M132/art intake |
| Floor/edge/wall texture tests | Aligned with biome pass | High | High | Medium | Needs scale/material contract | Beta Update | M132 |
| Asset sharing solution such as Drive | Production task | Medium | High | Low | Version-control confusion | Beta Update | M126 production notes |
| Document art tools | Partially documented | Medium | Medium | Low | Keep current | Beta Update | M126 |
| Martin implements portals after bosses to hub | Existing/partial direction | Critical | High | Medium | Core loop | Beta Core | M129 |
| Place art blocks in start board corners as debug entities | Historical debug task | Low | Low | Low | Debug-only | Cut/Defer | Superseded by Developer Lab |
| Spatial/model dimensions guideline | Existing docs/pipeline direction | High | High | Low | Must stay current | Beta Update | M126/M132 |
| APIs/tools for VisionOS development | Existing platform docs partial | Medium | Medium | Medium | Unity vs RealityKit confusion | Beta Update | M126 production notes |
| RealityKit/Reality Composer Pro pipeline | Conflicts with current Unity project | Low | Low | High | Wrong production stack | Cut/Defer | Keep Unity as canonical |
| Xcode/simulator notes | Useful for platform builds only | Medium | Medium | Low | Not main implementation environment | Post-Beta Backlog | Platform QA notes |
| Codex/ChatGPT for planning and implementation | Existing workflow | High | Medium | Low | Must not replace review | Beta Update | M126 |
| Higgsfield/Meshy/Hitem3D/Suno | Useful reference/asset tools | Medium | Medium | Medium | Generated asset consistency/legal/quality | Prototype Later | Art pipeline notes |
| Zbrush/Blender/Substance | Rafal pipeline | High | High | Low | None significant | Beta Update | M132 |
| Existing document library inventory | Partial in repo | Medium | High | Low | Duplicates must be merged | Beta Update | M126 |

## 7. Beta Scope Decision

### Beta Core

Required before beta slice lock:

- Ship-Soul loop is playable and understandable.
- Souls have visible run value and at least one permanent/ship use.
- Boss clear returns or advances the player through the loop.
- HUD shows health, current run economy, item/card state, and minimap without debug clutter.
- Normal/golden rewards and sparse room pacing are readable.
- At least one shop/choice surface exists in ship or hub.
- 2-3 beta biomes/world identities are selected and documented.
- 1-2 special encounters are selected, not a whole NPC network.
- Challenge mode remains curated and limited.

### Beta Exclusions

Do not implement for beta unless a later review explicitly reopens scope:

- Full companion AI.
- Full biomass economy with corpse consumption.
- Zero gravity/upside-down worlds.
- Large character unlock puzzle system.
- Many new biomes.
- Full achievement unlock matrix.
- Secret underworld from Swallower.
- Deep alternate/dark world systems.
- Full deck/throw-card interaction model.
- Direct tribute planets based on other IP.

## 8. Proposed Implementation Roadmap

### M126: Master Design Lock

Goal: finalize the GDD, idea catalogue, beta scope, and full-vision backlog before runtime changes.

Key outputs:

- Master design document and PDF handoff.
- Accepted beta scope list.
- Deferred full-vision backlog.
- Decision log entries for soul/biomass/ship direction.
- Production notes that Unity is canonical and RealityKit notes are legacy context only.

Acceptance:

- Every PDF idea has a sector and recommendation.
- Beta scope is small enough for a 6-9 month slice.
- No unnamed placeholder X items/worlds are treated as implementation tasks.

### M127: HUD + Run Readability Pass

Goal: make normal gameplay readable without debug text.

Key outputs:

- Health hearts/avatar display.
- Run souls, coins, active item, card/consumable, and optional key slots.
- Minimap visual pass and enlarged-map design decision.
- Debug fields hidden from normal gameplay.
- Pickup/reward reveal language aligned with M42/M58.

Acceptance:

- A new player can identify health, current room, reward state, and available item/card.
- HUD works across standard and platform presentation modes.
- Developer/debug overlays remain available only through explicit debug routes.

### M128: Soul + Biomass Economy Design Pass

Goal: clarify economy roles before adding more currencies.

Key outputs:

- Souls: run pickup, bank/extract, death risk, ship use.
- Coins: run/shop role.
- Biomass: design-only or tiny prototype decision.
- Black Orb and generic resources deferred.
- Soul-spending UI copy for ship repair/unlocks.

Acceptance:

- Souls do not feel like duplicate coins.
- Player understands whether continuing a run risks souls.
- Biomass is either explicitly deferred or prototyped in one contained test path.

### M129: Ship-Soul Loop Greybox

Goal: make the ship the beta's structural heart.

Key outputs:

- Derelict Sanctuary greybox start/return route.
- Portal Engine or equivalent run launch device.
- Basic ship repair/unlock interaction using souls.
- Boss clear return or extraction flow.
- Memory/document clue surface.

Acceptance:

- Player can start at ship, enter run, clear a boss, return, and spend/see soul progress.
- Existing menus remain fallback, but ship communicates the fantasy.
- The ship is safe, eerie, and useful, not just a decorative menu.

### M130: Reward + Chest Risk Pass

Goal: make rewards meaningful and optional risk readable.

Key outputs:

- Normal and golden chest tuning.
- Reward rarity policy for beta.
- One optional-risk chest prototype decision: corrupted, soul, mimic, or demonic.
- Healing/HP Up/card reward review.
- Deferred chest backlog documented.

Acceptance:

- Ordinary room rewards remain sparse.
- Treasure/boss/shop rewards feel more meaningful than ordinary rooms.
- Risk chest uses explicit player consent and a readable consequence.

### M131: Room Type Expansion Lock

Goal: choose beta room types and stop the room-goal explosion.

Key outputs:

- Beta room whitelist: safe start, combat, treasure, boss, shop/hub, maybe one challenge room.
- Room-goal backlog for waves, survival, levers, traps, defend object, secret cave, life/death.
- Door lock/unlock readability pass.
- Branch/minimap role clarity.

Acceptance:

- Every beta room type has a clear goal and reward expectation.
- Deferred room types are recorded but not required for beta.
- Room flow supports the ship-soul loop.

### M132: Biome + World Selection Lock

Goal: select 2-3 beta worlds and park the rest.

Recommended beta candidates:

- Prehistoric/Before Teeth for creature identity.
- Ancient Egypt/Sunken Cartouche for readable historical-fantasy identity.
- Fallen Future/Rust Choir for mechanical enemies and ship contrast.

Alternative candidate:

- Black Keep/medieval if art assets make it faster than one of the above.

Key outputs:

- Beta biome whitelist.
- Visual requirements for floors, walls, portals, doors, chests, props, enemy color/silhouette.
- Full-vision biome backlog.
- ArtPass asset acceptance checklist update.

Acceptance:

- Each selected beta biome has a distinct material/palette/prop plan.
- No more than 3 biomes are treated as beta scope.
- Tribute/IP-reference planets are excluded.

### M133: NPC/Special Encounter Prototype Set

Goal: add one or two memorable encounters without building a full quest system.

Recommended candidates:

- Soul Eater as soul-price shop/encounter.
- Escapist as timed reward creature.
- Mimic as chest risk if chest visual tells are ready.
- Drunk NPC only if beer tone fits and implementation stays simple.

Key outputs:

- Selected special encounter list.
- Reward/risk rules.
- Art requirements.
- Deferred NPC/companion backlog.

Acceptance:

- Special encounters add identity without blocking the core loop.
- Each encounter has one clear interaction and one clear outcome.
- Companions and deep NPC quest chains remain post-beta.

### M134: Combat Feel + Control Coherence

Goal: make the action set feel intentional.

Key outputs:

- Melee-first tuning.
- Ranged support tuning.
- Guard/roll/dodge readability review.
- Knockback and attack tier review.
- Controller/mouse/keyboard mapping pass.
- Consume and jump decisions confirmed as beta or deferred.

Acceptance:

- Melee is satisfying and understandable.
- Ranged is useful but not dominant by default.
- Defensive actions have clear timing and feedback.
- Control sheet matches the actual build.

### M135: Beta Slice Lock + PDF Handoff

Goal: freeze beta content and hand off a testable slice.

Key outputs:

- Beta content whitelist.
- Known issues.
- Tester route.
- Updated PDF handoff.
- Whole-game audit report.
- QA checklist for ship, run, boss, reward, return, save/continue, challenge, Developer Lab, and platform scenes.

Acceptance:

- Someone outside implementation can launch, play, test, and report.
- The slice has a clear beginning, loop, boss moment, reward moment, and return-to-ship moment.
- Scope is frozen except bug fixes and obvious polish.

## 9. Implementation Dependencies

| Dependency | Needed For | Notes |
|---|---|---|
| Current HUD/minimap/door readability work | M127 | Finish and validate before adding new UI surfaces. |
| Existing save/run persistence | M128/M129 | Soul extraction and ship unlocks depend on reliable state. |
| Existing portals/hub/branch flow | M129/M131 | Reframe around ship and temporary hubs. |
| Existing reward/chest systems | M130 | Expand carefully from normal/golden chest base. |
| Existing challenge mode | M135 | Curate rather than expand broadly. |
| ArtPass wrapper and Developer Lab | M132/M133 | Required for visual safety and asset intake. |
| Whole-game audit scaffold | M135 | Convert planning scaffold into real beta evidence. |

## 10. Full-Vision Backlog

Preserve these as future design strengths, but do not force them into beta:

- Full companion system with rescued creatures.
- Drunk NPC quest chain with beer, house key, variants, and achievements.
- Alien meteor rescue chain and Falling Stars ability rewards.
- Dark Soul curse chain and dark-energy unlock.
- Swallower underworld secret world.
- Chameleon disguise enemy.
- Portal chests and secret levels.
- Life/Death Rooms.
- Double-boss branch variants.
- Dark worlds and alternate worlds.
- Completion currency such as Black Orb.
- Character puzzle-piece unlocks.
- Full achievement unlock matrix.
- Zero-gravity/upside-down world.
- Time-loop world.
- Hive planet with reactive environment boss.
- Empty metropolis, radioactive sea, weather worlds, Roman/Japan historical worlds.
- Full 3D archive/model viewer.
- Deep ship module network beyond the beta repair/unlock loop.

## 11. Cut Or Deferred Without Active Design

These should not become tasks until redesigned:

- Placeholder X item/world/boss slots.
- Direct tribute planets to other games or films.
- RealityKit as the main implementation pipeline for this Unity project.
- Generic "resources" currency without a concrete use.
- Card that kills all monsters as a common/systemic effect.
- Jump-based achievement before jump is part of the core game.
- The joke identity names from the PDF.

## 12. Review Checklist Before Runtime Work

Use this checklist before committing implementation updates based on this document:

- Does the task strengthen the Ship-Soul Loop?
- Does it improve combat, soul meaning, rewards, ship progression, or readability?
- Is it in M126-M135, or is it explicitly post-beta?
- Can it be tested in a deterministic route?
- Does it avoid adding a broad system before the beta loop is stable?
- Does it preserve ArtPass visual-only safety?
- Does it keep debug/developer surfaces out of normal gameplay?
- Does the player understand the risk before accepting it?

## 13. Source Coverage

The following PDF idea groups are covered:

- Game idea notes and next-step ideas.
- Opening narrative, amnesia, portal, hidden identity, and document reveal.
- HUD, health, currencies, minimap, active items, and card deck.
- Rarity tiers and world/cursed-world rarity distribution.
- World/biome concepts.
- Consumables, cards, single-use, multi-use, always-active, special items, Falling Stars.
- Biomass, souls, Black Orb, resources, extraction choice.
- Normal/golden/mimic/biomass/soul/demonic/corrupted/portal chests.
- Rafal/Martin production tasks and asset pipeline notes.
- Isaac/Diablo differentiation notes.
- Unlock categories.
- NPCs, special encounters, companions.
- Special enemies and boss placeholders.
- Challenge and achievement ideas.
- Room types and gameplay layers.
- World/hub/level structure.
- Storytelling questions and black-hole/ship-machine concept.
- Location category lists.
- Control/action list.
- Scene I and optional story devices.

## 14. Final Position

The PDF contains enough material for a full roguelite, but the beta should not try to become the full roguelite. The strongest course is:

1. Lock this master design.
2. Finish readability/HUD and existing stability work.
3. Make souls meaningful.
4. Build the ship return/unlock loop.
5. Select a tiny content subset.
6. Add one memorable risk or special encounter.
7. Lock the beta slice and test it hard.

That path keeps the weirdness alive without letting it eat the schedule.
