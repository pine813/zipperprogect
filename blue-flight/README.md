# 蓝色能量星球 失重飞行关卡源码

可组合进主游戏的 Unity 2D 单关模块。玩家操控蓝色能量星球持续向前飞行，用上下推力、冲刺和能量弹穿过太空。六颗青色引力碰撞球会在接触后加速并改变飞行方向。

本目录只保存源代码、文本格式场景与预制体、资源标识和项目配置，不包含可执行游戏、压缩包或 Unity 缓存。美术和音效均由代码在运行时生成。

## 打开与运行

1. 用 **Unity 2022.3.62f3c1** 打开本目录 `blue-flight`。
2. 打开 `Assets/BlueRunner/Samples/BlueRunnerTest.unity`。
3. 点击 Play，直接进入本关。场景在运行时生成，编辑状态只有关卡根对象是正常现象。

| 输入 | 操作 |
| --- | --- |
| W / ↑ / Space | 持续向上飞行 |
| S / ↓ | 持续向下飞行 |
| A / ←、D / → | 减速、加速前进 |
| J | 发射能量弹，可按住连射 |
| Shift / K | 冲刺 |
| Esc | 暂停或继续 |
| R | 回到最近的空中补给点 |

没有重力、地板和跳跃机制。松开上下方向键后，稳定系统会平滑消除纵向漂移。画面上下有无伤导航范围限制。相机跟随玩家前进，星空、行星与漂浮碎片通过分层视差向后滚动。

## 关卡与碰撞规则

- 航线长 120 单位，包含 12 架无人机、10 块磁性碎片、44 枚晶体、两个补给环和出口。
- 六颗引力碰撞球位于 17、34、53、70、89、107 单位处。
- 碰撞球上半部将玩家向右上方弹射，下半部向右下方弹射。接近正面时按球内箭头选择默认方向，偏转约 35 度。
- 借力初速至少为 13 单位/秒，正常巡航为 6.8，效果在约 0.9 秒内衰减；玩家可以随时反向修正。
- 总速度上限为 18。同一颗球有 1 秒恢复时间，单次接触不会反复叠加借力。返回补给点会清除当前加速。
- 青色碰撞球不扣护盾。紫色碎片与敌人接触会扣护盾，短暂无敌防止连续伤害。

## 源码结构

| 文件 | 职责 |
| --- | --- |
| `Runtime/PlayerController.cs` | 失重飞行、冲刺、碰撞借力、速度上限、输入接口 |
| `Runtime/GravityBumper.cs` | 实体圆形碰撞体、单次接触触发与恢复时间 |
| `Runtime/LevelBuilder.cs` | 关卡布局、碰撞球、敌人、背景视差与运行时生成 |
| `Runtime/GameRoot.cs` | 单关流程、护盾、补给、弹药发射、统计与通关事件 |
| `Runtime/WorldActors.cs` | 敌人、晶体、伤害障碍、相机、出口与特效 |
| `Runtime/Art.cs`、`Runtime/GameAudio.cs` | 程序绘制素材与合成音效 |
| `Samples/RunnerChecks.cs` | 可执行程序真实物理与通关检查，普通游玩默认不运行 |
| `Editor/RunnerBuilder.cs` | 本独立测试工程的场景生成、导出与构建工具 |

以上路径均相对于 `Assets/BlueRunner`。

## 接入主游戏

将 `Assets/BlueRunner/Runtime` 与 `Prefabs` 连同对应 `.meta` 文件复制到主项目。`Samples` 只用于独立验证，`Editor` 构建工具不必复制。

把 `BlueRunnerLevel.prefab` 放在世界原点，保持单位缩放。给 `GameRoot.hostCamera` 指定正交相机（建议 Orthographic Size 为 5），调用 `BeginLevel()`。预制体默认关闭自动开始、演示键盘、演示相机和 HUD。

```csharp
level.BeginLevel();
level.Player.FeedFlightInput(verticalAxis, forwardAxis, dashPressed);
// verticalAxis: -1 向下，0 稳定高度，1 向上
// forwardAxis: -1 减速，0 巡航，1 加速
level.Fire();
level.SetPaused(true);
```

监听 `GameRoot.Completed` 接收 `BlueRunner.LevelResult`，字段为 `seconds`、`defeated`、`energy`、`retries`。主流程负责下一关，组件不会自行加载其他场景。

模块不修改全局重力或 `Time.timeScale`。指定相机会使用 `CameraFollow`，接入时暂时关闭其他相机跟随组件，退出后由主流程恢复。当前按单个活动关卡实例设计。样例键盘使用旧输入系统，主游戏使用新 Input System 时保持 `demoKeyboard=false`，由 Input Action 回调驱动上述接口。

## 本地构建与验证

用 Unity 批处理调用 `BlueRunner.Editor.RunnerBuilder.Build`，或从编辑器调用同一静态方法。该工具会重新生成样例场景和预制体，并设置当前测试工程的构建选项，仅应在本目录这个独立工程内运行。

构建输出位于 `Builds/ZeroGravity/`，关卡导出包位于 `Builds/Exports/`，两者均被 Git 忽略。

构建后运行：

```powershell
.\Builds\ZeroGravity\ZeroGravity.exe --flight-test --test-output C:/Temp/BlueFlightChecks
```

检查覆盖无重力、上下控制、局部暂停、护盾、真实碰撞后的加速与上下偏转、重复触发保护、速度限制、补给恢复以及完整通关。结果写入指定目录的 `flight-report.json`。最新版已在 Unity 2022.3.62f3c1 下通过 23 项运行检查。
