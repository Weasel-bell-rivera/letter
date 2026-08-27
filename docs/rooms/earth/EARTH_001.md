# EARTH_001：墙行初见

## 状态

- 当前状态：已保留原竖墙巡逻灰盒并加入一个固定投火兵；通用Prefab、Scene与专项自动测试均已完成，等待人工手感试玩。
- 是否允许制作灰盒：是；允许保留Enemy-A并加入本文批准的一个Enemy-B投火兵。
- 已批准房间名称：墙行初见。

## 地图登记

- 地图编号：`EARTH_001`。
- 地图来源：`docs/maps/MAP.md`。
- 所属区域：土之区域。
- 已登记相邻房间：上方`WIND_018`和左侧`EARTH_002`。
- 世界进入顺序与土区解锁条件尚未获批；本次简单灰盒不创建正式出口，也不改变已登记连接。
- 后续若实现正式房间出口，必须先与`WIND_018`、`EARTH_002`的房间文档同步目标入口ID。

## Unity资源

- 计划Scene：`Assets/Scenes/Levels/Earth/Earth_001.unity`。
- 当前Scene状态：已创建并登记到Build Settings。
- 编辑器构建器：`Assets/Editor/Earth001RoomBuilder.cs`。
- Terrain Tile：`Assets/Tiles/Earth/Earth001TerrainGraybox.asset`。
- 计划结构：标准Tilemap静态地形与通用Prefab动态对象组合。
- 不创建`Earth001Controller`或其他房间专用玩法脚本。

## 房间定位

- 房间类型：双敌人观察与镜像诱导原型验证房。
- 主要目标：保留竖墙巡逻观察，并让Player在连续地面上首次验证“投火兵锁定最近角色”的镜像诱导解法。
- 预期洞察：Enemy-A的路线由显式竖直路径决定；Enemy-B一旦预警就不再修正锁定点，可以用MirrorClone骗走弧线火球。
- 操作压力：低到中。
- 预计观察时间：约`30～60秒`。
- 本房不是土区正式机制教学房，不批准新的土区环境规则、美术语言、机关或世界进度结论。

## 使用机制

必须出现：

- 一条连续、水平的普通地面。
- 一面与地面连接的连续普通竖墙。
- 一个保持Prefab连接的`VerticalWallPatrolEnemy2D`实例。
- 一个保持Prefab连接的`GroundFireThrowerEnemy2D`实例；它直接放在Scene中，不使用Spawner。
- Player、镜子系统、统一房间重置系统和通用相机系统。

禁止出现：

- 特殊镜墙、寒冰地面、传送带、移动平台、门、压力板或固定危险区。
- 第三只敌人或任何未在本文登记的敌人类型。
- 墙角绕行、墙面缺口、动态墙体或运行时修改Tilemap。
- 房间专用敌人、伤害、镜子或重置逻辑。
- 为适配房间而修改`VerticalWallPatrolEnemy2D.prefab`的通用规则。

## 灰盒布局

下图按`1×1 Unity unit`标准Grid描述。地面和墙壁都属于同一个`Terrain` Tilemap；敌人是独立Prefab实例。

```text
 y= 6        T
 y= 5        T
 y= 4        T        ↑
 y= 3        T      [Enemy-A]
 y= 2        T        │
 y= 1        T        │  显式竖直路径
 y= 0        T        │
 y=-1        T      [Enemy-A]
 y=-2        T          [Enemy-B]      P →
 y=-3        T
 y=-4  TTTTTTTTTTTTTTTTTTTTTTTTTT
       -13  -6  -5        0        8  12   x
```

图例：

- `T`：标准`Terrain` Tilemap中的普通静态地形。
- `Enemy-A`：同一个敌人的上下路径端点示意，不表示两个敌人实例。
- `Enemy-B`：固定投火兵，站在中央地面，不移动。
- `P`：Player原型出生位置。
- 墙位于敌人左侧，Enemy-A在墙的右表面上下移动，因此使用Prefab默认`WallSide.Left`。

## Tilemap与空间配置

### Terrain地面

- `Terrain` Tilemap的地面使用Cell范围`x=-13～12`、`y=-4`，共`26`个连续`1×1` Tile。
- 地面上表面位于世界坐标`y=-3`。
- 地面必须返回安全、静态`StaticSolid`表面语义。
- 相邻Tile通过`TilemapCollider2D + CompositeCollider2D + Static Rigidbody2D`合并为连续碰撞边界。
- 地面满足通用镜子空间条件时允许放置地面镜；本房不改变任何镜子规则。

### Terrain竖墙

- 同一`Terrain` Tilemap在Cell `x=-6`、`y=-3～5`放置`9`个连续Tile。
- 墙体世界范围为`x=-6～-5`、`y=-3～6`，与地面无缝连接。
- 墙面是普通、安全、静态`StaticSolid`，不是`SpecialMirrorWall`，不能安装墙面镜。
- 墙面全程覆盖Enemy-A的路径和Collider高度，不能存在Tile空洞或Composite碰撞断缝。
- 墙右侧至少保留`4 units`净空，避免敌人与Player被墙角或其他Collider夹住。

### Tile表现

- 本阶段只批准中性灰盒表现，不确定土区正式美术。
- Scene实现获批后，为本房创建或复用符合标准的`Terrain` Tile资产；Tile外观不得决定`StaticSolid`语义。
- 静态地面和普通墙壁不是Prefab，不创建房间专用地面Prefab或墙壁Prefab。

## 动态对象与实例配置

### Player入口

- 原型入口实例名：`PrototypeEntrance`。
- 入口组件：`RoomEntrance2D`，稳定入口ID为`DEFAULT`，并且是本房唯一默认入口。
- 初始位置：`(8, -2.08)`，面向左，使Player进入后直接看向墙和Enemy-A。
- 初始位置位于安全地面，距离Enemy-B大于其`7 units`探测范围，进入Scene不会立即触发攻击。
- 本次灰盒不建立正式房间出口；Player通过`R`键或死亡重置重复观察。
- Scene不序列化Player；`RoomPlayerSpawner2D`直接打开Scene时在该入口实例化统一的`Assets/Prefabs/Gameplay/Characters/Player.prefab`。

### Enemy-A

- 实例名：`Enemy-A`。
- 通用Prefab：`Assets/Prefabs/Gameplay/Enemies/VerticalWallPatrolEnemy2D.prefab`。
- 根对象位置：`(-4.54, 1)`。
- 墙面侧别：`Left`，使用位于实例左侧的普通竖墙。
- 下端点局部偏移：`-2 units`，世界坐标`y=-1`。
- 上端点局部偏移：`+2 units`，世界坐标`y=3`。
- 移动速度：`1.5 units/s`。
- 端点等待：`0.3 s`。
- 初始方向：向上。
- 不覆盖Prefab的Collider、Damage Trigger、墙面探测距离、表面资格或重置规则。

### Enemy-B：投火兵

- 实例名：`Enemy-B-FireThrower`。
- 通用Prefab：`Assets/Prefabs/Gameplay/Enemies/GroundFireThrowerEnemy2D.prefab`。
- 根对象位置：`(0, -2.5)`；`0.9 unit`高的实体Collider底部贴合`y=-3`地面上表面。
- 初始朝向：右，朝向Player入口一侧。
- 使用统一配置`Assets/Settings/Enemies/DefaultGroundFireThrowerEnemy.asset`，房间不覆盖探测、预警、弹道、冷却、半径或寿命。
- Enemy-B与入口的初始中心距离约`8 units`，大于`7 units`探测距离；Player向左接近后才进入预警。
- Player在Enemy-B右侧地面放置镜子后，可通过共享反向水平输入让MirrorClone向左靠近、Player向右远离，使MirrorClone成为最近目标。
- Enemy-B进入`0.8 s`预警后锁定世界位置；回收镜像不会取消攻击，火球仍飞向原锁定点。
- Enemy-B的身体与火球分别遵循`docs/systems/GROUND_FIRE_THROWER_ENEMY.md`，不在房间脚本中复制逻辑。

## Prefab需求

| 实例 | 通用Prefab | 资产路径 | 本房允许配置 |
|---|---|---|---|
| `Enemy-A` | `VerticalWallPatrolEnemy2D` | `Assets/Prefabs/Gameplay/Enemies/VerticalWallPatrolEnemy2D.prefab` | 位置`(-4.54, 1)`；左墙；局部端点`-2/+2`；速度`1.5`；等待`0.3`；初始向上 |
| `Enemy-B-FireThrower` | `GroundFireThrowerEnemy2D` | `Assets/Prefabs/Gameplay/Enemies/GroundFireThrowerEnemy2D.prefab` | 位置`(0, -2.5)`；初始向右；不覆盖统一攻击数值 |
| `BackgroundLavaDrip-A` | 纯背景表现Prefab | `Assets/Prefabs/Visual/Regions/Fire/BackgroundLavaDrip2D.prefab` | 位置`(-9, 8.5)`；位于左侧墙后；只播放流柱、滴落与飞溅表现，无碰撞、危险或表面语义 |

- `Enemy-A`与`Enemy-B-FireThrower`都必须保持与通用Prefab的连接，不得解包后复制组件。
- Player使用统一Prefab和房间生成流程；Scene只保存`DEFAULT`入口和通用Spawner，不覆盖Player组件、移动、输入、视觉或镜子解锁状态。
- 镜子、相机和`RoomResetSystem`沿用项目通用运行时结构，不创建EARTH_001专用Prefab或脚本。
- `Terrain`地面和墙壁使用Tilemap，不属于Prefab需求。
- `BackgroundLavaDrip-A`是本房的临时装饰实例，不批准新的土区机制或正式美术语言；它不得进入玩家路线，也不得被当作岩浆危险区。

## 镜头设计

- 镜头模式：固定单屏。
- 是否使用全局默认比例：是，`16:9`基准下正交尺寸`7`。
- 固定镜头中心：`(0, 2)`。
- 相机可显示边界：`x=-13～13`、`y=-5～9`。
- 必须同时可见：Player出生位置、Enemy-B、完整竖墙、Enemy-A上下两个端点和两者之间的净空。
- 采用固定单屏的理由：必须同时看清两只敌人、镜像诱导距离和完整火球弧线。
- 不改变Player尺寸，不因Enemy-A移动而缩放、平移或切换相机目标。
- `CameraFollow2D`保存显式房间边界但在本固定单屏房间禁用逐帧跟随；运行时Spawner可以完成Player引用绑定，不启用镜头移动。
- 无镜头例外；死亡或手动重置后保持相同固定构图。

## 初始状态与预期流程

初始状态：

- Player位于右侧安全地面并面向左。
- Player由通用Spawner在`DEFAULT`入口生成；镜子是否已解锁完全读取全局进度和存档状态，房间不覆盖，场景初始没有MirrorClone。
- Enemy-A位于路径锚点`(-4.54, 1)`，验证到左侧连续普通墙面后向上移动。
- Enemy-B位于`(0, -2.5)`并朝右守卫，处于`Guarding`；初始Player在探测距离外，场景中没有火球。
- 房间没有门、压力板、存档状态、检查点或正式出口。

预期流程：

1. Player进入后在固定镜头中同时看到Enemy-B、竖墙和Enemy-A完整路径，但入口处不会触发投火攻击。
2. Enemy-A继续在世界`y=-1～3`之间往返；Player向左接近Enemy-B后看到举火、身体变亮与锁定点标记。
3. Player先观察一次非追踪弧线：离开锁定点即可躲过火球，火球碰地销毁。
4. Player在Enemy-B右侧安全地面放置镜子，通过共享输入让MirrorClone向Enemy-B靠近、Player向右远离。
5. MirrorClone成为最近无遮挡目标后取得锁定；Player利用`0.8 s`预警继续离开锁定路线，火球被引向镜像旧位置。
6. 任一火球或敌人身体命中Player时执行完整房间重置；命中MirrorClone只回收镜像，Enemy-A和Enemy-B当前阶段不整体重置。

## 重置、死亡与场景切换

- Player接触Enemy-A：执行完整房间死亡重置，镜像清除、镜子回手、Player回到`PrototypeEntrance`。
- MirrorClone接触Enemy-A：只执行镜像死亡和镜子回收，不整体重置房间，Enemy-A继续当前巡逻阶段。
- Player被Enemy-B身体或火球命中：执行完整房间重置，火球清除，Enemy-B回到`Guarding`。
- MirrorClone被Enemy-B身体或火球命中：只执行镜像死亡和镜子回收；已经进入Windup的Enemy-B继续瞄准原锁定点，其他状态不重置。
- 手动重置：Player回到入口，Enemy-A恢复锚点、初始向上、零等待和有效墙面验证初态；Enemy-B回到`(0,-2.5)`、初始向右和`Guarding`，全部在途火球清除。
- 已放置镜子时重复左键、主动回收镜子和镜像单独死亡均沿用通用镜子规则，不改变Enemy-A路径配置。
- 场景切换：不携带镜子放置、MirrorClone、Enemy-A位置、方向、等待或墙面接触缓存。
- 本房不写入长期房间状态或敌人状态。

## 软锁与逃课检查

- 场景没有必须完成的机关链，因此不存在机关软锁。
- Player出生点与Enemy-A路径不重叠，进入Scene不会立即死亡。
- Player入口与Enemy-B相距大于探测范围，且入口和投火兵之间无遮挡，玩家能主动决定何时进入预警。
- Enemy-A整个路径旁保持连续有效墙面，不会因墙面缺口停止并关闭伤害。
- 墙顶高于Enemy-A上端点及Collider，墙底与地面连续，敌人不会到端点时离墙悬空。
- Player始终可以按`R`恢复初始状态；死亡后的入口区域安全。
- 火球无追踪且有最大寿命；重置清除全部在途火球，不会在入口留下延迟伤害。
- 地面镜不能安装在普通竖墙上，Enemy-A也不能成为镜子放置表面或安全站立平台。

## 已批准实施范围

本次灰盒实施范围为：

1. 创建`Assets/Scenes/Levels/Earth/Earth_001.unity`。
2. 使用标准Grid和`Terrain` Tilemap搭建本文的水平地面与连续竖墙。
3. 保留一个`VerticalWallPatrolEnemy2D.prefab`实例，并以Prefab实例方式新增一个`GroundFireThrowerEnemy2D.prefab`。
4. 配置`DEFAULT`入口、通用Player Spawner、镜子、重置和固定单屏相机结构；Scene不保存Player实例。
5. 创建必要的中性灰盒Tile资产并登记Scene；不制作土区正式美术或新机制。

任何布局、机制、出口或数值变化都应先回写本文档并再次确认。

## 灰盒实现记录

- 已创建`Assets/Scenes/Levels/Earth/Earth_001.unity`并登记到Build Settings。
- 已创建中性灰盒Tile `Assets/Tiles/Earth/Earth001TerrainGraybox.asset`。
- Scene使用标准Grid与Terrain Tilemap搭建`26`格连续地面和`9`格连续竖墙。
- Scene包含一个保持Prefab连接的`Enemy-A`，实例位置与巡逻参数符合本文。
- Scene包含一个保持Prefab连接的`Enemy-B-FireThrower`，实例位置与统一投火配置符合本文。
- Scene包含一个保持Prefab连接的纯视觉`BackgroundLavaDrip-A`，放在左侧墙后且不包含任何玩法碰撞或危险组件。
- Scene不保存Player实例，包含唯一`DEFAULT`入口、`RoomPlayerSpawner2D`、`RoomResetSystem`和固定单屏相机。
- Unity `6000.5.7f1`主编辑器脚本编译与Additive构建器校验通过。
- `GroundFireThrowerEnemyAssetTests` EditMode测试`3/3`通过。
- `GroundFireThrowerEnemyPlayModeTests` PlayMode测试`3/3`通过，覆盖入口安全、Player锁定、镜像诱导、锁定保持、投掷和完整重置清弹。

## 实施后验收标准

- Scene编号、路径和房间文档完全一致。
- 静态地面与墙壁使用标准`Terrain` Tilemap，不用拉伸Sprite或房间专用Collider代替。
- Scene中恰好有一个保持Prefab连接的`VerticalWallPatrolEnemy2D`实例和一个保持Prefab连接的`GroundFireThrowerEnemy2D`实例。
- Scene没有序列化Player实例，并且恰好有一个`DEFAULT`入口和一个`RoomPlayerSpawner2D`。
- Enemy-A只在世界`y=-1～3`之间稳定竖直往返，速度和等待符合本文配置。
- Enemy-A整条路径持续取得安全静态`StaticSolid`墙面，不绕角、不跨缝、不攀附特殊镜墙。
- Player进入时能同时看见完整墙面、完整敌人路径和自身安全位置。
- Enemy-A接触Player与MirrorClone时分别执行正确的死亡流程。
- Enemy-B只在`7 units`内且无遮挡时选最近目标，锁定后不追踪；游戏画面没有探测圆。
- Enemy-B火球与身体命中Player和MirrorClone时分别执行正确的死亡流程。
- Player死亡、手动重置和重新进入Scene都不会留下Enemy-B火球、锁定点或冷却状态。
- Player死亡、手动重置和重新进入Scene均恢复完整初始状态。
- 地面镜规则保持不变；普通竖墙和Enemy-A均不是合法镜子放置表面。
- Scene不包含EARTH_001专用玩法脚本，也没有解包复制通用Prefab。

## 用户确认记录

- 已确认房间名称“墙行初见”。
- 已确认本文的Tilemap范围、墙面位置和Enemy-A实例数值。
- 已确认本阶段只制作无正式出口的Prefab观察灰盒；相邻房间和世界流程确定后再补连接。
- 已确认按上述固定投火、最近目标、锁定位置和镜像诱导机制编写设计文档并实现代码、Prefab，在`EARTH_001`固定放入一个实例。

## 未验证风险

- 尚未进行人工手感试玩；自动测试已验证状态与生命周期，但仍需主观确认`0.8 s`预警、`2 units`弧高和锁定点标记在实际操作中的可读性。
- 尚未用人工操作验证火球与未来门Prefab的开关碰撞组合；首版Terrain碰撞、角色命中与重置路径已经由实现和专项测试覆盖。
- 土之区域的正式视觉和区域机制仍未定义；当前Terrain与投火兵外观都是服务机制识别的中性灰盒表现。

