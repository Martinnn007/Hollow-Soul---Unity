# M133: NPC/Special Encounter Prototype Set

## Summary
M133 adds two rare optional special encounter prototypes: `Soul Eater` and `Escapist`. These are terminal branch leaves, never required for boss access, and never boss-key eligible. The milestone stays practical: one clear interaction, one clear outcome, and no quest system.

## Branch Rule
- Normal world-loop branches roll a deterministic `15%` chance to add one optional terminal `SpecialEncounter` room.
- The rule applies to prologue and hub-selected normal branches.
- The rule does not apply to spaceship, Developer Lab, or challenge-only scaffolds.
- Special rooms are leaves, not boss-path critical, and do not replace boss, treasure, secret, wave, or corrupted rooms.
- The encounter kind is seeded `50/50`: `SoulEater` or `Escapist`.

## Soul Eater
- Non-hostile NPC/shop prototype.
- Uses current-run `Souls` copy only. Do not use `Unbanked Souls` or `Banked Souls` in this runtime interaction.
- Offers one seeded rare build reward for `10 Souls`.
- If the player has fewer than 10 Souls, show `Need 10 Souls` feedback.
- A successful purchase spends current-run souls and grants the reward through the existing reward application and reveal systems.
- The room does not lock doors and does not require combat.

## Escapist
- Timed escape hunt room.
- Doors lock on entry and a `20s` timer starts.
- One luminous wisp-style Escapist target appears.
- Kill before escape: clear the room, unlock doors, and spawn an existing `Golden Chest`.
- Timer expires: the Escapist leaves, the room clears, doors unlock, and no reward is granted.
- There is no penalty on failure.

## Presentation
- Templates: `special_soul_eater_single_1x1` and `special_escapist_single_1x1`.
- Special rooms inherit the active branch biome.
- Room Designer can preview and edit both templates.
- Minimap/HUD exposes `Special Encounter`, with room labels `Soul Eater` and `Escapist`.
- Escapist combat status shows the active timer.

## Deferrals
Mimic, Drunk NPC, companions, full NPC quest chains, deeper special encounter systems, biomass, Black Orb, and new chest kinds are deferred.

## Interfaces
- Adds `BranchRoomRole.SpecialEncounter`.
- Adds `SpecialEncounterKind.SoulEater` and `SpecialEncounterKind.Escapist`.
- Adds `SpecialEncounterResolver.SpecialEncounterRollPercent = 15`.
- No profile save schema changes.
- No reward schema, economy schema, chest-kind, companion, quest, biomass, or Black Orb changes.
