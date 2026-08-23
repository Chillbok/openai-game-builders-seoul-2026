# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project summary

- 2D Unity project for OpenAI Game Builders Seoul 2026.
- Target platform is a browser Web build.
- Project root: `/Users/junho/Repositories/openai-game-builders-seoul-2026`.

## Confirmed environment

- Unity `6000.3.20f1` (Unity 6.3 LTS), confirmed by `ProjectSettings/ProjectVersion.txt`.
- Universal Render Pipeline 2D, using `com.unity.render-pipelines.universal` `17.3.0`.
- Input System, using `com.unity.inputsystem` `1.19.0` and `PlayerInput`.
- Unity Test Framework `1.6.0` is installed.
- Build Settings contains one enabled scene: `Assets/Scenes/SampleScene.unity`.
- No first-party `.asmdef`/`.asmref` files or Unity MCP package were found.

## Important packages and frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP with 2D lighting | Confirmed | `Packages/manifest.json`, `Assets/Scenes/SampleScene.unity` |
| Input | Input System actions through `PlayerInput` | Confirmed | `Packages/manifest.json`, `Assets/InputSystem_Actions.inputactions`, `Assets/Prefabs/Player.prefab` |
| Physics | 2D physics and custom `NoPushCollisionMover2D` movement helper | Confirmed | `Packages/manifest.json`, `Assets/Scripts/Physics/` |
| Tests | Unity Test Framework installed; one Editor test file exists | Confirmed | `Packages/manifest.json`, `Assets/Tests/Editor/NoPushCollisionMover2DTests.cs` |
| Networking | Multiplayer Center package is present, but no first-party multiplayer usage was found | Confirmed | `Packages/manifest.json`, `Assets/Scripts/` |

## Directory structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Scripts/Player/` | Player movement, combat input, animation, dodge, stats and HUD | Confirmed | First-party scripts |
| `Assets/Scripts/Enemy/` | Enemy stats, state machine, attacks, animation and health bar | Confirmed | First-party scripts |
| `Assets/Scripts/ScriptableObjects(Scripts)/` | `PlayerData` and `EnemyData` configuration plus runtime-state types | Confirmed | First-party scripts |
| `Assets/Scripts/StateMachineBehaviourScripts/` | Animation state callbacks and attack events | Confirmed | First-party scripts |
| `Assets/Scripts/Physics/` | Collision-aware 2D movement helpers | Confirmed | First-party scripts |
| `Assets/Prefabs/` | Player, enemy, attack beacon, health bar and HUD prefabs | Confirmed | Prefab inventory |
| `Assets/Animations/` | Player and enemy Animator Controllers and clips | Confirmed | Asset inventory |
| `Assets/Scenes/` | Current gameplay scene | Confirmed | `SampleScene.unity` |
| `Assets/Tests/Editor/` | Edit Mode-style Unity test source | Confirmed | Test inventory |

## Assembly boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `Assembly-CSharp` | All first-party runtime and editor scripts | Unity runtime packages | No first-party assembly definitions; global namespace |

## Scenes and startup flow

- Build scenes: `Assets/Scenes/SampleScene.unity` is the only enabled scene.
- Likely startup scene: `SampleScene.unity`, confirmed by `ProjectSettings/EditorBuildSettings.asset`.
- Scene loading flow: no first-party scene-loading system was found; startup/runtime flow is otherwise unverified.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Component composition | Player and enemy behavior are split across focused `MonoBehaviour` components on prefabs | Confirmed | `Assets/Prefabs/Player.prefab`, `Assets/Prefabs/Enemy/DemonEnemy.prefab` |
| Data/runtime separation | `PlayerData` and `EnemyData` create per-instance mutable runtime state; assets are not mutated during play | Confirmed | `Assets/Scripts/ScriptableObjects(Scripts)/PlayerData.cs`, `EnemyData.cs` |
| Event callbacks | Stat controllers publish HP, damage, death and soul/kill-count events to presentation/state components | Confirmed | `PlayerStatController.cs`, `EnemyStatController.cs` |
| Animation-driven timing | Attack hit, prepare pause and finish points use animation callbacks with timer fallbacks | Confirmed | `PlayerAttackAnimationEvents.cs`, `EnemyStateMachine.cs` |
| Global lookup fallback | Enemy state machine uses player tag lookup and `FindFirstObjectByType` for missing references | Confirmed | `EnemyStateMachine.cs` |

## Coding conventions

- Namespace style: first-party scripts use the global namespace.
- Serialized fields: private fields with `[SerializeField]`, `[Header]` and `[Tooltip]`; public read-only properties expose state.
- Runtime behavior: Unity lifecycle methods and explicit component references; avoid adding duplicate authoritative state.
- Async: no first-party async pattern found.
- Comments/docs: short Korean comments describe lifecycle and gameplay responsibilities; XML documentation is used selectively.

## Testing and validation

- EditMode tests: `Assets/Tests/Editor/NoPushCollisionMover2DTests.cs`.
- PlayMode tests: none found.
- CI/build validation: no repository CI workflow or checked-in build script found.
- Project guidance requests `unity test`, `unity build --target WebGL` and `unity logs --follow` where applicable.
- This onboarding did not run tests or builds, and no Unity Editor was connected.

## Available Unity tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| `unity.connection.status` | unavailable | `unity status` returned no connected Editor |
| `unity.editor.version` | available from repository | `ProjectSettings/ProjectVersion.txt` |
| `unity.console.read` | unavailable | No connected Editor/MCP |
| `unity.scene.list` | unavailable | No connected Editor/MCP; Build Settings inspected from YAML |
| `unity.scene.inspect` | unavailable | No connected Editor/MCP |
| `unity.buildsettings.read` | available from repository | `ProjectSettings/EditorBuildSettings.asset` |
| `unity.gameobject.inspect` | unavailable | No connected Editor/MCP |
| `unity.asset.search` | available from repository | `rg`/asset inventory |
| `unity.package.read` | available from repository | `Packages/manifest.json`, `packages-lock.json` |
| `unity.tests.list` | available from repository | `Assets/Tests/` inventory |
| `unity.tests.run` | not run | User requested planning, not validation |
| `unity.playmode.read` | not run | Onboarding does not enter Play Mode |
| `unity.profiler.read` | unavailable | No connected Editor/MCP |

## Important constraints

- Preserve Unity version, packages, render pipeline, input system and existing serialized references.
- Execution implementation must keep mutable stun/execution state in enemy runtime components/state, not ScriptableObject assets.
- Existing player input has no `Execution` action; adding one requires updating `Assets/InputSystem_Actions.inputactions` and the Player prefab's action lookup only.
- Existing movement intentionally ignores enemy push collisions through `NoPushCollisionMover2D`; execution approach must preserve obstacle blocking and avoid permanently changing layer collision rules.
- Web build must avoid unsupported native/file/thread assumptions and must handle browser focus/input loss.

## Unknowns and confidence

- Exact execution animation and presentation asset are undecided in the design document; confidence: confirmed unknown.
- Boss identification and boss-specific max-heal behavior have no current runtime abstraction; confidence: confirmed unknown.
- Pause menu integration and camera/overlay ownership are not present in the inspected first-party code; confidence: confirmed unknown.
- Connected Unity Console state, current scene runtime behavior, and WebGL build output were not verified; confidence: confirmed unknown.

## Source files inspected

- `AGENTS.md`
- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/TagManager.asset`
- `Packages/manifest.json`
- `Assets/InputSystem_Actions.inputactions`
- `Assets/Scripts/Player/PlayerStatController.cs`
- `Assets/Scripts/Player/PlayerMoveController.cs`
- `Assets/Scripts/Player/PlayerAnimationController.cs`
- `Assets/Scripts/Player/PlayerDodge.cs`
- `Assets/Scripts/Enemy/EnemyStatController.cs`
- `Assets/Scripts/Enemy/EnemyStateMachine.cs`
- `Assets/Scripts/ScriptableObjects(Scripts)/PlayerData.cs`
- `Assets/Scripts/ScriptableObjects(Scripts)/EnemyData.cs`
- `Assets/Prefabs/Player.prefab`
- `Assets/Prefabs/Enemy/DemonEnemy.prefab`
- Wiki notes: `플레이어 세부 기획(처형)`, `세부 기획(처형)`, `처형 시스템 아이디어 브레인스토밍`, `플레이어 세부 기획(영혼 충전)`, `적 세부 기획(상태 머신)`, `적 세부 기획(스테이터스)` and `플레이어 세부 기획(공격)`.

## Last analyzed commit and date

- Commit: `315c6b2c02adf50229fee36f861e458b8f925915`
- Date: 2026-08-23 (Asia/Seoul)

<!-- unity-onboarding:generated:end -->
