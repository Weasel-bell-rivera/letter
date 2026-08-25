# FIRE_011：双重冷却

## 状态与权威来源

- 房间ID：`FIRE_011`
- 区域：火之区域
- 当前状态：灰盒制作中；用户已明确批准按第三张概念图继续实现
- Unity Scene：`Assets/Scenes/Levels/Fire/Fire_011.unity`
- 地图连接：以`docs/maps/MAP.md`为准；本次不改变连接
- 火区规则：`docs/regions/FIRE_REGION.md`
- 门与压力板：`docs/systems/DOOR_SYSTEM.md`
- 重置：`docs/systems/RESET_SYSTEM.md`

## 地图连接

`FIRE_011`位于`FIRE_010`左侧、`FIRE_012`右侧。本次落实从右侧`FIRE_010`进入、完成谜题后向左进入`FIRE_012`的主路径。

| 本房出口或入口 | 方向 | 相邻房间 | 目标入口ID | 状态 |
|---|---|---|---|---|
| `Entrance-DEFAULT` | 右 | `FIRE_010` | `DEFAULT` | 已配置 |
| `Exit-A` | 左 | `FIRE_012` | `DEFAULT` | 已配置；`FIRE_012`仍为草案 |

## 房间定位

- 房间类型：周期喷发、镜像诱敌与单板门组合房
- 主要目标：在喷发冷却开始时让Player向左通过喷发和门，同时让MirrorClone向右踩板并吸引投火者
- 预期洞察：先观察喷发周期，再用一次共享输入同时制造门开启和镜像替身窗口
- 难度：普通；预计完成时间约2～3分钟
- 失败压力：低；入口、喷发右侧和门右侧均可安全等待或回退

## 已批准机制与排除项

### 使用机制

- 地面镜与MirrorClone反向水平移动
- 单压力板持续控制门
- 周期喷发：预警`1s`、危险`1s`、冷却`2s`，重置后从预警开始
- 水平投火者与MirrorClone替身承伤
- Player死亡重置与MirrorClone死亡自动回收镜子

### 明确不包含

- 固定岩浆、双压力板锁存、检查点、Spawner和房间专用运行时代码
- 抛物线火球、升降岩浆及其他未批准机制

## 标准网格布局

- Grid：`1×1 Unity unit`
- 房间外轮廓与相机边界：`X[-15,15]`、`Y[-7,7]`

```text
┌────────────────────────────────────────────────────────────┐
│                                                            │
│ Exit ◀  D ─── [喷发] ─── P/M ───────── [A]█  ◀◀◀ ● H      │
│                                                            │
│ ██████████████████████████████████████████████████████████ │
└────────────────────────────────────────────────────────────┘
```

- `P/M`：默认入口与建议放镜位置
- `[喷发]`：竖直周期危险区，两侧都有安全等待区
- `[A]`：单压力板，右侧静态墙稳定阻挡MirrorClone
- `D`：由`Plate-A`持续控制的出口门
- `H`：固定投火者，MirrorClone接近压力板后才进入攻击带

## Tilemap配置

| Tilemap层 | 使用范围 | 碰撞 | 表面语义 | 备注 |
|---|---|---|---|---|
| `Background` | 空层 | 无 | 无 | 标准结构 |
| `Terrain` | 地面、边界、门封墙和压力板止挡墙 | 实体Composite | `StaticSolid` | 安全、允许地面镜 |
| `OneWayPlatform` | 空层 | 无 | 无 | 标准结构 |
| `SpecialMirrorWall` | 空层 | 无 | 无 | 不使用 |
| `Hazard` | 空层 | 无 | 无 | 动态喷发使用Prefab |
| `Decoration` | 放镜提示 | 无 | 无 | 不参与玩法判断 |
| `Foreground` | 空层 | 无 | 无 | 标准结构 |

## Prefab需求

| 实例ID | 通用Prefab | 资产路径 | 初始位置/状态 | 实例配置 |
|---|---|---|---|---|
| `Eruption-A` | `EruptionHazard2D` | `Assets/Prefabs/Gameplay/Hazards/EruptionHazard.prefab` | `(-3.5,0)`；预警开始 | 统一`1/1/2s`周期 |
| `Plate-A` | `PressurePlate2D` | `Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab` | `(4,-1.7)`；弹起 | 通用占用规则 |
| `Door-A` | `Door2D` | `Assets/Prefabs/Gameplay/Doors/Door2D.prefab` | `(-8.5,-1.5)`；关闭 | 显式引用`Plate-A` |
| `Enemy-H1` | `HorizontalFireballEnemy2D` | `Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab` | `(9.5,-1.5)`；向左 | 只覆盖位置与朝向 |
| `Exit-A` | `RoomExit2D` | `Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab` | `(-11,-1)` | 目标`Fire_012/DEFAULT` |

固定投火者直接放在Scene中；本房不使用敌人Spawner。

## 相机配置

- 镜头模式：固定单屏；全部关键因果对象同时可见
- 是否使用全局默认比例：是；正交尺寸`7`
- 相机可显示边界：`X[-15,15]`、`Y[-7,7]`
- 必须同时可见：当前阶段的喷发状态、Player路径、门，以及MirrorClone接近压力板与投火者的关系
- 构图例外：无

## 预期流程

1. Player在入口安全区观察喷发完成预警和危险阶段。
2. 喷发进入冷却后，Player放置地面镜并持续向左。
3. Player向左通过喷发区域；MirrorClone反向向右接近压力板。
4. MirrorClone踩下`Plate-A`并被右侧墙挡住，`Door-A`开启。
5. MirrorClone进入投火者攻击带并承受火球；Player在其消失前通过已经开启的门。
6. 镜像中弹后自动回收，门关闭在Player身后；Player进入左侧出口。

## 失败、重置与边界

- Player被喷发击中：完整重置；喷发回到预警起点，门关闭，敌人回到`Watching`。
- MirrorClone被喷发或火球命中：只清除镜像并回收镜子；压力板释放、门关闭，喷发和敌人不整体重置。
- 镜像过早死亡：Player可留在门右侧安全区返回中央重新放镜。
- 门上方由静态Terrain封闭，并遵守通用防夹规则。
- 不放镜时`Door-A`保持关闭，不能直接进入出口。
- 场景切换不保留喷发计时、镜子、MirrorClone、压力板、门、敌人目标或火球。

## 最小验收标准

- 标准Tilemap层级和显式Terrain语义完整。
- 通用喷发Prefab具有预警、危险、冷却视觉和危险Trigger。
- Scene没有本地Player，门显式引用压力板，投火者保持Prefab连接。
- 不放镜时门关闭，不存在直接走到出口的路线。
- 未运行PlayMode或人工试玩；周期容错、镜像中弹前的过门时间和防夹仍需后续确认。
