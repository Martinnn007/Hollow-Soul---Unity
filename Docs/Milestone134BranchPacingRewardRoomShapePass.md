# M134: Branch Pacing + Reward Room Shape Pass

## Summary
M134 adds ordinary non-combat `Reward` rooms to normal world-loop branches as pacing breaks. These rooms are not special encounters, do not run combat, and keep doors open while offering a modest supply-cache reward chance.

## Branch Pacing
- Normal world-loop branches get `1` guaranteed Reward room on the main route.
- Each branch rolls a deterministic `50%` chance for a second Reward room.
- Reward rooms are selected from ordinary origin-to-boss-path body rooms.
- Reward rooms never replace Origin, Boss, Treasure, Secret, Corrupted Chest, Wave, or Special Encounter rooms.
- Reward rooms are excluded from boss-key placement.

## Shape Policy
- Standard body-room placement favors smaller readable rooms.
- Default body shape weights are:
  - `1x1`: `30`
  - `2x1`: `25`
  - `1x2`: `20`
  - `2x2`: `15`
  - `L`: `10`
- `2x2` and L rooms remain present as medium-rarity layout texture.

## Runtime Behavior
- `BranchRoomRole.Reward` is a true non-combat room.
- On entry, Reward rooms immediately mark cleared and reward pending.
- Doors do not lock for Reward-room combat.
- Encounter planning skips Reward rooms, so enemies do not spawn even when reused templates contain enemy markers.
- Hazards, spikes, obstacles, decor, pickups, and reward/chest markers remain active.

## Reward Cache Roll
Reward rooms use the M134 wooden-cache roll:
- `2%` Golden Chest
- `30%` Normal Chest
- `34%` loose coins
- `24%` HP refill
- `10%` nothing

Combat rooms keep the M52 standard-room sparse reward baseline.

## Interfaces
- Reuses `BranchRoomRole.Reward`.
- Adds `BranchPacingPolicy` constants for Reward-room count and body-room shape weights.
- Adds `ProceduralRewardResolver.RollM134RewardRoomCacheReward`.
- No save schema, reward schema, economy schema, chest-kind, biome, or room-template API changes.

## Deferrals
M134 does not add new room roles, new chest kinds, new room art packs, new special encounters, biomass, Black Orb, or new reward schemas.
