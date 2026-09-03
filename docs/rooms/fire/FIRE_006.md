# FIRE_006：上下之间（暂定）

> 2026-08-31：补登记本Scene到Build Settings，修复FIRE_005既有出口无法加载本房的配置遗漏；未改Scene、核心目标或无出口范围。核查记录见[FIRE_REPAIR_REVIEW.md](FIRE_REPAIR_REVIEW.md)。

## 状态

- 当前状态：待试玩
- 是否允许制作灰盒：是
- 批准范围：用户已批准左侧攀爬、上层横向通道和永久收藏品；房间完成目标、出口和返回流程仍未定义
- 主要目标：Player通过左侧台阶取得收藏品，并经上层通道越过中央长岩浆沟

## 地图登记

- 地图编号：`FIRE_006`
- 地图来源：`docs/maps/MAP.md`
- 所属区域：火之区域
- 相邻房间：`FIRE_005`
- 连接关系：`FIRE_005`位于本房间右侧；本房间当前没有登记左侧相邻房间
- 房间结构定位：当前地图中的支路末端房；左侧安全平台不得在未更新地图前作为通往其他房间的出口

## Unity资源

- Scene：`Assets/Scenes/Levels/Fire/Fire_006.unity`
- Tile Palette：`Assets/TilePalettes/Fire.prefab`
- 当前状态：空间灰盒已按标准Tilemap结构重构，等待人工试玩观察构图与碰撞边界

## Prefab需求

- 当前批准范围不包含出口、门、压力板、喷发器、移动平台或复活点；新增一个永久收藏品占位实例。
- 收藏品使用通用Prefab `Assets/Prefabs/Gameplay/Collectibles/PlaceholderPermanentCollectible.prefab`，Scene实例只配置稳定ID与位置，不复制拾取或存档逻辑。
- 静态地面、上层通道和固定岩浆分别使用标准静态地形与`Hazard` Tilemap，不创建对应Prefab。
- Player、镜子生命周期和房间重置由全局系统提供，不得作为本房间专用Prefab复制。
- 当前Scene仍以内嵌Player、重置组件和岩浆Trigger完成空间灰盒验证；进入正式内容迁移时必须改为全局房间系统与标准Tilemap结构。

## 房间定位

- 房间类型：空间灰盒；具体玩法类型不在本次批准范围内
- 区域阶段：位于`FIRE_005`之后，只能使用此前已经教学并批准的规则
- 主要移动方向：从左向右
- 预计完成时间：不适用；当前Scene没有完成目标
- 操作压力：未定义；失败后可以立即在左侧入口重新尝试
- 设计重点：进入房间时同时展示中央危险区、上部结构和右侧安全平台

## 已确认的空间结构

方向约定：世界坐标右为`+X`，左为`-X`，上为`+Y`，下为`-Y`。

- 房间采用固定镜头可完整观察的横向单屏构图。
- Player从房间左侧中部的安全平台进入，初始面向右。
- 左侧平台同时承担观察区、稳定落地区和重新尝试区；空间必须足够Player稳定站立、转身和尝试放置镜子。
- 房间右侧中部设置安全平台，与左侧出生平台处于同一高度。
- 右侧平台当前不放置目标或出口。
- 房间中央下方设置一条横向长岩浆沟，约占可玩宽度的三分之一。
- 岩浆沟宽度必须大于Player的可靠普通跳跃距离，不能从左侧平台一次普通跳跃直接到达右侧平台。
- 岩浆沟表面不提供实体支撑，也不是有效镜子放置表面。
- 岩浆沟左右两端保留清晰的安全边界，危险Collider必须与可见岩浆边界一致。
- 房间上部设置横跨左右两侧的实体结构。
- 上部结构中央保留顶墙与横向承重平台，并在左右侧墙各开出`2 units`高的入口，形成可步行通过的上层通道。
- Player从左侧台阶跳入通道，向右穿过中央岩浆上方，再从右端离开并落到安全岸。
- Player进入时必须能同时看见右侧安全平台、中央岩浆沟、上层通道和左侧安全平台。
- 房间不设置隐藏通道。

## 灰盒布局示意

```text
┌──────────────────────────────────────┐
│##############      ##################│
│             #      #                 │
│             ########                 │
│                                      │
│    T                                 │
│────────┐  ~~~~~~~~~~~~~~~~~~  ┌──────│
│        └──────────────────────┘      │
└──────────────────────────────────────┘

T  Player直接打开Scene时的默认出生与重置位置
#  上部静态实体结构
~  持续危险的岩浆沟
─  安全静态平台
```

示意图只表达相对位置、危险分隔和上下空间关系，不代表最终Unity单位或像素比例。

## 机制边界

### 当前允许使用

- Player基础移动与跳跃
- 已确认的镜子与MirrorClone规则
- 静态、平坦、安全地面上的镜子放置
- 明确标记的特殊垂直墙面镜，但本次Scene未加入
- 已批准的岩浆伤害
- 已批准的周期喷发，但当前空间灰盒中未加入
- 已批准的门、压力板和Trigger，但当前空间灰盒中未加入
- 统一的死亡、手动重置和场景切换规则

### 当前禁止使用

- Player重力反转或在天花板行走
- 将失重之羽带入火区
- 为本房间单独改变Player或MirrorClone的重力、输入映射或碰撞规则
- 持续燃烧、热量传递、点燃、熔化、上升热流、冷却凝固和过热等尚未批准的火区候选机制
- 把岩浆临时变为平台，或允许镜子放在岩浆表面
- 为了复现参考构图而新增房间专用核心机制

## 初始状态

- Player从左侧出生点进入并面向右。
- 镜子处于已解锁且由Player持有的状态。
- MirrorClone不存在。
- 岩浆持续处于危险状态，不移动、不改变相位。
- 左右安全平台和上部结构均为静态实体。
- 当前Scene不包含门、压力板、喷发器、移动平台或复活点。
- 当前Scene包含一个可选永久收藏品，但不包含出口、房间完成目标或完成状态。

## 收藏品配置

- ID：`FIRE_006:COLLECTIBLE:01`
- 类型：`PermanentCollectible`
- 数量关系：全游戏`7`个可选收藏品中的`1`个。
- 位置：左侧最高平台上方，世界坐标`(-11, 2.5, 0)`。
- 到达路径：Player从左侧出生平台依次跳上三段静态Terrain平台后抵达。
- 必经或可选：可选；不参与房间完成、区域解锁或结局。
- 当前表现：`0.65 × 0.65 unit`金黄色方块占位，后续可替换正式视觉，但不得改动永久ID。
- 拾取与持久化：只有Player可以拾取；领取后ID写入`SaveData.collectedPermanentIds`，并同步`FIRE`区域的`regionProgress`与全世界收藏计数；死亡、手动重置、切换房间和重启均不恢复。
- 防重复与软锁：已领取存档重进房间时实例隐藏且不可交互；MirrorClone接触不结算；不领取不会阻挡Player返回或继续移动。

## 预期观察顺序

1. Player进入后首先位于安全的左侧平台。
2. Player看见右侧安全平台以及横在两者之间的长岩浆沟。
3. Player判断岩浆沟不能依靠一次普通跳跃直接越过。
4. Player取得收藏品后进入上层通道，从岩浆上方抵达右岸。

## 核心洞察与预期解法

- 核心路径为“左侧三级攀爬取得收藏品，再从上层通道越过岩浆”。
- 上层通道只要求Player步行通过，不要求在`2 units`净高的通道内部跳跃。
- 右侧安全平台没有目标对象或通往其他房间的出口。
- 当前Scene不是可判定完成的正式关卡；未来补充玩法目标时，需要重新更新本页与对应验收测试。

## 已知失败情况

- Player尝试直接跳过长岩浆沟：落入岩浆，触发玩家死亡重置。
- MirrorClone进入岩浆：只执行镜像死亡联动；Player不死亡，镜子自动回手。
- Player尝试在岩浆表面放置镜子：放置失败，房间状态保持不变，并使用统一失败反馈。
- 已经放置镜子时再次按左键：现有镜子和MirrorClone保持不变，并使用统一失败反馈。
- Player或MirrorClone碰到上层通道实体：产生普通实体碰撞，不触发额外房间专用行为。

## 重置规则

- Player死亡或手动重置：Player返回左侧入口，速度和临时运动状态清空。
- 已解锁的镜子回到Player手中，MirrorClone立即消失并清理全部Trigger、压力板和事件占用。
- 岩浆恢复持续危险状态。
- MirrorClone单独死亡不重置整个房间。
- 离开并重新进入房间时，不保留已放置镜子、MirrorClone或临时机关状态。
- 当前不设置复活点；若最终解法需要中途复活点，必须另行确认并遵守`docs/systems/CHECKPOINT_SYSTEM.md`。

## 软锁与逃课检查

- 左侧入口区必须始终是安全区域，Player死亡或手动重置后可以立即重新观察和尝试。
- 岩浆沟不得窄到可以用普通跳跃、Collider边缘站立或冲刺惯性直接越过。
- 岩浆沟也不得宽到遮挡全部有效观察关系，或让失败后的重复流程过长。
- 岩浆上方不得存在未在示意图和文档中记录的隐藏碰撞平台。
- 上部结构边缘不得形成可以站在Collider缝隙上的非预期落脚点。
- 左侧安全平台不得在当前地图登记下被实现为通往未登记房间的出口。
- 未来加入核心解法后，必须重新检查镜子回收、MirrorClone死亡和错误操作是否会把Player困在无法返回或无法重置的位置。

## 验收标准

- 房间主要结构无需图片也能通过本页文字和ASCII示意准确重建。
- 进入房间时能够同时读到出生位置、右侧安全平台、中央岩浆沟和上层通道。
- 中央岩浆沟不能通过一次可靠普通跳跃直接跨越。
- Player与MirrorClone进入岩浆时分别执行各自已确认的死亡流程。
- 镜子不能放置在岩浆表面，放置失败不改变房间状态。
- Player死亡、手动重置和重新进入房间时恢复确定的初始状态。
- 不引入任何未批准的火区机制，也不改变既有镜子和MirrorClone规则。
- Scene中除一个可选永久收藏品外，不存在出口、完成目标、门、压力板、喷发器、移动平台或房间专用脚本。

## 本次批准边界

- 房间名称不作为本次制作的阻塞条件，继续沿用暂定名“上下之间”。
- `T`明确表示左侧出生与重置位置，不是右侧目标。
- 上层穿越路线已确认；房间完成目标、出口和返回流程仍不在本次实现范围内。
- 本次批准不代表上述玩法决定已经获得批准。

## 实施限制

- 用户已明确批准生成当前空间灰盒。
- Scene不得为了形成可完成解法而自行加入新机制、目标或出口。
- 后续若增加玩法对象，必须先更新本文档，并且只使用权威文档中已批准的机制。

## 灰盒实现记录

- 实装Scene：`Assets/Scenes/Levels/Fire/Fire_006.unity`。
- 静态安全平台、房间边界和上层通道位于`Grid/Terrain`；使用`TilemapCollider2D + CompositeCollider2D + Static Rigidbody2D`合并碰撞，并显式配置`StaticSolid`与地面镜放置语义。
- 中央固定岩浆位于`Grid/Hazard`，可见Tile边界与Trigger边界一致，并显式配置`Hazard`表面语义和通用`Hazard2D`。
- 中央岩浆的可见格使用`Assets/Tiles/Fire/Fire006Lava.asset`，其Sprite来自`Assets/Art/Generated/Fire/Fire006LavaTile.png`；该房间专用美术Tile不改变8格危险范围、Trigger或表面语义。
- `Background`、`OneWayPlatform`、`SpecialMirrorWall`、`Decoration`和`Foreground`标准层已保留为空层；本房没有借此新增对应玩法语义。
- 纯表现根`EnvironmentVisuals/01 Color and Fog Backdrop`使用最远景倍率`1.00`并显式引用主相机，仅水平跟随；`Backdrop_FireCavern`使用本房专用`Assets/Art/Fire/Backgrounds/fire006_fog_light_v1.png`作为颜色/烟雾底，Unlit Sprite、`Sorting Order -100`、缩放`1.8`。不烘焙玩法布局。
- 独立`EnvironmentVisuals/03 Far Environment`使用标准倍率`0.85`并显式引用同一相机，仅水平跟随；左右岩柱复用透明模块`Assets/Art/Fire/Backgrounds/fire006_far_buttress_v1.png`，位于`(-9,-1)`和`(10,0)`，缩放分别`1.5`和`1.25`，`Sorting Order -60`、Alpha `0.38`。子对象仅含Transform与SpriteRenderer，无碰撞或玩法语义。无内容的第2、4、5、7层暂省略，不宣称完整八层已实现。
- `Grid/Terrain`继续使用同一`FireTerrainBasaltCenter` Tile与原有70格布局，仅将Tilemap表现乘色设为`(0.78, 0.58, 0.62, 1)`，使玩法岩体融入火区暖暗调色；Collider、Tile引用、表面语义和镜子放置语义均保持不变。
- 左下三级攀爬路线中位于Tilemap行`y=-2`、列`x=-9..-7`的短台阶下方增加纯表现对象`PlatformUnderside_Fire006_LeftLowerStep`，使用`Assets/Art/Environment/Fire/PlatformUndersides/fire_platform_underside_short.png`形成不规则岩体底缘，`Sorting Order -1`且仅含`Transform + SpriteRenderer`；对象不包含Collider、Trigger、表面语义或玩法脚本，不改变台阶顶面、跳跃距离和镜子放置结果。其余三张同目录素材仅作为未落位候选模块保留。
- 纯表现根`EnvironmentVisuals/08 Foreground Occlusion`使用标准倍率`0.20`，显式引用主相机且仅水平跟随；两个Sprite复用`Assets/Art/Fire/Decorations/fire006_low_rubble_v1.png`，中心分别为`(-12,-7.5)`、`(10,-7.5)`，缩放`0.5`，`Sorting Order 25`。可见顶部低于地面，不覆盖Player、镜子、落点或岩浆边界；仅Transform与SpriteRenderer，无碰撞、触发或玩法脚本。
- 使用共享Palette `Assets/TilePalettes/Fire.prefab`，迁移工具提供独立Palette同步入口，不需要重建Scene。
- 固定正交镜头尺寸：`7.5`；可一次观察完整构图。
- 左右安全平台各宽`8 units`，中央岩浆沟宽`8 units`；岩浆可见边界与Trigger Collider一致。
- `T`位于左侧平台`(-9, -3.1)`，同时作为Player出生点和`RoomResetSystem`重置点。
- 上部结构由连续Terrain Tile组成左右横梁、顶墙和中央承重平台；移除`(-3,3)`、`(-3,4)`、`(2,3)`、`(2,4)`四格侧墙，形成左右各`2 units`高的通道口；房间两端仍由Terrain Tile形成静态边界墙。
- 使用通用`PlayerController2D`、`MirrorPlayer2D`、`RoomResetSystem`、`MirrorSurface2D`和`Hazard2D`组件，没有房间专用脚本。
- 左侧新增三段静态Terrain攀爬平台；最高平台位于Tilemap行`y=1`，与顶部墙保持`3 units`净高，避免Player起跳时撞顶；其上放置`FIRE_006:COLLECTIBLE:01`占位方块，位置`(-11, 2.5, 0)`。
- Unity `6000.5.7f1`批处理已验证Scene可反序列化加载、入口与重置引用正确、镜子已解锁、两段安全地面可放镜子、岩浆为持续Hazard Trigger、U形障碍Collider完整，且Scene没有目标或出口。
- 当前项目快照批量编译通过，EditMode测试`13/13`通过；针对本Scene的临时PlayMode验证`1/1`通过，覆盖镜子放置、MirrorClone岩浆死亡联动、Player岩浆死亡重置及入口复位。
- 全量PlayMode当前为`7/11`通过；4项失败位于既有`LifecyclePlayModeTests`的Center、输入回收和存档失败重试用例，不涉及或加载`FIRE_006`。
- 尚未完成前台人工试玩；需要在Game View中确认构图可读性、上部碰撞边缘和失败后的手感。
