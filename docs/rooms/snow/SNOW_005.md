# SNOW_005：开门放敌

## 状态

- 当前状态：灰盒已搭建
- 是否允许制作灰盒：是
- 实现入口：`Assets/Editor/SnowRegionRoomsBuilder.cs`

## 地图登记

- 地图编号：`SNOW_005`
- 地图来源：`docs/maps/MAP.md`
- 所属区域：雪之区域
- 相邻房间与连接方向：以 `docs/maps/MAP.md` 为准

## Unity资源

- 计划Scene：`Assets/Scenes/Levels/Snow/Snow_005.unity`
- 当前状态：已创建并登记到Build Settings

## 灰盒实现记录

- 房间名称：开门放敌
- 主要目标：用单压力板门改变敌人路线与冻结位置
- 使用机制：压力板、门、可冻结敌人、FrozenGround
- 静态结构：标准`Terrain`、`FrozenGround`及其余标准Tilemap层；未使用的层保持空层
- 动态对象：全部使用共享Prefab实例；固定Scene敌人不迁移到Spawner
- 入口：`Entrance-DEFAULT`，由统一`RoomPlayerSpawner2D`生成Player
- 出口：严格指向`docs/maps/MAP.md`登记的相邻雪区房间，目标入口统一为`DEFAULT`
- 相机：固定单屏，正交尺寸`7`
- 重置：Player死亡或手动重置恢复入口、镜子、门、压力板、敌人、冻结状态、临时胡萝卜/雪人和周期落雪初始相位
- 验证：已通过构建器静态配置校验与Unity Console检查；未运行PlayMode或人工试玩
- 运行时风险：实际跳跃容错、冰面停点、敌人通过门的时机及落雪窗口仍需后续试玩微调

## 实施限制

- 当前灰盒只组合已记录的雪区和通用系统规则，不允许在Scene中覆盖共享数值或增加房间专用运行时规则。
