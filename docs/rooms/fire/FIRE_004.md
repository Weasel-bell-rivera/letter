# FIRE_004：留在另一边

## 状态

- 当前状态：已批准
- 是否允许制作灰盒：是
- 主要目标：教学镜像、压力板和门

## 你填写

- 房间名称：留在另一边
- 入口位置：左下角
- 出口位置：右下角
- 教学目标：让MirrorClone持续占用压力板，为Player打开出口门
- 必须出现的机关：镜子、镜像、压力板、门
- 禁止出现的机关：岩浆、周期喷发以及其他会分散注意力的机制
- 希望玩家想到的关键解法：在中央放镜，让反向移动的MirrorClone走向左侧压力板，同时Player走向右侧门
- 难度：简单
- 预计完成时间：1～2分钟
- 其他要求：压力板和门必须同时可见，不绘制两者之间的连线

## Codex补充

### 房间布局

- 建议灰盒尺寸：`24×8 units`。
- 玩家从左侧进入后到达中央放置区。
- 压力板位于放置区左侧，出口门位于右侧。
- 从中央可同时看到压力板、连接线、门和出口。
- 地面平坦，不加入跳跃要求。

```text
┌──────────────────────────────────┐
│ S────── P ───── M ───── D ─── E │
│          ╰──────────────╯        │
└──────────────────────────────────┘

P 压力板    M 建议放置区    D 门    E 出口
```

### 对象清单

| ID | 对象 | 初始状态 | 作用 |
|---|---|---|---|
| Plate-A | 压力板 | 未激活 | 控制Door-A |
| Door-A | 门 | 关闭 | 阻挡出口 |
| MirrorHint-A | 建议放置区 | 静态提示 | 引导地面镜放置 |
| Exit-A | 出口 | 被门阻挡 | 完成房间 |

控制关系：`Plate-A → Door-A`，使用持续占用逻辑。

### Prefab需求

| 实例ID | 通用Prefab或实现方式 | 资产路径 | 状态与房间配置 |
|---|---|---|---|
| Plate-A | `PressurePlate` | `Assets/Prefabs/Gameplay/Switches/PressurePlate.prefab` | 待创建；允许Player和MirrorClone占用 |
| Door-A | `Door` | `Assets/Prefabs/Gameplay/Doors/Door.prefab` | 待创建；初始关闭，控制源为Plate-A |
| Exit-A | `RoomExit` | `Assets/Prefabs/Gameplay/Exits/RoomExit.prefab` | 待创建；被Door-A阻挡 |
| MirrorHint-A | `Decoration` Tilemap | 不适用 | 静态视觉提示，不改变镜子放置规则 |

- 创建本房间灰盒前必须先实现并验证`PressurePlate`、`Door`和`RoomExit`通用Prefab，再以Prefab实例配置控制关系；不得把门控逻辑写入本房间Scene。

### 初始状态

- 玩家从左侧进入并面向右。
- 镜子由玩家持有，MirrorClone不存在。
- Plate-A未激活，Door-A关闭。
- 房间没有危险区域。

### 预期解法

1. 玩家到达中央建议放置区并面向右。
2. 玩家放置地面镜，MirrorClone生成在镜子左侧。
3. Player向右移动，MirrorClone向左移动。
4. MirrorClone站上Plate-A并停止移动。
5. Door-A保持开启，Player通过门进入出口。

预期洞察：MirrorClone可以留在另一侧持续维持机关；收回镜子会释放压力板。

### 常见错误

- MirrorClone踩板后收回镜子：压力板释放，门关闭。
- Player自己踩压力板：离开后门关闭，无法同时到达出口。
- 放置方向错误：可以收回镜子、改变朝向后重新放置。

### 重置规则

- 手动重置后Player回入口，镜子回手，MirrorClone消失。
- Plate-A清空占用，Door-A恢复关闭。
- 门关闭路径被角色占用时暂停，不夹死角色。

### 软锁与逃课检查

- Door-A关闭时不能把Player困在无法重置的区域。
- Player不得利用门延迟直接穿过出口。
- MirrorClone消失后Plate-A必须立即释放。
- 门上方和下方不得存在绕过路线。

### 验收标准

- Player和MirrorClone都能触发Plate-A，动态物体不能。
- MirrorClone占用时Door-A保持开启，离开或消失后关闭。
- Door-A不会压死、推出或穿过角色。
- 房间无需跳跃即可完成。
- 首次测试者能通过压力板和门的状态变化理解因果关系。
