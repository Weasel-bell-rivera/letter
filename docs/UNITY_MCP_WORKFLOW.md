# Unity MCP开发工作流

## 文档职责

本文定义在当前2D平台解谜项目中使用Unity MCP读取和操作Unity Editor的流程。Unity MCP用于缩短Scene、Prefab、组件、脚本、Console和可视化验证的反馈周期，不重新定义玩法、房间、区域、输入、碰撞、重置或存档规则。

- 游戏与系统行为仍以对应设计文档为准。
- `AGENTS.md`中的任务范围、耗时操作确认、测试限制和Definition of done继续生效。
- 使用MCP不能绕过设计审批、测试确认、Unity资源安全或Git改动范围要求。

## 使用前检查

1. 阅读任务涉及的设计、系统、区域和房间文档。
2. 检查Git状态，识别并保留用户已有的未提交改动。
3. 确认Unity窗口中的MCP状态为`Session Active`，目标客户端为Codex且已配置。
4. 读取`mcpforunity://instances`；存在多个Unity实例时，显式选择当前项目实例。
5. 读取`mcpforunity://editor/state`并确认：
   - 项目与Unity实例正确。
   - 当前Scene符合任务目标。
   - `is_compiling`为`false`。
   - 没有待处理的Domain Reload或资源刷新。
   - `ready_for_tools`为`true`。

Editor未就绪时只等待并重新读取状态，不连续提交修改命令。

## 标准工作流

1. 使用资源和只读工具确认当前Scene、目标对象、组件、Prefab连接和Console状态。
2. 说明当前行为、预期行为、修改目标与明确排除项。
3. 优先建立最小范围的可运行原型或增量修改。
4. 使用Unity MCP修改Scene、Prefab或组件时，先定位精确对象；不得只凭名称猜测资源语义或依赖。
5. 多个同类且相互独立的操作优先使用批处理；存在依赖顺序时必须按顺序执行并在失败时停止。
6. 修改脚本后等待编译和Domain Reload结束，再读取Console错误。
7. 通过资源查询、组件回读、Scene View或Game View截图验证结果。
8. 只执行无需额外确认的低成本检查；耗时操作按`AGENTS.md`申请当次确认。
9. 检查Git diff，确认没有无关文件、意外Scene脏改动或`.meta`异常。
10. 汇报修改内容、Editor状态、验证证据、未执行测试和剩余风险。

## 只读操作

与当前任务直接相关的下列操作通常可以作为低成本检查执行：

- 读取Unity实例、Editor状态、项目版本和当前Scene。
- 查询Hierarchy、GameObject、组件和序列化字段。
- 查询Prefab、资源、Layer、Tag、Sorting Layer和项目包信息。
- 读取Console中的错误、警告和日志。
- 查询测试列表及当前测试任务状态，但不自动启动测试。
- 获取用于检查构图、对象位置和视觉状态的Scene View或Game View截图。

只读检查也应控制返回范围。大型Hierarchy、组件属性和资源搜索必须分页或先读取摘要，避免一次返回整个项目数据。

## Scene与Prefab操作

- 修改Scene或Prefab前，先确认当前Scene、Prefab Stage和目标资产路径。
- 正式房间仍采用标准Tilemap静态地形与通用Prefab动态对象组合。
- 房间Scene只保存房间实例配置，不复制Player、镜子、存档或全局状态系统。
- 通用行为不得写死在具体房间Scene或房间专用脚本中。
- 优先实例化现有通用Prefab，并保持Prefab连接。
- 实例覆盖只能使用对应系统文档和Prefab目录明确允许的字段。
- 不直接编辑二进制资源，不手动删除或随意修改`.meta`文件。
- 移动Unity资源时必须保留对应`.meta`文件和GUID关系。
- 保存Scene前回读关键对象和组件，并确认没有意外脏Scene。

## 脚本与编译验证

创建或修改C#脚本后：

1. 等待`mcpforunity://editor/state`中的`is_compiling`变为`false`。
2. 确认`is_domain_reload_pending`为`false`。
3. 读取Console中的Error；必要时同时读取相关Warning和堆栈。
4. 确认`ready_for_tools`恢复为`true`。
5. 只有编译成功后，才挂载新组件、设置新类型字段或继续依赖该脚本的Scene操作。

脚本创建或编辑已经触发编译时，不额外执行重复的全量资源刷新。

## 本项目重点验证

修改涉及公共玩法或房间组合时，应按任务范围检查：

- Player、镜子和MirrorClone的对象、组件及生命周期引用。
- 镜子放置、重复放置、回收和MirrorClone清理路径。
- Player死亡、MirrorClone死亡、手动重置和场景切换后的状态。
- Trigger、压力板、门和事件占用是否在对象清除时正确释放。
- Tilemap层、Collider与显式表面语义是否一致。
- Player与MirrorClone是否使用统一移动参数，房间实例没有覆盖基础参数。
- 相机边界、Player与MirrorClone同时可见要求及房间构图。
- 房间ID、Scene路径、入口出口和相邻房间文档是否一致。

上述检查默认先使用代码审查、资源查询、组件回读和截图等低成本方式。PlayMode、完整测试和人工试玩不因使用MCP而自动追加。

## 截图与可视化验证

- 需要观察实际画面时优先请求内联截图，并限制在足以判断问题的分辨率。
- Scene View用于检查网格、Collider、Gizmo、边界和编辑器布局。
- Game View用于检查相机构图、Sprite、渲染层级和运行时视觉反馈。
- 截图只能作为对应视觉状态的证据，不能替代碰撞、重置、生命周期或存档测试。
- 截图前确认当前Scene和目标相机，避免把其他打开场景的画面当作验证结果。

## 耗时操作与测试确认

以下操作仍严格遵循`AGENTS.md`，通过Unity MCP执行也必须先说明内容、目的和大致耗时，并等待用户对当次操作明确确认：

- 启动Unity Editor批处理。
- 运行PlayMode测试。
- 运行完整EditMode测试套件。
- 运行完整项目编译、完整资源重新导入或可能触发Library重建的操作。
- 复制完整项目、大量资源或创建测试副本。
- 预计超过1分钟或大量占用CPU、内存、磁盘的检查。
- 测试因失败、超时或环境问题后的再次运行。

用户只批准当前明确列出的操作。不得自动扩大测试范围、追加人工试玩或重复运行失败测试。

## 异常恢复

- 工具返回Busy：等待后读取Editor状态，不高频重复提交。
- 脚本编译或Domain Reload导致断线：等待连接恢复，再重新读取实例和Editor状态。
- 多个Unity实例：显式选择`<ProjectName>@<hash>`，不要依赖默认路由。
- 文件版本过期：重新读取目标和最新哈希，再基于最新内容修改。
- 命令无结果：检查Console、当前Scene、目标实例和工具返回值，不假定操作成功。
- Scene或Prefab出现意外修改：停止保存和后续操作，检查Git diff并报告冲突。

## 汇报要求

使用Unity MCP实施修改后的报告至少包含：

- 使用的Unity实例、Unity版本和Scene。
- 修改的文件、Scene、Prefab和GameObject。
- 是否保存Scene或Prefab。
- 编译与Console检查结果。
- 资源回读、组件检查或截图等验证证据。
- 已运行且获准的测试及结果。
- 未运行的PlayMode、完整测试或人工试玩及对应风险。
- Git diff中是否存在任务开始前已有或与任务无关的改动。

