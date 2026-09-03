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
- 正式移动剪影目录：`Assets/Art/Characters/Player/SilhouetteV1/`
- 保留表现帧目录：`Assets/Art/Characters/Player/HandDrawn/`
- 放置镜图片：`Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Coin/coin_gold_side.png`
- 放置镜视觉Prefab：`Assets/Prefabs/Gameplay/Mirrors/PlacedMirror.prefab`

正式Player移动视觉使用从已确认男孩动作稿制作的透明剪影PNG帧：

- `player_idle_00.png`至`player_idle_01.png`
- `player_walk_00.png`至`player_walk_07.png`
- `player_jump_00.png`至`player_jump_10.png`
- `player_hit_00.png`至`player_hit_03.png`（暂时继续使用`HandDrawn/`资源）
- `player_happy_00.png`至`player_happy_01.png`（暂时继续使用`HandDrawn/`资源）

所有图片使用一致的`512 × 512`画布、中心Pivot、约`284.444 Pixels Per Unit`、Bilinear Filter和无压缩配置，使完整画布高度保持`1.8 Unity units`。移动剪影使用白色透明蒙版，由Player的`SpriteRenderer`染为近黑色；MirrorClone复用同一帧并染为浅白色。不得在房间中替换单张图片形成不同尺寸或碰撞轮廓的Player变体。

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

Player根对象同时挂载通用`FreezingVisual2D`。该组件读取`FreezingGroundActor2D`进度以及Player的`FrozenGround`首格渐冻进度，控制Player视觉子树的冰蓝染色和霜层Overlay，不自行修改移动、碰撞、输入或死亡规则。MirrorClone运行时创建后使用同一个表现组件。

- `BoxCollider2D`尺寸固定为`0.8 × 1.8 Unity units`。
- `Rigidbody2D`使用Dynamic Body、Continuous Collision Detection和Interpolate，并禁止物理旋转。
- 重力由`PlayerController2D`依据`DefaultPlayerMovement.asset`计算，不在Prefab或房间中复制重力常量。
- `PlayerInput`引用统一Input Action Asset，默认Action Map为`Player`，通知模式为`Send Messages`。
- Player根Transform缩放固定为`1,1,1`。
- Visual只允许通过统一配置校准显示高度，必须保持图片原始宽高比，不得非等比压缩到Collider宽度。
- Prefab不得包含房间编号、世界坐标、入口ID、检查点或房间专用对象引用。

房间不得覆盖Player尺寸、Collider、Rigidbody、移动资产、Input Actions、镜子规则、视觉缩放、死亡流程或场景切换生命周期。

## 视觉状态

- 稳定站立且无水平输入：2帧`idle`循环。
- 稳定落地且存在水平输入：8帧`walk`循环，以`12 FPS`播放。
- 离地：从头播放11帧`jump`，到达末帧后保持，直到重新落地；不得因滞空过长循环播放起跳动作。
- `duck`表现暂时回退到`idle`；当前没有Duck输入，不得仅因素材存在而新增下蹲玩法。
- `front`使用2帧`happy`，只用于明确的正面展示、交互或过场表现；当前普通移动不自动转为正面。
- `hit`使用4帧受击表现；当前死亡和重置时序未批准额外延迟，不得为了播放完动画延迟伤害结算。

未进入当前状态机的图片仍必须导入并由Player视觉组件持有明确引用，避免房间或脚本临时加载任意路径。

## 房间入口与生成

每个正式房间至少包含一个`RoomEntrance2D`：

- 每个入口具有房间内唯一且稳定的`EntranceId`。
- 每个房间必须有一个安全的`DEFAULT`入口。
- 入口Transform决定Player Collider中心的生成位置。
- 入口可配置初始面向，但不得覆盖Player其他规则。
- 入口必须能够容纳完整Player Collider，不能与墙壁、门、危险区或动态对象重叠。
- 入口应优先沿离开对应出口的方向，为完整Player Collider与出口Trigger边界保留至少`1.0 Unity unit`净距；`DEFAULT`入口也应优先满足与最近出口的同一净距，不得通过缩小Player Collider或出口Trigger规避。
- 对窄平台、垂直连接或既有关卡中无法安全满足上述净距的入口，通用`RoomExit2D`必须在Player生成或房间重置后保持未武装，直到Player完整离开出口边界外扩`1.0 Unity unit`的释放区后才允许触发。不得在具体房间脚本中复制或绕过该防回触流程。
- 双向或分支房间连接必须为每个已实现来源配置稳定的来源入口ID，统一命名为`FROM_<来源房间ID>`；出口必须显式请求目标房中的对应来源入口，不得把不同方向的返回连接全部指向`DEFAULT`。
- `DEFAULT`只承担直接打开Scene、无有效目标入口时的安全回退，以及房间文档明确指定的默认进入方向；来源入口不得同时标记为默认入口。

每个正式房间包含一个通用`RoomPlayerSpawner2D`：

1. 读取场景切换请求中的目标`EntranceId`；直接打开Scene时使用`DEFAULT`。
2. 验证场景中没有已有Player实例。
3. 找到目标入口；无效入口回退到`DEFAULT`，仍无有效入口时停止生成并报告配置错误。
4. 实例化唯一的`Player.prefab`。
5. 从存档服务应用镜子能力等永久状态；房间不得覆盖解锁状态。
6. 将Player和MirrorPlayer显式绑定到`RoomResetSystem`。
7. 将Player Transform绑定到当前房间的`CameraFollow2D`，先应用房间相机边界，再建立构图。
8. 同步物理Transform，确认生成位置安全后恢复Player输入。

If the requested entrance exists but cannot safely contain the complete Player collider at runtime, the spawner retries once at the room's explicit `DEFAULT` entrance. It restores Player control only after that fallback also passes the same safety test. The save service records the actual resolved entrance after successful spawning, never the rejected requested entrance.

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
- Collider、Rigidbody、PlayerInput、移动资产和五组共27帧视觉图片配置正确。
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
