# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project summary

- 2D Unity project for OpenAI Game Builders Seoul 2026.
- Target platform is a browser Web build.
- Project root: `/Users/junho/Repositories/openai-game-builders-seoul-2026`.

## Confirmed environment

- Unity `6000.3.20f1` (Unity 6.3 LTS).
- Universal Render Pipeline 2D (`com.unity.render-pipelines.universal` 17.3.0).
- Input System (`com.unity.inputsystem` 1.19.0).
- Unity Test Framework is installed (`com.unity.test-framework` 1.6.0).
- No first-party assembly definitions or Unity MCP package were found in the repository.

## Structure and architecture

- Runtime scripts are under `Assets/Scripts/` and are currently in the global namespace.
- Player behavior is composed from `PlayerMoveController`, `PlayerAnimationController`, `SpriteFlip`, and `PlayerStatController` on the player prefab.
- Configurable player values are stored in `Assets/ScriptableObjects/PlayerData.asset` through `PlayerData`.
- Per-player mutable combat values are created at runtime through `PlayerRuntimeState`; the ScriptableObject is not mutated.
- Player input uses `PlayerInput` and actions from `Assets/InputSystem_Actions.inputactions`.

## Coding and validation conventions

- Existing scripts use private serialized fields, Unity lifecycle methods, and explicit component references.
- Preserve Unity YAML references and paired `.meta` files when changing assets.
- Validate changed scripts with Unity compilation when the Editor is available; otherwise perform static reference and YAML checks and report the limitation.
- Relevant validation should include runtime behavior and browser constraints when the feature reaches Web build integration.

## Important constraints

- Do not change Unity version, packages, render pipeline, input system, or project settings without an explicit feature need.
- Avoid global singletons and duplicate authoritative runtime state.
- Keep gameplay state in runtime objects/components rather than ScriptableObject assets.

## Unknowns

- Build Settings startup scene and live Unity Editor/Console state were not inspected through a Unity MCP in this session.
- No existing automated test assembly or CI command was found in the repository.

## Source files inspected

- `AGENTS.md`
- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `Assets/Scripts/ScriptableObjects(Scripts)/PlayerData.cs`
- `Assets/Scripts/Player/PlayerMoveController.cs`
- `Assets/Scripts/Player/PlayerAnimationController.cs`
- `Assets/Prefabs/Player.prefab`
- Wiki notes under `../openai-game-builders-seoul-2026.wiki/Notes/`, especially player status, defense, attack, dodge, execution, and soul charge documents.

## Last analyzed commit and date

- Commit: `95e430184ea0bc6e64936d32930f4cb8f23a351e`
- Date: 2026-08-20 (Asia/Seoul)

<!-- unity-onboarding:generated:end -->
