# 2026-3C-Class

2026 华夜工作室 3C 授课演示 Demo。项目通过一个训练场展示角色移动与转向、镜头切换，以及由输入驱动的战斗状态机和连段。

## 运行演示

1. 用 Unity Hub 将仓库根目录添加为项目，使用 **Unity 2022.3.62f3c1** 打开。
2. 等待 Unity 导入资源和包，打开 `Assets/Scenes/SampleScene.unity`。
3. 点击 **Play**，选中 **Game** 窗口，沿青色标线走近训练靶。

## 操作

| 输入 | 动作 |
| --- | --- |
| WASD / 左摇杆 | 移动并朝移动方向转身 |
| 鼠标左键（A） / E | 普攻，A 和 E 共用三段连段 |
| EEA / AEA | 第三段分别派生山摇和地动 |
| Q | 短按轻击，长按后松开施放电系重击 |
| 左 Shift | 闪避，可取消后摇并重置连段 |
| C / R | 切换镜头 / 重置训练 |

山摇或地动出招后，在 **0.9 秒后摇**内按完 EEA 或 AEA。第三键会取消后摇并直接释放下一次强化技，跳过前两段普攻。

## 项目资料

- 在 Play Mode 选择 **Tools → Gameplay 3C → Verify Training Lab**，在 Console 查看玩法检查结果。
- 战斗时序、闪避和代码分工见[训练场说明](Assets/ThreeCDemo/README.md)。
- Blender 源文件和 FBX 重建步骤见[模型制作说明](ArtSource/ThreeCDemo/README.md)。
- 按 Git Flow 从 `develop` 创建功能或文档分支，向 `develop` 提交 PR。`main` 用于发布。
