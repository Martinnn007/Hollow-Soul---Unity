# Milestone 20: Branch Features, Shops, Secrets, Keys/Locks

M20 adds the first run-continuation layer after the M19 encounter branch. Fresh runs now use `m20_branch_features_v1`, which keeps the seeded macro branch and encounter content while adding one visible debug secret room, a boss-key locked boss path, and an inter-branch hub after boss completion.

Key behavior:
- The farthest eligible non-origin, non-boss, non-secret room grants a `BossKeyPickup` instead of its normal reward.
- The connection into the boss room is locked with `BranchConnectionLockKind.BossKey`.
- Using the key on the boss door consumes it and permanently unlocks that connection for the current branch snapshot.
- The secret room is visible/debug-marked, optional, no-combat, auto-clears on entry, and grants one bonus reward.
- The boss portal opens an in-run inter-branch hub instead of banking souls or returning to the main menu.
- The hub shop spends run-local souls and offers one heal plus two seeded item/card rewards.
- The three next-branch portals derive deterministic seeds from the current seed, branch depth, and portal index.

M20 intentionally does not add small keys, branch-final extraction, final shop art, secrets hidden from debug UI, or meta banking.
