# Hollow Unity Foundation

`Assets/_Hollow` is the canonical root for Hollow-owned Unity content.

Milestone 0 keeps this intentionally small:

- Runtime code is split into assembly-definition folders under `Scripts/`.
- Authored content will live under `Data/` as ScriptableObjects.
- Gameplay state should stay in serializable C# models and services.
- MonoBehaviours should act as scene, input, UI, or presentation adapters.
- Platform-specific behavior should go through `Hollow.Platform`, not fork gameplay logic.

