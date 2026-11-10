# 星渊 · 归途

一款以梵高《星月夜》为灵感、使用 **Unity 2022.3.62f3c1** 制作的 2D 轨道引力碰撞游戏。

你是一颗恒星，不能横移，只能沿当前轨道逆时针或顺时针转向。借行星、恒星与流星的碰撞抬高轨道，在星穹边界内航行，寻找归航阵。

![星渊归途标题](zipperproject/Screenshots/title.png)

## 下载

- **Windows 可玩包**：[release/StarryAbyss-Windows.zip](release/StarryAbyss-Windows.zip)
- **Unity 完整工程包**：[release/StarryAbyss-UnityProject.zip](release/StarryAbyss-UnityProject.zip)

Windows 包解压后双击 `StarryAbyss.exe` 即可游玩。  
Unity 工程使用 **Unity 2022.3.62f3c1** 打开。

## 最新版本内容

- 三层星图：内圈尘光带、中圈行星空、外圈归航带。
- 归航阵在前 60 秒隐藏，之后随机显现，每 15–21 秒移动到外环新位置。
- 黑洞周期性进入吸积期：引力增强、盘面变亮、抛出内圈星屑。
- 行星受击会缩小、抛出碎石，多次受击后碎裂成真实碎片。
- 流星雨带落点预警、可躲避，并掉落星屑与临时增益。
- 星屑、连携、航迹分、圈层星屑目标与本地最高纪录。
- 六种随机星相、本地星相图鉴、擦身借力与完美借力。
- 逆流河隐藏事件：连续逆时针绕行三圈可额外选择一次星象。
- 柔和版画面、界面与音效，整体降低饱和度与高光强度。

完整玩法说明见 [zipperproject/README.md](zipperproject/README.md)。

## 仓库结构

```text
zipperproject/        Unity 工程源码（Assets / Packages / ProjectSettings / 截图）
release/              可直接下载的 Windows 与 Unity 资源包
README.md             本说明
```

## 操作

| 按键 | 作用 |
|---|---|
| `A` / `←` | 沿当前轨道逆时针转向 |
| `D` / `→` | 沿当前轨道顺时针转向 |
| `Space` | 擦身 / 完美借力；标题启程；过场跳过；结算下一局 |
| `P` / `Esc` | 暂停 / 继续 |
| `M` | 静音 |
| `V` | 辅助模式 |
| `F11` / `Alt + Enter` | 全屏 / 窗口切换 |

---

*Starry Abyss · The Way Home*
