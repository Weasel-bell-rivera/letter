# Player Prefab与房间生成系统

## 状态与权威范围

- 当前状态：已批准；用户已确认采用统一Player Prefab，并在切换房间时重新生成Player，不让Player通过`DontDestroyOnLoad`跨场景保留。
- 本文档是Player Prefab结构、房间生成、运行时绑定、视觉资源和迁移规则的权威来源。
- 移动数值与运动结果仍以`docs/PLAYER_MOVEMENT.md`为准。
- 输入动作仍以`docs/INPUT_AND_CONTROLS.md`为准。
- 镜子与MirrorClone仍以`docs/MIRROR_MECHANIC.md`为准。
- 死亡、重置与场景切换状态仍以`docs/systems/RESET_SYSTEM.md`为准。
- 长期进度与跨房间谜题状态仍以`docs/systems/SAVE_SYSTEM.md`为准，不保存在Player实例中。

## 设计目标

- 全项目只有一个Player Prefab资产，所有房间使用完全一致的角色结构和引用。
- 房间Scene不保存Player实例，只保存入口、复活点、相机边界和房间玩法对象。
- 每次进入房间时，由通用房间生成组件在目标入口实例化新的Player。
- 死亡或手动重置只重置当前Player实例，不重新实例化Player。
- 切换房间时不携带速度、MirrorClone、已放置镜子、Trigger占用、支撑Collider或临时区域效果。
- 永久能力、永久谜题进度和跨房间谜题状态由独立状态系统恢复，不依赖Player对象是否存活。

## 资源路径

- Player Prefab：`Assets/Prefabs/Gameplay/Characters/Player.prefab`
- 基础移动参数：`Assets/Settings/Player/DefaultPlayerMovement.asset`
- 输入动作：`Assets/Settings/InputSystem_Actions.inputactions`
- 角色图片目录：`Assets/Art/Kenney/NewPlatformerPack/Sprites/Characters/Double/`
- 放置镜图片：`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Coin/coin_gold_side.png`
- 放置镜视觉Prefab：`Assets/Prefabs/Gameplay/Mirrors/PlacedMirror.prefab`

使用以下Kenney Double角色图片：

- `character_green_idle.png`
- `character_green_jump.png`
- `character_green_walk_a.png`
- `character_green_walk_b.png`
- `character_green_duck.png`
- `character_green_front.png`
- `character_green_hit.png`

所有图片使用一致的Sprite导入设置、Pivot、Pixels Per Unit、Point Filter和无压缩配置。不得在房间中替换单张图片形成不同尺寸或碰撞轮廓的Player变体。

## Prefab层级

```text
Player
└─ Visual
```

Player根对象包含：

- `Rigidbody2D`
- `BoxCollider2D`
- `PlayerInput`
- `PlayerController2D`
- `MirrorPlayer2D`

`Visual`包含一个`SpriteRenderer`和`PlayerVisual2D`。Player和MirrorClone必须复用同一视觉尺寸、Pivot、基础Sprite轮廓和朝向规则；MirrorClone只通过透明度、颜色、材质、排序或已批准特效区分身份。

Player Prefab不包含常驻的手持镜子子对象。镜子处于`Held`状态时只保留逻辑持有状态，不显示镜子；左键成功放置后由`MirrorPlayer2D`实例化统一的`PlacedMirror.prefab`，回收、死亡重置或场景切换时立即销毁该视觉。

## 固定物理与输入配置

- `BoxCollider2D`尺寸固定为`0.8 × 1.8 Unity units`。
- `Rigidbody2D`使用Dynamic Body、Continuous Collision Detection和Interpolate，并禁止物理旋转。
- 重力由`PlayerController2D`依据`DefaultPlayerMovement.asset`计算，不在Prefab或房间中复制重力常量。
- `PlayerInput`引用统一Input Action Asset，默认Action Map为`Player`，通知模式为`Send Messages`。
- Player根Transform缩放固定为`1,1,1`。
- Visual只允许通过统一配置校准显示高度，必须保持图片原始宽高比，不得非等比压缩到Collider宽度。
- Prefab不得包含房间编号、世界坐标、入口ID、检查点或房间专用对象引用。

房间不得覆盖Player尺寸、Collider、Rigidbody、移动资产、Input Actions、镜子规则、视觉缩放、死亡流程或场景切换生命周期。

## 视觉状态

- 稳定站立且无水平输入：`idle`。
- 稳定落地且存在水平输入：`walk_a`与`walk_b`循环。
- 离地：`jump`。
- `duck`图片保留给未来明确批准的下蹲或压低表现；当前没有Duck输入，不得仅因图片存在而新增下蹲玩法。
- `front`图片保留给明确的正面展示、交互或过场表现；当前普通移动不自动转为正面。
- `hit`图片保留给统一受击表现；当前死亡和重置时序未批准额外延迟，不得为了播放动画延迟伤害结算。

未进入当前状态机的图片仍必须导入并由Player视觉组件持有明确引用，避免房间或脚本临时加载任意路径。

## 房间入口与生成

每个正式房间至少包含一个`RoomEntrance2D`：

- 每个入口具有房间内唯一且稳定的`EntranceId`。
- 每个房间必须有一个安全的`DEFAULT`入口。
- 入口Transform决定Player Collider中心的生成位置。
- 入口可配置初始面向，但不得覆盖Player其他规则。
- 入口必须能够容纳完整Player Collider，不能与墙壁、门、危险区或动态对象重叠。

每个正式房间包含一个通用`RoomPlayerSpawner2D`：

1. 读取场景切换请求中的目标`EntranceId`；直接打开Scene时使用`DEFAULT`。
2. 验证场景中没有已有Player实例。
3. 找到目标入口；无效入口回退到`DEFAULT`，仍无有效入口时停止生成并报告配置错误。
4. 实例化唯一的`Player.prefab`。
5. 从存档服务应用镜子能力等永久状态；房间不得覆盖解锁状态。
6. 将Player和MirrorPlayer显式绑定到`RoomResetSystem`。
7. 将Player Transform绑定到当前房间的`CameraFollow2D`，先应用房间相机边界，再建立构图。
8. 同步物理Transform，确认生成位置安全后恢复Player输入。

生成流程不得依赖GameObject名称查找Player Prefab，不得在运行时用`AddComponent`重新拼装Player。

## 生命周期

### 进入房间

- 每次进入房间都创建一个新的Player Prefab实例。
- Player不使用`DontDestroyOnLoad`。
- 场景切换请求只传递目标房间和入口ID，不传递Player对象引用。
- 镜子已永久解锁时，新Player以镜子回手、MirrorClone不存在的状态开始；未解锁时保持未获得。

### 房间内重置

- 手动重置或Player死亡使用当前Player实例。
- 按`RESET_SYSTEM.md`顺序冻结输入、清理MirrorClone与交互占用、恢复房间对象、清空Player临时运动状态并移动到当前复活点。
- 重置后相机立即恢复当前复活点对应构图。

### 离开房间

- 冻结Player输入。
- 立即回收镜子并销毁MirrorClone。
- 清理Trigger、压力板、支撑Collider、表面速度和临时区域效果。
- 卸载旧Scene时销毁旧Player实例。
- 目标Scene按目标入口重新生成Player。

## 跨场景谜题状态

- 跨场景谜题不得通过保留Player GameObject、Player子对象或组件字段保存。
- 永久谜题状态写入存档级稳定ID。
- 只在当前尝试中保留的跨房间状态使用独立的全局运行时状态服务，并明确死亡、手动重置、离开谜题区域、退出游戏和读取存档时的清理规则。
- 新增跨场景谜题前必须同步更新`RESET_SYSTEM.md`、`SAVE_SYSTEM.md`和所有相关房间文档。

## 允许的入口实例配置

- 入口位置。
- 入口ID。
- 初始向左或向右。

不允许房间覆盖：

- Player Prefab引用为房间专用变体。
- Player组件集合、Collider、Rigidbody或视觉缩放。
- 移动、输入、镜子、MirrorClone、重置和存档规则。
- 镜子是否已解锁。

## 迁移规则

- 现有Builder中的`CreatePlayer`逻辑必须删除，改为创建入口和通用生成组件。
- 现有Scene中的内嵌Player必须移除，不得保留为禁用备份。
- 相机和重置系统在Scene资产中不再序列化引用某个Player实例，运行时由生成组件显式绑定。
- 迁移不得改变房间地形、机关位置、入口位置、Player移动参数或镜子玩法规则。

## 验证要求

### EditMode

- Player Prefab可独立加载，必需组件与内部引用完整。
- Collider、Rigidbody、PlayerInput、移动资产和七张视觉图片配置正确。
- Prefab根坐标为零、缩放为一，不含房间对象引用。
- 所有正式房间Scene都没有序列化Player实例，并且恰好有一个`DEFAULT`入口和一个通用生成组件。
- 房间Builder不再调用`AddComponent`拼装Player。

### PlayMode

- 直接加载代表性Scene后只生成一个Player，并出现在安全`DEFAULT`入口。
- 从一个房间出口进入另一个房间后，旧Player销毁，新Player在目标入口生成。
- 移动、跳跃、可变跳跃高度、朝向和动画状态正确。
- 镜子未解锁与已解锁两种存档状态均正确恢复；放置、回收和重复放置不变。
- `Held`和`Unobtained`状态不显示镜子，成功放置后显示`coin_gold_side`镜子视觉，回收后再次隐藏。
- MirrorClone继续复用Player Collider、移动参数和视觉尺寸。
- 手动重置、Player死亡、MirrorClone死亡和场景切换不残留Trigger、压力板、支撑Collider、表面速度或输入订阅。
- Camera和RoomReset绑定新Player，不保留旧房间Player引用。
- 教学房、普通房、Fire008、寒冰房、传送带房和极限跳跃房均通过代表性验证。
