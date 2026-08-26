# SNOW_007：温暖岛

## 状态

- 当前状态：灰盒已搭建
- 是否允许制作灰盒：是
- 实现入口：`Assets/Editor/SnowRegionRoomsBuilder.cs`

## 地图登记

- 地图编号：`SNOW_007`
- 地图来源：`docs/maps/MAP.md`
- 所属区域：雪之区域
- 相邻房间与连接方向：以 `docs/maps/MAP.md` 为准

## Unity资源

- 计划Scene：`Assets/Scenes/Levels/Snow/Snow_007.unity`
- 当前状态：已创建并登记到Build Settings

## 灰盒实现记录

- 房间名称：温暖岛
- 主要目标：分段穿越冻结地面并在普通地面恢复
- 使用机制：FreezingGround、普通安全地面
- 静态结构：标准`Terrain`、`FrozenGround`及其余标准Tilemap层；未使用的层保持空层
- 动态对象：全部使用共享Prefab实例；固定Scene敌人不迁移到Spawner
- 地面视觉：本房间8格`FreezingGroundCell2D`保留共享碰撞、`FreezingGround`表面语义与重置行为，仅在Scene实例上覆盖为`Assets/Art/Snow/Tiles/snow_ice_ground_tile_64_v3.png`；普通恢复岛使用`Assets/Tiles/Snow/Snow007/Snow007Terrain.asset`，其Sprite为低色差、粗分区的`Assets/Art/Earth/Terrain/LowPolyEarthTile-v4.png`，以暖土色与冰蓝冻结地面保持明确区分。视觉替换不改变`StaticSolid`或`FreezingGround`语义
- 入口：保留唯一`DEFAULT`安全入口，并为每个已实现相邻来源配置`FROM_<来源房间ID>`入口
- 出口：严格指向`docs/maps/MAP.md`登记的相邻雪区房间，并显式请求目标房的`FROM_<本房ID>`入口
- 相机：有边界的Player跟随，正交尺寸`7`，双轴跟随，平滑时间`0.15秒`；显式显示边界为`X[-20,20]`、`Y[-7,7]`。入口构图使用实体墙边界`X[-13,13]`、`Y[-7,7]`，运行时按实际画面宽高比计算入口相机中心，使出生侧视野边缘对齐对应实体墙；16:9左侧入口的相机中心约为`X=-0.556`，更宽画面则重新计算，不写死该数值。Player到达水平中央构图线后开始水平跟随。左右外墙以外使用统一雪区纯色背景延伸，不放置玩法对象或相邻房间内容；16:9下相机中心X可在约`[-7.56,7.56]`范围内跟随，Y按边界居中
- 重置：Player死亡或手动重置恢复入口、镜子、门、压力板、敌人、冻结状态、临时胡萝卜/雪人和周期落雪初始相位
- 验证：已通过构建器静态配置校验与Unity Console检查；未运行PlayMode或人工试玩
- 运行时风险：实际跳跃容错、冰面停点、敌人通过门的时机及落雪窗口仍需后续试玩微调

## 实施限制

- 当前灰盒只组合已记录的雪区和通用系统规则，不允许在Scene中覆盖共享数值或增加房间专用运行时规则。
