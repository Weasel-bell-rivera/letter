# W1 临时音效候选库

状态：仅供试听确认，尚未导入 `Assets/`，未修改任何 Prefab 或 Scene。

## 命名规则

`Prefab名__触发事件__Candidate编号.ext`

场景环境候选使用：`区域名__用途__Candidate编号.ext`。

## 来源与许可证

### 100 CC0 SFX #2

- 作者：rubberduck
- 来源：https://opengameart.org/content/100-cc0-sfx-2
- 许可证：CC0
- 原始包：`_source_packs/sfx_100_v2.zip`
- 用于本目录的大部分机械、风、石块、环境循环和机关候选。

### Platformer Sounds

- 作者：yd
- 来源：https://opengameart.org/content/platformer-sounds-terminal-interaction-door-shots-bang-and-footsteps
- 许可证：CC0
- 原始包：`_source_packs/yd-Sounds.zip`
- 用于移动平台、火球发射和爆炸候选。

### Various Sound Effects

- 作者：Julie Damsgaard / Spring Spring / Spring Enterprises
- 来源：https://opengameart.org/content/various-sound-effects-0
- 许可证：CC0
- 本轮成功下载：`powered_door.wav`、`teleport.wav`、`slip_on_ice.wav`
- 用于门、镜子生成和房间切换候选。

## Prefab 候选映射

| Prefab | 事件 | 候选文件 | 当前接线状态 | 初步判断 |
|---|---|---|---|---|
| `PlacedMirror` | 放置/镜像生成 | `PlacedMirror__PlaceOrCloneSpawn__Candidate01.wav` | 需新增 | 传送质感明显；可能偏魔法，需要试听确认 |
| `RoomExit2D` | 场景切换 | `RoomExit2D__Transition__Candidate01.wav` | 需新增 | 适合作为空间转换声，需与镜子声避免完全相同 |
| `Door2D` | 门体运动 | `Door2D__Move__Candidate01.wav` | 需新增 | 重型机械门候选；96kHz，合入前应转48kHz |
| `Door2D` | 开关门 | `Door2D__OpenClose__Candidate02.ogg` | 需新增 | 更短、更通用的门声候选 |
| `PermanentLatchDoorGroup2D` | 永久锁定 | `PermanentLatchDoorGroup2D__Latched__Candidate01.ogg` | 已有字段 | 可直接替代程序生成锁定提示音 |
| `PressurePlate2D` | 压下 | `PressurePlate2D__Pressed__Candidate01.ogg` | 需新增 | 明确机械开关反馈 |
| `PressurePlate2D` | 释放 | `PressurePlate2D__Released__Candidate01.ogg` | 需新增 | 与压下声成对 |
| `MovingPlatform2D` | 运动循环 | `MovingPlatform2D__MovingLoop__Candidate01.ogg` | 需新增 | 齿轮感较强，适合机械平台，不一定适合自然区域 |
| `GroundConveyor2D` | 运行循环 | `GroundConveyor2D__RunningLoop__Candidate01.ogg` | 需新增 | 连续机械循环候选 |
| `SinkingEarthBlock2D` | 下沉/回升 | `SinkingEarthBlock2D__Move__Candidate01.ogg` | 需新增 | 石块摩擦候选 |
| `SinkingEarthBlock2D` | 到达底部 | `SinkingEarthBlock2D__BottomImpact__Candidate01.ogg` | 需新增 | 石质撞止反馈 |
| `FreezablePatrolEnemy2D` | 完全冻结 | `FreezablePatrolEnemy2D__Freeze__Candidate01.ogg` | 已有字段 | 冰/玻璃结晶感，可直接替代程序生成音 |
| `FreezingGroundCell2D` | 滑动/冻结累积 | `FreezingGroundCell2D__SlipOrFreezeProgress__Candidate01.wav` | 需新增 | 冰面滑动候选；不建议同时承担完全冻结结算声 |
| `TemporaryCarrotPickup2D` | 拾取 | `TemporaryCarrotPickup2D__Pickup__Candidate01.ogg` | 需新增 | 轻量拾取反馈 |
| `SnowmanGate2D` | 满足/让路 | `SnowmanGate2D__Satisfied__Candidate01.ogg` | 需新增 | 锁扣感明确，但可能过于机械 |
| `Checkpoint2D` | 激活 | `Checkpoint2D__Activate__Candidate01.ogg` | 需新增 | 短促正向反馈 |
| `WindRayEnemy2D` | 预警 | `WindRayEnemy2D__Windup__Candidate01.ogg` | 已有逻辑、需暴露正式Clip | 空气聚集感 |
| `WindRayEnemy2D` | 冲刺 | `WindRayEnemy2D__Dash__Candidate01.ogg` | 已有逻辑、需暴露正式Clip | 快速气流感 |
| `WindRayEnemy2D` | 恢复 | `WindRayEnemy2D__Recovery__Candidate01.ogg` | 已有逻辑、需暴露正式Clip | 气流泄散感 |
| `SacrificialWindRayEnemy2D` | 冲刺 | `SacrificialWindRayEnemy2D__Dash__Candidate01.ogg` | 继承同类逻辑 | 暂与普通风鳐共用候选 |
| `HorizontalFireballEnemy2D` | 发射 | `HorizontalFireballEnemy2D__Launch__Candidate01.ogg` | 已有逻辑、需暴露正式Clip | 偏科幻，可能只适合作为临时候选 |
| `HorizontalFireballProjectile2D` | 撞击 | `HorizontalFireballProjectile2D__Impact__Candidate01.ogg` | 需新增 | 爆裂反馈，需控制响度 |
| `ArcFireballProjectile2D` | 撞击 | `ArcFireballProjectile2D__Impact__Candidate01.ogg` | 需新增 | 暂与水平火球共用候选 |
| `EruptionHazard` | 喷发 | `EruptionHazard__Erupt__Candidate01.ogg` | 需新增 | 雷鸣式低频爆发，仅作近似候选 |
| `RisingLava2D` | 移动循环 | `RisingLava2D__MovingLoop__Candidate01.ogg` | 需新增 | 液体循环近似，尚缺灼热与黏稠感 |
| `MovingTornado2D` | 移动循环 | `MovingTornado2D__MovingLoop__Candidate01.ogg` | 需新增 | 风声循环候选 |
| `WindTurbineSwitch2D` | 运行循环 | `WindTurbineSwitch2D__RunningLoop__Candidate01.ogg` | 需新增 | 机械循环候选 |

## 场景环境候选

| 区域 | 候选文件 | 初步判断 |
|---|---|---|
| Center | `Center__AmbienceLoop__Candidate01.ogg` | 中性环境底噪候选 |
| Wind | `Wind__HighAltitudeAmbienceLoop__Candidate01.ogg` | 空旷环境候选；仍需叠加明确风层 |
| Fire | `Fire__CavernAmbienceLoop__Candidate01.ogg` | 洞穴底噪候选；仍需叠加岩浆与火焰 |
| Snow | `Snow__OpenAmbienceLoop__Candidate01.ogg` | 安静开放环境候选；仍需低密度寒风 |
| Earth | `Earth__MineAmbienceLoop__Candidate01.ogg` | 工业/矿井候选；需确认是否过于现代施工现场 |

## 暂未找到足够匹配的 Prefab

以下对象不应为了“有声音”强行套用不匹配素材，本轮暂不提供候选：

- `Player`：需要成套且材质一致的脚步、跳跃、落地和死亡变体。
- `Spring2D`：需要干净的压缩/释放成对音效。
- `PeriodicSnowfall2D`：需要连续、克制且不会误导危险阶段的冰雪素材。
- `WindColumn2D`、`TornadoGenerator2D`、`WindDeflector2D`：需要彼此可区分的持续风、生成和转向反馈。
- `GroundFireThrowerEnemy2D`、`PatrollingHorizontalFireballEnemy2D`、`VerticalWallPatrolEnemy2D`：需要先确定与其基础攻击/移动声的复用边界。

## 确认后再做

确认候选后才执行：

1. 将选中素材复制到正式 `Assets/Audio/` 目录。
2. 统一采样率、声道、响度、头尾静音和循环点。
3. 创建Unity `.meta` 并配置导入压缩策略。
4. 为缺少音频字段的通用组件增加可复用音频接线。
5. 增量写入对应 Prefab；不在房间 Scene 中复制通用声音规则。
