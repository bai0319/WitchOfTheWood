# Witch of the Wood（林中女巫）

一款 **2D 俯视角探索 / 炼金 / 解谜** 小游戏，为课程双人小组作业。

玩家扮演林中小屋里的女巫，在森林与小屋之间采集草药、按订单炼制药水、推进剧情对话与线索日记。

---

## 技术栈

| 项目 | 说明 |
|---|---|
| 引擎 | **Unity 2022.3.62f3c1** |
| 渲染 | 2D（`com.unity.feature.2d`） |
| 地图 | Tiled `.tmx` / `.tsx`，通过 **SuperTiled2Unity** 导入 |
| 视觉辅助 | Cinemachine 2.10.7 |
| UI | uGUI + TextMeshPro 3.0.9 |
| 脚本语言 | **C#**（.NET Standard / Unity 类库） |

> 本项目是 Unity / C# 工程，**不是 Python 项目**，因此没有 `requirements.txt`。
> 第三方依赖由 `Packages/manifest.json`（Unity Package Manager）管理，用 Unity Hub 打开工程时会自动还原。

---

## 核心系统（Assets/Scripts）

| 脚本 | 职责 |
|---|---|
| `PlayerController.cs` | 玩家移动 / 交互输入 |
| `CameraFollow.cs` | Cinemachine 相机跟随 |
| `DialogSystem.cs` | 对话 / 剧情文本 |
| `ClueDiarySystem.cs` | 线索日记 |
| `InventorySystem.cs` | 背包 / 物品 |
| `PotionCraftingSystem.cs` | 药水炼制配方 |
| `OrderPanelSystem.cs` | 订单面板 |
| `SkillBarSystem.cs` | 技能栏 |
| `GameSettings.cs` / `MainMenu.cs` | 设置 / 主菜单 |
| `SceneTransitionManager.cs` / `DoorTransition.cs` | 场景切换 / 过门 |
| `SenlinGameManager.cs` / `SenlinUIManager.cs` | 森林场景总控 |
| `TiledObjectLoader.cs` / `TiledTilemapReader.cs` | Tiled 地图对象 / 瓦片加载 |
| `InteractableObject.cs` / `InteractableHighlighter.cs` | 可交互物与高亮 |
| `UIGenerator.cs` / `UIManager.cs` | UI 动态生成 |
| `ChineseFontImporter.cs` | 中文字体导入 |
| `AutoCreateManagers.cs` / `Editor/AutoBuildSettings.cs` | 启动自动挂载 / 编辑器构建设置 |

---

## 目录结构

```
Witch of the Wood/
├── Assets/
│   ├── Scenes/          # Unity 场景 .unity
│   ├── Scripts/         # C# 源码（23 个）
│   ├── Sprites/         # 精灵图 / 瓦片 PNG
│   ├── Characters/      # 角色动画、Animator Controller
│   ├── Fonts/           # 中文字体 + TMP SDF
│   ├── TextMesh Pro/    # TMP 资源
│   ├── UI/  UI Toolkit/ # UI
│   └── Tiled2Unity/     # Tiled 导入产物
├── Packages/
│   └── manifest.json    # UPM 依赖清单
├── ProjectSettings/     # 工程设置（Unity 版本、Graphics 等）
├── com.seanba.super-tiled2unity/  # 本地 Tiled 导入包
└── README.md  .gitignore
```

> `Library/`、`Temp/`、`obj/`、`Logs/`、`UserSettings/`、`.vs/` 等由 Unity 自动生成，已在 `.gitignore` 中忽略，不入库。

---

## 快速开始

1. 安装 **Unity Hub**，并在 Hub 中加装 **Unity 2022.3.62f3c1**（带 **2D 工作负载**）。
2. Clone 本仓库：
   ```bash
   git clone https://github.com/<你的用户名>/WitchOfTheWood.git
   ```
3. 在 Unity Hub 中 → `Add project from disk` → 选择本工程根目录（包含 `Assets/` 的那一层）。
4. 首次打开会自动还原 `Packages/manifest.json` 中的依赖，等待编译完成。
5. 打开 `Assets/Scenes/` 下的主场景，按 ▶ 运行。

---

## 标签

`Unity` `C#` `2D-Game` `Game-Jam` `Tiled` `SuperTiled2Unity` `Course-Project`
