# FIRE_004：借火开门

## 状态与定位

- 当前状态：灰盒完成，待运行时试玩验证
- 房间类型：水平投火者与火球锁存门的小型组合房
- 主要目标：让玩家第一次主动利用投火者的火球激活机关，而不是只把火球当作伤害
- 核心解谜点：用MirrorClone改变投火者的目标方向，使右向火球命中`FireballLatch`并锁存打开出口门
- 次要操作点：观察蓄力与冷却反馈，在门开启后安全通过
- 难度：简单
- 预计完成时间：约30～60秒

本房控制在一个核心洞察和一个低压力通过窗口内，不叠加普通压力板、第二扇门或其他火区机制。

## 地图连接

依据`docs/maps/MAP.md`，`FIRE_004`上接`FIRE_003`、下接`FIRE_005`。

| 本房入口或出口 | 位置 | 相邻房间 | 目标入口ID |
|---|---|---|---|
| `EntranceFromFIRE003`（`DEFAULT`） | 左下安全观察区 | `FIRE_003` | `DEFAULT` |
| `Exit-Back-to-FIRE003` | 左侧 | `FIRE_003` | `FROM_FIRE_004` |
| `EntranceFromFIRE005` | 右侧门后 | `FIRE_005` | `FROM_FIRE_005` |
| `Exit-To-FIRE005` | 右侧门后 | `FIRE_005` | `DEFAULT` |

- 正向流程为左进右出。
- 左侧返回出口在边墙`X=-12`处保留`Y=-2～0`的两格高通行缺口；右侧前进出口在边墙`X=11`处保留`Y=0～2`的两格高通行缺口。出口Trigger位于缺口内侧，背后不得存在Terrain墙Tile。
- 从`FIRE_005`返回时，Player出生在锁存板与出口门右侧；投火者向右发射的火球会先命中锁存板，因此可以重新打开门并返回左侧，不形成返回软锁。
- 入口和出口Trigger必须错开释放区，避免入场后立即反向切换Scene。

## 使用与排除机制

### 使用

- 地面镜、MirrorClone反向水平输入和独立物理
- 一个固定Scene水平投火者`HorizontalFireballEnemy2D`
- 一个竖直落地安装的`FireballLatch`模式`PressurePlate2D`
- 一扇由该锁存板控制的普通`Door2D`
- 静态安全地形、两个方向的房间入口与出口

### 不使用

- 普通`Occupancy`压力板或永久双压力板门控组
- 第二个投火者、第二扇机关门或第二个锁存板
- 岩浆、周期喷发、移动平台、Spawner、检查点
- 房间专用运行时代码或对投火者共享数值的实例覆盖

## 标准网格布局

- Grid：`1×1 Unity unit`
- 房间边界：`X=-12～12`、`Y=-3～7`
- 相机模式：固定单屏
- 正交尺寸：`7`，使用全局默认比例
- 地形由左侧低位安全观察区、两级台阶、中央射击层和右侧门后出口区组成。
- 左侧低位入口的角色中心低于投火者攻击带，允许先观察完整的蓄力、发射和冷却反馈。
- 中央射击层为平坦安全地面；MirrorHint位于`X=0`附近。
- 投火者固定放置在中央射击层地面上，Collider底部贴地且不与Terrain重叠；`FireOrigin`与锁存板处在同一水平弹道上。
- 锁存板位于投火者右侧、镜像诱敌位置左侧，保证右向火球先命中锁存板而不是MirrorClone。
- 门上方使用静态Terrain封闭，不能跳过。

```text
┌──────────────────────────────────────────────┐
│                                              │
│  低位安全区↗     P ◇ M      H → F    C D E▶ │
│  ◀E/S          ←Player   投火者  锁存板  门 │
│ ████████__█████████████████████████████████ │
└──────────────────────────────────────────────┘

◇ 建议放镜区   H 投火者   F FireballLatch
C 镜像诱敌位置 D 出口门   E 出口
```

示意图只表达关系，不代表弹道提示线；游戏内不绘制攻击带、瞄准线或机关连线。

## 建议坐标与Prefab

| 实例ID | 通用Prefab或实现 | 建议位置 | 初始配置 |
|---|---|---:|---|
| `Enemy-H1` | `HorizontalFireballEnemy2D` | `(2.5,0.5)` | 固定Scene实例；Collider底部贴合`Y=0`地面；初始向右；不覆盖共享数值 |
| `Latch-A` | `PressurePlate2D` | `(5.5,0.625)` | 旋转`90°`竖直安装，视觉底部贴合`Y=0`地面；`FireballLatch`；初始未激活 |
| `Door-A` | `Door2D` | `(8.5,1)` | 初始关闭；唯一控制源为`Latch-A` |
| `MirrorHint-A` | `Decoration` Tile | `(0,0)` | 无碰撞提示，不改变放置规则 |
| `EntranceFromFIRE003` | `RoomEntrance2D` | `(-10.5,-1.08)` | `DEFAULT`；面向右 |
| `Exit-Back-to-FIRE003` | `RoomExit2D` | `(-11.5,-1.1)` | 与左侧竖墙列中心对齐；目标`Fire_003/FROM_FIRE_004` |
| `EntranceFromFIRE005` | `RoomEntrance2D` | `(10.5,0.92)` | `FROM_FIRE_005`；面向左 |
| `Exit-To-FIRE005` | `RoomExit2D` | `(11.5,0.9)` | 与右侧竖墙列中心对齐；目标`Fire_005/DEFAULT` |

坐标是首轮灰盒落点。正式制作时可以按标准网格微调静态台阶和安全净空，但不得改变“投火者—锁存板—镜像诱敌位”的水平先后关系。

固定投火者直接放置在Scene中，不使用Spawner或`EnemySpawnPoint2D`。

## 控制关系

`Enemy-H1发射的水平火球 → Latch-A（FireballLatch） → Door-A`

- Player、MirrorClone和普通动态对象不能激活`Latch-A`。
- 火球命中`Latch-A`后立即销毁，锁存板在当前房间尝试内保持激活，`Door-A`保持开启。
- MirrorClone随后死亡、消失或镜子被收回，不解除已经激活的`Latch-A`。
- 手动重置、Player死亡重置、离开并重新进入房间时，`Latch-A`和`Door-A`恢复初始关闭状态。
- 门的防夹只延迟已经收到的关闭命令，不作为开门信号。

## 正向预期解法

1. Player从`FIRE_003`进入左下安全区，在攻击带高度之外观察投火者的一轮反馈。
2. Player沿两级台阶进入中央射击层，在`MirrorHint-A`附近放置地面镜。
3. Player向左移动到约`X=-6.5`并停在投火者攻击半宽之外；MirrorClone反向移动到约`X=6.5`，越过投火者与`Latch-A`并进入诱敌位置。
4. MirrorClone成为唯一或明显更近的合格目标，投火者锁定右方并开始蓄力。
5. 右向火球先命中`Latch-A`，火球销毁，`Door-A`锁存开启。
6. Player收回镜子或让MirrorClone停留，经过已经开启的门，从`Exit-To-FIRE005`进入`FIRE_005`。

关键稳定条件：Player停留位置必须超出投火者`6 units`攻击半宽，MirrorClone诱敌位置必须位于投火者右侧且在攻击带内；解法不得依赖距离平局。

## 返回流程

1. Player从`FIRE_005`返回并生成在门右侧。
2. Player位于投火者右侧攻击带内；投火者向右蓄力。
3. 火球在接触Player之前先命中位于两者之间的`Latch-A`，锁存打开`Door-A`。
4. Player穿门向左，经`Exit-Back-to-FIRE003`返回`FIRE_003`。

返回入口必须保留足够安全距离，确保火球Collider不可能绕过锁存板直接命中出生中的Player。

## 失败、重置与软锁

- Player被投火者本体或火球命中：执行完整房间重置，清除MirrorClone、镜子占用、敌人目标与在途火球；投火者恢复`Watching`，锁存板与门恢复未激活/关闭。
- MirrorClone被火球命中：只清除镜像并自动回收镜子；投火者保持当前阶段，已激活的`Latch-A`继续锁存。
- 手动重置：按统一顺序恢复入口、投火者、锁存板和门的初始状态。
- 火球击中关闭的`Door-A`只会销毁，不会开门；正式地形必须保证正确右向弹道先经过`Latch-A`。
- 镜像诱敌失败时，Player可以退回低位安全区或中央提示区重新放镜，不会落入单向坑。
- Door-A关闭路径被角色占用时延迟关闭，不压死、推出或穿过角色。
- 门上方和下方均不得留下绕行空间。
- 两个出口必须表现为边墙缺口；对应两格高通道内不得有Terrain Collider阻挡Player穿过出口Trigger。

## Tilemap与Scene制作要求

- Scene：`Assets/Scenes/Levels/Fire/Fire_004.unity`
- Editor构建器：`Assets/Editor/Fire004RoomBuilder.cs`
- 地形Tile：`Assets/Tiles/Graybox/Fire004Terrain.asset`
- 镜子提示Tile：`Assets/Tiles/Graybox/Fire004MirrorHint.asset`
- 标准层：`Background`、`Terrain`、`OneWayPlatform`、`SpecialMirrorWall`、`Hazard`、`Decoration`、`Foreground`
- `Terrain`：房间边界、低位观察区、两级台阶、中央射击层和门上封墙；使用显式`StaticSolid`、安全和可放置地面镜语义。
- `Decoration`：只放置镜子提示和不影响判定的观察引导。
- `OneWayPlatform`、`SpecialMirrorWall`与`Hazard`保持为空。
- Player、镜子和MirrorClone由通用系统提供，不序列化进房间Scene。
- 所有动态玩法对象保持通用Prefab连接，不复制逻辑到房间脚本。

当前Scene已按上述结构生成并保存，且已加入Build Settings；Builder、Scene、Tile与提示资产保持一致。

## 相机与信息可见性

- 固定单屏，相机中心建议为`(0,2)`。
- 可显示边界：`X=-12～12`、`Y=-3～7`。
- 必须同时可见：两个方向的入口/出口、中央放镜提示、投火者、锁存板、门和镜像诱敌位置。
- 投火者、锁存板和门之间不绘制连线；通过水平共线布局、火球运动、锁存反馈和门开启表现表达因果。
- 左下入口不得处于无预警直射线内。

## 最小验收标准

- 房间只有一个投火者、一个`FireballLatch`和一扇受控门；没有额外压力板或危险机制。
- 正向进入时，Player可以在安全区观察，MirrorClone能稳定成为右侧目标，火球必先命中`Latch-A`。
- Player不能踩踏或碰撞激活`Latch-A`，普通火球命中能锁存开门。
- 镜像消失不关闭已锁存的门；手动重置、Player死亡和重新进入房间会关闭门并重置锁存板。
- 从`FIRE_005`返回时，不需要镜像也能利用Player自身作为右侧目标安全打开门。
- 门不能被跳过或夹死角色，两侧均保留可重试和返回路线。
- 未运行PlayMode或人工试玩；攻击带高度、镜像目标距离、返回出生安全距离和通过窗口仍需灰盒完成后验证。
