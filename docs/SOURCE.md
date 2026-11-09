# Source and dependencies

Protocol: IronWall is a team project for ECE4300 Human–Computer Interaction. The files in [source/](../source/) are selected interaction scripts from the submitted project.

| Component | Original project |
| --- | --- |
| Editor | Tuanjie 1.8.5 (2022.3.62t7) |
| Hand tracking | Ultraleap Tracking and Tracking Preview 7.3.0 |
| Input | Unity Input System 1.14.3 and Windows input interop |
| Rendering | Universal Render Pipeline 14.1.0 |
| Navigation and cameras | AI Navigation 1.1.7 and Cinemachine 2.10.3 |

The scripts refer to scene objects and components in the original project. The full editable project also uses Unity's Tower Defense Template, Particle Pack and Starter Assets, and Dungeon Mason's Robot Hero asset. Obtain third-party materials through their original providers under their respective licenses. The playable build is available under [Releases](https://github.com/Penguinblast/protocol-ironwall/releases).

## Reading the code

| File | Role |
| --- | --- |
| [LeftController.cs](../source/LeftController.cs) | Left-hand input and cursor tracking |
| [RighthandDetector.cs](../source/RighthandDetector.cs) | Right-hand fist and pinch state |
| [SkillController.cs](../source/SkillController.cs) | Gesture-driven movement and skill inputs |
| [Viewmanager.cs](../source/Viewmanager.cs) | Active view state and skill input gating |
| [HandFollowCamera.cs](../source/HandFollowCamera.cs) | Camera movement driven by tracked hand position |

The names follow the original course submission. See the [team credits](../README.md#team-defected-robot) for contributions.
