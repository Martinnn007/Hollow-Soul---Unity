# M106 Behavior Graph Subgraph Library Report

- Library path: `Assets/_Hollow/Data/EnemyUnityBehavior/M106/Subgraphs`.
- Runtime source of truth remains Hollow combat execution.
- Subgraphs:
  - `notice_player` / `NoticePlayer` / output `FacePlayer`.
  - `investigate_noise` / `InvestigateNoise` / output `Wander`.
  - `flee` / `Flee` / output `Flee`.
  - `circle` / `Circle` / output `MovePreferredRange`.
  - `approach_action_range` / `ApproachActionRange` / output `MovePreferredRange`.
  - `request_attack_slot` / `RequestAttackSlot` / output `StartMeleeAction`.
  - `start_action` / `StartAction` / output `StartMeleeAction`.
  - `recover_hold` / `RecoverHold` / output `Hold`.
- Docs: `Docs/Hollow_M106_Behavior_Graph_Subgraph_Library.md`.
