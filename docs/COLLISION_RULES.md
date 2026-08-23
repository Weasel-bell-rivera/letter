# 碰撞规则

## Unity Layers

建议使用：

- Player
- MirrorClone
- Ground
- MirrorSurface
- Hazard
- Trigger
- Door
- Collectible

## 基本规则

- Player与Ground、Door发生实体碰撞。
- MirrorClone与哪些对象碰撞，必须在镜像机制文档中明确定义。
- Trigger不产生实体阻挡。
- Collectible通过Trigger检测拾取。
- Hazard通过Trigger或明确的碰撞接触造成死亡。
- 不允许单个房间私自修改全局Layer Collision Matrix。

## 镜子实体规则（已确定）

- 镜子不阻挡 Player、MirrorClone 或动态物体。
- 镜子不能作为玩家或镜像的站立平台。
- 镜子可以使用Trigger或专用查询接收危险、回收和生命周期交互。
- 镜子的逻辑边界不得在放置时与墙壁、门或其他实体Collider重叠。
- MirrorSurface Layer表示允许放置的表面，不代表镜子本身具有实体阻挡。

## 镜像碰撞与交互矩阵（已确定）

| 对象 | 实体碰撞 | 可触发 | 可拾取/推动 | 伤害或死亡 | 状态 |
|---|---|---|---|---|---|
| Player | 否；双方可以互相穿过，地面镜生成瞬间允许Collider完全重叠 | 不适用 | 双方不互相推动 | 接触本身不造成伤害 | 已确定 |
| Ground | 是 | 不适用 | 不适用 | 否 | 已确定 |
| Door | 是；完全关闭时持续阻挡，外侧接触不能开门 | 不适用；门体接触不是控制信号 | 不适用 | 仅在已开启的门收到关闭命令后，关闭路径被角色占用时暂停，不造成夹压伤害 | 已确定 |
| Hazard | 由危险类型决定是否实体阻挡 | 是 | 否 | 可以伤害或杀死镜像 | 已确定 |
| Trigger/压力板 | 否 | 是 | 否 | 否 | 已确定 |
| Collectible | 否 | 可以进入检测范围，但不得完成拾取 | 镜像不能拾取 | 否 | 已确定 |
| 移动平台 | 是；按MirrorClone局部重力方向判断支撑 | 否 | 与Player相同，受到支撑时随平台获得同一固定步位移 | 平台本身不造成伤害 | 已确定 |
| 地面传送带 | 是；只有水平上表面提供传送带速度 | 否 | Player与MirrorClone受到同一世界空间表面速度；侧面、底部不生效 | 不造成伤害；抵墙时由正常实体碰撞阻挡 | 已确定 |
| 竖直墙面巡逻敌人 | 是；默认实体轮廓`0.72 × 0.90 units`且中心向墙偏移`0.10 unit`，不得持续挤压或夹住角色 | 是；默认Damage Trigger为`0.82 × 0.98 units`且同步向墙偏移 | 不能拾取、推动或作为站立表面 | 接触Player触发完整房间重置；接触MirrorClone只触发镜像死亡联动 | 已确定 |
| 动态物体 | 是 | 视对象类型 | 与Player完全一致：Player能推动或被推动时镜像也能，Player不能时镜像也不能；使用相同质量、推力和判定 | 视对象类型 | 已确定 |

## 生命周期清理

- 镜像消失或死亡时，必须立即从所有Trigger、压力板和动态交互对象的占用列表中移除。
- 镜像销毁前必须取消事件订阅，不能留下持续激活的机关状态。
- 镜像与 Player 的交互差异必须通过一致的视觉或声音反馈向玩家说明。
- Unity 的 Physics 2D Layer Collision Matrix 必须与本表一致。
- 只有已开启的门收到控制源的关闭命令后，Player或MirrorClone占用关闭路径才会暂停物理关闭；对象离开后继续完成关闭。完全关闭的门被角色从外侧接触时保持关闭和实体阻挡，不得开门、变色或禁用Collider。门不得压死、推出或穿过角色。
- 门、压力板和移动对象的完整规则见 `docs/systems/DOOR_SYSTEM.md` 与 `docs/systems/MOVING_OBJECTS.md`。
- 地面传送带的支撑、表面速度和安全规则见`docs/systems/CONVEYOR_SYSTEM.md`。
