# SNOW_001：冰门寒阶

## 状态

- 当前状态：灰盒Scene已按批准设计搭建。
- 是否允许制作灰盒：是。
- 开放灰盒条件：
  - 已完成：`FreezablePatrolEnemy2D.prefab`创建并通过独立测试。
  - 已完成：指定雪地图片导入Unity项目并创建`FrozenGround` Tile资产。
  - 已完成：雪区寒冰地面加速度与减速度确认为`0 units/s²`并写入`docs/PLAYER_MOVEMENT.md`，Player与MirrorClone使用同一实现。
  - 已完成：现有`PressurePlate2D.prefab`与`Door2D.prefab`能够通过`Door2D.controlSource`通用Scene序列化配置建立`Plate-A → Door-A`关系，不依赖房间专用脚本或Builder运行时补线。
  - 已完成：现有`Door2D.prefab`补充并验证对活动及冻结敌人的防夹行为。

本文批准房间设计，不批准用Scene内嵌脚本、临时敌人或错误表面语义绕过上述前置条件。

## 地图登记

- 地图编号：`SNOW_001`。
- 所属区域：雪之区域。
- 地图来源：`docs/maps/MAP.md`。
- 唯一相邻房间：`SNOW_002`，位于本房间下方。
- 本房不得增加通往其他房间的出口。
- `Entrance-A`和`Exit-A`都使用已登记的`SNOW_001 ↔ SNOW_002`连接；对应的`SNOW_002`生成点ID在设计`SNOW_002`时同步确定。
- 地图编号不代表访问顺序；本房是否为玩家首次进入雪区的房间仍以`docs/maps/WORLD_PROGRESSION.md`后续决定为准。

## Unity资源

- 计划Scene：`Assets/Scenes/Levels/Snow/Snow_001.unity`。
- 当前Scene状态：已创建并登记到Build Settings。
- 正式结构：标准Tilemap静态地形与通用Prefab动态对象组合。
- 不创建`Snow001PuzzleController`或其他房间专用玩法脚本。

## 房间定位

- 房间名称：冰门寒阶。
- 房间类型：雪区安全组合房。
- 主要目标：Player直接踩住压力板打开敌人门，使巡逻敌人走上寒冰地面冻结；Player再把冻结敌人作为踏板到达返回出口。
- 预期洞察：压力板不仅改变通路，也能改变敌人的运动结果，把活动危险转化为可利用的地形。
- 操作压力：低。
- 预计首次完成时间：约`60～120秒`。
- 本房没有限时解冻、随机敌人路线、移动平台、坠落伤害或强制精确跳跃。

本房不使用`SpecialMirrorWall`，也不要求玩家放置镜子。特殊墙面镜当前属于风区机制，不得在本房恢复或以雪区外观重新引入。

## 使用机制

必须出现：

- Player可直接踩到的水平普通压力板。
- 压力板持续占用控制的普通门。
- 固定范围来回巡逻的可冻结敌人。
- 静态`FrozenGround`寒冰地面。
- 冻结敌人作为安全踏板。
- 通往`SNOW_002`的房间出口。

禁止出现：

- 镜子或MirrorClone作为本房预期解法。
- `SpecialMirrorWall`或任何旋转重力MirrorClone路线。
- 永久锁存门控组。
- 第二种敌人。
- 移动平台、移动寒冰地面或周期移动障碍。
- 可破坏、融化、生成或消失的Tile。
- 限时自动解冻。
- 房间专用门控、敌人或冻结脚本。

## 房间布局

下图为侧视关系图，不代表最终Unity单位或像素比例：

```text
┌──────────────────────────────────────────────────────┐
│ Entrance-A   P ── [Plate-A] ─ Observation ─────┐     │
│             ====================================│     │
│                                                v     │
│             Enemy-A → |Door-A|  r  III F  #### │     │
│             ====================================####  │
│                                      return path ↓   │
│                                           Exit-A     │
│                                           SNOW_002   │
└──────────────────────────────────────────────────────┘
```

图例：

- `[Plate-A]`：水平安装在Player观察通道上的普通压力板。
- `Door-A`：阻挡Enemy-A通向寒冰区的普通门。
- `r`：门右侧的安全落地区及可返回控制区的静态台阶。
- `I`：`FrozenGround` Tilemap。
- `F`：Enemy-A首次脚部接触寒冰后的预期冻结位置。
- `#`：需要冻结敌人作为中间踏板才能可靠登上的高台。

## 空间结构

### 入口与控制区

- `Entrance-A`位于房间下侧偏左，通过地图中已登记的向下连接接收来自`SNOW_002`的Player。
- 入口必须是静态、安全且能容纳完整Player Collider的位置。
- Player从入口可以直接看到`Plate-A`、`Door-A`、Enemy-A、寒冰地面和目标高台。
- `Plate-A`水平安装在上层观察通道，Player从入口向右移动即可直接踩住。
- Player占用`Plate-A`时可以观察下方门、敌人和寒冰区，等待Enemy-A通过门并冻结。

### 压力板观察区

- `Plate-A`使用现有`PressurePlate2D.prefab`，保持默认水平姿态。
- Player可以稳定站在板上持续维持占用；敌人不能到达或触发该压力板。
- 本区不包含`SpecialMirrorWall`、镜像控制井或旋转压力板。

### 敌人通道与寒冰区

- Enemy-A在`Door-A`左侧的连续普通地面上来回巡逻。
- Enemy-A初始面向右；即使在门关闭时转向，门打开后也会在固定巡逻循环内再次向右并通过门。
- `Door-A`右侧依次为安全普通地面`r`、短`FrozenGround`区和目标高台。
- 目标高台左边缘使用一列从下层高度连接到高台底部的普通`Terrain`竖墙，封闭寒冰右侧的下坠缺口；该竖墙不具有危险或特殊镜墙语义。
- 寒冰区长度必须使Enemy-A首次脚部接触时冻结在目标高台的可靠标准跳跃范围内。
- 寒冰区与门之间保留至少一个完整Enemy Collider长度的普通安全地面，确保敌人不会在门关闭路径内冻结。
- Player从观察通道右端下降时落在`Door-A`右侧的`r`安全区，不直接落在活动敌人或寒冰地面上。
- `r`区通过静态台阶提供返回观察通道和`Plate-A`的路线，提前离开压力板不会造成软锁。

### 高台与出口

- 高台高度必须大于Player从`r`区直接使用标准跳跃能够可靠到达的高度。
- Enemy-A冻结后，其顶部作为唯一必要的中间踏板，使Player能够跳上高台。
- 高台后的静态返回通道下降到房间下侧偏右的`Exit-A`。
- `Exit-A`使用现有`RoomExit2D.prefab`并返回唯一相邻房间`SNOW_002`。
- 本房不自行授予区域完成、永久收藏或世界解锁状态。

## Tilemap需求

本房静态内容使用以下标准Tilemap层：

| Tilemap | 本房用途 | 碰撞与语义 |
|---|---|---|
| `Background` | 雪山、天空与远景冰层 | 无碰撞 |
| `Terrain` | 入口、观察通道、Enemy-A普通巡逻地面、`r`安全区、高台和返回通道 | 实体碰撞，`StaticSolid` |
| `FrozenGround` | `Door-A`右侧、目标高台前的短寒冰地面 | 实体碰撞，`FrozenGround` |
| `Decoration` | 不参与玩法的积雪、冰晶和环境装饰 | 无碰撞 |
| `Foreground` | 不遮挡关键机关的少量前景 | 无碰撞 |

- 本房不使用`OneWayPlatform`或`Hazard` Tilemap。
- 普通Terrain、寒冰地面和装饰冰雪必须具有清晰不同的视觉语言。
- 本房不得包含`SpecialMirrorWall` Tilemap或该表面语义。
- 不得根据Tile资源名或图片路径判断冻结、镜子放置或表面类型；玩法只读取统一表面语义。

## 寒冰Tile美术来源

用户指定的原始路径：

```text
C:\Users\Xingliang\Downloads\kenney\_new-platformer-pack-1.1\Sprites\Tiles\Default\terrain\_snow\_block.png
```

该路径在当前机器上不存在。已确认同一资源包中的实际文件为：

```text
C:\Users\Xingliang\Downloads\kenney_new-platformer-pack-1.1\Sprites\Tiles\Default\terrain_snow_block.png
```

图片已人工检查，为`64×64`雪地块Sprite。资源包`License.txt`声明为Creative Commons Zero（CC0）。正式导入时：

- 不让Scene或文档运行时依赖Downloads目录。
- 原图已复制到`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_snow_block.png`，对应`.meta`已由Unity生成。
- 已创建`Assets/Tiles/Snow/FrozenGroundSnowBlock.asset`作为`FrozenGround` Tile资产。
- `FrozenGroundSnowBlock.asset`使用上述Sprite，但表面语义由`FrozenGround` Tilemap及统一表面组件提供，不从Sprite名称推断。
- 本房普通`Terrain`不得复用同一Sprite作为可站立普通地面，避免玩家无法区分普通地面和寒冰地面。
- 如需强化寒冰可读性，只允许增加不改变原图内容的统一Tilemap色调、材质高光或无碰撞装饰层；不得改变碰撞与语义边界。

图片已经导入上述项目路径，Unity导入设置为单Sprite、`64 PPU`、Point Filter，并已创建`Assets/Tiles/Snow/FrozenGroundSnowBlock.asset`。Tile使用Grid Collider；`FrozenGround`语义仍由标准Tilemap上的`SurfaceSemantic2D`显式提供，不依赖资源名称。

普通地面和普通墙壁统一使用用户指定资源包Default目录中的`terrain_sand_block_center.png`，导入项目路径为`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png`，对应Tile为`Assets/Tiles/Snow/SnowTerrainGraybox.asset`。该贴图只改变`Terrain`表现，不改变其`StaticSolid`语义。

## 动态对象与控制关系

| 实例 | 类型 | 初始状态 | 控制或作用 |
|---|---|---|---|
| `Plate-A` | 普通压力板 | 未激活 | 持续控制`Door-A` |
| `Door-A` | 普通门 | 关闭 | 阻挡Enemy-A进入寒冰区 |
| `Enemy-A` | 可冻结巡逻敌人 | `Active`、初始面向右 | 通过门后踩冰冻结并成为踏板 |
| `Exit-A` | 房间出口 | 可用但地形不可达 | 返回`SNOW_002` |

控制关系：

```text
Player占用Plate-A
        |
        v
     Door-A开启
        |
        v
Enemy-A通过门并踩上FrozenGround
        |
        v
Enemy-A进入Frozen状态
```

- `Plate-A → Door-A`使用普通持续占用逻辑，不锁存、不保存。
- Player离开`Plate-A`后压力板释放，`Door-A`按防夹规则关闭。
- Enemy-A一旦在门右侧冻结，即使Player离开压力板且门关闭，也保持冻结直到本次房间尝试重置。
- 敌人不能触发`Plate-A`。
- `Door-A`只能由`Plate-A`控制；Player、MirrorClone、活动Enemy-A或冻结Enemy-A碰到完全关闭的门时，门保持关闭、原色关闭表现和实体阻挡。
- `Door-A`保持两个标准地形格高；从门顶到上层观察通道之间使用`Terrain` Tilemap静态墙封闭，不拉伸门图或Collider。
- 只有`Door-A`已经开启、`Plate-A`随后释放并请求关闭时，关闭路径被活动或冻结敌人实际占用才必须等待；防夹不能生成开门命令，也不能改变敌人冻结状态。

## Prefab需求

| 实例 | 通用Prefab | 资产路径 | 当前状态与本房配置 |
|---|---|---|---|
| `Plate-A` | `PressurePlate2D` | `Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab` | 默认水平姿态，位于上层观察通道，Player可直接到达并持续占用 |
| `Door-A` | `Door2D` | `Assets/Prefabs/Gameplay/Doors/Door2D.prefab` | 已存在；初始关闭，通过序列化`controlSource`引用`Plate-A`；活动及冻结敌人防夹测试已通过 |
| `Enemy-A` | `FreezablePatrolEnemy2D` | `Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab` | 已创建并通过独立测试；房间实例配置初始位置、初始方向、左右局部端点、速度和端点等待时间 |
| `Exit-A` | `RoomExit2D` | `Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab` | 已存在；目标为`SNOW_002`，目标入口ID待`SNOW_002`设计时同步确定 |

- Player、镜子和MirrorClone由全局运行时系统管理，不作为本房重复Prefab实例。
- `FrozenGround`是静态Tilemap，不创建寒冰地面Prefab。
- Enemy-A的活动和冻结使用同一个Prefab实例，不创建`FrozenEnemy` Prefab。
- 本房只配置实例位置、方向、巡逻端点、门尺寸和引用关系，不修改通用Prefab规则。

## 初始状态

- Player从`Entrance-A`进入，位于安全普通地面并能看到全房主要因果链。
- 已解锁的镜子由Player持有，场景中没有MirrorClone。
- `Plate-A`未激活。
- `Door-A`完全关闭。
- Enemy-A处于`Active`状态，在门左侧固定范围来回巡逻。
- `FrozenGround`保持静态，Enemy-A尚未冻结。
- `Exit-A`处于可用状态，但高台在当前地形条件下不可直接到达。
- 房间没有检查点、永久锁存机关或长期房间状态。

## 预期解法

1. Player观察`Plate-A`与下方`Door-A`、Enemy-A和寒冰区；压力板与门之间不绘制连线。
2. Player向右移动并直接踩住`Plate-A`，`Door-A`开启。
3. Player保持占用并等待Enemy-A按固定巡逻向右通过`Door-A`。
4. Enemy-A跨过门右侧普通地面，脚部首次稳定接触`FrozenGround`并进入`Frozen`状态。
5. Player确认冻结完成后离开`Plate-A`；压力板释放、`Door-A`关闭，Enemy-A保持冻结。
6. Player从观察区下降到`r`安全区。
7. Player踩上冻结的Enemy-A，再跳上目标高台。
8. Player沿静态返回通道下降，从`Exit-A`返回`SNOW_002`。

## 失败、重置与重复尝试

### 提前离开压力板

- `Door-A`尝试关闭；若敌人在门路径中则等待路径清空。
- Enemy-A尚未冻结时继续保持活动状态和当前巡逻阶段。
- Player可以返回`Plate-A`重新踩住，不需要死亡或强制重置。

### Player接触活动敌人

- Player按敌人系统规则死亡并执行完整房间重置。
- MirrorClone消失，镜子回手。
- `Plate-A`释放、`Door-A`关闭。
- Enemy-A恢复初始位置、方向、巡逻阶段和活动状态。

### 手动重置

- Player返回`Entrance-A`，速度和寒冰运动状态清零。
- 镜子回到Player手中，MirrorClone消失并清理交互占用。
- `Plate-A`恢复未激活，`Door-A`恢复关闭。
- Enemy-A恢复初始位置、方向、巡逻阶段、伤害和活动视觉。
- 静态Terrain和`FrozenGround` Tilemap保持不变。

### 场景切换与重新进入

- 不携带镜子放置、MirrorClone、压力板占用、门状态、敌人位置或冻结状态。
- 重新进入时按本房初始状态创建。
- 本房不写入敌人冻结或普通门状态到长期存档。

## 软锁与逃课检查

- Player可以直接触发`Plate-A`；本房不要求镜子或MirrorClone参与开门。
- Player不能从入口、观察通道或`r`区直接跳上目标高台。
- 寒冰右侧由目标高台支撑竖墙封闭，Player跳跃失败时停留在可重试区域，不会落入无地形空间。
- Player不能站在活动Enemy-A顶部越过高台；活动敌人仍造成伤害。
- Player不能在Enemy-A或冻结Enemy-A顶部放置镜子。
- Player不能在普通寒冰外观装饰上触发冻结或放置规则。
- 提前离开压力板后，Player始终可以从`r`区返回`Plate-A`重新尝试。
- `Door-A`关闭时不能夹住Player、MirrorClone、活动Enemy-A或冻结Enemy-A。
- Enemy-A的目标冻结位置必须完全位于`Door-A`关闭范围之外。
- Player踩板期间拥有安全等待位置，不要求与Enemy-A巡逻相位进行精确同步。
- 出口Trigger不得从高台下方、Collider边缘或返回通道外提前触发。

## 视觉与反馈

- `Plate-A`、`Door-A`、Enemy-A、寒冰区和目标高台应尽量同时可见；压力板和门之间不绘制连线。
- `Plate-A`激活时沿用现有压力板按下反馈。
- `Door-A`沿用普通临时开启视觉，不使用永久锁存颜色。
- Enemy-A活动时有明确来回移动和转向反馈；冻结瞬间提供结霜特效与声音。
- 冻结Enemy-A的顶部必须形成清楚、平整、可站立的视觉轮廓。
- 指定`terrain_snow_block.png`只用于本房`FrozenGround`玩法层，普通Terrain使用不同灰盒或正式视觉。
- 前景和装饰不得遮挡压力板、门、敌人、寒冰边界或出口。

## 相机配置

- 镜头模式：常规Player跟随；MirrorClone和Enemy-A不作为跟随目标。
- 使用雪区统一参数：是。正交尺寸为`7`；当前站立Sprite有效高度约`1.52 Unity units`，不得用`1.8 Unity units`的Collider高度代替可见主体高度计算。
- 相机可显示边界：世界坐标`Rect(-14, -3, 29, 14)`，即左`-14`、右`15`、下`-3`、上`11`。
- 跟随轴：水平与垂直均启用，使用`0.15秒`平滑时间；边界钳制后的相机中心范围为X约`[-1.56, 2.56]`、Y固定为`4`。
- 初始构图：相机中心为`(0, 4)`；Player从`Entrance-A`进入后位于画面左侧，为向右观察谜题路线保留空间。
- 必须同时进入视野的玩法对象：`Plate-A`、`Door-A`、Enemy-A、`FrozenGround`寒冰区和目标高台；Player移动到返回通道后，出口应在进入触发范围前可见。
- 构图边界：完整相机视野不得越过上述房间边界；Player靠近左右边缘时相机分别停止，返回内部后平滑恢复跟随。
- 镜头例外：使用已批准的雪区统一正交尺寸`7`；镜头不因Enemy-A冻结、镜子放置、MirrorClone生成或门状态改变而缩放、换目标或跳转。

## 验收标准

- Scene编号、路径以及到`SNOW_002`的连接与地图一致。
- Scene中不存在`SpecialMirrorWall` Tilemap、特殊墙面语义或墙镜预期解法。
- Player可以从入口直接走到水平`Plate-A`。
- Player稳定占用`Plate-A`时`Door-A`保持开启，离开后释放。
- `Door-A`完全关闭时，Player、MirrorClone、活动Enemy-A或冻结Enemy-A从外侧接触都不会使其开启、变色或禁用Collider。
- Enemy-A在固定端点间确定性来回巡逻，不追踪Player或MirrorClone。
- Enemy-A通过门并脚部接触`FrozenGround`后只冻结一次。
- 敌人侧面或头部接触寒冰不冻结。
- Enemy-A冻结后立即停止伤害，并能被Player和MirrorClone稳定踩踏。
- Player可以利用冻结Enemy-A完成标准跳跃到达高台，不依赖像素级站位。
- Player无需使用镜像即可打开敌人路线，但仍无法绕过冻结踏板到达出口。
- 提前离开压力板、Player死亡、手动重置和重新进房均不会留下错误门、敌人或占用状态。
- `Door-A`不会夹住或穿过活动及冻结敌人。
- `FrozenGround`的可见、碰撞和表面语义边界一致，Tile接缝不会重复冻结敌人。
- 修改Sprite、Tile或GameObject名称不会改变冻结或镜子放置结果。
- Scene不包含房间专用玩法脚本，也不包含解包复制的通用Prefab。
- 相机使用雪区统一正交尺寸`7`，完整视野不越过房间边界。
- Player在房间内移动时相机平滑跟随；接近左右边界时正确停止，返回内部后平滑恢复。
- Player死亡或手动重置后，相机回到`Entrance-A`对应构图，不保留重置前的追踪速度或偏移。

## 未验证风险

- 寒冰地面的统一加速度和减速度已经确认为`0 units/s²`；仍需在正式房间中人工验证滑行距离与空间容错。
- `FreezablePatrolEnemy2D.prefab`、雪地图片、`FrozenGroundSnowBlock.asset`、通用板门绑定和敌人防夹已经完成自动验证，仍需在正式房间中人工试玩视觉反馈与平台跳跃容错。
- `SNOW_002`尚未设计，因此`Entrance-A`和`Exit-A`对应的目标生成点ID仍待同步确定。
- `Assets/Scenes/Levels/Snow/Snow_001.unity`已按本文档完成灰盒搭建；后续只需在Unity中进行人工试玩和尺寸微调。
