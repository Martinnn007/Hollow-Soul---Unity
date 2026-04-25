# Hollow Soul - Unity

Unity project for Hollow Soul.

## Current Foundation

Milestone 0 has started under `Assets/_Hollow`.

- `Assets/_Hollow/Scripts` contains the initial assembly split.
- `Assets/_Hollow/Data`, `Art`, `Audio`, `Prefabs`, `Scenes`, `Settings`, `Shaders`, `Tools`, and `Tests` are the canonical Hollow-owned roots.
- `Docs/Milestone0Foundation.md` describes the current foundation and validation command.
- `Docs/Milestone1MenuProfilesAndRouting.md` describes the first menu/profile/platform routing layer.

## Required Setup

- Unity Editor `6000.4.1f1`
- Git
- Git LFS

Install and enable Git LFS before cloning:

```bash
git lfs install
```

Clone the project:

```bash
git clone https://github.com/Martinnn007/Hollow-Soul---Unity.git
cd "Hollow-Soul---Unity"
git lfs pull
```

Open the cloned folder in Unity Hub using Unity `6000.4.1f1`.

## Collaboration Rules

- Commit `Assets/`, `Packages/`, `ProjectSettings/`, `.gitignore`, `.gitattributes`, and `README.md`.
- Do not commit generated folders such as `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, or `UserSettings/`.
- Keep `.meta` files committed. Unity uses them to preserve asset references.
- Pull before starting work and push when a task is complete.
- Prefer separate branches for feature work:

```bash
git checkout main
git pull
git checkout -b feature/short-description
```

- Avoid editing the same scene or prefab at the same time unless you coordinate first. Unity text serialization is enabled, but scene and prefab merges can still be awkward.

## Large Files

This repo uses Git LFS for large binary assets such as textures, models, audio, video, and fonts. If a large asset appears as a tiny pointer file after cloning, run:

```bash
git lfs pull
```
