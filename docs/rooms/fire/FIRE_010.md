# FIRE_010：熔沟守门

## 状态与权威来源

- 房间ID：`FIRE_010`
- 区域：火之区域
- 当前状态：灰盒制作中；用户已明确批准第二张概念图的设计与实现
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
- 未运行PlayMode或人工试玩；跳跃距离、双角色同步位置和门防夹仍需后续运行时确认。
