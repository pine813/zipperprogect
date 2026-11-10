# Zipper 双游戏串联（可玩链接）

把两个独立的 Unity 游戏连成一条通关流程：**打通 Starry Abyss 星上归途 → 自动进入 Zipper：海洋与珍珠**。

## 〇、当前状态（已完成 ✅）

| 项 | 状态 |
| --- | --- |
| 第一章 Starry Abyss（队友） | 已打 WebGL：**33.3 MB**，放进 `game-a/` |
| 第二章 Zipper（我的） | 已打 WebGL：**17.6 MB**，放进 `game-b/` |
| 通关信号桥 | 两边都已编译进构建（`game-complete` 符号已确认存在） |
| 本地链接 | `http://127.0.0.1:8099/` |

**对队友工程做的改动（需要提交回他的仓库）**：
1. 新增 `Assets/Scripts/WebBridge.cs`
2. 新增 `Assets/Scripts/ZipperHandoff.cs`
3. 新增 `Assets/Plugins/WebGL/ZipperBridge.jslib`
4. 新增 `Assets/Editor/ZipperWebGLBuild.cs`（WebGL 构建入口）
5. 修改 `Assets/Scripts/StarryGame.cs`：`ShowEnd()` 里 `result.Escaped` 时调用 `ZipperHandoff.Schedule(4.5f)`
   —— 通关结局出现后停留 4.5 秒，然后通知外壳切到第二章。

```
zipper-web/
├─ index.html                     ← 串联外壳（入口，需用 http 打开）
├─ game-a/                        ← Starry Abyss 的 WebGL 构建
├─ game-b/                        ← Zipper 的 WebGL 构建
└─ bridge/                        ← Unity 侧通关桥源码（备份/给其他工程复用）
```


---

## 一、两个游戏怎么衔接

### 方案 A：自动衔接（推荐，改 3 行代码）

1. 把 `bridge/` 里的两个文件放进**队友的 Unity 工程**：
   - `WebBridge.cs` → 任意 `Assets/Scripts/` 下
   - `ZipperBridge.jslib` → `Assets/Plugins/WebGL/` 下
2. 在队友游戏的「通关/胜利」处调用一次：
   ```csharp
   WebBridge.NotifyGameComplete();
   ```
3. 队友把游戏打成 WebGL，输出放进 `game-a/`。
4. 把本工程的 WebGL 构建放进 `game-b/`（见下）。
5. 用 http 打开 `index.html`：队友的游戏跑完会自动跳到我的第二章；我的游戏通关后显示「全部通关」。

### 方案 B：不改队友代码的兜底

外壳右上角有「**通关了，进入下一个 →**」按钮，任何情况下都能手动切换。
即使队友的工程完全不动，也能玩完整条流程。

### 方案 C：队友只给构建、不给源码

如果队友给的是一个已经打好的 WebGL 构建，且他的通关界面能打开链接，
让他把通关时的跳转地址写成 `?game=b`（相对当前页面），例如：
```js
location.href = '../index.html?game=b';
```

---

## 二、把我的游戏打成 WebGL

Unity 菜单：`Tools/Zipper/打包 WebGL`

输出在工程目录 `Build/WebGL/`，把**整个文件夹内容**复制到 `game-b/`（保证 `index.html` 直接位于 `game-b/` 下）。

命令行方式（CI 用）：
```
Unity.exe -batchmode -projectPath <工程路径> ^
  -executeMethod Zipper.EditorTools.WebGLBuilder.Build ^
  -logFile build.log
```

---

## 三、发布成公开链接（GitHub Pages）

你已经有一个仓库 `github.com/pine813/zipperprogect`，两种放法二选一：

**放 `docs/`（推荐）**
1. 把 `zipper-web/` 整个内容推到仓库的 `docs/` 目录：
   ```
   docs/index.html
   docs/game-a/...
   docs/game-b/...
   ```
2. 仓库 Settings → Pages → Source 选 `Deploy from a branch` → 分支 `main`，目录 `/docs` → Save。
3. 等 1~2 分钟，链接是：
   ```
   https://pine813.github.io/zipperprogect/
   ```
   直接进我的第二章：`https://pine813.github.io/zipperprogect/index.html?game=b`

**放根目录**
把三个目录放到仓库根目录，Pages 的目录选 `/ (root)`，链接同上。

> WebGL 构建体积通常 20~60 MB，GitHub 单文件限制 100 MB，正常能推。若嫌大可以开 Build 里的压缩，或把构建产物放到 Release 再链接。

---

## 四、本地预览

Unity WebGL **不能直接双击 index.html**（`file://` 会被浏览器拦），需要 http：

```powershell
cd zipper-web
python -m http.server 8080
# 浏览器打开 http://127.0.0.1:8080/
```

---

## 五、已知限制

- 两个游戏通过「外壳页 + iframe」串联，各自还是独立构建；它们的存档、进度不互通。
- 队友游戏如果用了只支持 Windows 的插件，就打不了 WebGL，这时只能改成「合并进同一个 Unity 工程」，或者各自出 Windows 包、外壳做成两个 exe 的启动器。
- 自动跳转依赖 `postMessage`，所以必须在外壳页里打开（不能单独开 `game-a/index.html` 期待它跳到第二章）。
