# FIRE_005：双线穿越

## 状态

- 当前状态：已批准
- 是否允许制作灰盒：是
- 主要目标：组合FIRE_001至FIRE_004的知识

## 你填写

- 房间名称：双线穿越
- 入口位置：左下角
- 出口位置：右下角
- 教学目标：综合使用岩浆、周期喷发、镜像、压力板和门，不引入新规则
- 必须出现的机关：岩浆、周期喷发、镜子、镜像、压力板、门
- 禁止出现的机关：任何尚未批准或尚未教学的新机制
- 希望玩家想到的关键解法：先让MirrorClone越过短岩浆沟并留在压力板，再让Player观察喷发节奏通过出口门
- 难度：普通
- 预计完成时间：2～3分钟
- 其他要求：每一步都有安全停留区，不要求同时执行两个精确时机操作

## Codex补充

### 房间布局

- 建议灰盒尺寸：`30×10 units`。
- 玩家从左侧进入后到达中央安全放置区。
- 放置区左侧是MirrorClone路线：短岩浆沟后设置Plate-A。
- 放置区右侧是Player路线：单个周期喷发器、Door-A和出口。
- Plate-A与Door-A不绘制连线，通过状态变化和空间布局表达因果；喷发器前设置安全等待区。

```text
┌──────────────────────────────────────────┐
│ P ── ~~~ ── Plate-A ── M ── [F] ── D ─ E│
│                     ╰──────────────╯     │
└──────────────────────────────────────────┘

P 入口方向  ~ 岩浆  M 放置区  F 喷发器  D 门  E 出口
```

逻辑上，M左侧是MirrorClone任务，M右侧是Player任务；图示不代表最终像素比例。

### 对象清单

| ID | 对象 | 初始状态 | 作用 |
|---|---|---|---|
| Lava-A | 岩浆Trigger | 持续危险 | 检查MirrorClone跳跃与生存 |
| Plate-A | 压力板 | 未激活 | 控制Door-A |
| Eruption-A | 周期喷发器 | 从预警阶段开始 | 阻挡Player路线 |
| Door-A | 门 | 关闭 | 阻挡出口 |
| Exit-A | 出口 | 被门阻挡 | 完成房间 |
| MirrorHint-A | 建议放置区 | 静态提示 | 建立左右任务分工 |

控制关系：`Plate-A → Door-A`，使用持续占用逻辑。

### Prefab需求

| 实例ID | 通用Prefab或实现方式 | 资产路径 | 状态与房间配置 |
|---|---|---|---|
| Plate-A | `PressurePlate` | `Assets/Prefabs/Gameplay/Switches/PressurePlate.prefab` | 待创建；允许Player和MirrorClone占用 |
| Door-A | `Door` | `Assets/Prefabs/Gameplay/Doors/Door.prefab` | 待创建；初始关闭，控制源为Plate-A |
| Eruption-A | `EruptionHazard` | `Assets/Prefabs/Gameplay/Hazards/EruptionHazard.prefab` | 待创建；使用本房记录的固定周期并从预警阶段开始 |
| Exit-A | `RoomExit` | `Assets/Prefabs/Gameplay/Exits/RoomExit.prefab` | 待创建；被Door-A阻挡 |
| Lava-A | `Hazard` Tilemap | 不适用 | 固定持续危险区，不创建岩浆Prefab |
| MirrorHint-A | `Decoration` Tilemap | 不适用 | 静态视觉提示，不改变镜子放置规则 |

- 创建本房间灰盒前必须先实现并分别验证上表通用Prefab，再通过Prefab实例组合；本房间只配置位置、周期、初始状态和`Plate-A → Door-A`关系。

### 初始状态

- Player位于左侧入口，随后进入中央放置区。
- 镜子由Player持有，MirrorClone不存在。
- Plate-A未激活，Door-A关闭。
- Eruption-A从预警阶段开始并按固定周期运行。
- Lava-A持续危险。

### 预期解法

1. Player到达M并面向右，放置地面镜。
2. Player向右、MirrorClone向左移动。
3. Player使用跳跃输入，让MirrorClone越过左侧短岩浆沟。
4. MirrorClone到达Plate-A后停止，Door-A保持开启。
5. Player在Eruption-A前的安全区观察周期。
6. Player在冷却阶段通过喷发区域和Door-A，到达出口。

预期洞察：一次输入同时推动两条任务线；先稳定MirrorClone任务，再处理Player路线。

### 常见错误

- MirrorClone落入岩浆：镜像消失、镜子回手；Player可返回M重试。
- MirrorClone尚未踩板，Player先到门前：门关闭，但可以安全返回。
- MirrorClone踩板后收回镜子：Door-A关闭。
- Player在喷发危险阶段前进：Player死亡并完整重置。

### 重置规则

- Player死亡或手动重置：Player回入口，镜子回手，MirrorClone消失。
- Plate-A清空占用，Door-A关闭。
- Eruption-A恢复预警阶段起点。
- Lava-A保持持续危险。
- MirrorClone单独死亡不重置喷发器或其他房间状态。

### 软锁与逃课检查

- M、Plate-A前和Eruption-A前都必须有安全停留区域。
- MirrorClone死亡后Player必须能返回M，不能被Door-A困住。
- Door-A上方、下方和碰撞边缘不得存在绕过路径。
- Lava-A宽度必须低于普通安全跳跃距离。
- 只设置一个喷发器，不要求连续穿越多个时相机关。
- 门关闭路径被角色占用时遵循防夹规则。

### 验收标准

- MirrorClone可以用一次普通跳跃稳定越过Lava-A。
- MirrorClone占用Plate-A时Door-A持续开启，镜像消失后立即关闭。
- Eruption-A的预警、危险和冷却阶段清晰且固定。
- Player死亡执行完整重置；MirrorClone死亡只执行镜像联动。
- 房间不存在不可恢复的卡死状态。
- 不使用未批准机制即可完成。
- 首次测试者能在3分钟内理解“先稳定镜像任务，再通过玩家路线”的结构。
