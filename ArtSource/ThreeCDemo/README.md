# Training Lab 模型源文件

在 Blender 5.0 打开 `TrainingLabModels.blend`。角色和靶子各自位于独立集合，装甲、关节与反馈灯均可单独编辑。导出资产以脚底或底座中心为原点，单位为米。

从项目根目录运行 `blender -b --factory-startup --python-exit-code 1 --python ArtSource/ThreeCDemo/build_models.py`，可按脚本中的参数重建源文件和 `Assets/ThreeCDemo/Models` 下的两个 FBX。脚本重建会覆盖这三个产物。若直接编辑 Blender 场景，请保留未合并的源文件，并分别将每个模型的主体和反馈网格导出到对应 FBX。

在 Unity 的 `Assets/Scenes/SampleScene.unity` 中，可用 **Tools → Gameplay 3C → Update Training Models** 重新装配模型。角色面罩 `Visor_FX` 与靶心 `ImpactCore_FX` 各有独立渲染器，用于蓄力和受击反馈。
