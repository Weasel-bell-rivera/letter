# FIRE_019：三廊引火

## 2026-09-01 FIRE_020连接补全

本节替代下文所有“无出口”“最终锁存未定”的旧制作边界。用户已确认FIRE_020位于本房右侧并双向连接；本次只增量补全底层诱火结果和中层出口，不运行历史整房Builder，也不改变原三廊谜题。

- 底层新增`Latch-Exit-to-FIRE020 (-5.5,-4.5)`，使用`FireballLatch`模式并旋转90度安装在固定投火者的水平射线上。
- 中层新增`Door-Exit-to-FIRE020 (9.5,1)`，唯一控制源为上述锁存板；`X=9,Y[2,3]`补实体门帽，防止绕过。
- 右边界`X=11,Y[0,1]`打开；`Exit-to-FIRE_020 (11.5,1)`目标为`Fire_020/FROM_FIRE_019`。
- 新增返回入口`Entrance-FROM_FIRE_020 (8,0.92)`，位于出口门左侧，初始朝左；重新进入时即使锁存状态已重置也不会被困在门外。
- FIRE_019当前Scene为手工落位权威；`Fire019RoomBuilder.cs`仍是过期的无出口历史入口。连接由`Fire020RoomBuilder.cs`增量维护。

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
- 当前状态：灰盒中；已补全通往FIRE_020的正式双向连接。
- Unity Scene：`Assets/Scenes/Levels/Fire/Fire_019.unity`。
- Editor Builder：`Assets/Editor/Fire019RoomBuilder.cs`。
- 地图连接：位于`FIRE_018`右侧、`FIRE_020`左侧；已实现`FIRE_019 ⇄ FIRE_020`，本次未修改`FIRE_018` Scene连接。
- 机制依据：`docs/MIRROR_MECHANIC.md`、`docs/regions/FIRE_REGION.md`、`docs/systems/DOOR_SYSTEM.md`、`docs/systems/PATROLLING_HORIZONTAL_FIREBALL_ENEMY_PROPOSAL.md`与`docs/systems/RESET_SYSTEM.md`。

## 房间定位

- 房间类型：巡逻投火者放行与镜像诱火组合房。
- 主要目标：Player先在第一通道踩板，打开第二通道中挡住巡逻投火者的门；敌人通过门进入右侧后，Player下落到第二通道右段，在安全地面放镜，让MirrorClone从中层缺口落入第三通道并引诱固定投火者射击。
- 预期洞察：门会明确改变巡逻投火者的折返点；Player与MirrorClone随后可沿非对称路线分流。
- 当前完成边界：实现三通道、入口、放行门、压力板、两名投火者、底层锁存开关、FIRE_020出口、相机与统一重置。

## 三通道结构

```text
第一通道  左上入口 → [Plate-A] → 上层右侧下落口
                         │持续控制
第二通道  巡逻投火者 ⇄ [Door-A] → 右侧观察/放镜区 → [出口门] → FIRE_020
                                     │MirrorClone从X=4缺口落下
第三通道  固定投火者 H → [FireballLatch] → MirrorClone诱敌空间
```

- 第一通道位于上层，Player可以先观察巡逻投火者被关闭的`Door-A`挡住并折返。
- 第二通道垂直位置低于第一通道；“右下角”指该通道右段。巡逻投火者始终在连续水平安全地面上移动，不增加跳跃、下落或跨层规则。
- 第三通道位于底层，固定投火者面向右；MirrorClone从第二通道`X=4`的缺口进入其水平攻击带。
- 中层右侧通过内部出口门连接边界出口；底层与上层外边界保持封闭。

## 实际灰盒网格

- Grid：`1×1 Unity unit`。
- 房间边界：`X[-12,11] Y[-7,7]`。
- 第一通道地形：地面Tile位于`Y=4`，`X[-11,10]`；`X=7`留有Player下落口，与中层`X=4`镜像缺口错开。
- 左上入口：`Entrance-DEFAULT (-10.5,5.92)`。
- `Plate-A (-2.5,5.15)`：`Occupancy`模式，持续控制`Door-A`。
- 第二通道地形：地面Tile位于`Y=-1`；`X[-11,3]`与`X[5,10]`，`X=4`为MirrorClone下落缺口。
- `Door-A (0.5,1)`：标准`1×2 units`普通门，初始关闭；门上方`X=0,Y[2,3]`由Terrain封闭，不能跳过。
- `Enemy-Middle-Patrolling (-7.5,0.5)`：初始向右巡逻。
- 建议放镜提示：Decoration `X=7,Y=0`；只是视觉提示，不参与镜子合法性判断。
- 第三通道地形：地面Tile位于`Y=-6`，`X[-11,10]`。
- `Enemy-Lower-Fixed (-8.5,-4.5)`：面向右，服务MirrorClone诱火。
- `Latch-Exit-to-FIRE020 (-5.5,-4.5)`：`FireballLatch`模式，命中后锁存开启出口门。
- `Door-Exit-to-FIRE020 (9.5,1)`：标准`1×2 units`普通门；上方门帽位于`X=9,Y[2,3]`。
- `Entrance-FROM_FIRE_020 (8,0.92)`：FIRE_020返回时的安全生成点。
- `Exit-to-FIRE_020 (11.5,1)`：目标`Fire_020/FROM_FIRE_019`；右墙`X=11,Y[0,1]`为出口洞口。
- 相机：固定单屏，中心`(0,0)`，正交尺寸`7`。

## 对象与控制关系

| 实例ID | 通用Prefab/组件 | 初始状态 | 作用 |
|---|---|---|---|
| `Plate-A` | `PressurePlate2D.prefab` | `Occupancy`、未占用 | Player持续占用时打开`Door-A` |
| `Door-A` | `Door2D.prefab` | 关闭 | 唯一控制源为`Plate-A`；关闭时也是巡逻敌人的实体折返点 |
| `Enemy-Middle-Patrolling` | `PatrollingHorizontalFireballEnemy2D.prefab` | `Patrolling`、向右 | 门打开后可以进入第二通道右段 |
| `Enemy-Lower-Fixed` | `HorizontalFireballEnemy2D.prefab` | `Watching`、面向右 | 被第三通道中的MirrorClone诱导并水平射击 |
| `Latch-Exit-to-FIRE020` | `PressurePlate2D.prefab` | `FireballLatch`、未激活 | 消耗底层水平火球并锁存 |
| `Door-Exit-to-FIRE020` | `Door2D.prefab` | 关闭 | 由底层锁存板持续控制，打开FIRE_020出口通道 |
| `Entrance-FROM_FIRE_020` | `RoomEntrance2D`配置 | 安全入口、朝左 | 从FIRE_020返回时生成Player |
| `Exit-to-FIRE_020` | `RoomExit2D.prefab` | 未武装 | 离开释放区后允许进入`Fire_020/FROM_FIRE_019` |
| `Entrance-DEFAULT` | `RoomEntrance2D`配置 | 安全入口 | 运行时生成Player；Scene不序列化Player |

两名敌人都是固定Scene Prefab实例，不使用Spawner。`Gameplay/Exits`只包含通往FIRE_020的出口。

## 预期流程

1. Player从左上入口进入，观察巡逻投火者走到关闭的`Door-A`前停顿并折返。
2. Player踩住`Plate-A`，等待`Door-A`开启及巡逻投火者完整通过门洞。
3. Player离开压力板并从上层`X=7`缺口落到第二通道右段；门按通用防夹规则完成关闭。中层`X=3`的实体止挡墙限制巡逻投火者，避免其走入`X=4`缺口。
4. Player到达`X≈7`的安全放镜区并放置地面镜。
5. 共享输入使Player向右移动时MirrorClone向左移动；MirrorClone到达`X=4`缺口后落入第三通道。
6. MirrorClone进入底层固定投火者的水平攻击带；火球向右命中`Latch-Exit-to-FIRE020`并被消耗。
7. 锁存板持续打开中层右侧`Door-Exit-to-FIRE020`；Player穿门到达右边界。
8. Player进入`Exit-to-FIRE_020`，目标房从`FROM_FIRE_019`入口生成Player。

## 失败、重置与软锁

- Player被投火者或火球命中：完整重置；Player回左上入口，MirrorClone清除、镜子回手，门与压力板恢复初始状态，两名敌人恢复初始位置、方向和状态，全部火球清除。
- MirrorClone被击中：只执行镜像死亡和镜子自动回收；巡逻投火者位置、方向和攻击阶段不重置。
- Player过早离开`Plate-A`：敌人占用门关闭路径时使用通用防夹等待；否则门重新关闭并成为其折返点，Player可重新尝试。
- 手动重置与重新进入房间恢复全部房间瞬时状态。
- 底层锁存一旦激活，MirrorClone死亡或镜子回收不关闭出口门；手动重置、Player死亡和重新进房会清除锁存并重新关闭出口门。
- 从FIRE_020返回时Player生成在出口门左侧，不依赖已清除的锁存状态通过门，因此不会形成返回软锁。

## 相机与信息可见性

- 模式：固定单屏；使用全局默认角色比例。
- 必须同时可见：上层入口与压力板、中层两扇门及巡逻敌人、右侧放镜区、中层下落缺口、底层固定投火者、锁存板和MirrorClone诱敌空间。
- Game Camera静态截图已确认三层、门、压力板、两名敌人和放镜提示进入画面；截图不替代运行时验证。

## 静态验收与未验证风险

- Unity Scene验证：0个missing script、0个broken Prefab、0个Scene结构问题。
- 组件回读：1个`RoomPlayerSpawner2D`、2个`PressurePlate2D`、2个`Door2D`、1个巡逻投火者、2个水平投火攻击组件、1个`RoomExit2D`。
- Builder静态断言：Scene不序列化Player；压力板为`Occupancy`；门显式引用压力板；两名敌人保持共享Prefab连接；Terrain具有Tilemap、Composite Collider及Static Rigidbody。
- 未运行PlayMode、完整EditMode、人工试玩或Batch编译。
- 尚未验证：Player从上层落到门右侧的容错、巡逻敌人过门时序、Door防夹、实际镜像生成位置、共享输入下MirrorClone能否稳定进入底层、底层火球锁存时序，以及双向切场的运行结果。
