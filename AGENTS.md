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

- Unity version: `6000.5.8f1`
- Unity executable: `/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity`
- Batch compile check: `"/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/liuxingliang/W1 -logFile /tmp/w1-unity-compile.log`
- EditMode tests: `"/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/liuxingliang/W1 -runTests -testPlatform EditMode -testResults /tmp/w1-editmode-results.xml -logFile /tmp/w1-editmode.log`
- PlayMode tests: `"/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/liuxingliang/W1 -runTests -testPlatform PlayMode -testResults /tmp/w1-playmode-results.xml -logFile /tmp/w1-playmode.log`

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
- 新房间先使用基础Sprite和Collider完成灰盒验证，再添加正式美术。

## Player movement

- 玩家移动设计标准见 `docs/PLAYER_MOVEMENT.md`。
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
