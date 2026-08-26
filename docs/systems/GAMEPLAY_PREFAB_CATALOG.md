# 通用玩法Prefab目录

## 状态与职责

- 当前状态：首批通用Prefab资产已创建，等待编辑器内人工试玩验证。
- 本文记录通用玩法Prefab的计划资源路径、对象结构、可配置字段、禁止覆盖项和验证要求。
- 本文不重新定义玩法规则。门与压力板行为以`docs/systems/DOOR_SYSTEM.md`为准，复活点以`docs/systems/CHECKPOINT_SYSTEM.md`为准，重置以`docs/systems/RESET_SYSTEM.md`为准，静态地形与危险区以`docs/systems/LEVEL_GEOMETRY_SYSTEM.md`为准。
- 房间文档只记录Prefab实例、稳定ID、空间位置、初始状态和允许的实例覆盖，不得复制Prefab内部的通用行为。

## 计划目录

```text
Assets/Prefabs/Gameplay/
├─ Characters/
│  └─ Player.prefab
├─ Mirrors/
│  └─ PlacedMirror.prefab
├─ Platforms/
│  └─ MovingPlatform2D.prefab
├─ Surfaces/
│  ├─ GroundConveyor2D.prefab
│  └─ FreezingGroundCell2D.prefab
├─ Snow/
│  ├─ SnowmanGate2D.prefab
│  └─ TemporaryCarrotPickup2D.prefab
├─ Hazards/
│  ├─ PeriodicSnowfall2D.prefab
│  └─ RisingLava2D.prefab
├─ Wind/
│  ├─ WindColumn2D.prefab
│  ├─ MovingTornado2D.prefab
│  ├─ TornadoGenerator2D.prefab
│  └─ WindDeflector2D.prefab
├─ Enemies/
│  ├─ FreezablePatrolEnemy2D.prefab
│  ├─ VerticalWallPatrolEnemy2D.prefab
│  ├─ WindRayEnemy2D.prefab
│  ├─ SacrificialWindRayEnemy2D.prefab
│  ├─ HorizontalFireballEnemy2D.prefab
│  └─ Projectiles/
│     └─ HorizontalFireballProjectile2D.prefab
├─ Doors/
│  ├─ Door2D.prefab
│  └─ PermanentLatchDoorGroup2D.prefab
├─ Switches/
│  ├─ PressurePlate2D.prefab
│  └─ WindTurbineSwitch2D.prefab
├─ Checkpoints/
│  └─ Checkpoint2D.prefab
└─ Exits/
   └─ RoomExit2D.prefab
```

- 移动平台、地面传送带、门、压力板、检查点和出口路径已经创建。
- **非必须：除非用户明确要求，否则不自动追加代表性房间试玩、PlayMode测试或完整自动测试。**未执行时记录未验证风险；公共机制发生变化时只维护与变化直接相关的最小测试定义。
- `FreezablePatrolEnemy2D.prefab`已经创建并通过独立EditMode与PlayMode测试；正式房间只允许覆盖已批准的巡逻实例参数。
- `VerticalWallPatrolEnemy2D.prefab`已经创建；正式房间只允许覆盖已批准的墙面侧别、竖直路径、速度、等待和视觉参数。
- `WindRayEnemy2D.prefab`已经创建并用于`WIND_001`灰盒；统一数值已确认。**非必须：除非用户明确要求，否则不运行其独立EditMode与PlayMode测试**，未运行状态作为风险记录。
- `HorizontalFireballEnemy2D.prefab`与其火球Prefab已有已确认规则、运行时代码和可重复构建器，并已由Unity Editor生成；尚未获准进入正式房间。**非必须：除非用户明确要求，否则不进行PlayMode试玩验证。**
- `MovingTornado2D.prefab`使用`Assets/Art/Generated/Wind/small_tornado_3frame_handpainted.png`的三帧手绘透明Sprite动画，循环速率`8 FPS`。动画只改变Sprite，不改变`0.8×0.8 units`伤害Trigger、速度、方向、门阻挡或重置规则。
- `Player.prefab`由通用房间生成系统管理，不作为房间Scene中的重复Prefab实例；完整结构、视觉、入口绑定和生命周期规则见`docs/systems/PLAYER_PREFAB.md`。
- `PlacedMirror.prefab`只由`MirrorPlayer2D`在成功放置时生成；`Held`和`Unobtained`状态不在Player下保留镜子视觉。
- 静态墙壁、台阶、低顶、固定平台和返回通道使用标准Tilemap结构，不创建房间专用Prefab。
- 固定岩浆等静态危险区使用`Hazard` Tilemap及统一危险组件，不为单个房间创建岩浆Prefab。
- 周期升降岩浆使用`Assets/Prefabs/Gameplay/Hazards/RisingLava2D.prefab`；视觉与Trigger共同连续移动，默认周期为`1/2/1.5/2/2.5s`，房间不得改变伤害对象、镜子交互、非支撑性质或重置规则。
- `RisingLava2D.prefab`正式灰盒视觉使用AI生成并人工选定的手绘透明Sprite：`Assets/Art/Generated/Fire/lava_rising_handpainted.png`；Sprite只负责表现，不改变`2×1 units`危险边界、移动距离或周期。
- 静态寒冰地面使用`FrozenGround` Tilemap，不把单块寒冰制作成Prefab；移动寒冰平台仍使用移动平台Prefab。
- 新增冻结地面使用`FreezingGroundCell2D.prefab`按整数格位置组合；房间不得覆盖统一冻结参数。
- `Player.prefab`与`FreezablePatrolEnemy2D.prefab`挂载共享`FreezingVisual2D`；MirrorClone由统一生成流程运行时自动挂载。房间不得复制或覆盖其冻结颜色、霜层透明度和响应速度。
- `SnowmanGate2D.prefab`与`TemporaryCarrotPickup2D.prefab`使用显式Scene引用配对，只表达同房间临时挡路状态。
- `PeriodicSnowfall2D.prefab`复用通用周期危险状态机；房间只配置暴露区位置和尺度，不覆盖雪区统一周期。

## 敌人Prefab原型与Variant登记

敌人原型的判定、Prefab Variant适用条件和禁止覆盖项以`docs/systems/ENEMY_SYSTEM.md`为准。本目录负责记录每个基础Prefab、其Variant资产以及允许覆盖的具体字段。

当前敌人Prefab关系如下：

| 敌人原型 | 基础Prefab | Variant | 关系状态 |
|---|---|---|---|
| 可冻结地面巡逻敌人 | `FreezablePatrolEnemy2D.prefab` | 无 | 独立基础Prefab |
| 竖直墙面巡逻敌人 | `VerticalWallPatrolEnemy2D.prefab` | 无 | 独立基础Prefab |
| 风区逐风鳐 | `WindRayEnemy2D.prefab` | 无 | 独立基础Prefab |
| 火区投火者 | `HorizontalFireballEnemy2D.prefab` | 无 | 独立基础Prefab，等待PlayMode试玩验证 |

上述基础Prefab互不构成Variant关系。以后新增Variant时，必须在对应原型条目中记录：

- Variant资产路径及其基础Prefab。
- 复用目的和允许使用的区域或房间范围。
- 相对于基础Prefab的全部批准覆盖项。
- 共享配置资产及不得由Variant覆盖的字段。
- 对基础Prefab与全部既有Variant执行的兼容性验证。

Scene中的单个敌人实例不是Variant。位置、巡逻端点、初始方向、守卫点等房间专用配置应保留为Scene实例覆盖，不得为每个房间创建敌人Variant。房间文档使用Variant时，必须同时记录敌人原型、实际Variant资产路径和房间实例覆盖；未使用Variant时记录对应基础Prefab。

## `Player.prefab`

资产路径：`Assets/Prefabs/Gameplay/Characters/Player.prefab`

Player Prefab的组件结构、角色图片、入口生成、重置、场景切换和迁移规则统一见`docs/systems/PLAYER_PREFAB.md`。房间Scene只保存`RoomEntrance2D`和通用生成组件，不保存Player实例，也不得制作房间专用Player变体。

## `PlacedMirror.prefab`

资产路径：`Assets/Prefabs/Gameplay/Mirrors/PlacedMirror.prefab`

Prefab使用`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Coin/coin_gold_side.png`，保持原始宽高比并显示在Player和MirrorClone前层。它不包含实体Collider，不作为平台或阻挡物；位置、旋转、生成、回收和销毁完全由`MirrorPlayer2D`及`docs/MIRROR_MECHANIC.md`统一控制。房间不得直接放置该Prefab，也不得覆盖镜子图片、尺寸或生命周期。

## `GroundConveyor2D.prefab`

资产路径：`Assets/Prefabs/Gameplay/Surfaces/GroundConveyor2D.prefab`

```text
GroundConveyor2D
└─ Visual
   ├─ BeltRenderer
   └─ DirectionIndicator
      └─ Marker
```

根对象包含Static `Rigidbody2D`、非Trigger `BoxCollider2D`、安全静态`Conveyor`表面语义和`GroundConveyor2D`。`Visual`包含由`ConveyorVisual2D`驱动的带体颜色与方向箭头动画。

Prefab负责：

- 向真正由水平上表面支撑的Player或MirrorClone提供世界空间表面速度。
- 保证双方使用同一方向、速度和启停规则。
- 拒绝侧面、底面和侧向重力MirrorClone的错误表面速度。
- 管理初始启用状态、运行时启停、视觉同步和房间重置。
- 返回`Conveyor`语义，使镜子放置系统稳定拒绝该表面。

房间实例允许覆盖位置、长度、左右方向、`0.5～4.5 units/s`速度、初始启用状态以及不改变玩法边界的视觉和声音。实例不得旋转，不得覆盖Player/MirrorClone资格、镜子交互、基础移动参数、伤害或重置规则。

Prefab验证要求：

- 必需组件、Collider、视觉与内部引用完整。
- 方向箭头在接触前可见，动画方向与实际表面速度一致，停用时视觉静止且可区分。
- 无输入Player和正常重力MirrorClone获得相同表面速度。
- 侧向重力MirrorClone接触侧面时不获得水平传送带速度。
- 启停和重置不留下累计速度或接触引用。
- 镜子放置失败不生成镜子或镜像，也不改变传送带状态。

## `MovingPlatform2D.prefab`

计划路径：`Assets/Prefabs/Gameplay/Platforms/MovingPlatform2D.prefab`

```text
MovingPlatform2D
└─ Visual
```

`Visual`默认使用`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_stone_cloud_middle.png`并横向平铺；图片以`64 PPU`、Point Filter和Full Rect导入。默认Collider与图片不透明边界对齐。

根对象包含Kinematic `Rigidbody2D`、非Trigger `BoxCollider2D`、返回非静态`DynamicSurface`的`SurfaceSemantic2D`和`MovingPlatform2D`。通用行为以`docs/systems/MOVING_OBJECTS.md`为准。

Prefab负责：

- 在两个本地坐标端点间按固定速度确定性往返。
- 执行端点等待、初始相位、初始方向和初始运行状态。
- 稳定承载Player、MirrorClone及其他受到平台支撑的物理对象。
- 按MirrorClone局部重力判断支撑方向。
- 在房间重置时恢复位置、方向、计时和运行状态。
- 保持动态表面语义，使镜子放置系统拒绝该平台。

房间实例允许覆盖路径锚点、旋转、平台尺寸、两个本地端点、速度、等待、初始相位、初始方向、初始运行状态和区域视觉；不得覆盖承载资格、表面语义、镜子交互或重置规则。

Prefab验证要求：

- 必需组件和`Visual`引用完整，Collider与可见轮廓一致。
- Rigidbody2D保持Kinematic、插值和连续碰撞检测。
- Prefab根坐标为零，端点使用本地偏移，不携带房间编号或正式世界坐标。
- Player和MirrorClone均能稳定随平台移动并主动离开。
- 重置恢复初始相位、方向、等待计时和运行状态。
- `SurfaceSemantic2D`始终返回安全、非静态`DynamicSurface`。

## `SinkingEarthBlock2D.prefab`

资产路径：`Assets/Prefabs/Gameplay/Earth/SinkingEarthBlock2D.prefab`

```text
SinkingEarthBlock2D
├─ Visual
└─ TopMarker
```

根对象包含Kinematic `Rigidbody2D`、非Trigger `BoxCollider2D`、安全且非静态的
`DynamicSurface`语义和`SinkingEarthBlock2D`。Prefab按上表面实际承重质量沿世界竖直方向下沉，卸重后缓慢恢复，并通过通用重置协议恢复初始高度。它不包含`MirrorSurface2D`，任何状态都不能放置镜子。

完整规则、实例可覆盖字段和验收要求见`docs/systems/SINKING_EARTH_BLOCK_SYSTEM.md`。

## `FreezablePatrolEnemy2D.prefab`

资产路径：`Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab`（已创建）

```text
FreezablePatrolEnemy2D
├─ Visual
│  ├─ ActiveVisual
│  ├─ FrozenVisual
│  └─ FreezeEffect
├─ BodyCollider
├─ DamageTrigger
├─ GroundProbe
└─ SurfaceProbe
```

根对象至少包含：

- `Rigidbody2D`，禁止物理旋转。
- 与活动和冻结视觉轮廓一致的实体Collider。
- 管理巡逻、冻结和重置状态的通用组件。
- 统一房间重置接口。

Prefab负责：

- 管理`Active`、`Freezing`和`Frozen`状态。
- 在显式配置的左右端点之间确定性地来回巡逻。
- 到达端点后按配置时间停顿并转向。
- 通过脚部查询读取`FrozenGround`表面语义。
- 对Player和MirrorClone执行`docs/systems/ENEMY_SYSTEM.md`定义的伤害规则。
- 冻结后停止巡逻、关闭伤害并切换为安全可站立状态。
- 在房间重置时恢复初始位置、方向、巡逻阶段、伤害和视觉状态。

房间实例允许覆盖：

- Transform位置与初始朝向。
- 左右巡逻端点。
- 移动速度。
- 端点等待时间。
- 不改变玩法轮廓和状态规则的雪区视觉配置。

房间实例不得覆盖：

- Player和MirrorClone的伤害资格。
- 脚部接触`FrozenGround`的冻结条件。
- 冻结后关闭伤害和允许双方踩踏的规则。
- 压力板触发资格、镜子放置资格和长期存档规则。
- 重置和场景切换规则。

Prefab资产不得包含正式房间的默认世界坐标、房间编号或巡逻路线。活动与冻结必须是同一个Prefab实例的状态切换，不创建独立的`FrozenEnemy` Prefab。

Prefab验证要求：

- Prefab资产可独立加载，必需组件和内部引用完整。
- `DamageTrigger`在`Active`和`Freezing`状态启用，只在`Frozen`状态关闭。
- 原始敌人视觉在冻结过程中和冻结后保持显示；冰蓝同轮廓Overlay随进度叠加，完全冻结时再显示外围冰壳效果。
- 敌人在首次接触寒冰格内逐渐减速并结冰，到格中心才进入`Frozen`。
- 冻结后速度为零且不再执行巡逻。
- 重置后恢复初始位置、方向、巡逻阶段、伤害和视觉。
- 巡逻端点为空、顺序错误或范围不足时报告配置错误。
- 修改Tile、Sprite、GameObject或房间名称不会改变冻结结果。

## `VerticalWallPatrolEnemy2D.prefab`

资产路径：`Assets/Prefabs/Gameplay/Enemies/VerticalWallPatrolEnemy2D.prefab`

```text
VerticalWallPatrolEnemy2D
├─ Visual
│  └─ BodyVisual
├─ BodyCollider
├─ DamageTrigger
└─ WallProbe
```

根对象包含Kinematic `Rigidbody2D`、禁止物理旋转的实体Collider、独立Damage Trigger、管理竖直路径与墙面验证的通用组件，以及统一房间重置接口。完整行为以`docs/systems/ENEMY_SYSTEM.md`为准。

Prefab负责：

- 在显式配置的本地上下端点之间确定性巡逻。
- 在每个固定物理步验证配置侧的安全静态`StaticSolid`墙面。
- 执行端点等待、初始方向、墙面意外中断时的停止和反向。
- 对Player和MirrorClone执行统一敌人伤害规则。
- 在重置时恢复路径锚点、初始位置、方向、等待计时、墙面接触和伤害状态。

房间实例允许覆盖位置、墙面侧别、上下端点、初始方向、`0.5～3.0 units/s`速度、`0～1.0 s`等待时间，以及不改变玩法轮廓的区域视觉。不得覆盖有效墙面类型、伤害资格、冻结资格、机关资格、路径方向、重置或长期存档规则。

Prefab验证要求：

- Prefab根坐标为零，路径端点使用本地竖直偏移，不包含房间编号或正式世界坐标。
- Rigidbody2D保持Kinematic、零重力、插值和连续碰撞检测；默认Sprite按`128 PPU`、Point Filter和Full Rect导入。
- 上下端点非法、初始位置无有效墙面或路径不竖直时停止运动、关闭伤害并报告错误。
- 正常路线不依赖Tile、Sprite、GameObject、Tilemap名称或Collider边界自动生成。
- Player和MirrorClone分别触发正确死亡流程，敌人不推动、夹住或形成可站立表面。
- 手动重置、Player死亡和重新进入房间完整恢复初始状态；MirrorClone单独死亡不重置敌人。

Prefab默认使用`Assets/Art/Kenney/NewPlatformerPack/Sprites/Enemies/Double/Snail/snail_walk_a.png`，实体Collider为`0.72 × 0.90 units`，Damage Trigger为`0.82 × 0.98 units`，二者中心均向配置墙面偏移`0.10 unit`；墙面探测距离为`0.16 unit`。

## `WindRayEnemy2D.prefab`

资产路径：`Assets/Prefabs/Gameplay/Enemies/WindRayEnemy2D.prefab`（已创建）

```text
WindRayEnemy2D
├─ Visual
│  ├─ BodyVisual
│  ├─ TargetMarker
│  └─ DashTrail
├─ DamageTrigger
└─ LineOfSightOrigin
```

根对象包含：

- Kinematic `Rigidbody2D`，禁止物理旋转。
- 与可见身体轮廓一致的伤害Trigger。
- 身体与伤害轮廓约为`1.15 × 0.7 units`；游戏中不显示外围探测圈。
- `BodyVisual`使用Kenney Double Bee三帧循环飞行动画；Animator只负责Sprite表现，不驱动玩法位移或伤害。
- 管理`Guarding`、`Windup`、`Dashing`、`Recovering`和`Returning`的通用组件。
- 统一房间重置接口。
- 读取风区统一逐风鳐数值配置的引用；Prefab和房间实例不复制感知与攻击常量。

Prefab负责：

- 按固定世界距离和无遮挡条件感知Player与MirrorClone。
- 确定性选择最近目标，在平局时使用已批准规则。
- 记录锁定点、执行预警和直线冲刺。
- 命中Player或MirrorClone时调用对应的统一生命周期流程。
- 管理喘息、返回守卫点、本体警觉变化、目标标记和状态音效。
- 在房间重置时恢复守卫点、初始朝向、伤害、目标、速度、计时和视觉状态。

房间实例允许覆盖：

- Transform位置，作为初始守卫点。
- 初始视觉朝向。
- 不改变玩法轮廓、感知边界或反馈含义的风区视觉配置。

房间实例不得覆盖：

- 感知半径、预警时间、冲刺速度、最大冲刺行程、喘息时间或返回速度。
- Player与MirrorClone的目标资格、最近目标和平局规则。
- 遮挡、锁定点、命中、镜子交互、门防夹或重置规则。
- 压力板触发资格、平台资格、镜子放置资格和长期存档规则。

Prefab资产不得包含正式房间世界坐标、房间编号、诱敌点或通过路线。完整行为与统一数值以`docs/systems/WIND_RAY_ENEMY.md`为准。

Prefab验证要求：

- Prefab资产可独立加载，必需组件和内部引用完整。
- 游戏内不绘制精确感知范围；接近边缘时只出现轻微本体警觉变化。
- 目标标记和冲刺方向与已记录的锁定点一致。
- 冲刺不追踪、不转向且不穿透实体Collider。
- MirrorClone死亡或主动回收不会重置逐风鳐。
- 重置后恢复守卫点和`Guarding`状态，不残留目标、速度、计时或视觉。

## `SacrificialWindRayEnemy2D.prefab`

资产路径：`Assets/Prefabs/Gameplay/Enemies/SacrificialWindRayEnemy2D.prefab`。

- 与`WindRayEnemy2D.prefab`共享层级、碰撞轮廓、动画、统一数值资产和冲刺状态机。
- 根`WindRayEnemy2D`组件的命中结果固定为`DefeatAfterHit`，房间实例不得覆盖。
- 命中MirrorClone后清理镜像并进入`Defeated`；命中Player后由完整房间重置恢复。
- 因命中后的敌人生命周期不同，它是独立基础Prefab，不是Unity Prefab Variant。

## 风区环境Prefab

- `Assets/Prefabs/Gameplay/Wind/WindColumn2D.prefab`：常吹与周期风柱共用的Trigger体积、方向反馈和确定性周期控制。
- `Assets/Prefabs/Gameplay/Wind/MovingTornado2D.prefab`：Kinematic移动、角色伤害、实体阻挡和最大路程生命周期。
- `Assets/Prefabs/Gameplay/Wind/TornadoGenerator2D.prefab`：显式Prefab引用、固定生成间隔、数量上限、出生占用和统一重置清理。
- `Assets/Prefabs/Gameplay/Wind/WindDeflector2D.prefab`：匹配来风、实体风影、固定`90°`输出、压力板切换和移动龙卷风转向。
- `Assets/Prefabs/Gameplay/Switches/WindTurbineSwitch2D.prefab`：持续风接收、方向匹配、直接风与导风输出查询、普通门控制和重置释放。
- 完整结构、允许配置与统一数值见`docs/systems/WIND_ENVIRONMENT_SYSTEM.md`。

## `EruptionHazard.prefab`

- 通用Prefab：`Assets/Prefabs/Gameplay/Hazards/EruptionHazard.prefab`
- 根对象使用`EruptionHazard2D`，子对象使用通用`Hazard2D` Trigger。
- 默认固定周期为预警`1s`、危险`1s`、冷却`2s`，重置后从预警开始。
- Prefab负责周期、危险启停和基础颜色反馈；房间不得复制周期运行时代码。

## `PressurePlate2D.prefab`

计划路径：`Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab`

```text
PressurePlate
├─ Visual
└─ FeedbackEffect（可选）
```

根对象至少包含：

- `BoxCollider2D`，配置为Trigger。
- `PressurePlate2D`。

`Visual`至少包含`SpriteRenderer`，用于显示弹起、临时按下和永久锁存三种可区分状态。

通用压力板使用以下Double目录状态图：

- 弹起：`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Switch/switch_yellow.png`
- 按下或永久锁存：`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Switch/switch_yellow_pressed.png`

永久锁存继续通过统一青色调、门状态和声音区分，不创建另一套压力板Prefab，也不绘制压力板到门的连线。

Prefab负责：

- 只接受Player和MirrorClone作为有效占用者。
- 管理多Collider和多对象占用，最后一个有效对象离开后才释放。
- MirrorClone死亡、消失或镜子回收时清理对应占用。
- 提供激活状态，并参与统一房间重置。

Prefab不负责：

- 决定控制哪一扇门。
- 按Player或MirrorClone身份写死不同压力板。
- 单独保存永久锁存进度。

房间文档中的`P`或`C`后缀只表示预期解法中的占用者，不改变通用触发资格。

## `Door2D.prefab`

计划路径：`Assets/Prefabs/Gameplay/Doors/Door2D.prefab`

```text
Door
├─ Visual
└─ Blocker
```

Prefab至少包含：

- `Door2D`。
- 与可见门体一致的实体Collider。
- 关闭、临时开启和永久开启的可区分表现。

门体视觉由上下两段Double目录Sprite组成：

- 关闭：`door_closed.png`与`door_closed_top.png`
- 开启：`door_open.png`与`door_open_top.png`

四张图片位于`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Door/`。上下两张图各保持一个标准地形格高，整扇门固定为两个格子高并完整占据一个标准格宽，标准Collider为`1 × 2 units`，不得在房间实例中拉伸。临时开启使用原色开启图，永久锁存使用统一青色调；两种开启状态仍复用同一个`Door2D.prefab`。

Prefab负责：

- 根据通用控制源切换门状态。
- 保证Player、MirrorClone、敌人或其他动态对象与门体接触时不会生成开门命令或改变门的视觉、锁存和Collider状态。
- 仅在门已经开启并收到关闭命令后，对Player、MirrorClone、活动敌人和冻结敌人执行统一防夹规则；防夹只能延迟关闭，不能把逻辑目标状态改回开启。
- 在房间重置时恢复正确状态。

普通单板门通过Scene实例上的序列化`Door2D.controlSource`引用独立的`PressurePlate2D` Prefab实例；该引用可在Inspector中配置并随Scene保存，不依赖房间脚本或Builder在运行时调用`Configure`。永久锁存门控组继续由`PermanentLatchDoorGroup2D`直接管理门状态，不配置该单板控制源。

房间实例允许覆盖门的位置，但不得覆盖两格标准尺寸、防夹、碰撞资格或重置规则。需要更高门洞时，剩余部分使用标准`Terrain` Tilemap静态墙封闭，不能拉伸门或添加随门消失的隐藏Collider。房间必须把敌人的目标冻结位置配置在门关闭范围之外。

## `PermanentLatchDoorGroup2D.prefab`

计划路径：`Assets/Prefabs/Gameplay/Doors/PermanentLatchDoorGroup2D.prefab`

```text
PermanentLatchDoorGroup
├─ PlateA                 ← PressurePlate2D嵌套Prefab
├─ PlateB                 ← PressurePlate2D嵌套Prefab
└─ Door                   ← Door2D嵌套Prefab
```

根对象包含`PermanentLatchDoorGroup2D`，并在Prefab内部固定引用：

- `plateA`指向`PlateA`。
- `plateB`指向`PlateB`。
- `door`指向`Door`。
- `connectionRenderers`保持为空；通用组合Prefab不绘制压力板到门的连线。

房间实例必须配置：

- 全游戏唯一且格式正确的`DoorGroupId`。
- 两块压力板和门的空间位置。
- 门的合法尺寸。

房间实例不得覆盖：

- 双板永久锁存条件。
- 单板临时开启逻辑。
- Player和MirrorClone的触发资格。
- 保存、读取和重置行为。
- 门的防夹规则。

同一房间的多组机关必须复用同一个组合Prefab，通过实例ID和Transform覆盖形成布局差异，不得复制为多个房间专用Prefab资产。

### `DoorGroupId`模板约束

- Prefab资产本身允许`DoorGroupId`为空，因为模板不属于任何正式房间。
- 正式Scene中的每个实例必须覆盖为有效ID；空ID不得进入测试或构建。
- 不得在Prefab资产中填写会被多个实例复制的默认正式ID。
- 编辑器验证必须检查ID格式、全项目唯一性、内部引用完整性、两块压力板不是同一对象，以及同一门或压力板没有被多个门控组引用。
- 当前`PermanentLatchDoorGroup2D`在内部引用已配置但ID为空时会报告无效配置；实现Prefab前必须调整模板与Scene实例的验证时机，并为该行为增加测试。该调整不得改变门控玩法规则。

## `Checkpoint2D.prefab`

计划路径：`Assets/Prefabs/Gameplay/Checkpoints/Checkpoint2D.prefab`

```text
Checkpoint
├─ Visual
└─ ActivationEffect（可选）
```

根对象至少包含Trigger Collider和`Checkpoint2D`。

- 只有Player能够激活。
- MirrorClone不能激活。
- 房间实例只配置位置和视觉方向。
- 复活位置必须能容纳完整Player Collider，且不能与墙壁、门、危险区或动态物体重叠。

## `RoomExit2D.prefab`

计划路径：`Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab`

Prefab负责统一场景出口检测和切换请求。房间实例必须配置目标房间、目标入口和出口方向，不得自行携带镜子、MirrorClone或临时机关状态跨场景。

## 正式房间Scene配置规则

动态对象统一放在：

```text
Room
└─ Gameplay
   └─ DynamicObjects
      ├─ DoorGroup实例
      ├─ Enemy实例
      ├─ Checkpoint实例
      └─ RoomExit实例
```

允许的Prefab实例覆盖：

- Transform位置与朝向。
- 权威系统允许配置的尺寸。
- 全局唯一稳定ID。
- 初始状态。
- 目标房间和目标入口。
- 纯表现连接线的长度和方向。
- 敌人的初始朝向、巡逻端点、速度和端点等待时间。

禁止的Prefab实例覆盖：

- Player或MirrorClone交互资格。
- 锁存、存档和重置规则。
- 门的防夹行为。
- Player、镜子或MirrorClone的输入、碰撞、重力和生命周期规则。

## 验证要求

通用Prefab创建后至少验证：

- Prefab资产可加载，必需组件和内部引用完整。
- Scene实例不存在空ID、非法ID或重复`DoorGroupId`。
- 单块压力板只产生临时开门，两块压力板同时激活只锁存一次。
- MirrorClone回收、死亡和消失会释放未锁存压力板占用。
- 已锁存门控组跨手动重置、玩家死亡、重新进房和存档读取保持完成。
- 未锁存门控组在重置后恢复初始状态。
- 多个门控组相互独立，不共享门、压力板或保存键。
- 完全关闭的门被Player、MirrorClone、活动敌人或冻结敌人从外侧接触时仍保持关闭、关闭视觉和实体阻挡，且不改变颜色。
- 已开启的门收到关闭命令且关闭路径被占用时只延迟关闭；对象离开后完成关闭，不产生新的开启命令。
- 门不会夹死、推出、穿过或永久困住Player、MirrorClone、活动敌人或冻结敌人。
- Checkpoint只能由Player激活。
- 可冻结巡逻敌人按固定范围来回移动，踩上`FrozenGround`后只冻结一次。
- 冻结敌人停止伤害、可被双方稳定踩踏，并在重置后完整恢复活动状态。
- 逐风鳐按距离、遮挡和最近目标规则确定性锁定Player或MirrorClone，完成冲刺、喘息和返回循环。
- 逐风鳐命中Player与MirrorClone分别进入正确生命周期流程，并在重置后完整恢复守卫状态。
- Scene中没有为单个房间解包并复制通用玩法逻辑。
