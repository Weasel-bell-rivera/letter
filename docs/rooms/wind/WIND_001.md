# WIND_001：诱风高台

## 状态

- 当前状态：房间灰盒与逐风鳐通用Prefab已实现；自动测试因本机Unity许可证问题待执行。
- 是否允许制作灰盒：是。
- 已确认范围：一条静态Tilemap平台；逐风鳐位于房间右上方；核心解法是使用MirrorClone将逐风鳐引开。
- 本房不批准其他风区道具、风力、移动平台、门、压力板或房间专用机制。

## 地图登记

- 地图编号：`WIND_001`。
- 地图来源：`docs/maps/MAP.md`。
- 所属区域：风之区域。
- 唯一相邻房间：`WIND_002`，位于本房下方。
- 当前灰盒只验证平台、镜子诱敌、逐风鳐攻击和重置，不创建正式场景出口；通往`WIND_002`的出口与目标入口须在两房连接设计获批后补充。

## Unity资源

- Scene：`Assets/Scenes/Levels/Wind/Wind_001.unity`。
- 编辑器构建器：`Assets/Editor/Wind001RoomBuilder.cs`。
- Terrain Tile：`Assets/Tiles/Wind/Wind001TerrainGraybox.asset`。
- Hazard Tile：`Assets/Tiles/Wind/Wind001FallHazardGraybox.asset`。
- 当前状态：上述WIND_001专用灰盒资源已创建，Scene已加入Build Settings。

## 房间定位

- 房间类型：逐风鳐安全教学与Prefab验证房。
- 主要目标：让Player观察逐风鳐的距离感知，通过地面镜生成MirrorClone，并让MirrorClone成为更近目标，把逐风鳐从右上守卫点引向平台中部。
- 预期洞察：Player向左移动时，地面镜像向右移动；双方与逐风鳐的距离会快速拉开，MirrorClone因此能稳定抢走目标。
- 操作压力：低。
- 预计首次完成时间：约`30～60秒`。
- 本房不要求牺牲MirrorClone；主动回收或成功躲避都允许逐风鳐继续冲向已经记录的锁定点。

## 使用机制

- 静态、安全、允许地面镜放置的普通`Terrain`平台。
- 地面镜的重叠生成和水平输入镜射。
- 逐风鳐的圆形距离感知、无遮挡查询、最近目标、位置锁定、直线冲刺、喘息和返回。
- Player被逐风鳐命中时的完整房间重置。
- MirrorClone被逐风鳐命中时的镜像死亡和镜子回收，不重置逐风鳐。
- 平台下方的通用`Hazard`坠落回收区，只负责保证失败路径可重试。

禁止出现：

- 失重、风力推动、角色重力变化或空中镜子放置。
- 特殊镜墙、移动平台、门、压力板、检查点和正式出口。
- 第二只逐风鳐或其他敌人。
- 房间专用感知、目标选择、伤害或重置脚本。
- 运行时生成、删除或修改Tilemap。

## 灰盒布局

```text
 y= 3                                           range feedback
                                              .---------------.
 y= 2                                        (   WindRay-E    )
                                              '---------------'

 y=-1              P/M  ← Player     MirrorClone →          Goal
 y=-2  ============================================================
 y=-3  TTTTTTTTTTTTTTTTTTTTTTTTTTTT  ← Terrain Tilemap

 y=-7  XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ← Hazard Tilemap
       -14          -5      0       6        10             13   x
```

图例：

- `P/M`：Player初始位置以及首次地面镜放置位置。
- `WindRay-E`：逐风鳐唯一实例及初始守卫点。
- `Goal`：当前灰盒中的安全通过目标标记，不是正式房间出口。
- `T`：静态`Terrain` Tilemap平台。
- `X`：平台下方的通用坠落Hazard回收区。

## 空间与实例配置

### Terrain平台

- 使用标准`Terrain` Tilemap，Tile覆盖`x=-14～13`、`y=-3`，共`28`个连续`1×1` Tile。
- Tilemap使用Static `Rigidbody2D`、`TilemapCollider2D`和`CompositeCollider2D`合并连续碰撞。
- 表面返回静态、安全的`StaticSolid`语义。
- 平台是水平、静态、安全且不可破坏的普通地面，满足空间条件时允许地面镜放置。
- Tile接缝不得导致Player或MirrorClone卡顿、弹起、错误离地或错误放置镜子。

### Player与镜子

- 原型入口`PrototypeEntrance`位于`(-5, -1.08)`。
- Player初始携带并已解锁镜子，仅用于独立灰盒验证；正式世界进入条件仍以全局进度为准。
- Player进入时位于逐风鳐感知范围之外，并能在相机内同时观察逐风鳐和平台主要空间。
- 推荐首次操作：原地放置地面镜，然后向左移动。

### 逐风鳐

- 实例名：`WindRay-UpperRight`。
- 通用Prefab：`Assets/Prefabs/Gameplay/Enemies/WindRayEnemy2D.prefab`。
- 初始守卫点：`(6, 2)`。
- 初始状态：`Guarding`。
- 初始视觉朝向：左下方，只是表现配置，不改变圆形感知范围。
- Player在入口处与逐风鳐距离约`11.42 units`，不得在进入Scene时触发锁定。
- MirrorClone向右移动到逐风鳐统一感知半径内时，应明显比向左移动的Player更近，稳定成为锁定目标。

### 坠落回收区

- `Hazard` Tilemap覆盖`x=-16～15`、`y=-7`。
- Hazard可见边界、Trigger边界和实际伤害边界一致。
- Player接触后执行完整房间重置。
- MirrorClone接触后只执行镜像死亡与镜子回收。
- Hazard不破坏镜子，也不改变逐风鳐状态。

## 逐风鳐统一原型数值包（已确认）

以下数值已经确认。它们属于风区统一配置，不允许WIND_001单独覆盖：

| 参数 | 确认值 | 设计理由 |
|---|---:|---|
| 感知半径 | `6 units` | 入口保持安全；向右移动的MirrorClone可在平台中部稳定触发 |
| 感知边缘提示距离 | `0.75 units` | 角色进入实际范围前逐风鳐本体出现轻微冷色警觉变化，不绘制探测圈 |
| 预警时间 | `0.75 s` | 足够确认目标和锁定点，不要求帧级闪避 |
| 冲刺速度 | `12 units/s` | 明显快于Player，但预警后仍能通过离开锁定点躲避 |
| 最大冲刺行程 | `7 units` | 完整覆盖感知范围，同时限制异常场景中的远距离位移 |
| 喘息时间 | `1.5 s` | 给Player明确观察和转向时间 |
| 返回速度 | `2 units/s` | 与冲刺形成明显区别，并提供稳定通过窗口 |
| 位置容差 | `0.05 units` | 避免锁定点与守卫点附近反复抖动 |

这些数值必须同步保存到`Assets/Settings/Enemies/DefaultWindRayEnemy.asset`，Prefab与房间实例只引用统一资产。

## 预期流程

1. Player在入口安全观察右上方逐风鳐；靠近时通过本体警觉变化理解它即将发现角色。
2. Player在入口附近的Terrain平台放置地面镜，Player和MirrorClone重叠生成。
3. Player向左移动；MirrorClone按地面镜规则向右移动。
4. MirrorClone进入感知半径后成为比Player更近的目标。
5. 逐风鳐进入`Windup`，目标标记指向MirrorClone，并记录该时刻的锁定点。
6. Player继续移动、停止或主动回收镜子都不会改变本次锁定点。
7. 逐风鳐直线冲向平台中部，离开右上守卫位置。
8. 逐风鳐喘息并缓慢返回；Player利用窗口通过右侧目标标记。
9. 失败时可以按`R`或通过Player死亡立即恢复入口、镜子和逐风鳐初始状态。

## 重置与边界情况

- 手动重置和Player死亡：镜像消失、镜子回手、逐风鳐回到`(6, 2)`与`Guarding`，Player回到`PrototypeEntrance`，清空双方速度和敌人全部目标、锁定点与计时。
- MirrorClone被逐风鳐或坠落Hazard杀死：只回收镜像与镜子；逐风鳐保持当前攻击阶段。
- Windup期间主动回收镜像：逐风鳐继续攻击已记录锁定点。
- 已放置镜子时再次左键：沿用核心无效反馈，不改变逐风鳐状态。
- Player在平台边缘坠落：下方Hazard触发完整重置，不能无限下落形成软锁。
- Scene切换：不携带逐风鳐位置、状态、目标、计时、镜子放置或MirrorClone。

## Prefab需求

| 实例 | 通用Prefab | 资产路径 | 本房允许配置 |
|---|---|---|---|
| `WindRay-UpperRight` | `WindRayEnemy2D` | `Assets/Prefabs/Gameplay/Enemies/WindRayEnemy2D.prefab` | 位置`(6, 2)`；初始视觉朝向左下 |

Terrain与坠落Hazard为静态Tilemap，不创建房间专用运行时Prefab。Player、镜子系统、相机和`RoomResetSystem`沿用项目已有通用运行时组件，不引入WIND_001专用玩法脚本。

## 自动验证计划

### EditMode

- Prefab资产、统一配置和全部内部引用可加载。
- Prefab根对象不携带正式房间坐标或房间编号。
- WIND_001包含标准Tilemap层、28个连续Terrain Tile和有效Composite碰撞。
- Scene中恰好存在一个保持Prefab连接的`WindRayEnemy2D`实例。
- Player、镜子系统、相机和`RoomResetSystem`各一套，Scene登记到Build Settings。
- 逐风鳐位置、平台范围、Hazard范围和入口位置与本文一致。

### PlayMode

- Scene加载后Player稳定落地，逐风鳐保持`Guarding`且没有初始目标。
- Player和MirrorClone在相同距离与无遮挡条件下具有相同目标资格。
- MirrorClone比Player更近时逐风鳐锁定MirrorClone。
- 锁定后移动或回收MirrorClone不会改变锁定点。
- 冲刺不追踪、不转向且不会穿过Terrain。
- 命中MirrorClone不重置逐风鳐；命中Player执行完整房间重置。
- 手动重置恢复Player、镜子和逐风鳐初始状态。
- Player和MirrorClone坠落分别进入正确的重置或镜像死亡流程。

## 验收标准

- 玩家通过多次无惩罚尝试和本体警觉变化估计大致感知距离，并能读取当前目标和攻击锁定点。
- 不依赖其他风区道具即可完成“观察—放镜—镜像诱敌—Player通过”的闭环。
- 预期解法不要求牺牲MirrorClone，不要求随机等待或极限操作。
- Player死亡、MirrorClone死亡、主动回收、重复放置、手动重置和场景切换均符合权威规则。
- Scene静态地形使用标准Tilemap，逐风鳐使用保持连接的通用Prefab。
- 没有房间专用脚本复制敌人、镜子、伤害或重置逻辑。
