# FIRE_009：替身窗口

## 状态与权威来源

- 房间ID：`FIRE_009`
- 区域：火之区域
- 当前状态：灰盒制作中；用户已明确批准本房设计与实现
- Unity Scene：`Assets/Scenes/Levels/Fire/Fire_009.unity`
- 区域规则：`docs/regions/FIRE_REGION.md`
- 地图连接：以`docs/maps/MAP.md`为准；本次不改变既有连接
- 静态几何：`docs/systems/LEVEL_GEOMETRY_SYSTEM.md`
- 相机：`docs/systems/CAMERA_SYSTEM.md`
- 重置：`docs/systems/RESET_SYSTEM.md`

## 地图连接

`FIRE_009`既有相邻节点为上方`FIRE_008`、左侧`FIRE_010`、右侧`FIRE_013`和下方`FIRE_015`。本次最小灰盒只落实从`FIRE_008`进入、完成教学后向左前往`FIRE_010`的主路径；通往`FIRE_013`和`FIRE_015`的分支保留给后续房间扩展，不改变`docs/maps/MAP.md`中的连接。

| 本房出口或入口 | 方向 | 相邻房间 | 目标入口ID | 状态 |
|---|---|---|---|---|
| `Entrance-DEFAULT` | 上 | `FIRE_008` | `DEFAULT` | 本次已配置为默认入口 |
| `Exit-A` | 左 | `FIRE_010` | `DEFAULT` | 已配置 |
| 预留分支 | 右 | `FIRE_013` | 待定 | 后续实现 |
| 预留分支 | 下 | `FIRE_015` | 待定 | 后续实现 |

## 房间定位

- 房间类型：水平投火者与镜像诱敌的安全引入房
- 主要目标：让MirrorClone成为唯一合格目标并承受一发火球，Player利用该次攻击继续向左到达出口
- 预期洞察：镜像可以作为一次性替身；镜像中弹只会清除镜像并回收镜子，不会重置Player
- 失败压力：低；Player中弹或手动重置后回到房间默认入口
- 预计完成时间：约30～60秒

## 已批准机制与排除项

### 使用机制

- 地面镜放置、MirrorClone反向水平输入及独立死亡
- 水平投火者、固定水平攻击带、蓄力、火球和冷却
- Player死亡重置、MirrorClone死亡后自动回收镜子
- 静态安全地面与固定Scene敌人Prefab实例

### 明确不包含

- 门、压力板、岩浆、周期喷发、检查点和运行时Spawner
- 抛物线火球、升降岩浆及其他未批准机制
- 房间专用运行时代码或敌人数值覆盖

## 标准网格布局

- Grid：`1×1 Unity unit`
- 房间外轮廓与相机可显示边界：`X[-13,13]`、`Y[-7,7]`
- 默认入口位于中央安全区；出口位于左侧；投火者固定在右侧

```text
┌────────────────────────────────────────────────────┐
│                                                    │
│  Exit-A        Player / M       MirrorClone        │
│    ◀               P  ◇  ········· C   ◀◀◀ ● H   │
│                                                    │
│ ██████████████████████████████████████████████████ │
└────────────────────────────────────────────────────┘
```

- `◇`：建议地面镜放置格，世界格`(0,-2)`上方
- `C`：MirrorClone向右移动后进入投火者攻击带的位置
- `H`：固定投火者，初始向左
- 点线只表示双方反向移动关系，不是实体或攻击提示

## Tilemap配置

| Tilemap层 | 使用范围 | 碰撞 | 表面语义 | 备注 |
|---|---|---|---|---|
| `Background` | 空层 | 无 | 无 | 保留标准结构 |
| `Terrain` | 地面、左右墙和顶边 | 实体Composite | `StaticSolid` | 静态、安全、允许地面镜 |
| `OneWayPlatform` | 空层 | 无 | 无 | 保留标准结构 |
| `SpecialMirrorWall` | 空层 | 无 | 无 | 本房不使用墙面镜 |
| `Hazard` | 空层 | 无 | 无 | 本房无环境危险 |
| `Decoration` | 放镜提示格 | 无 | 无 | 青色灰盒提示，不参与规则判断 |
| `Foreground` | 空层 | 无 | 无 | 保留标准结构 |

## Prefab需求

| 实例ID | 通用Prefab或组件 | 资产路径 | 初始位置/状态 | 允许的实例配置 |
|---|---|---|---|---|
| `Enemy-H1` | `HorizontalFireballEnemy2D` | `Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab` | `(7,-1.5)`；固定Scene实例；初始向左 | 只配置位置与初始朝向 |
| `Exit-A` | `RoomExit2D` | `Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab` | `(-7,-1)`；目标`Fire_010/DEFAULT` | 只配置目标Scene和入口 |

Player、镜子和MirrorClone由统一房间生成与镜子系统提供，不在Scene中复制实例。

## 敌人出生与生成

不适用；本房使用固定Scene敌人Prefab实例，不创建Spawner或`EnemySpawnPoint2D`。

## 相机配置

- 镜头模式：固定单屏
- 使用全局默认比例：是；正交尺寸`7`
- 相机可显示边界：`X[-13,13]`、`Y[-7,7]`
- 必须同时可见：入口、建议放镜位置、MirrorClone诱敌位置、投火者、火球走廊和出口
- 构图例外：无

## 预期流程

1. Player从中央安全入口出现；初始位置略超出投火者`6 units`半宽攻击带。
2. Player在提示格附近放置地面镜。
3. Player向左移动，MirrorClone按地面镜映射向右移动。
4. MirrorClone先进入攻击带；Player继续位于攻击带外，因此投火者稳定选择MirrorClone。
5. 投火者完成蓄力并向左发射；MirrorClone承受火球后死亡，镜子自动回收。
6. Player已在向左接近出口，继续前进并进入`Exit-A`。

## 失败、重置与边界

- Player被敌人本体或火球命中：清除镜像与在途火球，投火者恢复`Watching`，Player回到默认入口。
- MirrorClone被命中：只清除镜像并回收镜子；Player和房间不整体重置，投火者保持当前阶段。
- 手动重置：清除镜子、MirrorClone、目标、蓄力、冷却和在途火球，恢复入口状态。
- 重复放镜：沿用全局单镜规则；已有镜子时再次左键无效。
- 软锁：无；镜像过早回收或未成功诱敌时，Player可退回中央安全区重新放镜。
- 逃课风险：Player可能凭移动和跳跃直接避开火球；首次灰盒保留这一运行时风险，待人工试玩后再通过地形微调处理，不改变敌人共享数值。
- 场景切换：不保留镜子、MirrorClone、投火者状态或在途火球。

## 最小验收标准

- Scene使用标准Tilemap层级，`Terrain`具有显式`StaticSolid`、安全和地面镜语义。
- Scene没有序列化Player，恰好有一个默认入口、一个`RoomPlayerSpawner2D`、一个`RoomResetSystem`和一个固定投火者Prefab实例。
- 投火者保持Prefab连接、位置为`(7,-1.5)`、初始向左且不覆盖共享攻击数值。
- 入口、放镜提示、诱敌位置、投火者和出口在固定单屏内同时可见。
- 未运行PlayMode或人工试玩；目标选择、命中窗口和直接逃课仍需后续运行时确认。
