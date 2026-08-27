# 投火兵设计

## 定位与状态

- 当前状态：核心规则、首版统一数值、通用Prefab与`EARTH_001`固定实例均已实现；专项EditMode与PlayMode测试各`3/3`通过，等待人工手感试玩。
- 投火兵是固定站在地面的远程敌人，不巡逻、不追逐、不跳跃。
- 核心解法是利用Player与MirrorClone同时存在，让距离更近的镜像取得锁定，再在预警期间把Player移出火球路线。
- 投火兵是通用敌人，不属于土区专属机制；放入`EARTH_001`不批准新的土区规则。
- 首版不包含残留火焰、弹跳、分裂、追踪、多连发、随机散布或运行时Spawner。

## 状态机

```text
Guarding
   │ 距离内且无遮挡的合法目标
   v
Windup（0.8 s，锁定世界位置）
   │ 计时结束
   v
Throw（生成一枚弧线火球）
   │
   v
Cooldown（1.8 s，不重新索敌）
   │
   └──────────────> Guarding
```

### Guarding

- 敌人停在Scene配置的守卫点，保持零线速度和零角速度。
- 每个固定物理步查询Player和MirrorClone。
- 目标必须位于以`LineOfSightOrigin`为中心的`7 units`世界距离内，并且敌人与目标Collider中心之间没有非Trigger实体Collider遮挡。
- 同时存在多个合法目标时选择直线距离最近者；距离相同则稳定选择Player。
- 探测范围只用于逻辑与编辑器选中Gizmo，不在游戏画面绘制探测圆。

### Windup

- 进入状态的同一固定物理步记录目标Collider中心的世界坐标，之后不更新锁定点。
- 敌人转向锁定点，身体变亮，手中火球由小变大，并在锁定点显示脉冲十字标记。
- 预警持续`0.8 s`；该阶段不生成伤害火球。
- 被锁定的MirrorClone死亡或被主动回收不取消攻击，投火兵仍向原锁定点投掷。
- Player和MirrorClone都可以在预警期间离开锁定点，从而无伤躲避。

### Throw与Cooldown

- Windup结束时从Prefab内显式`ThrowOrigin`生成一枚`ArcFireballProjectile2D`。
- 火球读取统一配置：参考速度`7 units/s`、锁定点上方弧高`2 units`、最大寿命`3 s`、碰撞半径`0.35 unit`。
- 火球先沿通过锁定点的确定性抛物线运动；越过锁定点后保持该抛物线的末端速度和重力继续下落，不追踪角色。
- Cooldown持续`1.8 s`，期间敌人不索敌、不提前开始下一次预警。
- Cooldown结束后回到Guarding，允许重新选择当时最近且无遮挡的目标。

## 火球碰撞与伤害

- 火球使用Kinematic `Rigidbody2D`、Trigger圆形Collider和每个固定物理步的圆形扫掠，避免高速穿过薄Collider。
- 命中Player：火球立即停用并进入统一Player死亡重置流程。
- 命中MirrorClone：火球立即销毁，只执行镜像死亡和镜子回收；Player与整个房间不重置。
- 命中Terrain、关闭的门或其他非Trigger实体Collider：火球立即销毁。
- 开门后Collider已关闭，火球可以穿过门洞；不得通过门名称判断。
- 火球忽略投掷者自身Collider和无伤害Trigger；超过最大寿命后销毁。
- 首版火球不伤害其他敌人、不破坏镜子、不触发压力板、不拾取道具，也不产生持续火焰区域。

## 敌人身体与表面资格

- 根对象使用Kinematic `Rigidbody2D`，关闭重力与旋转，不受Player、MirrorClone或其他动态物体推动。
- 可见身体、非Trigger实体Collider和独立Damage Trigger保持小怪尺度：实体`0.9 × 0.9 units`，伤害范围`1.0 × 1.0 units`。
- 身体接触Player时触发完整房间重置；接触MirrorClone时只执行镜像死亡联动。
- 身体返回非静态、不安全的`DynamicSurface`语义，不能作为镜子放置表面或安全平台。
- Damage Trigger覆盖实体轮廓，因此角色不能通过踩在敌人顶部把它当作平台。

## 重置与生命周期

- 手动重置或Player死亡重置时，投火兵回到Scene初始守卫点与初始朝向，状态恢复为`Guarding`，清空目标、锁定点、阶段计时和全部由它生成的在途火球。
- MirrorClone单独死亡、镜子主动回收或镜子被破坏时，不重置投火兵；如果已经进入Windup，攻击继续指向原锁定点。
- 场景卸载会销毁投火兵和所有火球；重新进入房间从Prefab与Scene实例初态创建，不携带攻击状态。
- 投火兵的状态、目标、阶段计时和火球都属于`RoomAttemptState`，不写入长期存档。

## 通用资源与房间配置边界

- 统一配置：`Assets/Settings/Enemies/DefaultGroundFireThrowerEnemy.asset`。
- 敌人Prefab：`Assets/Prefabs/Gameplay/Enemies/GroundFireThrowerEnemy2D.prefab`。
- 火球Prefab：`Assets/Prefabs/Gameplay/Projectiles/ArcFireballProjectile2D.prefab`。
- 房间实例只允许覆盖Transform位置和初始左右朝向；不得复制或覆盖探测、预警、弹道、冷却、伤害与重置常量。
- 固定Scene敌人直接作为连接的Prefab实例放置，不要求为统一形式迁移到Spawner。

## 统一数值

| 参数 | 首版值 |
|---|---:|
| 探测距离 | `7 units` |
| 举火预警 | `0.8 s` |
| 火球参考速度 | `7 units/s` |
| 火球弧高 | `2 units` |
| 攻击冷却 | `1.8 s` |
| 火球最大寿命 | `3 s` |
| 火球碰撞半径 | `0.35 unit` |
| 身体实体Collider | `0.9 × 0.9 units` |
| 身体Damage Trigger | `1.0 × 1.0 units` |

## 验收标准

- 距离外、被实体地形遮挡或已经进入Cooldown时不锁定角色。
- Player与MirrorClone都合法时确定性选择最近者，平局选择Player。
- Windup开始后锁定点不随目标移动或消失而改变。
- 画面中不存在探测范围圆，预警只由敌人、蓄力火球和落点标记表达。
- 火球轨迹不依赖渲染帧率、不追踪、不随机，且高速运动不会穿过角色或静态碰撞。
- 火球和身体分别正确区分Player完整重置与MirrorClone单独死亡。
- 手动重置、Player死亡和重新进入Scene都不会留下在途火球、旧锁定点或冷却计时。
- Prefab不包含正式房间编号、世界坐标或房间专用逻辑。
