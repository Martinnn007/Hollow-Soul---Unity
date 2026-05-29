# M130: Reward + Chest Risk Pass

M130 is a runtime and lock-artifact milestone. It keeps the beta reward economy lean and readable, while adding one optional-risk endpoint prototype.

## Decisions

- ordinary rooms stay sparse: coins, HP refill, normal/golden chest, or nothing.
- Normal Chests remain practical rewards with coins or HP refill.
- Golden Chests remain stronger rewards with coins, healing, or cards.
- Corrupted Chest rooms are rare extra branch-ending leaves.
- Corrupted Chest rooms roll at 10% on normal world-loop procedural branches.
- Corrupted Chest rooms never replace boss, secret, or treasure rooms.
- Corrupted Chest rooms prefer the dedicated `corrupted_chest_single_1x1` endpoint room when that template is available.
- The dedicated room is a small shrine-style endpoint with no enemies, clear chest access, altar rocks, and nonblocking corrupted decor.
- The Room Designer exposes a Corrupted Chest designer marker that previews with the corrupted chest prefab role.
- Corrupted Chests use two-step consent before opening.
- Opening a Corrupted Chest grants a curated rare build reward plus coins.
- Opening a Corrupted Chest applies -1 max HP for the rest of the run.

## Runtime Copy

- Warning: `Open Corrupted Chest? Gain a rare reward. Lose 1 max HP for this run. Interact again to confirm.`
- Reward result: `Corrupted Chest: <reward> gained. -1 max HP for this run.`

## Deferrals

- No Soul Chest runtime work.
- No Mimic Chest runtime work.
- No Demonic Chest runtime work.
- No biomass, Black Orb, or generic-resource runtime work.
- No deck UI work.
- No save schema changes.

## Acceptance

- Ordinary room rewards stay sparse and M52-compatible.
- Corrupted rooms appear rarely as terminal optional-risk endpoints.
- Corrupted Chest designer marker exports as `spawn_point_corruptedChest`.
- The player must explicitly confirm the corrupted chest before accepting the risk.
- The reward and -1 max HP consequence are readable in HUD/reveal copy.
- The max HP loss persists through branch hubs and ends with the run.
