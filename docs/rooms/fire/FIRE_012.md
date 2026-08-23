# FIRE_012：熔岩之上的镜像

## 状态

- 当前状态：草案
- 是否允许制作灰盒：否
- 升级条件：补齐完整房间设计并获得用户批准

## Unity资源

- Scene：`Assets/Scenes/Levels/Fire/Fire_012.unity`
- Region：`docs/regions/FIRE_REGION.md`
- Mirror rules：`docs/MIRROR_MECHANIC.md`
- Level rules：`docs/LEVEL_DESIGN.md`

## Prefab需求

- 当前草案尚未定义具体玩法对象，因此Prefab清单为待确定。
- 补齐房间设计时，必须为每个动态玩法对象记录通用Prefab、资产路径、实例ID和房间配置；所需Prefab不存在时，必须先实现并验证该通用Prefab，才能申请灰盒制作批准。
- 静态地形、固定危险区和纯装饰必须明确说明使用Tilemap，不得与Prefab职责混淆。

## 房间定位

- 区域：火之区域
- 区域序号：12
- 类型：镜像机制组合房
- 预计完成时间：3～5分钟
