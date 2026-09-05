# 周期障碍系统

## 状态与范围

- 当前状态：已确定。
- 本文定义通用伸缩长矛、水晶激光和翻转定时平台。
- 三者都是Prefab动态玩法对象，不使用Tile名称、Sprite名称、房间编号或区域外观推断玩法规则。
- 房间只配置位置、朝向、尺寸和周期参数，不复制核心脚本。

## 共同规则

- 周期使用固定物理步推进；相同配置从房间初始状态开始必须产生相同结果。
- 危险或失去支撑之前必须有独立、可见的警告阶段。减弱动态设置可以降低装饰动画，但不得删除警告轮廓、改变周期或危险判定。
- Player和MirrorClone具有完全相同的触发资格。命中Player执行完整房间重置；命中MirrorClone只执行镜像死亡和镜子自动回收。
- 普通周期障碍不破坏、推动或移动已经放置的镜子。
- 三者都是房间瞬时状态，不写入长期存档。手动重置、Player死亡和重新进入房间后恢复初始相位、计时、姿态与Collider状态。
- 非安全阶段不得成为镜子放置表面；危险物使用显式`Hazard`语义或`Hazard2D`，翻转平台始终返回`DynamicSurface`，因此全部拒绝镜子放置。

## 伸缩长矛

Prefab：`Assets/Prefabs/Gameplay/Hazards/RetractableSpear2D.prefab`

- 根Transform的本地`+Y`是伸出方向；房间通过旋转Prefab支持地面、墙面和顶面安装。
- 根Transform位于安装口中心；收起状态只在安装口保留可读的矛尖提示，矛身退入安装面后方。
- 固定循环为：收起等待 → 警告 → 伸出 → 保持 → 收回。
- 警告阶段不造成伤害；长矛伸出超过显式阈值后直到收回低于阈值前启用`Hazard2D`。
- 视觉长矛与伤害Collider使用同一移动部件，二者不得出现方向或长度错位。
- 长矛不提供实体支撑，不允许骨钉反弹，也不改变角色速度。

## 水晶激光

Prefab：`Assets/Prefabs/Gameplay/Hazards/CrystalLaser2D.prefab`

- 根Transform的本地`+X`是射线方向；长度、宽度与朝向由实例显式配置。
- 固定循环为：预警线 → 危险光束 → 冷却。
- 预警线显示完整射线路径但不造成伤害；只有危险光束阶段启用同尺寸`Hazard2D` Collider。
- 首版激光使用固定长度，不自动从墙体、Tile名称或目标位置推断终点，也不追踪Player或MirrorClone。
- 激光不提供实体碰撞，不被角色、镜子或普通动态物体遮挡。

## 翻转定时平台

Prefab：`Assets/Prefabs/Gameplay/Platforms/TimedFlipPlatform2D.prefab`

- 固定循环为：稳定可站立 → 警告 → 翻转失去支撑 → 恢复稳定。
- 稳定与警告阶段使用非Trigger实体Collider；翻转阶段关闭实体Collider，Player、MirrorClone和其他受重力对象按自身物理自然下落。
- 平台不直接改写乘客位置、不建立父子Transform、不施加额外速度，也不造成伤害。
- 翻转完成后视觉正反面切换，但世界位置、实体尺寸和支撑规则不变。
- 平台始终是安全、非静态`DynamicSurface`；即使停止循环或处于稳定阶段也不能放置镜子。
- 该Prefab是周期支撑障碍，不属于`MovingPlatform2D`的两端点运输平台，也不得替代移动门。

## 实例配置与验收

房间允许覆盖周期时间、长矛伸出距离、激光长度/宽度、根Transform朝向和翻转平台尺寸。修改尺寸时必须同步视觉边界与Collider。

最低验收：

- 警告、危险/失去支撑和恢复顺序可观察且周期确定。
- Player与MirrorClone结果一致，镜像死亡不会重置Player。
- 手动重置、Player死亡和重新进房恢复初始相位。
- 视觉、伤害Collider与实体Collider边界一致。
- 镜子放置查询拒绝三种Prefab，失败不改变现有镜子或MirrorClone状态。
