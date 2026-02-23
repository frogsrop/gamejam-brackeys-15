# frogsrop.unity — Scene Documentation

Main playable scene for the Brackeys Game Jam 15 project. It is a 2D level where the player can move between rooms, report or restore ghost activity, and take screenshots.

---

## Top-Level Hierarchy

| Object | Role |
|--------|------|
| **GameManager** | Toggles which room (vision panel) is visible. Holds 6 `visionPanels`; `Start()` activates room 5. |
| **Level** | Root for all level geometry, rooms, and events. Single transform at (≈ -0.73, -3.78, 0). |
| **Camera** | Main Camera (tag: MainCamera). Cinemachine Brain + URP 2D. |
| **CinemachineCamera** | Virtual camera (e.g. CinemachinePositionComposer, distance 10) for follow/ framing. |
| **EventSystem** | Input / UI. |
| **Canvas** | World Space Canvas (Render Mode 2). Holds **Hints** and **PhotoRoot** (screenshot UI). |

---

## Level Structure (children of Level)

Level has **8 children**:

| Child | Purpose |
|-------|---------|
| **Background** | All room/area sprites in one container: Forest, Kitchen, Coridor, Coridor 2, bath_0, and other named sprites. Z and scale vary for visual layering. |
| **Floor** | Floor tiles (e.g. floor_0_0, floor (1), Puddle Ground). |
| **Walls** | Wall colliders and sprites: wall, wall (1)–(5), wall_bathroom-2_0. Layer 6 for walls. |
| **Doors** | Door sprites and **Teleporter** pads. Contains teleporter pairs and **Teleporter** components that call `GameManager.ActivateRoom(roomId1/roomId2)` and move the player. |
| **Panels** | Six panel GameObjects (vision panels). Only one is active at a time; `GameManager.ActivateRoom(id)` switches which panel is shown (1–6). |
| **Events** | **Root** (event tree). Holds all event **Nodes** (AnimationNode, AudioAnimationNode, AlwaysActiveNode, etc.) and **ReportGhostActivityTrigger**s. |
| **Shelves** | “Shelves” room with bottles, lights, and a **ReportGhostActivityTrigger** (F = report, Q = restore). Uses `ReportedWrongText` / `ReportedGhostText` (e.g. bottles vs washing machine). |
| *(Prefab instance)* | One child is a prefab instance (stripped transform); likely player or another runtime object. |

---

## Rooms and Navigation

- **Vision panels**: 6 panels under **Panels**; each panel is a “room view.” `GameManager.ActivateRoom(roomId)` (1–6) activates the corresponding panel and deactivates the others.
- **Teleporters**: Under **Doors**. Each **Teleporter** has:
  - `teleport1` / `teleport2` (TeleporterPad GameObjects)
  - `roomId1` / `roomId2` (e.g. 2 and 5)
  - `keepX` / `keepY` and `offset` for positioning the player and switching room.
- **Background** holds room art: e.g. Kitchen, Coridor, Coridor 2, Forest, bath_0, and other sprites, all under one transform.

---

## Events and Ghost System

- **Events** (GameObject) has the **Root** script (event tree root).
  - **Root**: `tickIntervalSeconds: 5`, `tickDelaySeconds: 5`, `ghostEventChance: 0.48`, `ghostToNormalSeconds: 2`, `drawTreeGizmos: true`. Two top-level node children.
  - **Root** drives the tree: normal activation, ghost activation (nodes with no active parent), and a timer to turn ghost nodes back to normal when they have an active non-ghost parent.
- **ReportGhostActivityTrigger** appears on:
  - **Shelves** (bottles): report/restore hints, `ReportedWrongText` / `ReportedGhostText` set.
  - **Puddle** (under Events): report/restore, no custom text.
  - Other event nodes (e.g. AnimationNode/AudioAnimationNode) with report/restore and optional custom text.
- **ReportGhostActivityTrigger**:
  - **F** = report ghost activity (fires `OnReported` with node transform, `wasGhost`, and feedback text).
  - **Q** = restore linked node (`RestoreNode()`).
  - Feedback text is chosen by correctness: ghost vs non-ghost → `ReportedGhostText` or `ReportedWrongText`.

---

## Lighting and Atmosphere

- **Global Light 2D** (under one of the level branches).
- **Light** (e.g. under Message or another container): URP 2D Light(s).
- **ambient**: Ambient/atmosphere object.
- **Puddle**, **chandelier_0 (3)/(4)**, **bottle** variants: props and small visuals.

---

## UI (Canvas)

- **Hints**: RectTransform with VerticalLayoutGroup + ContentSizeFitter. Contains hint texts (E, F, Q, etc.) for interact, report, restore. Positioned in world space (e.g. anchored center, offset); **CharacterControl** drives visibility and text from `hintsRoot`, `hintsE`, `hintsF`, `hintsQ`, `reportFeedbackText`.
- **PhotoRoot**: Initially inactive. Contains an **Image** (screenshot display) and related UI. **CharacterControl** shows it when the player reports (F), captures a screenshot, and can show ghost-only elements when `wasGhost` is true.

---

## Key Scripts in Scene

| Script | Where used | Role |
|--------|------------|------|
| **GameManager** | GameManager | `ActivateRoom(roomId)` to show one of 6 vision panels. |
| **Teleporter** | Doors (multiple) | Teleport player between pads and switch room by `roomId1`/`roomId2`. |
| **TeleporterPad** | Doors | Trigger pads for teleport (e.g. “one”, “two” pads). |
| **Root** | Events | Event tree: tick, normal/ghost activation, ghost→normal timer. |
| **ReportGhostActivityTrigger** | Shelves, Puddle, other nodes | Report (F), Restore (Q), hints, feedback text. |
| **AnimationNode** / **AudioAnimationNode** | Event nodes | Play animation/audio; can be activated by Root or restored. |
| **AlwaysActiveNode** | One event node | Always active (e.g. Rain). |
| **CinemachineBrain** | Camera | Drives camera from CinemachineCamera. |
| **CinemachineCamera** | CinemachineCamera | Follow/compose (e.g. position composer). |

---

## Player and Input

- **CharacterControl** is expected on the player object (likely from a prefab or another root object): movement, interact (E), report (F), restore (Q), hint text updates, screenshot capture on report, and `reportFeedbackText` (ReportedGhostText / ReportedWrongText).
- Input comes from **EventSystem** and the **Input System** (Player action map: Move, Interact, Report, Restore).

---

## Summary

**frogsrop.unity** is a single level with:

1. **Level** — Background, Floor, Walls, Doors, Panels, Events, Shelves (and one prefab).
2. **Room system** — 6 vision panels; GameManager and Teleporters switch which room is visible and where the player is.
3. **Event system** — Root + nodes (animation/audio/always-active) and Report/Restore triggers with ghost vs normal logic and feedback text.
4. **Camera** — Main Camera + Cinemachine for 2D follow.
5. **UI** — World Space Canvas with Hints and PhotoRoot (screenshot + report feedback).

File location: `Assets/Game/Scenes/frogsrop.unity`.
