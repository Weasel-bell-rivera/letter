# FIRE_008：错位熔炉

## 状态

- 当前状态：灰盒中
- 是否允许制作灰盒：是；用户已明确要求实现本页方案
- 升级条件：完成自动验证与编辑器内人工试玩，确认三段解谜的操作容错和反馈辨识度

## 地图登记

- 地图编号：`FIRE_008`
- 地图来源：`docs/maps/MAP.md`
- 所属区域：火之区域
- 上方相邻房间：`FIRE_007`
- 下方相邻房间：`FIRE_009`
- 入口方向：上方，由`FIRE_007`进入
- 出口方向：下方，通往`FIRE_009`
- 本设计不新增、删除或改变房间连接

## Unity资源

- 计划Scene：`Assets/Scenes/Levels/Fire/Fire_008.unity`
- 当前状态：灰盒Scene已创建并加入Build Settings
- 灰盒`Terrain`使用与`SNOW_001`普通地形相同的Default目录贴图：`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png`；该选择只影响视觉，不改变`StaticSolid`碰撞和镜子放置语义。

## 房间定位

- 房间类型：镜像独立物理与地形错位的组合房
- 区域阶段：位于`FIRE_007`之后，要求玩家已经理解地面镜、镜像反向移动、压力板、门和永久锁存双压力板门控组
- 主要目标：让Player和MirrorClone连续利用不同地形产生位置差，完成三组双压力板机关
- 主要洞察：共享输入不代表相同运动结果；一方被地形阻挡或无法跳跃时，另一方仍按自己的物理状态继续运动
- 难度：普通偏难
- 预计完成时间：5～8分钟
- 失败压力：低；每段完成后永久锁存并设置阶段复活点，不要求重复完成已经通过的解谜点

## 镜头配置

- 镜头模式：常规跟随，同时跟随Player的水平和垂直移动。
- 全局比例：使用`docs/systems/CAMERA_SYSTEM.md`定义的默认比例；正交镜头尺寸为`7`，当前`1.8 Unity unit`高的Player约占`16:9`屏幕高度的`12.9%`。
- 跟随表现：使用通用缓动跟随，缓动时间为`0.15秒`；镜头不得逐帧锁死Player，也不得因放置、回收或重新生成MirrorClone而改变目标、缩放或跳转。
- 相机可显示边界：`X[-14, 15]`、`Y[-14, 14]`，对应房间完整外轮廓；限制作用于完整视野而不是相机中心点。
- 边缘行为：视野触及相机边界后停止对应方向的跟随，Player继续移动并逐渐靠近屏幕边缘；Player返回内部后恢复跟随。
- 必须同时可见的对象：当前解谜段的`M`放置区、两块压力板、对应永久门、两条主要移动路线及该段岩浆或低顶限制。相邻两段可以只显示用于建立方向关系的局部入口，不要求整间三层房间同时入镜。
- 重置与场景切换：Player死亡或手动重置后，镜头立即采用当前检查点对应的受限构图；进入房间时先应用本房边界，不保留旧房间偏移。
- 构图例外：无。

## 使用机制

### 已批准机制

- 地面镜放置与回收
- MirrorClone生成、反向水平输入和独立物理结算
- Player与MirrorClone独立碰撞、落地和跳跃判定
- 静态地形、台阶、实体墙和低顶通道
- Player与MirrorClone均可触发的压力板
- 永久锁存双压力板门控组
- 岩浆伤害
- 检查点、玩家死亡重置、镜像死亡联动和场景切换

### 明确不使用

- 过关钥匙或正式`ProgressionItem`；当前项目尚未批准具体实例
- 特殊墙壁镜
- 周期喷发器
- 移动平台或移动障碍
- 失重之羽
- 热量传递、点燃、熔化、热流、冷却凝固和过热等尚未批准的火区候选机制
- 房间专用的输入、重力、碰撞或镜像修正规则

## 整体空间结构

房间采用三层折返布局。Player从上方进入，在上层向左完成解谜点一；通过门后安全下降到中层，向右完成解谜点二；再次下降到下层，向左完成解谜点三，最后从下方出口进入`FIRE_009`。

每个下降连接都位于对应永久门之后。下降高度必须安全，不设置坠落伤害，也不允许在未完成当前机关时绕过门进入下一层。

```text
                              来自 FIRE_007
                                   ↓
┌────────────────────────────────────────────────────────────────────┐
│ 上层  D1 │ [1P]────────────M1──────[1C]█      S1 / CP0             │
│           Player ←          → MirrorClone                          │
│       ↓安全下降                                                   │
│                                                                    │
│ 中层  CP1 / S2    █[2C]──低顶──M2──台阶──[2P] │ D2                │
│                         MirrorClone ←    → Player                  │
│                                                        安全下降↓   │
│                                                                    │
│ 下层  D3 │ [3P]█──低顶──~~~──M3──低顶──~~~──[3C]█    S3 / CP2     │
│           Player ←                 → MirrorClone                   │
│       ↓                                                            │
│                    Exit：通往 FIRE_009                             │
└────────────────────────────────────────────────────────────────────┘
```

图例：

- `S1～S3`：进入每个解谜段时的安全观察区
- `M1～M3`：建议镜子放置区；均为静态、水平、安全地面
- `[nP]`：预期由Player占用的压力板
- `[nC]`：预期由MirrorClone占用的压力板
- `D1～D3`：永久锁存门
- `CP0～CP2`：入口或阶段复活点
- `█`：实体挡墙或卡位墙
- `低顶`：允许角色正常行走，但会清晰阻止完整跳跃
- `~~~`：持续危险的短岩浆沟

压力板虽然标注了预期占用者，但仍遵循通用规则：Player和MirrorClone都可以触发，不按对象名称写死。

## 解谜点一：距离校准

### 布局

```text
前进方向 ←

D1 │ [1P]────────────M1──────[1C]█
            Player ←    → MirrorClone
```

- `[1C]`紧邻右侧卡位墙，距离`M1`较近。
- `[1P]`位于`M1`左侧较远处。
- 两块压力板和`D1`必须能在进入上层时被同时观察到；不使用压力板到门的绘制连线。

### 预期解法

1. Player从`S1`到达`M1`，面向左放置地面镜。
2. Player持续向左移动，MirrorClone按地面镜规则向右移动。
3. MirrorClone先到达`[1C]`，随后被板后的卡位墙阻挡，继续输入也不会离开压力板。
4. Player继续向左，利用这段额外移动距离到达`[1P]`。
5. 两块压力板同时激活，第一组机关永久锁存，`D1`完全开启。
6. Player回收镜子，通过`D1`并安全下降到`CP1`。

### 本段洞察

MirrorClone被地形阻挡后，Player仍可继续移动；双方不会被系统强制恢复对称位置。

## 解谜点二：选择性跳跃

### 布局

```text
                                              前进方向 →

                      M2──台阶──┐────────[2P] │ D2
█[2C]────低顶通道─────┘         └─Player上层路线
      MirrorClone ←                 → Player
```

- Player向右路线设置一个必须跳上的普通静态台阶。
- MirrorClone向左路线在对应位置设置低顶通道。
- 低顶下保留正常行走空间，但视觉和碰撞必须明确表示无法完成标准跳跃。
- `[2P]`位于右侧高台末端，`[2C]`位于左侧低顶通道末端并紧邻卡位墙。

### 预期解法

1. Player从`CP1`进入`S2`，到达`M2`后面向右放置地面镜。
2. Player向右移动，MirrorClone向左进入低顶通道。
3. Player到达台阶时按下跳跃。
4. Player成功跳上右侧高台；MirrorClone同时接收跳跃输入，但被低顶地形限制，仍留在下层路线。
5. 继续向右，Player到达`[2P]`，MirrorClone到达`[2C]`并被墙挡住。
6. 第二组机关永久锁存，`D2`完全开启。
7. Player回收镜子，通过`D2`并安全下降到`CP2`。

### 本段洞察

Jump输入同时交给双方，但双方根据自己的落地状态和周围地形独立判定实际运动结果。

## 解谜点三：交替校准

### 布局

```text
前进方向 ←

D3 │ [3P]█──低顶B──~~~A──M3──低顶A──~~~B──[3C]█
           Player ←                 → MirrorClone
```

- Player路线从`M3`向左依次经过：岩浆沟A、低顶B、`[3P]`。
- MirrorClone路线从`M3`向右依次经过：低顶A、岩浆沟B、`[3C]`。
- 两处岩浆沟都必须明显小于可靠普通跳跃距离，并在起跳侧提供足够安全站立区。
- 两段低顶分别与对侧的岩浆沟处于相同输入时机，用地形选择本次真正能够跳跃的角色。

### 预期解法

1. Player从`CP2`进入`S3`，在`M3`面向左放置地面镜。
2. 持续向左移动：Player到达左侧岩浆沟A时，MirrorClone进入右侧低顶A。
3. 第一次跳跃：Player越过岩浆沟A；MirrorClone被低顶A限制。
4. 继续向左：Player进入左侧低顶B时，MirrorClone到达右侧岩浆沟B。
5. 第二次跳跃：MirrorClone越过岩浆沟B；Player被低顶B限制。
6. 继续向左，Player到达`[3P]`，MirrorClone到达`[3C]`；双方均被压力板后的墙稳定卡住。
7. 第三组机关永久锁存，`D3`完全开启。
8. Player回收镜子，通过`D3`并从下方出口进入`FIRE_009`。

### 本段洞察

玩家需要连续读取两条路线，并利用交错的地形限制，让同一个共享跳跃输入先只作用于Player、再只作用于MirrorClone。

## 对象与控制关系

| ID | 对象 | 初始状态 | 作用 |
|---|---|---|---|
| `FIRE_008:DOOR_GROUP:01` | 永久锁存门控组 | 未锁存 | 管理`Door-1`、`Plate-1P`和`Plate-1C` |
| `Door-1` | 门 | 关闭 | 阻挡上层到中层的连接 |
| `Plate-1P` | 压力板 | 弹起 | 第一组输入 |
| `Plate-1C` | 压力板 | 弹起 | 第一组输入 |
| `FIRE_008:DOOR_GROUP:02` | 永久锁存门控组 | 未锁存 | 管理`Door-2`、`Plate-2P`和`Plate-2C` |
| `Door-2` | 门 | 关闭 | 阻挡中层到下层的连接 |
| `Plate-2P` | 压力板 | 弹起 | 第二组输入 |
| `Plate-2C` | 压力板 | 弹起 | 第二组输入 |
| `FIRE_008:DOOR_GROUP:03` | 永久锁存门控组 | 未锁存 | 管理`Door-3`、`Plate-3P`和`Plate-3C` |
| `Door-3` | 门 | 关闭 | 阻挡通往`FIRE_009`的最终出口 |
| `Plate-3P` | 压力板 | 弹起 | 第三组输入 |
| `Plate-3C` | 压力板 | 弹起 | 第三组输入 |
| `Lava-A` | 岩浆Trigger | 持续危险 | 第三段Player路线的短沟 |
| `Lava-B` | 岩浆Trigger | 持续危险 | 第三段MirrorClone路线的短沟 |
| `Checkpoint-1` | 检查点 | 未激活 | 第一组门后复活位置 |
| `Checkpoint-2` | 检查点 | 未激活 | 第二组门后复活位置 |
| `Exit-A` | 房间出口 | 被`Door-3`阻挡 | 通往`FIRE_009` |

三组门控组均使用AND逻辑和通用`PermanentLatchDoorGroup2D`，不得通过房间专用脚本实现。

## Prefab需求

通用Prefab的结构、职责和验证要求见`docs/systems/GAMEPLAY_PREFAB_CATALOG.md`。本房间只记录Scene实例和允许的房间配置，不重新定义门、压力板、复活点或出口的通用行为。

| 实例ID | 通用Prefab或实现方式 | 资产路径 | 状态与房间配置 |
|---|---|---|---|
| `FIRE_008:DOOR_GROUP:01` | `PermanentLatchDoorGroup2D` | `Assets/Prefabs/Gameplay/Doors/PermanentLatchDoorGroup2D.prefab` | 已创建；包含`Door-1`、`Plate-1P`和`Plate-1C`，初始未锁存 |
| `FIRE_008:DOOR_GROUP:02` | `PermanentLatchDoorGroup2D` | `Assets/Prefabs/Gameplay/Doors/PermanentLatchDoorGroup2D.prefab` | 已创建；包含`Door-2`、`Plate-2P`和`Plate-2C`，初始未锁存 |
| `FIRE_008:DOOR_GROUP:03` | `PermanentLatchDoorGroup2D` | `Assets/Prefabs/Gameplay/Doors/PermanentLatchDoorGroup2D.prefab` | 已创建；包含`Door-3`、`Plate-3P`和`Plate-3C`，初始未锁存 |
| Door-1～Door-3 | `Door2D`嵌套Prefab | `Assets/Prefabs/Gameplay/Doors/Door2D.prefab` | 已创建；初始状态由对应门控组和存档决定 |
| 六块压力板 | `PressurePlate2D`嵌套Prefab | `Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab` | 已创建；允许Player和MirrorClone占用，`P`与`C`后缀只表示预期解法 |
| Checkpoint-1、Checkpoint-2 | `Checkpoint2D` | `Assets/Prefabs/Gameplay/Checkpoints/Checkpoint2D.prefab` | 已创建；分别位于第一、第二组永久门之后 |
| Exit-A | `RoomExit2D` | `Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab` | 已创建；目标房间为`FIRE_009`，目标入口暂配置为`DEFAULT` |
| Lava-A、Lava-B | `Hazard` Tilemap | 不适用 | 固定持续危险区，不创建岩浆Prefab |
| M1～M3建议放置区 | `Decoration` Tilemap | 不适用 | 静态视觉提示，不改变镜子放置规则 |

### Scene层级

```text
Room
├─ Grid
│  ├─ Background
│  ├─ Terrain
│  ├─ OneWayPlatform
│  ├─ Hazard
│  ├─ Decoration
│  └─ Foreground
├─ Gameplay
│  ├─ DynamicObjects
│  │  ├─ DoorGroup01
│  │  ├─ DoorGroup02
│  │  ├─ DoorGroup03
│  │  ├─ Checkpoint01
│  │  └─ Checkpoint02
│  ├─ Entrances
│  └─ Exits
└─ RoomSystems
```

### 允许的实例覆盖

- 三组全局唯一`DoorGroupId`。
- 门、压力板、检查点和出口的Transform位置与朝向。
- 门的位置；门本体统一保持两个标准地形格高，不允许覆盖尺寸。
- `Exit-A`的目标房间和目标入口。

### 禁止的实例覆盖

- Player或MirrorClone触发压力板的资格。
- 单板临时开启、双板永久锁存、保存和重置规则。
- 门的防夹行为。
- Player、镜子或MirrorClone的输入、碰撞、重力和生命周期规则。

### 实施前置条件

- 本房所需通用Prefab已经创建；后续修改必须继续保持Scene实例的Prefab连接。
- 通用Prefab已先于房间Scene生成；不得以内嵌GameObject或房间专用脚本代替。
- 三个门控组复用同一个组合Prefab，通过实例ID和子对象Transform覆盖形成差异，不得复制三份Prefab资产。
- 组合Prefab资产本身不得携带会被复制的正式`DoorGroupId`；正式Scene实例必须填写有效且唯一的ID。
- 实现Prefab模板前，必须调整`PermanentLatchDoorGroup2D`对“模板空ID”和“Scene实例缺少ID”的验证时机，并增加空ID、重复ID和断开引用测试；不得借此改变门控玩法规则。

## 初始状态

- Player从上方`FIRE_007`入口出现，初始面向左。
- 镜子已经解锁并由Player持有，MirrorClone不存在。
- 三组永久门控组按存档状态初始化；未锁存组的门关闭、两块压力板弹起。
- 已在此前尝试中锁存的门控组直接恢复为门完全开启、两块压力板保持按下。
- 两处岩浆持续危险。
- 当前房间初始复活位置为上方入口；通过前两扇门后可依次激活`Checkpoint-1`和`Checkpoint-2`。

## 失败、重置和重试

- 未完成锁存时收回镜子：MirrorClone立即消失并释放压力板占用，门按剩余占用重新结算。
- 已完成锁存后收回镜子：对应门保持永久开启，两块压力板保持锁存表现。
- MirrorClone落入`Lava-B`：镜像消失、镜子回手；不重置整个房间。Player必须可以安全返回`M3`重新放置。
- Player落入`Lava-A`：执行玩家死亡重置，返回`Checkpoint-2`；镜子回手，MirrorClone消失。
- 手动重置或玩家死亡不会清除已经锁存的门控组。
- 未锁存的当前机关恢复初始状态，压力板和Trigger占用必须清空。
- 场景切换时不保留已放置镜子、MirrorClone或临时占用；永久锁存状态按存档保留。

## 软锁与逃课检查

- 每个`M`区必须允许Player稳定站立、转身、放置、回收和重新放置镜子。
- 每个解谜段在永久锁存之前，都必须允许Player返回本段放置区。
- 压力板后的卡位墙只能稳定角色位置，不能夹住角色或阻止镜子回收。
- `D1～D3`关闭时的高度和上方封闭结构必须阻止标准`3 units`跳跃绕过。
- `D1～D3`只能由各自门控组中的两块压力板及永久锁存状态控制；Player或MirrorClone碰到关闭门时，门必须保持关闭、原色关闭表现和实体阻挡。
- `D1～D3`均使用两个格子高的通用门Prefab；各门洞剩余高度由门上方的`Terrain` Tilemap静态墙封闭，不拉伸门图或门Collider。
- 只有已开启的门在压力板释放并收到关闭命令后，关闭路径被Player或MirrorClone实际占用时才遵循通用防夹规则。
- 从上一层到下一层的下降连接只能在对应门之后进入，不能从地形边缘提前跌落跳关。
- `Checkpoint-1`和`Checkpoint-2`必须位于安全、静态且能容纳完整Player Collider的位置，并放在对应永久门之后。
- 低顶Collider必须连续，不得在接缝处形成意外落地点或允许利用跳跃挤入地形。
- 两处岩浆的可见边界必须与Trigger一致，表面不提供支撑，也不能放置镜子。
- 岩浆宽度必须留有普通跳跃容错，不得把最终解谜变成极限操作挑战。
- `Door-3`关闭时不得从出口Trigger边缘提前触发切换场景。

## 视觉与反馈要求

- 三组压力板和对应门分别使用清晰一致的状态节奏；不绘制压力板到门的连线，也不能依赖文字才能理解。
- 临时单板激活与双板永久锁存必须沿用通用门控组的不同反馈。
- 低顶使用明确的压缩空间轮廓，使玩家能在跳跃前预测“这一侧跳不起来”。
- 卡位墙与压力板紧密相邻，让玩家能预判持续输入会把角色稳定留在板上。
- 第三段两组“低顶—岩浆”的对应关系必须能在安全观察区同时看见。
- 火区视觉只强化地形、岩浆和机关因果，不增加未批准的装饰性假危险。

## 验收标准

- `16:9`下Player站立主体占屏高度约为`12.9%`，处于全局`12%–14%`范围内。
- Player到达房间左、右、上、下边缘时，镜头视野分别停在`X=-14`、`X=15`、`Y=14`、`Y=-14`，不会显示房间外内容。
- Player离开边缘返回内部后镜头恢复跟随；镜头停止或恢复均不改变Player与MirrorClone的移动和碰撞结果。
- 放置、回收和重新生成MirrorClone不会改变镜头目标、缩放或房间边界。
- Player死亡或手动重置后，镜头立即恢复到当前检查点对应的正确构图。
- 房间包含三个可明确区分的解谜点，并且都必须同时协调Player与MirrorClone。
- 第一段能稳定验证一方被挡住时另一方继续运动。
- 第二段能稳定验证共享跳跃在不同地形下产生不同结果。
- 第三段需要先让Player单独跳跃，再让MirrorClone单独跳跃，顺序清晰且可重复。
- 三组压力板同时占用后分别只锁存一次，并使用全局唯一`DoorGroupId`保存。
- 已锁存机关跨手动重置、玩家死亡、重新进房和存档读取保持完成。
- 未锁存的关闭门被Player或MirrorClone从任一侧碰到时不会开启、变色或禁用Collider；只有对应压力板状态能够改变门。
- MirrorClone死亡不会重置整个房间；Player死亡按当前检查点完整重置。
- 镜子回收、重复放置、非法岩浆放置和场景切换遵循核心镜子规则。
- Player和MirrorClone不会在Tilemap接缝、低顶或卡位墙处卡入地形。
- 不使用钥匙、特殊墙壁镜或任何未批准区域机制即可完成。
- 房间从`FIRE_007`进入并从`FIRE_009`离开，Scene、房间文档和地图编号完全一致。

## 实施限制

- 本页方案已由用户明确要求实现，构成创建Unity Scene与所需通用Prefab的授权。
- 灰盒阶段必须先校准低顶高度、两处岩浆宽度、两块板之间的距离和卡位稳定性，再添加正式美术。
- 实现不得修改全局Player、镜子、MirrorClone、压力板、门、检查点或岩浆规则。
