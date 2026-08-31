# FIRE_019：三廊引火

> 2026-08-31 显示调整：按用户要求关闭本房 `Decoration` 的 `Tilemap Renderer`，保留对象、Tile 与坐标；本文中的该层提示位置仅作设计参考，当前不显示、不再要求提示标记可见。碰撞、镜子放置规则与解法不变。

## 2026-08-31 房间几何修复

本节替代下文涉及同一对象的旧坐标、旧入口目标和旧检查结论；未提及的玩法规则及美术记录不变。对应RV结果、设计冲突和验证范围见[FIRE_REPAIR_REVIEW.md](FIRE_REPAIR_REVIEW.md)。当前仍为待运行验证的灰盒，不代表全房验收通过。

Plate-A修正为(-2.5,5.15)，底边贴合上层Y=5路面。上层Player下落口由X=4移到X=7，避免与中层X=4缺口重合而连续跌到第三层。中层补充(3,0)、(3,1)实体止挡墙，将过门后的巡逻端限制在缺口左侧；敌人仍按通用遇墙折返规则，不增加悬崖检测。X=4中层缺口继续供右侧放镜区的MirrorClone下落。保持无出口、无最终锁存开关的已批准范围。

| 入口ID | Player Collider中心 |
|---|---|
| `DEFAULT` | `(-10.5,5.92)` |

本轮增量编辑Scene，未运行整房Builder。当前Scene是落位权威；历史重建入口尚未同步本轮布局，禁止用其覆盖正式Scene。仅FIRE_011构建器的共用喷发Prefab尺寸计算同步修复，不表示其旧房间布局已同步。未运行Unity自动测试或人工试玩，碰撞重建、完整解法、重置和画面遮挡仍需验证。

## 状态与权威来源

- 房间ID：`FIRE_019`。
- 区域：火之区域。
- 当前状态：灰盒中；用户已要求创建Scene，并明确本阶段暂不考虑出口。
- Unity Scene：`Assets/Scenes/Levels/Fire/Fire_019.unity`。
- Editor Builder：`Assets/Editor/Fire019RoomBuilder.cs`。
- 地图规划：位于`FIRE_018`右侧；本阶段未创建任何`RoomExit2D`，也未修改`FIRE_018` Scene连接。
- 机制依据：`docs/MIRROR_MECHANIC.md`、`docs/regions/FIRE_REGION.md`、`docs/systems/DOOR_SYSTEM.md`、`docs/systems/PATROLLING_HORIZONTAL_FIREBALL_ENEMY_PROPOSAL.md`与`docs/systems/RESET_SYSTEM.md`。

## 房间定位

- 房间类型：巡逻投火者放行与镜像诱火组合房。
- 主要目标：Player先在第一通道踩板，打开第二通道中挡住巡逻投火者的门；敌人通过门进入右侧后，Player下落到第二通道右段，在安全地面放镜，让MirrorClone从中层缺口落入第三通道并引诱固定投火者射击。
- 预期洞察：门会明确改变巡逻投火者的折返点；Player与MirrorClone随后可沿非对称路线分流。
- 当前完成边界：实现三通道、入口、放行门、压力板、两名投火者、放镜提示、相机与统一重置；不实现出口、出口门、最终锁存开关或房间连接。

## 三通道结构

```text
第一通道  左上入口 → [Plate-A] → 上层右侧下落口
                         │持续控制
第二通道  巡逻投火者 ⇄ [Door-A] → 右侧观察/放镜区 → （未来出口位置未定）
                                     │MirrorClone从X=4缺口落下
第三通道  固定投火者 H →→→→→→→ MirrorClone诱敌空间
```

- 第一通道位于上层，Player可以先观察巡逻投火者被关闭的`Door-A`挡住并折返。
- 第二通道垂直位置低于第一通道；“右下角”指该通道右段。巡逻投火者始终在连续水平安全地面上移动，不增加跳跃、下落或跨层规则。
- 第三通道位于底层，固定投火者面向右；MirrorClone从第二通道`X=4`的缺口进入其水平攻击带。
- 本阶段右侧边界保持封闭，不放置临时或正式出口。

## 实际灰盒网格

- Grid：`1×1 Unity unit`。
- 房间边界：`X[-12,11] Y[-7,7]`。
- 第一通道地形：地面Tile位于`Y=4`，`X[-11,10]`；`X=7`留有Player下落口，与中层`X=4`镜像缺口错开。
- 左上入口：`Entrance-DEFAULT (-10.5,5.92)`。
- `Plate-A (-2.5,5.3)`：`Occupancy`模式，持续控制`Door-A`。
- 第二通道地形：地面Tile位于`Y=-1`；`X[-11,3]`与`X[5,10]`，`X=4`为MirrorClone下落缺口。
- `Door-A (0.5,1)`：标准`1×2 units`普通门，初始关闭；门上方`X=0,Y[2,3]`由Terrain封闭，不能跳过。
- `Enemy-Middle-Patrolling (-7.5,0.5)`：初始向右巡逻。
- 建议放镜提示：Decoration `X=7,Y=0`；只是视觉提示，不参与镜子合法性判断。
- 第三通道地形：地面Tile位于`Y=-6`，`X[-11,10]`。
- `Enemy-Lower-Fixed (-8.5,-4.5)`：面向右，服务MirrorClone诱火。
- 相机：固定单屏，中心`(0,0)`，正交尺寸`7`。

## 对象与控制关系

| 实例ID | 通用Prefab/组件 | 初始状态 | 作用 |
|---|---|---|---|
| `Plate-A` | `PressurePlate2D.prefab` | `Occupancy`、未占用 | Player持续占用时打开`Door-A` |
| `Door-A` | `Door2D.prefab` | 关闭 | 唯一控制源为`Plate-A`；关闭时也是巡逻敌人的实体折返点 |
| `Enemy-Middle-Patrolling` | `PatrollingHorizontalFireballEnemy2D.prefab` | `Patrolling`、向右 | 门打开后可以进入第二通道右段 |
| `Enemy-Lower-Fixed` | `HorizontalFireballEnemy2D.prefab` | `Watching`、面向右 | 被第三通道中的MirrorClone诱导并水平射击 |
| `Entrance-DEFAULT` | `RoomEntrance2D`配置 | 安全入口 | 运行时生成Player；Scene不序列化Player |

两名敌人都是固定Scene Prefab实例，不使用Spawner。`Gameplay/Exits`根节点刻意保持为空。

## 预期流程

1. Player从左上入口进入，观察巡逻投火者走到关闭的`Door-A`前停顿并折返。
2. Player踩住`Plate-A`，等待`Door-A`开启及巡逻投火者完整通过门洞。
3. Player离开压力板并从上层`X=7`缺口落到第二通道右段；门按通用防夹规则完成关闭。中层`X=3`的实体止挡墙限制巡逻投火者，避免其走入`X=4`缺口。
4. Player到达`X≈7`的安全放镜区并放置地面镜。
5. 共享输入使Player向右移动时MirrorClone向左移动；MirrorClone到达`X=4`缺口后落入第三通道。
6. MirrorClone进入底层固定投火者的水平攻击带并诱导其射击。后续如何形成正式出口窗口留待出口设计时补充，本阶段不自行决定。

## 失败、重置与软锁

- Player被投火者或火球命中：完整重置；Player回左上入口，MirrorClone清除、镜子回手，门与压力板恢复初始状态，两名敌人恢复初始位置、方向和状态，全部火球清除。
- MirrorClone被击中：只执行镜像死亡和镜子自动回收；巡逻投火者位置、方向和攻击阶段不重置。
- Player过早离开`Plate-A`：敌人占用门关闭路径时使用通用防夹等待；否则门重新关闭并成为其折返点，Player可重新尝试。
- 手动重置与重新进入房间恢复全部房间瞬时状态。
- 因本阶段没有出口，房间无法正式完成；这是明确的制作边界，不应被当作运行时软锁验收通过。

## 相机与信息可见性

- 模式：固定单屏；使用全局默认角色比例。
- 必须同时可见：上层入口与压力板、中层门及巡逻敌人、右侧放镜提示、中层下落缺口、底层固定投火者和MirrorClone诱敌空间。
- Game Camera静态截图已确认三层、门、压力板、两名敌人和放镜提示进入画面；截图不替代运行时验证。

## 静态验收与未验证风险

- Unity Scene验证：0个missing script、0个broken Prefab、0个Scene结构问题。
- 组件回读：1个`RoomPlayerSpawner2D`、1个`PressurePlate2D`、1个`Door2D`、1个巡逻投火者、2个水平投火攻击组件、0个`RoomExit2D`。
- Builder静态断言：Scene不序列化Player；压力板为`Occupancy`；门显式引用压力板；两名敌人保持共享Prefab连接；Terrain具有Tilemap、Composite Collider及Static Rigidbody。
- 未运行PlayMode、完整EditMode、人工试玩或Batch编译。
- 尚未验证：Player从上层落到门右侧的容错、巡逻敌人过门时序、Door防夹、实际镜像生成位置、共享输入下MirrorClone能否稳定进入底层，以及底层火线的操作空间。
