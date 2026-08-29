# FIRE_018：双线引火

## 状态与权威来源

- 房间ID：`FIRE_018`。
- 区域：火之区域。
- 当前状态：灰盒中；用户已确认与`FIRE_017`相连并要求制作。
- Unity Scene：`Assets/Scenes/Levels/Fire/Fire_018.unity`。
- 区域规则：`docs/regions/FIRE_REGION.md`。
- 地图连接：`docs/maps/MAP.md`；左侧连接`FIRE_017`。
- 静态几何：`docs/systems/LEVEL_GEOMETRY_SYSTEM.md`。
- 镜子与镜像：`docs/MIRROR_MECHANIC.md`。
- 门与开关：`docs/systems/DOOR_SYSTEM.md`。
- 重置：`docs/systems/RESET_SYSTEM.md`。

## 地图连接

- `FIRE_017`右侧出口进入本房左上角`DEFAULT`入口。
- 本房左上角`Exit-Back-to-FIRE_017`返回`Fire_017/FROM_FIRE_018`。
- 本房当前是终端谜题房；打开`Door-Upper`即完成机关目标，不创建通往其他房间的出口。

## 房间定位

- 房间类型：火球诱导与镜像分路组合房。
- 主要目标：分别诱导上、下两条通道中的水平火球命中各自的锁存开关；两块开关均激活后，上方门才打开。
- 预期洞察：Player与MirrorClone可以在上下两条独立通道中同时成为诱敌目标；火球锁存允许玩家分两步完成AND条件。
- 失败压力：中；火球可以杀死Player或MirrorClone，但谜题可通过重新放镜或手动重置无惩罚重试。
- 预计完成时间：待灰盒验证。

## 已批准机制与排除项

### 使用机制

- 地面镜与MirrorClone：输入变换后独立物理，规则见`docs/MIRROR_MECHANIC.md`。
- 两个固定Scene投火者：使用`HorizontalFireballEnemy2D`，分别服务上、下通道，不使用Spawner。
- 两块`FireballLatch`压力板：每块只响应水平火球，命中后在当前房间尝试内保持激活。
- 一扇普通门：唯一控制源为两块`FireballLatch`，逻辑模式为AND，初始关闭。

### 明确不包含

- 不新增火球、镜像、门或压力板的房间专用行为。
- 不使用永久锁存双压力板门控组；本房两块火球锁存板在手动重置、Player死亡或重新进房时恢复未激活。
- 不使用运行时敌人生成、Spawner、房间专用脚本或候选火区机制。
- 当前不批准岩浆、周期喷发、升降岩浆、移动平台或额外门。

## 空间要求

- 房间由上下两条横向通道组成，入口安全区位于左上角。
- 两条通道在视觉上清楚分层，并通过中部的镜像放置/分路位置建立解题关系。
- Player必须能够在到达中部前观察到上、下两条火线、两块锁存板和上方关闭门的因果布局。
- 中部必须提供静态、水平、安全且空间充足的合法地面镜放置面。
- 放镜后，非对称地形应使MirrorClone能够进入下方通道，而Player继续留在上方通道；不得改变共享输入或镜像物理规则来强行分路。
- 上、下通道各自的投火者、诱敌位置和`FireballLatch`必须水平共线。正确弹道应先命中对应锁存板，不能先撞门、Terrain、另一角色或另一个锁存板。
- 上方门是完成后的唯一前进阻挡；门洞外不得留下标准跳跃可绕过的缝隙。
- 房间边界采用`X[-12,12] Y[-5,6]`的固定单屏灰盒；静态地形使用标准Tilemap层。

## 标准网格布局

- Grid：`1×1 Unity unit`。
- 上方通道地面：`Y=1`，中部`X=-1～0`留作下落缺口。
- 下方通道地面：`Y=-5`。
- 左上入口：`(-10.5,2.92)`；建议放镜点位于上层中部偏左。
- `Door-Upper`位于`(6.5,3)`，门上方以Terrain封闭至天花板。

## 对象与控制关系

| 实例ID | 对象 | 初始状态 | 作用 |
|---|---|---|---|
| `Enemy-Upper` | `HorizontalFireballEnemy2D`固定Scene实例，`(10.5,2.5)` | `Watching`、面向左 | 被上方Player诱导，向`Latch-Upper`发射水平火球 |
| `Enemy-Lower` | `HorizontalFireballEnemy2D`固定Scene实例，`(10.5,-3.5)` | `Watching`、面向左 | 被下方MirrorClone诱导，向`Latch-Lower`发射水平火球 |
| `Latch-Upper` | `PressurePlate2D / FireballLatch`，`(8.5,2.625)` | 未激活 | 上方火球命中后锁存本次尝试的第一项门条件 |
| `Latch-Lower` | `PressurePlate2D / FireballLatch`，`(8.5,-3.375)` | 未激活 | 下方火球命中后锁存本次尝试的第二项门条件 |
| `Door-Upper` | `Door2D`，`(6.5,3)` | 关闭、`ControlLogic.And` | 仅当两块锁存板均激活时开启 |
| `MirrorHint-Mid` | 无碰撞视觉提示 | 可见 | 标示中部建议放镜点，不参与放置合法性判断 |

控制关系：

```text
Enemy-Upper火球 → Latch-Upper ┐
                               ├─ AND → Door-Upper
Enemy-Lower火球 → Latch-Lower ┘
```

- 两块锁存板不要求在同一物理帧命中；任意顺序均可。
- 只激活其中一块时，`Door-Upper`保持关闭。
- 第二块激活后，`Door-Upper`开启并保持到本次房间尝试结束。
- Player、MirrorClone和普通动态物体不能直接激活两块锁存板。

## 预期流程

1. Player从左上角出生，沿上方通道前进并观察上下两条火线和关闭的`Door-Upper`。
2. Player到达中部合法放镜点并放置地面镜，使Player留在上方、MirrorClone进入下方通道。
3. MirrorClone在下方诱敌位置勾引`Enemy-Lower`发射；火球命中`Latch-Lower`并锁存，下方条件完成，但门仍关闭。
4. Player在上方诱敌位置勾引`Enemy-Upper`发射；火球命中`Latch-Upper`并锁存。
5. 两块锁存板均激活后，`Door-Upper`开启，Player从上方通路继续前进。

上下两个锁存条件允许交换完成顺序，但标准空间引导应优先让玩家理解“先用MirrorClone完成下方条件，再由Player完成上方条件”。

## 失败、重置与软锁

- Player被火球或投火者命中：执行完整房间重置；镜像清除、镜子回手、两块锁存板恢复未激活、门恢复关闭、两名投火者和全部在途火球恢复初始状态。
- MirrorClone被火球命中：只执行镜像死亡与镜子自动回收；已经命中的`FireballLatch`保持激活，Player可以回到中部重新放镜继续尝试。
- 手动重置：两块锁存板和门恢复初始状态，两名投火者恢复`Watching`并清除在途火球。
- 重新进入房间或场景切换：不保留两块火球锁存、门状态、镜像或在途火球。
- 任意一次诱敌失败后，Player必须能安全返回中部放镜点或按`R`重置；上下通道不得形成无法返回的单向软锁。
- 门的关闭路径遵循通用防夹规则，但角色碰门不能成为开门信号。

## 相机与信息可见性

- 镜头模式：固定单屏；中心`(0,0.5)`，正交尺寸`7`。
- 必须同时可见：左上入口、中部放镜点、上下两条通道的关键诱敌位置、两块锁存板和`Door-Upper`。
- 不绘制瞄准线或机关连线；通过水平共线布局、火球运动、锁存反馈和门开启表现表达因果。
- 如果单屏无法保持Player约占屏幕高度`12%–14%`并清楚展示两条通道，则改用已批准的边界跟随模式，不得缩小Player。

## 最小验收标准

- 房间恰好包含两条横向通道、两个固定投火者、两块`FireballLatch`和一扇AND控制的上方门。
- 入口出生点位于左上角，中部存在明确且合法的地面镜放置点。
- MirrorClone可以按通用规则进入下方通道诱导下方火球，Player可以留在上方诱导上方火球。
- 任意单块锁存板激活时门保持关闭；两块均激活后门才开启。
- 两块锁存板可按任意顺序完成，MirrorClone之后死亡不清除已经完成的锁存条件。
- 手动重置、Player死亡和重新进入房间会清除两块锁存并关闭门。
- 不存在绕门、误击另一块板、火球先撞Terrain或失败后无法重试的路径。

## 实施记录与未验证风险

- 已完成：FIRE_017双向连接、标准Tilemap静态灰盒、固定镜头、两名固定投火者、两块`FireballLatch`、AND门、入口与返回出口。
- Editor构建器：`Assets/Editor/Fire018RoomBuilder.cs`；动态对象保持共享Prefab连接，无房间专用运行时代码。
- 自动验证：Builder内验证对象数量、Prefab组件、锁存模式、AND引用、出口目标、Player不序列化及Terrain Collider；Unity Console无编译或构建错误。
- 人工试玩：未执行；诱敌距离、弹道净空、共享输入下的上下分路和操作容错尚未验证。
