# FIRE_020：火窗协作

## 状态与权威来源

- 房间ID：`FIRE_020`。
- 区域：火之区域。
- 当前状态：灰盒中；核心谜题、FIRE_019双向连接、高压周期喷发和右侧未来出口预留均已由用户确认。
- Unity Scene：`Assets/Scenes/Levels/Fire/Fire_020.unity`。
- Editor Builder：`Assets/Editor/Fire020RoomBuilder.cs`，是当前灰盒落位权威；菜单为`Tools/W1/Build FIRE-020 Greybox`。
- 机制依据：`docs/MIRROR_MECHANIC.md`、`docs/regions/FIRE_REGION.md`、`docs/systems/DOOR_SYSTEM.md`、`docs/systems/RESET_SYSTEM.md`。

## 地图连接

| 本房出口或入口 | 方向 | 相邻房间 | 目标入口ID | 状态 |
|---|---|---|---|---|
| `Entrance-FROM_FIRE_019` | 左侧进入 | `FIRE_019` | 本房入口 | 已实现 |
| `Exit-Back-to-FIRE_019` | 左 | `FIRE_019` | `FROM_FIRE_020` | 已实现 |
| `FutureExitAnchor-FIRE021` | 右 | 无 | 无 | 仅预留非功能锚点；FIRE_021未批准 |

## 房间定位

- 房间类型：高压组合挑战房。
- 主要目标：Player与MirrorClone分处上层和台阶，短暂打开火窗，引导固定投火者的火球命中锁存器并开启下层目标门。
- 预期洞察：占用板只在MirrorClone持续停留时开启火窗；Player必须同时处于另一侧诱敌，单人依次操作不能替代双角色协作。
- 失败压力：高；Player从镜子设置点前往诱敌位时必须穿过一次周期喷发。
- 无惩罚重试：Player死亡或手动重置恢复入口与全部瞬时状态；左侧返回FIRE_019的出口始终存在。

## 已批准机制与排除项

### 使用机制

- 地面镜放置、MirrorClone反向水平输入与独立物理。
- `Occupancy`压力板与普通门的持续控制。
- `FireballLatch`锁存板与普通门的锁存控制。
- 固定Scene投火者、水平火球和周期喷发。

### 明确不包含

- 不新增或覆盖镜子、MirrorClone、Player移动、碰撞、重置或火区全局规则。
- 不使用Spawner、运行时Tilemap修改、移动平台、岩浆、检查点或存档状态。
- 不创建FIRE_021 Scene或`RoomExit2D`；右侧完成区不能切场。
- 本次不制作正式环境美术、视差、动态雾或前景遮挡。

## 标准网格布局

- Grid：`1×1 Unity unit`。
- 可见结构边界：`X[-12,11] Y[-7,6]`，固定单屏完整显示；可玩最低表面为`Y=-4`。
- 下层可玩地面顶面：`Y=-4`；`X[-12,11],Y[-7,-4]`为纯色实体基座，避免固定镜头露出无意义空带。
- 台阶：`X[-2,0], Y[-3,-2]`，顶面`Y=-1`。
- 上层地面：`Y=0, X[1,10]`；`X=6`为空，作为解锁后的安全落口。
- 左墙出口洞：`X=-12,Y[-3,-2]`。
- 目标隔断：`X=7`；下层门上方使用`Y=-1,0`实体分隔，上层火窗上方使用`Y[3,5]`实体门帽。

```text
上层  台阶→ 放镜点 → [周期喷发] → Player诱敌 → [Latch] → [火窗门] ← 固定投火者
            MirrorClone向左跌落
                     ↓
下层  FIRE_019出口 ← [Plate-A] ───── 安全落口 ───── [目标门] → 完成区/未来锚点
```

## Tilemap配置

- Terrain Tile：`Assets/Tiles/Fire/Fire013SolidTerrain.asset`，均匀纯色、Grid Collider。
- 标准层：`Background`、`Terrain`、`OneWayPlatform`、`SpecialMirrorWall`、`Hazard`、`Decoration`、`Foreground`。
- 仅`Terrain`有Tile、`TilemapCollider2D`、`CompositeCollider2D`、Static `Rigidbody2D`、`StaticSolid`表面语义与安全地面镜面语义。
- `Decoration`的`TilemapRenderer`保持关闭；其余空Tilemap不承担碰撞或玩法语义。
- 八个环境功能职责中只实现玩法地形与角色层；背景轮廓、远中景、前后动态雾和前景遮挡均在灰盒阶段省略。

## Prefab与实例

| 实例ID | 通用Prefab | 位置/初始状态 | 控制或作用 |
|---|---|---|---|
| `Plate-A` | `PressurePlate2D.prefab` | `(0,-0.85)`；`Occupancy` | MirrorClone持续占用时开启`Door-FireWindow` |
| `Door-FireWindow` | `Door2D.prefab` | `(7.5,2)`；关闭 | 唯一控制源`Plate-A` |
| `Latch-Goal` | `PressurePlate2D.prefab` | `(5.5,1.625)`；旋转90度、`FireballLatch` | 消耗水平火球并锁存 |
| `Door-Goal` | `Door2D.prefab` | `(7.5,-2)`；关闭 | 唯一控制源`Latch-Goal` |
| `Eruption-A` | `EruptionHazard.prefab` | `(3.5,3)`；Warning开始 | 默认`1秒预警/1秒危险/2秒冷却` |
| `Enemy-Upper-Fixed` | `HorizontalFireballEnemy2D.prefab` | `(10.5,1.5)`；面向左 | 固定Scene实例；向Player或MirrorClone水平发射 |
| `Exit-Back-to-FIRE_019` | `RoomExit2D.prefab` | `(-11.5,-2)`；未武装 | 目标`Fire_019/FROM_FIRE_020` |

不适用Spawner：本房敌人为固定Scene Prefab实例，没有出生点、生成条件、重生策略或数量上限。

## 入口与参考锚点

| ID | 位置 | 配置 |
|---|---|---|
| `Entrance-DEFAULT` | `(-8,-2.08)` | 默认、朝右 |
| `Entrance-FROM_FIRE_019` | `(-9.5,-2.08)` | 非默认、朝右 |
| `MirrorSetupReference` | `(2.5,1.92)` | 仅制作参考，无玩法组件 |
| `PlayerLureReference` | `(4.5,1.92)` | 仅制作参考，无玩法组件 |
| `FutureExitAnchor-FIRE021` | `(10,-2.08)` | 非功能锚点，无`RoomExit2D` |

## 相机配置

- 镜头模式：固定单屏；不包含`CameraFollow2D`。
- 相机中心：`(0,0,-10)`；正交尺寸`7`；以16:9为基准。
- 使用全局默认角色比例，不改变Player尺寸。
- 必须同时可见：两处入口、返回出口、台阶与Plate-A、放镜点、周期喷发、诱敌位、Latch、上下两扇门、固定投火者、落口与右侧完成区。
- 无构图例外；其他宽高比与HUD遮挡仍需运行观察。

## 预期流程

1. Player从左下进入，确认可随时返回FIRE_019，再经地面和台阶的两次2单位高度变化到达上层。
2. Player经过Plate-A时只会短暂开门；离开后火窗重新关闭，无法单人利用该状态完成诱火。
3. Player在`MirrorSetupReference`附近的安全地面放镜。
4. Player向右移动并选择周期喷发的安全窗口；MirrorClone反向向左，从上层左端跌到台阶顶并压住Plate-A。
5. 火窗开启后，Player到达`PlayerLureReference`；固定投火者进入6单位攻击范围并向左发射。
6. 火球穿过火窗，命中Latch-Goal后被消耗；Door-Goal以锁存开启状态保持打开。
7. Player经`X=6`落口回到下层，召回镜子不会关闭已锁存的Door-Goal。
8. Player穿过Door-Goal进入右侧完成区；当前不能继续切换到FIRE_021，可返回左侧或重置。

## 重置、死亡与场景切换

| 事件 | Player | 镜子/MirrorClone | 门与压力板 | 投火者/火球 | 周期喷发 |
|---|---|---|---|---|---|
| 手动重置 | 回当前入口 | 镜子回手、MirrorClone清除 | Plate释放、Latch清除、两门关闭 | 回初始位置与Watching、清除火球 | 回Warning |
| Player死亡 | 同手动重置 | 同手动重置 | 同手动重置 | 同手动重置 | 同手动重置 |
| MirrorClone单独死亡/召回 | Player不移动 | MirrorClone消失 | Plate释放、火窗关闭；已命中的Latch和目标门保持 | 已锁定攻击按通用规则继续 | 不重置 |
| 重新进入房间 | 从请求入口新建Player | 镜子回手 | 初始未激活、两门关闭 | 初始状态、无在途火球 | Warning |
| 切换到FIRE_019 | 旧Player销毁 | 切场前立即回收 | 旧Scene销毁 | 旧Scene销毁 | 旧Scene销毁 |

## RV通用检查记录

| RV | 对象/关系 | 结果 | 静态证据或待验证内容 |
|---|---|---|---|
| RV-01 | `DEFAULT`、`FROM_FIRE_019` | 通过（静态） | 两入口位于下层安全地面上方0.92，远离门、敌人和喷发 |
| RV-02 | 两入口与左侧出口 | 通过（静态） | `FROM_FIRE_019`与出口Trigger边缘净距为1单位；DEFAULT更远，且出口保留通用释放区 |
| RV-03 | 地面→台阶→上层、X=6落口 | 待验证 | 两次高度差均2单位；实际跳跃容错和落口控制需运行确认 |
| RV-04 | Plate、Latch、敌人、喷发 | 通过（静态） | Plate贴台阶顶，敌人贴上层地面；Latch与火线同高；喷发危险柱底边贴上层地面 |
| RV-05 | 上下门与X=7隔断 | 通过（静态） | 门洞为1×2，实体隔断和门帽阻止绕过；打开后门洞无Terrain残留 |
| RV-06 | `FIRE_020 ⇄ FIRE_019` | 通过（静态） | Scene、Build Settings、MAP和双方来源入口ID一致；FIRE_021无出口组件 |
| RV-07 | Terrain与Eruption | 通过（静态） | Terrain语义显式；喷发为Prefab Trigger且上层支撑地形保留，符合喷发规则 |
| RV-08 | Plate→火窗、火球→Latch→目标门 | 通过（静态） | 序列化控制引用完整；水平射线无无关地形，关闭火窗是唯一预期遮挡 |
| RV-09 | 放镜点与双角色路线 | 待验证 | 静态距离匹配且预计生成空间可见；共享输入下稳定落板需实际运行确认 |
| RV-10 | 固定相机 | 待验证 | 正交尺寸7覆盖全部关键对象；HUD、实际宽高比和运行时可读性未验证 |
| RV-11 | 重置与失败恢复 | 待验证 | 所有对象使用通用`IRoomResettable`路径；时序和死亡恢复未运行验证 |
| RV-12 | Scene/Prefab/Builder/文档 | 通过（静态） | Scene不序列化Player，动态对象保持Prefab连接，Builder与本文坐标一致 |

## 实施记录与未验证风险

- 已完成：标准Tilemap灰盒、入口与返回出口、镜像占板火窗、周期喷发、固定投火者、火球锁存目标门、固定相机、统一重置配置、Build Settings和FIRE_019双向连接。
- 视觉：只实现纯色玩法层；Decoration Renderer关闭；正式环境分层素材全部省略。
- Play Mode短时观察：已执行；DEFAULT入口生成Player成功，两扇门保持初始关闭，固定镜头完整显示关键因果对象，Console为0个运行错误。该观察不包含完整解谜或切场试玩。
- 喷发Warning阶段当前没有独立可见预警柱，仅保留计时，属于已知可读性风险。
- 未执行：PlayMode自动测试、完整EditMode、Batch编译和完整人工试玩。
- 尚未验证：完整共享输入解法、跳跃与落口容错、喷发穿越手感、火球通过火窗命中Latch的实际时序、死亡重置、MirrorClone单独死亡以及双向切场。
