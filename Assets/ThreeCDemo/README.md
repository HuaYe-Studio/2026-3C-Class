# Gameplay 3C 演示 · Training Lab

在 Unity 2022.3 打开 `Assets/Scenes/SampleScene.unity`，点击 **Play** 并把焦点放到 **Game** 窗口。可以在 Hierarchy 中直接调整角色、靶子、训练场和镜头。

| 输入 | 演示行为 |
| --- | --- |
| WASD / 左摇杆 | 按世界坐标移动，角色平滑转向。斜向移动不会更快。 |
| 鼠标左键 ×3 | A 套普攻：A1 → A2 → A3，伤害 8 / 10 / 14。在后摇窗口内继续按 A 即可接下一段。 |
| E ×3 | E 套普攻：E1 → E2 → E3，伤害 8 / 10 / 14。A 与 E 共用同一个连段步数，可以自由穿插。 |
| E、E、鼠标左键 | 第三段按 A 时，由最近两招 E、E 派生「山摇」（Ground，40 伤害）。 |
| 鼠标左键、E、鼠标左键 | 第三段按 A 时，由最近两招 A、E 派生「地动」（Ground，45 伤害）。 |
| 强化技后摇内输入 EEA / AEA | 在默认 0.9 秒的后摇内按完三键，直接接「山摇」/「地动」。前两键只记录输入，第三键取消后摇并出招。A 为鼠标左键。 |
| 左 Shift | 有方向输入时朝输入方向闪避并转向，无输入时朝当前朝向闪避；后摇中按下可取消后摇并重置连段，Dodging 前 0.2 秒为无敌帧。 |
| 短按 Q / 按住 Q 后松开 | 短按为轻击（Normal，15 伤害），按住 0.3 秒后松开为重击（Electric，30 伤害）；重击对地面系无效。Q 不属于 A/E 连段。 |
| C | 在 Cinemachine 跟随镜头与训练场俯视镜头之间切换。 |
| R | 重置站位、连段、战斗状态、玩家血量与靶子血量。 |

沿青色标线接近靶子后尝试 A/E 连段、Q 和两段派生。攻击阶段持续 0.25 秒，普通攻击后摇持续 0.55 秒，两个阶段均锁定移动与转向；在后摇结束前继续输入 A 或 E 会推进共享连段步数，后摇超时则连段归零。A/E 共用段数，因此 A1 → E2、E1 → A2 都能直接接上对应段数。第三段按 A 时，状态机检查最近两招：E、E 派生山摇，A、E 派生地动。攻击进行中按下的下一段会被 `CombatActionBuffer` 保留到后摇窗口（不会被 0.2 秒短窗提前吞掉），后摇一开始就自动衔接。后摇期间按左 Shift 可取消后摇、重置连段并进入 0.4 秒的 Dodging；仅前 0.2 秒为无敌帧（`IsInvulnerable`），之后仍可能受伤。闪避方向在起手时锁定，速度随时间线性衰减；无方向输入时朝当前朝向冲刺。

闪避穿透：Dodging 期间会对选中的 Collider 调用 `Physics.IgnoreCollision`，结束后恢复。`Dodge Through Layers` 留空时会自动使用名为 `Enemy` 的 Layer（若存在），并且始终忽略当前攻击目标（训练靶子）的 Collider；也可以在 `PlayerController` 的 `Dodge Through Layers` 里手动指定 Layer。CharacterController 对 `excludeLayers` 的支持不稳定，因此这里用逐 Collider 忽略。

注意：创建 `Enemy` Layer 后，还需要把怪物对象的 Layer 改成 `Enemy`；只创建 Layer 不会自动改变对象所在的层。

在无敌帧内受击会触发 `PlayerController.PerfectDodge` 事件（伤害仍为 0），可以在这里接入子弹时间、回复能量等「极限闪避」效果。

代码按讲义的结构组织。`Training.inputactions` 定义按键和动作，`PlayerInputReader` 实现 `IPlayerInputSource` 读取输入；`PlayerMotion` 通过 `IMoveInput`、`IMover`、`IRotator` 驱动移动/转向，`PlayerDodge` 负责闪避位移；`CombatRules` 放置连段状态机（`ComboActionType`、共享 `ComboStep`、最近两招派生判定）、`CombatActionBuffer`、长短按判定、`DamageRules.ApplySkill`、`Health` 与 `PlayerHealth` 受击规则。`PlayerController` 只做装配：运动、战斗、出招表现、HUD 与相机分别由 `PlayerMotionController`、`PlayerCombatController`、`PlayerAttackExecutor`/`PlayerAttackView`、`PlayerHudView`、`PlayerCameraRig` 负责。

在 Play Mode 下可以通过 **Tools → Gameplay 3C → Verify Training Lab** 运行玩法检查。它从输入读取器注入动作并在 Console 输出结果。

参考讲义：[Class3 Gameplay 3C 入门](https://njupt-sast.feishu.cn/wiki/XZWlwcEhbiDUZwkf6Fwcn0XFnzc)，陈柏翰，软研 & 华夜，CC BY-NC-SA 4.0。
