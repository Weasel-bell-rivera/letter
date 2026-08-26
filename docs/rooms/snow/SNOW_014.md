# SNOW_014：冻结阶梯

## 状态

- 当前状态：灰盒已搭建
- 是否允许制作灰盒：是
- 实现入口：`Assets/Editor/SnowRegionRoomsBuilder.cs`

## 地图登记

- 地图编号：`SNOW_014`
- 地图来源：`docs/maps/MAP.md`
- 所属区域：雪之区域
- 相邻房间与连接方向：以 `docs/maps/MAP.md` 为准

## Unity资源

- 计划Scene：`Assets/Scenes/Levels/Snow/Snow_014.unity`
- 当前状态：已创建并登记到Build Settings

## 灰盒实现记录

- 房间名称：冻结阶梯
- 主要目标：在落雪周期内安排两个敌人的冻结位置
- 多阶段内容：
   - 阶段1：通过左侧FreezingGround到达第一控制位。
   - 阶段2：依次放行两名敌人，使其冻结在两个分离位置。
   - 阶段3：在落雪间歇连续使用两个冻结踏板抵达上层出口。
- 使用机制：PeriodicSnowfall2D、双敌人、双门、FrozenGround
- 静态结构：标准`Terrain`、`FrozenGround`及其余标准Tilemap层；未使用的层保持空层
- 动态对象：全部使用共享Prefab实例；固定Scene敌人不迁移到Spawner
- 入口：保留唯一`DEFAULT`安全入口，并为每个已实现相邻来源配置`FROM_<来源房间ID>`入口
- 出口：严格指向`docs/maps/MAP.md`登记的相邻雪区房间，并显式请求目标房的`FROM_<本房ID>`入口
- 相机：有边界的Player跟随，正交尺寸`7`，双轴跟随，平滑时间`0.15秒`；显式显示边界为`X[-13,13]`、`Y[-7,7]`，对应方向不大于完整视野时按边界居中
- 重置：Player死亡或手动重置恢复入口、镜子、门、压力板、敌人、冻结状态、临时胡萝卜/雪人和周期落雪初始相位
- 验证：已通过构建器静态配置校验与Unity Console检查；未运行PlayMode或人工试玩
- 运行时风险：实际跳跃容错、冰面停点、敌人通过门的时机及落雪窗口仍需后续试玩微调

## 实施限制

- 当前灰盒只组合已记录的雪区和通用系统规则，不允许在Scene中覆盖共享数值或增加房间专用运行时规则。
