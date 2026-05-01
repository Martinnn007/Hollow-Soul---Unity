# M66 Developer Lab Scene Authoring Pipeline Report

- Scene folder: `Assets/_Hollow/Scenes/DeveloperLab`
- Content asset: `Assets/_Hollow/Data/DeveloperLab/DeveloperLabContentDefinition.asset`
- Runtime JSON folder: `Assets/_Hollow/Data/Rooms/DeveloperLab`
- Expected scenes: 10
- Game scene wiring: `BranchSessionController` now has a Developer Lab content definition slot.

Manual export remains required. Saving a Unity lab scene does not automatically mutate runtime JSON.

## Local Generation Status

The editor tooling has been added, but batchmode generation was blocked in this environment by Unity licensing:

`Licensing initialization failed after 74.83s`

Run `Hollow/Generation/Generate Milestone 66 Assets` from the Unity Editor to create the scene assets and wire the generated content definition into game scenes.

M66 also includes an editor bootstrapper that auto-creates the 10 missing authoring scenes once after scripts reload, as long as no Developer Lab scenes already exist.
