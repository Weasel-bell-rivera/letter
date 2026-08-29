# FIRE_005：三层折返火路

## 状态

- 当前状态：待试玩
- 是否允许制作灰盒：是
- 主要目标：在三条上下排列的横向通道中连续观察投火者，并在底层用水平火球命中通道中部的锁存开关，打开最右侧出口门

## 地图登记

- 地图编号：`FIRE_005`
- 相邻房间：`FIRE_004`、`FIRE_006`
- `DEFAULT`入口来自`FIRE_004`，位于第一通道左上侧
- 第三通道右端出口通往`FIRE_006/DEFAULT`
- 返回`FIRE_004`使用第一通道左侧出口，目标入口为`FROM_FIRE_005`

## 房间结构

- 固定镜头单屏房，内部为三条上下排列的横向通道。
- 第一通道由左向右，在到达最右端投火者正上方之前，通过世界范围`X=[3,5]`的两格缺口进入第二通道。
- 第二通道由右向左，在到达最左端投火者正上方之前，通过世界范围`X=[-5,-3]`的两格缺口进入第三通道。
- 第三通道由左向右，整段地面统一下沉到Cell `Y=-7`；低净空火线的顶面也同步下移到Cell `Y=-4`，继续保持`2 units`净高。`FireballLatch`背后使用两格高的实体`Terrain`墙柱；统一下沉后的通道净空允许Player跳过墙柱。左右边界竖墙必须补到新地面，最右侧出口位置仍保留完整缺口。
- 两处落差均具有明确落地点，并分别落在对应投火者的左侧和右侧安全距离外；不得让Player直接掉到投火者头顶，不设置隐藏平台或不可恢复坑洞。

```text
┌────────────────────────────────────┐
│ E/P  ────────────────────────┐     │
│                              │     │
│     ┌─────────────────── H-1 ┘     │
│     │                              │
│     └ H-2 ───── F# ─────── D X     │
└────────────────────────────────────┘

P  DEFAULT出生点      E  返回FIRE_004
H  水平投火者         F  FireballLatch
D  竖直出口门         X  右侧通往FIRE_006
```

## 核心解谜流程

1. Player从第一通道左侧出生，向右移动，经提前设置的两格缺口落到第二通道投火者左侧。
2. Player主动靠近第二通道右端投火者，观察其蓄力与水平火球；随后向左移动，经提前设置的两格缺口落到第三通道投火者右侧。
3. Player落到第三通道后进入低净空火线；投火者开始蓄力。低顶阻止有效跳弹，Player必须在蓄力期间放置镜子并向右移动，使反向移动的MirrorClone进入Player与投火者之间，替Player承受第一发火球；不放镜或未让分身进入弹道时，Player必定被第一发火球击败并重置。
4. MirrorClone承弹消失、镜子自动回手后，Player继续向右靠近投火者攻击范围的最右临界位置。蓄力锁定右向后立即继续向右撤出弹道，利用火球速度和蓄力提前量，让火球越过Player原站位并命中`Latch-A`。
5. `Latch-A`锁存激活，第三通道最右侧的竖直`Door-A`打开。
6. Player跳上并越过`Latch-A`背后的实体墙柱，继续向右通过已经打开的门洞和`Exit-To-FIRE006`，切换到`FIRE_006/DEFAULT`。

## Prefab需求与落位

| 实例ID | 通用Prefab | 世界位置与姿态 | 配置与落位要求 |
|---|---|---|---|
| `Enemy-Mid` | `Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab` | `(6.5,-0.5)`；初始向左 | 固定Scene实例；实体底部贴合第二通道`Y=-1`表面；FireOrigin朝左净空 |
| `Enemy-Low` | 同上 | `(-7.5,-5.5)`；初始向右 | 固定Scene实例；实体底部贴合第三通道`Y=-6`表面；FireOrigin朝右净空 |
| `Lower-Fireline-Ceiling` | `Terrain` Tile | Cell`X=-3..0, Y=-4`，下表面`Y=-4` | 与第三通道地面上表面`Y=-6`形成`2 units`净高；Player和MirrorClone可站立通行，但没有足够空间用跳跃越过水平火球 |
| `MirrorHint-Low` | `Decoration` Tile | Cell`(-3,-6)` | 无Collider青色放镜提示；位于进入低净空火线的第一格，不改变地面镜合法性 |
| `Latch-A` | `Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab` | `(1.85,-5.375)`；旋转`90°` | `FireballLatch`；右边界贴合实体墙柱左面`X=2`，Trigger不嵌入Terrain |
| `Latch-Backstop` | `Terrain` Tile | Cell`(2,-6)`、`(2,-5)`，世界范围`X=[2,3]`、`Y=[-6,-4]` | 开关后的实体阻拦墙；参与Terrain碰撞。火球命中墙面开关后被消费，Player随后从墙顶跳过 |
| `Door-A` | `Assets/Prefabs/Gameplay/Doors/Door2D.prefab` | `(7.5,-5.0)`；旋转`0°` | 竖直门；实体底部贴合下沉后的第三通道`Y=-6`地面；由`Latch-A`控制 |
| `Exit-Back-to-FIRE004` | `Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab` | `(-8.5,4.2)` | 与左侧竖墙列中心对齐；完整缺口内无Terrain；目标`Fire_004/FROM_FIRE_005` |
| `Exit-To-FIRE006` | 同上 | `(8.5,-4.8)` | 位于第三通道最右侧，与右侧竖墙列中心对齐；Trigger下边界贴合下沉后的`Y=-6`地面且完整缺口无Terrain；目标`Fire_006/DEFAULT` |
| `Entrance-DEFAULT` | `RoomEntrance2D` | `(-7.0,3.92)` | 来自`FIRE_004`；第一通道左侧安全出生；面向右 |
| `EntranceFromFIRE006` | `RoomEntrance2D` | `(3.5,-5.08)` | 从`FIRE_006`返回时位于下沉后的第三通道门左侧；面向左 |

静态墙、三层地板、两处落差和出口缺口使用标准`Terrain` Tilemap，不创建房间专用碰撞对象。房间不使用Spawner。

两处下落口的Tilemap范围：

- `Drop-Upper`：第一通道地板Cell`(3,2)`、`(4,2)`留空；旧Cell`(5,2)`、`(6,2)`、`(7,2)`恢复为地板。
- `Drop-Middle`：第二通道地板Cell`(-5,-2)`、`(-4,-2)`留空；旧Cell`(-8,-2)`、`(-7,-2)`恢复为地板。

## 控制关系

`Enemy-Low发射的右向水平火球 → Latch-A（FireballLatch） → Door-A（右侧竖直门）`

- `Enemy-Mid`只承担第二通道的观察与通行压力，不连接机关。
- `Latch-A`只接受`HorizontalFireballProjectile2D`，Player、MirrorClone和普通动态对象不能激活。
- `Door-A`打开后保持锁存开启，直到手动重置、Player死亡重置或重新进入房间。

## 初始状态与重置

- Player从`Entrance-DEFAULT`出生；镜子已解锁且由Player持有；MirrorClone不存在。
- 两个投火者处于`Watching`；不存在在途火球。
- `Latch-A`未激活，`Door-A`关闭并阻挡第三层最右侧出口。
- 手动重置或Player死亡时：Player回当前入口，镜子回手，MirrorClone消失；两名投火者清除目标、阶段与火球；`Latch-A`和`Door-A`恢复初始状态。
- MirrorClone单独死亡只执行镜像死亡联动，不清除已锁存的`Latch-A`。
- 场景切换不保留在途火球、镜子、MirrorClone或临时机关状态。

## 软锁与逃课检查

- 两处落差只允许向下一层推进；每层落点必须安全，失败可通过统一重置重新尝试。
- 第一落点位于`Enemy-Mid`左侧，第二落点位于`Enemy-Low`右侧；Player不会落到敌人实体或DamageTrigger上，也不会在落地瞬间无预警受到伤害。
- 第二落点本身保留完整下落净空；其右侧低净空火线必须连续覆盖Cell`X=-3..0, Y=-4`，净高固定为`2 units`。第一发火球触发后，Player不能靠跳跃、退回上层或穿过Tile逃课，只能让MirrorClone在其左侧承弹。
- 放镜提示位于低净空火线入口。按地面镜输入映射，Player向右撤离时MirrorClone向左进入弹道；镜像承弹后只回收镜子，不重置房间，使Player可以继续进行第二阶段。
- 机关弹必须在投火者攻击范围最右临界位置诱发；Player若站得更右则投火者不蓄力，站得更左且没有利用蓄力时间右撤则会被火球击中。不得通过扩大共享检测范围或降低共享火球速度修正本房。
- 第三通道投火者到`Latch-A`的距离不得超过火球`2 seconds × 8 units/s`的最大行程，并预留Collider命中余量。
- `Latch-A`背后的两格Terrain墙柱必须保持实体碰撞，不能穿过；墙柱顶面为`Y=-4`，上方第二通道地板下表面为`Y=-2`，提供`2 units`站立净空。第三通道整段下沉后，Player必须能用共享跳跃参数跳上并越过墙柱。
- 左边界竖墙Cell `X=-9`必须补到`Y=-7`；右边界Cell `X=8`必须在地面Cell `Y=-7`及其上一格`Y=-6`连续封闭。右侧Exit占用的Cell `Y=-5..-3`保持无Terrain，不能为了补墙封死出口。
- 关闭的`Door-A`完整阻挡右侧门洞；不得从门边缘挤入出口Trigger。
- 打开的`Door-A`不在门洞留下Collider；右侧Exit背后不得有Terrain。
- Player或MirrorClone位于门关闭路径时仍遵守通用防夹规则。

## 相机

- 模式：固定单屏，不使用`CameraFollow2D`。
- 正交尺寸：`7 units`；相机中心约`(0,-0.25)`。
- 必须同时可见：第一通道入口、两处落差、两个投火者、第三通道中部锁存板、实体墙柱、下沉地面、右侧门和出口。
- 不得通过缩小Player或改变共享移动参数满足构图。

## 验收标准

- Scene、Builder和本文的对象位置、朝向、安装方式一致。
- 第一层入口位于左上角，路线稳定形成“右、下、左、下、右”的折返。
- 第二层投火者位于通道最右侧，第三层投火者位于通道最左侧。
- 第三层右向火球能在生命周期内命中`Latch-A`并锁存打开`Door-A`。
- `Latch-A`后的墙柱能阻挡Player和MirrorClone；开关区下沉一格后，Player能稳定跳上墙顶并越过，不会撞到第二通道地板。
- 第三层首次交战不放置镜子时，Player无法用有效跳跃越过火球并会被击败；正确放镜后，MirrorClone稳定进入Player左侧弹道并独自承受第一发火球。
- 第一发清场后，Player能在投火者攻击范围最右临界点触发第二发，并利用蓄力时间向右撤离，使火球命中`Latch-A`而不命中Player。
- 右侧竖直门关闭时阻挡出口，打开后Player可向右进入无Terrain阻挡的出口。
- 门、出口、锁存板和投火者不悬空、不陷入墙体；竖墙出口与墙列中心对齐。
- 不新增房间专用运行时代码，不改变镜子、MirrorClone、投火者、火球或锁存板的通用规则。

## 未验证风险

- 尚未进行PlayMode或人工试玩；“首次交战不用分身必死”、分身承弹稳定性、第三层极限站位、火球命中开关和右侧门防夹仍需运行时验证。若实测仍可在下落竖井内跳弹，应只微调第二落点与低顶起点，不改变共享Player或投火者参数。
