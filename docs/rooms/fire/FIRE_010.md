# FIRE_010：熔沟守门

> 2026-08-31 显示调整：按用户要求关闭本房 `Decoration` 的 `Tilemap Renderer`，保留对象、Tile 与坐标；本文中的该层提示位置仅作设计参考，当前不显示、不再要求提示标记可见。碰撞、镜子放置规则与解法不变。

## 2026-08-31 房间几何修复

本节替代下文涉及同一对象的旧坐标、旧入口目标和旧检查结论；未提及的玩法规则及美术记录不变。对应RV结果、设计冲突和验证范围见[FIRE_REPAIR_REVIEW.md](FIRE_REPAIR_REVIEW.md)。当前仍为待运行验证的灰盒，不代表全房验收通过。

Plate-A底边从悬空0.15 unit修正到Y=-2；根位置(7.25,-1.85)。保留岩浆、门、墙体、出口以及任务开始前的所有美术改动。

| 入口ID | Player Collider中心 |
|---|---|
| `DEFAULT` | `(0,-1.08)` |

| 已实现出口 | 中心 | 目标Scene/入口ID |
|---|---|---|
| `Exit-A to FIRE_011` | `(-11.5,-1)` | `Fire_011/DEFAULT` |

本轮增量编辑Scene，未运行整房Builder。当前Scene是落位权威；历史重建入口尚未同步本轮布局，禁止用其覆盖正式Scene。仅FIRE_011构建器的共用喷发Prefab尺寸计算同步修复，不表示其旧房间布局已同步。未运行Unity自动测试或人工试玩，碰撞重建、完整解法、重置和画面遮挡仍需验证。

## 状态与权威来源

- 房间ID：`FIRE_010`
- 区域：火之区域
- 当前状态：灰盒玩法落地；本轮视觉优化按连续两轮低增益条件停止，最佳静态63分；用户已明确批准第二张概念图的设计与实现
- Unity Scene：`Assets/Scenes/Levels/Fire/Fire_010.unity`
- 地图连接：以`docs/maps/MAP.md`为准；本次不改变既有连接
- 静态几何：`docs/systems/LEVEL_GEOMETRY_SYSTEM.md`
- 门与压力板：`docs/systems/DOOR_SYSTEM.md`
- 重置：`docs/systems/RESET_SYSTEM.md`

## 地图连接

`FIRE_010`位于`FIRE_009`左侧、`FIRE_011`右侧。本次落实从右侧`FIRE_009`进入、完成谜题后向左进入`FIRE_011`的主路径。

| 本房出口或入口 | 方向 | 相邻房间 | 目标入口ID | 状态 |
|---|---|---|---|---|
| `Entrance-DEFAULT` | 右 | `FIRE_009` | `DEFAULT` | 已配置 |
| `Exit-A` | 左 | `FIRE_011` | `DEFAULT` | 已配置 |

## 房间定位

- 房间类型：镜像分路、固定岩浆与单板门的简单组合房
- 主要目标：Player向左跨越短岩浆沟，MirrorClone反向向右踩住压力板，为Player持续开启出口门
- 预期洞察：共享输入会让双方走向相反目标；镜像必须留在压力板上，Player才能通过门
- 失败压力：低；Player死亡重置房间，MirrorClone死亡只回收镜子
- 预计完成时间：约1～2分钟

## 已批准机制与排除项

### 使用机制

- 地面镜、MirrorClone反向水平输入与独立物理
- 固定岩浆伤害
- Player与MirrorClone可触发的单压力板
- 单压力板持续控制门
- Player死亡、MirrorClone死亡、手动重置和场景切换

### 明确不包含

- 投火者、周期喷发、双压力板永久锁存、检查点和Spawner
- 抛物线火球、升降岩浆及其他未批准机制
- 房间专用运行时代码

## 标准网格布局

- Grid：`1×1 Unity unit`
- 房间外轮廓与相机可显示边界：`X[-13,13]`、`Y[-7,7]`

```text
┌──────────────────────────────────────────────────────┐
│                                                      │
│ Exit ◀  D    ███      ~~岩浆~~     M/P ───── [A]█   │
│                                                      │
│ ████████████████    ████████████████████████████████ │
└──────────────────────────────────────────────────────┘
```

- `M/P`：默认入口与建议放镜位置，世界`(0,-1.08)`
- `~~`：两格固定岩浆沟，Player预期跳过
- `[A]`：镜像压力板，右侧静态墙阻止镜像继续离开
- `D`：由`[A]`持续控制的两格高门

## Tilemap配置

| Tilemap层 | 使用范围 | 碰撞 | 表面语义 | 备注 |
|---|---|---|---|---|
| `Background` | 空层 | 无 | 无 | 标准结构 |
| `Terrain` | 地面、边界、门上方封墙和镜像止挡墙 | 实体Composite | `StaticSolid` | 安全地面允许地面镜 |
| `OneWayPlatform` | 空层 | 无 | 无 | 标准结构 |
| `SpecialMirrorWall` | 空层 | 无 | 无 | 本房不使用 |
| `Hazard` | 两格岩浆沟 | Trigger | `Hazard` | 使用通用`Hazard2D` |
| `Decoration` | 放镜提示 | 无 | 无 | 不参与玩法判断 |
| `Foreground` | 空层 | 无 | 无 | 标准结构 |

## Prefab需求

| 实例ID | 通用Prefab | 资产路径 | 初始位置/状态 | 实例配置 |
|---|---|---|---|---|
| `Plate-A` | `PressurePlate2D` | `Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab` | `(7.25,-1.7)`；弹起 | 不覆盖触发对象规则 |
| `Door-A` | `Door2D` | `Assets/Prefabs/Gameplay/Doors/Door2D.prefab` | `(-9.5,-1)`；关闭 | 控制源显式引用`Plate-A` |
| `Exit-A` | `RoomExit2D` | `Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab` | `(-11.5,-1)` | 目标`Fire_011/DEFAULT` |

Player、镜子和MirrorClone由统一系统提供，不在Scene中复制。

## 敌人出生与生成

不适用；本房没有敌人。

## 相机配置

- 镜头模式：固定单屏
- 是否使用全局默认比例：是；正交尺寸`7`
- 相机可显示边界：`X[-13,13]`、`Y[-7,7]`
- 必须同时可见：出口、门、岩浆沟、放镜区、压力板和镜像止挡墙
- 构图例外：无

## 预期流程

1. Player从中央入口出现并在提示格放置地面镜。
2. Player持续向左，MirrorClone反向向右。
3. Player在左侧跨过两格岩浆沟；MirrorClone沿右侧安全地面继续前进。
4. MirrorClone到达`Plate-A`并被右侧墙稳定挡住，`Door-A`持续开启。
5. Player通过已经开启的`Door-A`并进入左侧出口。

## 当前视觉落地（2026-08-31）

- 视觉权威为当前手工维护的Scene。`Assets/Editor/Fire010RoomBuilder.cs`仍为原灰盒重建入口，不会复现这些增量视觉内容；本次未运行Builder或重建房间。
- Terrain的75格保持原布局、碰撞和表面语义，使用本房独立`Assets/Tiles/Fire/Fire010SolidTerrain.asset`：既有白色方块Sprite乘纯色`RGBA(0.09,0.058,0.052,1)`。不添加地面纹理、噪声或渐变；ColliderType仍为Grid，与原Tile一致。岩浆继续使用原Hazard Tile配置。
- 新增独立`EnvironmentArt_Fire010`表现根，不包含碰撞或玩法组件，不移动Grid、Player生成点和机关。只复用既有美术，不修改共享Sprite导入或共享Prefab。

| 表现层 | 内容 | 水平相机跟随因子 | 排序 |
|---|---|---:|---:|
| `01_Backdrop` | `fire006_fog_light_v1`暖雾；中心`(-2,2.8)`，等比缩放2.2 | 1 | -100 |
| `02_ExtremeFar` | 两个低对比远岩柱，位置`(-4,-1.8)`、`(7,-2)`，底缘延伸画外 | 0.95 | -90、-89 |
| `03_Far` | 两个深色岩柱`(-8,-2.4)`、`(10,-1)`，以及3个顶部背景模块 | 0.85 | -70、-69、-60 |
| `08_Foreground` | `fire_foreground_frames_v1`的`_0`、`_3`下角剪影 | 0.2 | 30、31 |

- 四层均使用通用`ParallaxLayer2D`显式引用原Main Camera，仅水平跟随。其余表现层当前未填充；固定单屏不会引入镜头移动。
- 两个前景最高点分别为`Y=-3.3175`、`Y=-3.552`，均低于地形底部`Y=-3`，不覆盖玩法走廊。背景层位于Player、MirrorClone和镜子之后。
- 顶部模块复用`fire_foreground_frames_v1`子Sprite：`CeilingLeft`使用`_1`、位置`(-9,7)`、等比缩放1.8；`CeilingCenter`使用`_2`、位置`(1.8,7.2)`、缩放1.25；`CeilingRight`使用`_4`、位置`(10,7.1)`、缩放1.8。统一排序-60、颜色`RGBA(.15,.095,.07,.92)`，无碰撞。顶部内容大部分仍在画外，作为剩余视觉不足记录。
- 仅在本房Prefab实例覆盖表现字段：Door关闭颜色`(0.78,0.68,0.55,1)`；Plate未触发颜色`(0.85,0.75,0.6,1)`；Exit光标颜色`(0.32,0.7,0.5,0.23)`；Exit文字颜色`(0.85,0.9,0.78,1)`、characterSize为`0.065`。所有状态Sprite、控制源、激活/锁存颜色、触发条件及出口目标不变。
- 固定运行取图条件：1920×1080、16:9、原相机`(0,0,-10)`与正交尺寸7；默认出生后无操作、Player静止落地、无MirrorClone、未放镜。每轮暂停后以同一原相机RenderTexture方式截图。
- `w1-game-visual-review`单帧暂定评分：40.5 → 49.5 → 57 → 60 → 62 → 63；固定LIMBO基准87.5。后续第6、7轮均63分（各+0），因顶部裁切碎片缺陷撤销，已触发用户的连续两轮增益不足1分停止条件。最佳保留第5轮63分，未达到70；不继续第8轮。
- 仅优化静态表现；本房未新增动态雾、灰烬或闪烁灯光，动态D为N/A。新的动态评分模块不重算本轮历史静态分。
- 详细截图与逐项评分保存在本地`Temp/W1VisualOptimize/Fire_010/20260831/review.md`及`scores.json`；最终静态差异审计为同目录`static-audit-final.json`。最终Scene与`round5.unity`、`final-memory.unity`一致。Temp证据不作为游戏资源导入。

## 失败、重置与边界

- Player落入岩浆：完整房间重置；镜像消失、镜子回手、压力板弹起、门关闭。
- MirrorClone进入岩浆：只清除镜像并回收镜子；门按压力板实际占用恢复关闭，Player可回到中央重新尝试。
- 镜像踩板后被回收：压力板立即释放，门关闭。
- 防夹：门收到关闭命令时沿用通用防夹规则，不压死或推出角色。
- 软锁：门右侧始终可返回中央放镜区；关闭门不会把Player困在无法重置的位置。
- 逃课：关闭门洞由门上方静态Terrain封闭，不能从门上方跳过；Player自己踩板后无法同时穿过左侧门。
- 场景切换：不携带镜子、MirrorClone、压力板占用或门的临时状态。

## 最小验收标准

- Scene使用标准Tilemap层级；Terrain与Hazard语义明确且不依赖Tile名称。
- Scene没有本地Player，恰好有一个默认入口、一个Spawner和一个重置系统。
- 压力板、门和出口保持现有Prefab连接，门控制源显式引用`Plate-A`。
- 岩浆为Trigger且使用通用`Hazard2D`。
- 已进入Play Mode取得固定出生状态截图，未运行PlayMode自动测试或完整人工试玩；跳跃距离、双角色同步、门防夹、死亡/重置及场景切换仍未做运行时验证。既有碰撞、地形格位、相机、对象Transform及非表现Prefab配置已静态比较。
