# 关卡几何与Tilemap系统

## 状态与权威范围

- 当前状态：已确定
- 适用范围：所有正式房间
- 本文档是静态地形、Tilemap分层、碰撞、表面语义以及动态玩法对象与地形职责边界的权威来源。
- 现有原型Scene可以在迁移完成前保留当前灰盒实现，但进入正式内容制作前必须符合本文档。
- 镜子放置、镜像生成和输入映射仍以`docs/MIRROR_MECHANIC.md`为权威来源。

## 设计目标

关卡几何系统必须满足：

1. 支持大量独立房间的快速制作和修改。
2. 分离静态地形、美术表现和玩法对象的职责。
3. 让玩家能够通过统一视觉语言判断碰撞、危险和镜子放置规则。
4. 不因地形采用Tilemap而改变玩家、镜子或镜像的通用行为。
5. 不通过房间专用脚本改变全局或区域地形规则。

## 总体架构

正式房间采用混合架构：

- Tilemap负责静态空间、固定碰撞区域和环境装饰。
- Prefab与通用组件负责具有运行时行为或状态的玩法对象。
- 玩法层通过统一的表面查询读取碰撞点、表面法线和表面语义，不直接依赖Tile名称、Sprite名称、GameObject名称、具体Tilemap名称或房间编号。
- 一个正式房间仍对应一个Unity Scene。

不得因为一个对象在视觉上接近地形，就把具有运行时状态的玩法对象直接实现为普通Tile。

## 标准房间结构

```text
Room
├─ Grid
│  ├─ Background
│  ├─ Terrain
│  ├─ FrozenGround
│  ├─ FreezingGround
│  ├─ OneWayPlatform
│  ├─ SpecialMirrorWall
│  ├─ Hazard
│  ├─ Decoration
│  └─ Foreground
├─ Gameplay
│  ├─ DynamicObjects
│  ├─ EnemySpawning
│  │  ├─ EnemySpawnRegistry2D
│  │  ├─ Spawners
│  │  └─ SpawnPoints
│  ├─ Entrances
│  └─ Exits
└─ RoomSystems
```

房间可以省略不需要的层。没有运行时敌人生成的房间应省略`EnemySpawning`，固定Scene敌人仍放在`DynamicObjects`中。不得为相同玩法语义随意创建不同名称；新增标准层必须先更新本文档。

## Tilemap职责

| Tilemap | 碰撞 | 职责 |
|---|---|---|
| `Background` | 无 | 背景表现 |
| `Terrain` | 实体碰撞 | 静态地面和普通墙壁 |
| `FrozenGround` | 实体碰撞 | 雪区静态寒冰地面，向玩法层返回`FrozenGround`表面语义 |
| `FreezingGround` | 实体碰撞 | 雪区静态冻结地面，向玩法层返回`FreezingGround`表面语义 |
| `OneWayPlatform` | 单向碰撞 | 单向平台 |
| `SpecialMirrorWall` | 实体碰撞 | 明确允许安装镜子的特殊垂直墙面 |
| `Hazard` | Trigger或对应危险系统定义的检测方式 | 岩浆、尖刺等固定危险区域 |
| `Decoration` | 无 | 中景装饰 |
| `Foreground` | 无 | 前景遮挡 |

具体表面是否允许放置镜子，以`docs/MIRROR_MECHANIC.md`为准。Tilemap分层本身不得批准新的放置表面或危险规则。

## 静态碰撞

连续静态实体地形默认使用：

- `TilemapCollider2D`
- `CompositeCollider2D`
- `Rigidbody2D`，Body Type为Static

相邻瓦片应合并为连续碰撞边界，避免玩家或镜像在瓦片接缝处卡顿、弹起或产生错误落地状态。

- 碰撞边界必须与玩家能够观察到的地形边界一致。
- 装饰Tile不得生成玩法碰撞。
- 单向平台、危险区域和特殊镜墙必须与普通地形分开，不得混入同一个语义Tilemap。
- `FrozenGround`必须与普通地形和纯装饰冰雪分开；其可见边界、实体碰撞边界和表面语义边界必须一致。
- Collider形状、Physics Layer和Composite配置必须由统一模板提供，不得在单个房间中悄悄改变同类地形的物理规则。

## 表面语义与查询

地形系统必须向玩法层提供稳定的表面语义。首批通用类别为：

```text
StaticSolid
FrozenGround
FreezingGround
OneWayPlatform
SpecialMirrorWall
Hazard
DynamicSurface
Conveyor
Spring
```

该列表可以随已批准的新机制扩展，不是对未来表面类型的封闭限制。新增类别必须定义其碰撞、镜子交互、重置和场景切换规则。

统一表面查询结果至少包含：

- 世界坐标
- 表面法线
- 表面类型
- 来源Collider
- 是否静态
- 是否安全

玩法代码不得根据Tile资源名称、Sprite、GameObject名称、具体Tilemap名称或房间编号推断上述语义。区域美术替换不得改变表面查询结果。

## `FrozenGround` Tilemap

`FrozenGround`用于制作静态寒冰地面，不用于制作移动冰面、可破坏冰块或纯装饰冰雪。其区域玩法规则见`docs/regions/SNOW_REGION.md`。

`FrozenGround`的Collider必须使用`Assets/Settings/Physics/FrozenGround.physicsMaterial2D`。该材质摩擦力和弹力均为`0`；输入导致的切向加减速仍由Player与MirrorClone移动系统根据显式`FrozenGround`语义控制，不得仅依赖物理材质或对象名称判断寒冰状态。

标准组件为：

- `Tilemap`
- `TilemapRenderer`
- `TilemapCollider2D`
- `CompositeCollider2D`
- `Rigidbody2D`，Body Type为Static
- 返回`FrozenGround`的统一表面语义组件

相邻寒冰Tile必须合并为连续碰撞边界，不能因Tile接缝导致Player、MirrorClone或敌人意外离地、改变滑行结果或重复触发冻结。

- `FrozenGround`只表达静态碰撞和表面语义。
- Player与MirrorClone的寒冰运动状态由通用角色移动系统处理。
- 敌人冻结状态由通用敌人系统处理。
- 冻结敌人不会生成、删除或替换寒冰Tile。
- 外观类似冰面的背景、前景和装饰Tile必须放入无玩法碰撞的对应Tilemap，不得返回`FrozenGround`语义。
- 移动寒冰平台属于动态玩法对象，必须使用通用移动平台Prefab实现，不能放入静态`FrozenGround` Tilemap。

## `FreezingGround` Prefab

`FreezingGround`用于制作使Player、MirrorClone和Enemy逐渐冻结的静态地面，与低摩擦`FrozenGround`分开。首版使用`Assets/Prefabs/Gameplay/Surfaces/FreezingGroundCell2D.prefab`按标准一格一个实例组合。完整区域行为见`docs/regions/SNOW_REGION.md`。

- 标准Prefab包含一格大小的可见Sprite、实体`BoxCollider2D`、Static `Rigidbody2D`、显式`SurfaceSemantic2D`、`MirrorSurface2D`和格中心提供组件。
- 可见边界、实体碰撞边界和`FreezingGround`语义边界必须一致。
- 每个Prefab实例明确提供自身格中心；玩法层根据首次接触的Collider锁定对应实例，不得从对象名称推断格子。
- 表面只提供静态碰撞、网格映射和语义；冻结量、减速、恢复、死亡与Enemy状态由通用对象组件处理。
- 首版不允许运行时生成、删除或修改`FreezingGround`实例，也不允许将该语义用于移动平台或动态对象。
- 静态、水平、安全的`FreezingGround`在满足通用空间条件时允许放置镜子。

## 镜子放置集成

镜子放置系统通过统一表面查询获得碰撞点、法线和表面类型。使用`TilemapCollider2D`或独立`Collider2D`时必须返回一致的规则结果。

- 地面放置仅允许`docs/MIRROR_MECHANIC.md`批准的静态、水平、安全表面。
- 普通垂直墙面不允许放置镜子。
- 特殊垂直镜墙必须具有统一的表面语义和视觉标识。
- 危险、动态、倾斜、可破坏或空间不足的表面按镜子机制文档拒绝放置。
- 使用Tilemap不得改变镜像生成、输入映射、碰撞或生命周期规则。

## 动态玩法对象

Tilemap主要负责静态空间表达。任何具有运行时行为或状态的玩法对象，原则上应使用独立Prefab和通用组件实现。

满足下列任一条件的对象属于动态玩法对象：

- 会移动、旋转、缩放或改变碰撞范围。
- 会在运行时生成、消失、启用或禁用。
- 会响应Player、MirrorClone、镜子或其他玩法对象。
- 具有激活、关闭、损坏、冷却、锁存等状态。
- 具有计时、周期、顺序或事件驱动行为。
- 需要参与死亡、重置、检查点或场景切换流程。
- 需要保存、读取或持久化状态。
- 会影响其他玩法对象或被其他玩法对象影响。
- 需要独立的音效、动画、反馈或调试信息。

动态玩法对象包括但不限于：

- 门与压力板
- 移动平台和移动障碍
- 地面传送带
- 固定地面弹簧
- 周期喷发器
- 检查点和房间出口
- 可收集物
- 可破坏物
- 可推动物体
- 活动敌人和冻结敌人
- 开关、传感器和Trigger
- 后续新增的区域机关和解谜对象

上述清单仅为当前示例，不构成完整或封闭的机关类型列表。动态玩法对象应优先通过通用接口、组件和Prefab组合，不得在单个房间脚本中复制其核心规则。

## 房间文档中的Prefab声明

每份包含具体玩法对象设计的房间文档，都必须包含“Prefab需求”章节；尚未展开设计的占位文档可以暂不列资产，但必须在获得灰盒批准前补齐。该章节至少记录：

- 房间实例ID或适用对象。
- 使用的通用Prefab类型。
- Prefab资产路径；设计阶段尚未创建时明确标记为“待创建”。
- 本房间允许覆盖的实例参数，例如位置、尺寸、初始状态、控制源、路径、周期和区域表现。
- 静态地形、固定危险区或纯装饰明确使用Tilemap时，说明其不是Prefab，避免职责混淆。

Prefab落实规则：

- 如果房间需要的通用Prefab尚不存在，进入该玩法对象的Scene实现前必须先创建并验证对应Prefab，再通过Prefab实例加入房间。
- 不得用Scene内嵌GameObject长期代替文档声明的Prefab；现有灰盒原型可以暂时保留，但迁移为正式内容前必须完成Prefab替换并更新房间实施记录。
- 不得为了单个房间复制通用Prefab或核心脚本。房间只配置实例关系和已批准的可调参数。
- 区域外观差异优先使用共享逻辑Prefab的视觉子对象或Prefab Variant；Variant不得改变碰撞、重置、镜子或MirrorClone交互规则。
- Prefab创建、替换或Variant配置完成后，必须验证其Prefab连接没有断开，并验证重置、死亡、重复放置和场景切换。

### 运行时敌人生成声明

包含运行时敌人生成的房间文档还必须按照`docs/rooms/ROOM_TEMPLATE.md`分别提供“敌人出生点”和“敌人生成配置”两张表：

- “敌人出生点”记录`SpawnPointId`、网格或世界位置、出生Pose和完整敌人Collider所需的安全空间。
- “敌人生成配置”记录`SpawnerId`、敌人Prefab或定义、出生点引用、首次生成条件、重生策略、同时存在数量上限、出生点被占用时的处理，以及手动重置、Player死亡和重新进房时的结果。
- 出生点表只记录空间锚点，不得写入敌人攻击、移动、死亡或被击败规则。
- 固定Scene敌人继续在“Prefab需求”中记录，不得为了填写出生点表而改成运行时生成。
- 任一生成或重生规则仍未批准时，房间文档必须标记为“待确认”，不得进入对应Scene实现。

敌人出生点、Spawner、房间注册、空间安全、重置和场景切换的完整规则见`docs/systems/ENEMY_SPAWN_SYSTEM.md`。

## 新机关扩展规则

新增机关时，必须先确定：

1. 它属于静态地形、动态玩法对象，还是两者的组合。
2. 它与Player、镜子和MirrorClone如何交互。
3. 它是否参与死亡、重置、检查点和场景切换。
4. 它是否需要持久化。
5. 它属于全局机制、区域机制还是房间实例配置。
6. 它能否通过现有通用组件组合，还是需要新增通用组件。

新机关不需要预先列入本文档才能存在，但其实现必须遵守本文档的职责边界。

如果新机制需要在运行时修改Tilemap，例如破坏、生成、熔化或冻结地形，则必须先作为独立机制获得批准，并明确：

- Tile变化规则
- 碰撞重建时机
- 表面语义变化
- 镜子和镜像交互
- 重置行为
- 存档行为
- 场景切换行为

不得在具体房间脚本中直接修改Tilemap来实现未定义或未批准的新规则。

## 美术与玩法分离

- 视觉Tile和玩法语义不得形成不可替换的隐式绑定。
- 更换区域美术不得改变碰撞边界、表面类型、镜子放置合法性、危险范围或玩家与镜像的运动结果。
- 需要分别使用视觉Tilemap和碰撞或语义Tilemap时，必须通过编辑器验证工具或自动测试检查两者是否错位。
- 可交互对象必须与纯装饰Tile具有清晰、稳定的视觉差异。

## Tile Palette编辑器制作规则

Tile Palette是关卡制作期的编辑器资产，只负责组织可供绘制的`TileBase`资源，不属于房间运行时状态，也不得成为碰撞、表面语义、镜子放置或区域规则的判断来源。

- 正式灰盒使用可提交到版本控制的共享Palette，标准路径为`Assets/TilePalettes/<Region>.prefab`；同一区域房间不得默认创建一房一份的重复Palette。
- 房间设计文档必须记录制作该房间所使用的Palette路径；同一区域需要的Tile应逐步补充到同一共享Palette。
- Editor Builder创建或更新房间使用的Tile资源后，必须同步确保对应共享Palette存在，并将本次使用的Tile加入Palette，使生成后的Scene可以立即继续手工绘制。
- Palette同步必须可重复执行：已经存在的Tile不得重复添加，策划手工添加的Tile和布局不得被清空、替换或重排，只能把缺失Tile放入空闲格。
- Palette只能引用已有Tile资产，不得复制出仅供某个房间使用的同义Tile；确实具有不同Sprite、Collider类型或已批准语义的Tile除外。
- 必须提供不重建Scene的独立同步入口。刷新Palette不得修改房间Scene、Prefab实例、相机配置或运行时状态。
- Palette为空、缺失或没有包含房间所需Tile时，属于编辑器制作资产不完整；不影响已序列化Scene的显示，但该房间不得视为具备可继续编辑的完整灰盒交付物。

Palette同步的最低验证包括：

- Palette资产能够被Unity Tile Palette窗口识别。
- Grid为矩形`1×1 Unity unit`布局。
- 房间文档声明的必需Tile均存在且引用原始Tile资产。
- 连续执行同步不会新增重复项，也不会改变已有手工项。
- 同步前后房间Scene文件内容保持不变。

## 房间制作流程

1. 阅读地图、区域、房间和本文档。
2. 确认或同步房间文档声明的区域共享Tile Palette，并验证必需Tile可用。
3. 使用标准Tilemap层建立静态地形灰盒。
4. 配置静态碰撞和表面语义。
5. 使用通用Prefab添加动态玩法对象。
6. 房间需要运行时敌人生成时，按房间文档配置出生点、Spawner和房间级注册表，并验证ID、引用和出生安全空间。
7. 验证玩家和镜像移动以及瓦片接缝。
8. 验证镜子的合法与非法放置。
9. 验证死亡、重置、重复放置和场景切换；存在Spawner时同时验证重复请求、出生点占用和旧房间延迟请求清理。
10. 完成灰盒试玩后再替换正式美术Tile。

## 验收标准

- 玩家和镜像不会在瓦片接缝处卡顿、意外离地或改变运动结果。
- 可见地形与碰撞边界一致。
- 普通地面、普通墙壁、特殊镜墙和危险区域可以明确区分。
- 普通地面、静态寒冰地面和纯装饰冰雪可以明确区分。
- `FrozenGround`的可见、碰撞和语义边界一致，Tile接缝不会重复触发敌人冻结。
- Tilemap和独立Collider对镜子放置返回一致规则。
- `Conveyor`可见边界、实体Collider、方向反馈和表面速度有效范围一致，且镜子放置查询始终拒绝该表面。
- `Spring`可见展开轮廓、实体Collider和顶/左右有效面一致，且镜子放置查询始终拒绝该表面。
- 动态玩法对象不依赖房间专用脚本实现核心规则。
- 重置、死亡和场景切换不会留下错误的Tilemap或玩法对象临时状态。
- 使用运行时敌人生成的房间具有有效且唯一的`SpawnPointId`、显式Spawner引用和安全出生空间；固定Scene敌人不会被错误迁移到Spawner。
- 新增表面或运行时Tilemap变化具有相应设计文档和测试。
- 房间声明的共享Tile Palette存在、包含必需Tile，并可在不重建Scene的情况下安全重复同步。
