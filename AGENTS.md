# Project
这是一个2D平台解谜游戏。

玩家携带一面镜子。

开始修改前，先阅读：

- `docs/GAME_DESIGN.md`
- `docs/MIRROR_MECHANIC.md`

## Unity project

这是一个Unity 2D项目。

- Unity资源和运行时代码位于 `Assets/`。
- 项目设计文档位于 `docs/`，不要移动到 `Assets/`。
- 不要手动删除或随意修改 `.meta` 文件。
- 移动Unity资源时，必须同时保留对应的 `.meta` 文件。
- 不要直接编辑二进制资源。
- 修改Scene或Prefab前，先确认其序列化格式和依赖关系。
- 通用机制不能写死在具体房间Scene中。
- 房间专用脚本应保持最少，优先组合通用组件。


# Product principles

优先级依次为：

1. 机制清晰
2. 解谜产生顿悟感
3. 操作响应稳定
4. 关卡可以无惩罚地重新尝试
5. 视觉表现服务于信息传达



# Core mechanic constraints

镜子机制的完整定义见 `docs/MIRROR_MECHANIC.md`。
- 左键放置镜子，成功放置后生成镜像。
- 右键收回镜子，镜像同时消失。
- 修改镜子或镜像行为前，必须阅读并同步更新机制文档与相关测试。

- 镜子的放置条件必须明确且可预测。
- 玩家必须能在操作前判断镜子是否可以放置。
- 分身规则必须在所有关卡保持一致。
- 新关卡可以组合已有规则，但不能悄悄改变已有规则。
- 任何新增镜子或分身行为，都必须同步更新
  `docs/MIRROR_MECHANIC.md` 和相应测试。
- 必须明确玩家、镜子和分身之间：
  - 输入如何传递
  - 位置如何映射
  - 碰撞如何处理
  - 死亡和重置如何处理
  - 场景切换如何处理
- 镜像运动模型、地面镜物理形态和镜子放置目标属于核心产品决策，权威状态记录在 `docs/MIRROR_MECHANIC.md`。
- 如果上述任一核心决策仍标记为“待确定”，不得自行选择方案或开始镜子与镜像系统的正式实现；应停止相关部分并向用户提出明确问题。


# Engineering principles

- 将玩法规则与表现层分离。
- 不要在单个关卡脚本里复制核心机制。
- 使用明确的状态表示玩家、镜子和分身生命周期。
- 避免依赖帧率的玩法逻辑。
- 谜题对象通过通用接口交互，不直接依赖具体关卡。
- 修改公共机制时检查所有现有关卡的兼容性。
- 不修改与当前任务无关的文件。




# Required workflow

实施功能或修复问题时：

1. 阅读相关代码、场景和设计文档。
2. 说明当前行为和预期行为。
3. 优先建立最小可运行原型。
4. 实施修改。
5. 运行项目检查、测试和静态检查。
6. 验证重置、死亡、重复放置和场景切换等边界情况。
7. 汇报修改内容、验证证据以及未验证风险。


# Commands

- Unity version: `6000.5.7f1`（以 `ProjectSettings/ProjectVersion.txt` 为准）
- 项目路径不得写死为某台机器的绝对路径；运行命令时从Git仓库根目录解析。

### Windows PowerShell

```powershell
$UnityEditor = 'D:\03_Game\20_Unity\0_Editor\6000.5.7f1\Editor\Unity.exe'
$ProjectPath = (Resolve-Path (git rev-parse --show-toplevel)).Path

# Batch compile check
& $UnityEditor -batchmode -nographics -quit -projectPath $ProjectPath -logFile "$env:TEMP\letter-unity-compile.log"

# EditMode tests
& $UnityEditor -batchmode -nographics -quit -projectPath $ProjectPath -runTests -testPlatform EditMode -testResults "$env:TEMP\letter-editmode-results.xml" -logFile "$env:TEMP\letter-editmode.log"

# PlayMode tests
& $UnityEditor -batchmode -nographics -quit -projectPath $ProjectPath -runTests -testPlatform PlayMode -testResults "$env:TEMP\letter-playmode-results.xml" -logFile "$env:TEMP\letter-playmode.log"
```

### macOS shell

```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.5.7f1/Unity.app/Contents/MacOS/Unity"
PROJECT_PATH="$(git rev-parse --show-toplevel)"

# Batch compile check
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT_PATH" -logFile /tmp/letter-unity-compile.log

# EditMode tests
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT_PATH" -runTests -testPlatform EditMode -testResults /tmp/letter-editmode-results.xml -logFile /tmp/letter-editmode.log

# PlayMode tests
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT_PATH" -runTests -testPlatform PlayMode -testResults /tmp/letter-playmode-results.xml -logFile /tmp/letter-playmode.log
```

如果某台机器没有安装在上述默认位置，只调整该平台命令中的 `UnityEditor` 或 `UNITY_EDITOR` 本地变量，不修改Unity项目版本。

# Definition of done
任务只有在满足以下条件时才算完成：

- 功能在可运行场景中得到验证
- 不破坏现有镜子和分身行为
- 失败和重置路径可用
- 必要测试通过
- 设计规则发生变化时文档已经更新
- 没有无关文件改动


## World and region rules

- 地图由多个独立房间组成，房间按风、火、雪、土等元素区域组织。
- 世界地图、房间编号和房间之间的连接关系以 `docs/maps/MAP.md` 为权威来源。
- 游戏起点、区域进入顺序和区域解锁条件以 `docs/maps/WORLD_PROGRESSION.md` 为权威来源。
- 新增、删除、改名或改变房间连接时，必须同步更新 `docs/maps/MAP.md`、对应区域的 `ROOM_INDEX.md` 和房间设计文档。
- 设计或实现房间前，必须先在 `docs/maps/MAP.md` 中确认该房间的相邻房间和连接方向。
- Unity Scene、房间文档和地图中的房间编号必须完全一致；不得为同一房间创建不同编号。
- `docs/maps/MAP.md` 只定义世界结构和连接关系，不用于批准具体机制、布局或灰盒制作。
- 每个主要区域预计包含约50个房间，但质量优先于数量。
- 区域差异必须同时体现在视觉、环境行为和谜题机制中，不能只替换美术。
- 设计区域或房间前，必须阅读 `docs/LEVEL_DESIGN.md` 和对应的区域文档。
- 火之区域的规则见 `docs/regions/FIRE_REGION.md`。
- 不得为单个房间临时改变已经建立的元素、镜子或镜像规则。

## Room documentation

- 每个正式房间Scene必须有对应的房间设计文档。
- `Assets/Scenes/Levels/Fire/Fire_012.unity`
  对应 `docs/rooms/fire/FIRE_012.md`。
- 实现房间前，必须阅读对应的区域文档和房间文档。
- Scene与房间文档不一致时，必须同步更新。
- 新房间先按 `docs/systems/LEVEL_GEOMETRY_SYSTEM.md` 使用标准Tilemap结构完成静态地形灰盒，动态玩法对象使用基础Sprite和通用Prefab验证，再添加正式美术。

## Enemy spawning

- 设计或实现包含运行时生成敌人的房间前，必须阅读`docs/systems/ENEMY_SPAWN_SYSTEM.md`。
- 房间文档必须区分直接放置在Scene中的固定敌人Prefab实例，以及通过Spawner运行时创建的敌人。
- 使用Spawner时，房间文档必须分别记录：
  - `SpawnPointId`、网格位置、出生Pose和安全空间。
  - `SpawnerId`、敌人Prefab或定义、出生点引用、生成条件、重生策略、数量上限和出生点被占用时的处理。
- `EnemySpawnPoint2D`只定义敌人的出生ID与Pose，不得包含敌人行为、死亡条件、计时、波次或重生策略。
- 不得通过Tile、Sprite、GameObject、Tilemap名称、房间编号或Collider边界推断敌人出生点。
- 固定Scene敌人不要求为了统一形式迁移到Spawner。
- 生成条件、重生策略或重置结果仍标记为“待确定”或“待确认”时，不得自行选择方案或开始对应房间实现。

## Level geometry and Tilemap

- 关卡几何、Tilemap分层、碰撞和表面语义以 `docs/systems/LEVEL_GEOMETRY_SYSTEM.md` 为权威来源。
- 创建或修改正式房间的静态地形前，必须阅读该文档。
- 正式房间采用Tilemap静态地形与Prefab动态玩法对象组合的架构；现有原型允许按权威文档规定逐步迁移。
- 动态玩法对象的类型不局限于当前已有机关；凡具有运行时状态、交互、移动、重置、生命周期或持久化行为的对象，原则上使用通用组件和Prefab实现。
- 不得根据Tile名称、Sprite名称、GameObject名称、具体Tilemap名称或房间编号推断碰撞、危险或镜子放置规则。
- 新增运行时修改Tilemap的机制前，必须先明确其碰撞、表面语义、镜子与镜像交互、重置、存档和场景切换规则，并更新权威设计文档及测试。
- 不得为了单个房间直接修改Tilemap来实现未批准的全局或区域机制。

## Camera system

- 镜头比例、跟随构图、房间相机边界、固定单屏构图、重置和场景切换规则以`docs/systems/CAMERA_SYSTEM.md`为权威来源。
- 设计或实现正式房间、修改相机组件或调整房间镜头前，必须阅读该文档。
- 每个正式房间文档必须记录镜头模式、相机可显示边界、必须同时可见的玩法对象和经过批准的例外。
- 相机边界必须显式配置，不得根据Tile名称、Tilemap名称、GameObject名称或房间编号推断。
- 不得在单个房间脚本中复制或修改通用相机行为，不得通过改变Player尺寸满足房间构图。
- 修改通用相机规则时，必须同步更新权威文档和相关测试，并验证Player、MirrorClone、死亡重置、房间边缘停止跟随和场景切换。

## Player movement

- 玩家移动设计标准见 `docs/PLAYER_MOVEMENT.md`。
- Player Prefab结构、视觉资源、房间生成和场景切换生命周期见 `docs/systems/PLAYER_PREFAB.md`；修改Player Prefab、入口或生成系统前必须阅读该文档。
- 实际移动参数保存在
  `Assets/Settings/Player/DefaultPlayerMovement.asset`。
- 不得在具体房间Scene、Prefab或机关脚本中复制或覆盖基础移动参数。
- 调整移动速度、跳跃高度、重力或空中控制时，必须同步检查玩家、镜像和现有关卡。
- 修改移动手感后必须同步更新文档，并测试代表性的教学房、普通房和极限跳跃房。

## Unresolved product decisions

- 所有标记为“待确定”或“待确认”的项目都属于未批准规则。
- Codex不得自行批准区域机制、房间、输入设备、碰撞规则、重置规则或道具规则。
- 实现任务依赖未批准规则时，应停止相关部分并请求用户确认。
- 已确认的结论必须写回其权威文档后，才能用于实现。
- 重置规则见 `docs/systems/RESET_SYSTEM.md`。
- 道具规则见 `docs/systems/COLLECTIBLE_SYSTEM.md`。
- 长期存档、自动保存、损坏恢复和版本迁移规则见 `docs/systems/SAVE_SYSTEM.md`。
- 输入设备与控制规则见 `docs/INPUT_AND_CONTROLS.md`。
- 火区房间批准状态见 `docs/rooms/fire/ROOM_INDEX.md`。
- 失重之羽属于风之区域，规则见 `docs/regions/WIND_REGION.md`，不得作为火区或无区域限制的全局机制使用。
- 门和压力板规则见 `docs/systems/DOOR_SYSTEM.md`。
- 移动门、移动平台和周期移动障碍必须按 `docs/systems/MOVING_OBJECTS.md` 分开实现。
- 复活点规则见 `docs/systems/CHECKPOINT_SYSTEM.md`。
