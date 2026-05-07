# M108: Designer Debugging Pass

M108 unifies the enemy tuning overlays so Rafal and Pawel can inspect a room without mentally stitching together separate NavMesh, tactical, behavior, and attack-window readouts.

## Unified Enemy Overlay

The `Designer Enemy Debug` switch in the Developer Spawn Menu enables:

- NavMesh path tracing and path status.
- Tactical role, active threat slot, reserved action, and reservation path result.
- Unity Behavior graph state or Hollow behavior-tree node.
- Chosen command/action, AI LOD, current awareness, and awareness reason.
- Blocked/fallback reason from action spacing, NavMesh, locomotion, or scorer cooldowns.
- Current readability phase and active attack window countdown.

## Designer Workflow

Turn on `Designer Enemy Debug`, spawn or play a room, and read each enemy label from top to bottom: current awareness, active attack window, action choice, tactical slot, NavMesh path, blocked reason, and Behavior graph trace. The path line remains the visible movement guide; the text explains why that path and action were chosen.
