# FIRE_003：分流熔炉

## 状态

- 当前状态：手工灰盒已同步，待运行时验证
- 主要目标：在顶部放置地面镜，让Player与MirrorClone分别进入左右路线，通过两段交叉占板依次开门，最后由Player从右下侧出口前往`FIRE_004`

## 当前Scene规则

- 本文档反映`Assets/Scenes/Levels/Fire/Fire_003.unity`当前手工灰盒，不再以`Fire003RoomBuilder`中的旧生成布局为准。
- 房间只组合镜子、镜像、普通安全地形、`Occupancy`压力板和门。
- 当前Scene没有投火者、火球锁存板、永久双压力板门控组、岩浆或周期喷发器。
- 左上侧入口仍连接`FIRE_002`；右下侧出口连接`FIRE_004`。
- 地面镜把共享水平输入分到左右路线，两名角色需要以交叉占板方式为对方打开通路。
- 所有动态对象均为通用Prefab实例，不新增房间专用运行时脚本。

## 房间布局

- 相机仍配置为横向固定、纵向跟随，正交尺寸`7`。
- 相机可显示边界仍为`X=-12～12`、`Y=-21～15`；16:9下可见宽约`24.89 units`，可同时观察房间左右两侧。
- 当前`Terrain` Tilemap的压缩边界为`X=-12～12`、`Y=-20～15`，但实际有Tile的区域只在`Y=-2～14`，共`185`格。
- 顶部主活动区位于`Y=12`附近；默认入口`EntranceFromFIRE002`位于`(-7.5, 12.92)`，面向右。
- 顶部地板在`X=-9`与`X=7`各留一个下降口；镜子提示位于`(0,12)`，左右路线提示位于`(-8,6)`与`(8,6)`。
- 第一段压力板`Plate-Cross-A`位于`(-2.63,6.15)`，控制右侧的`Door-Cross-A`，门位于`(5.46,7.03)`。
- 第二段压力板`Plate-Cross-C`位于`(-8.39,-0.73)`，控制右侧的`Door-Cross-C`，门位于`(8.54,0.27)`。
- `Exit-A to FIRE_004`位于`(11.3,0.2)`，目标Scene为`Fire_004`。
- 左右路线由外墙、中央实体隔墙、错位平台和门洞形成；底部实体地面位于`Y=-2`。

```text
┌──────────────────────────────┐
│      入场      M             │
│   ▽左口             右口▽   │
│          [A]   ┃   D-A       │
│   左侧下降路线 ┃ 右侧下降路线│
│ [C]            ┃   D-C───E▶  │
└──────────────────────────────┘
```

示意图只表达控制关系；正式网格与坐标以上述Scene数据为准。

## 对象与控制关系

| ID | 类型 | Scene配置 |
|---|---|---|
| `Plate-Cross-A` | 压力板 | `Occupancy`；控制`Door-Cross-A` |
| `Door-Cross-A` | 普通门 | 位于第一段右路，由`Plate-Cross-A`持续控制 |
| `Plate-Cross-C` | 压力板 | `Occupancy`；控制`Door-Cross-C` |
| `Door-Cross-C` | 普通门 | 位于第二段右路，由`Plate-Cross-C`持续控制 |
| `Exit-A to FIRE_004` | 房间出口 | 位于右侧，连接`Fire_004/DEFAULT` |

当前Scene共包含`2`个`PressurePlate2D`、`2`个`Door2D`和`1`个`RoomExit2D` Prefab实例。

## 预期解法

1. Player从`FIRE_002`进入顶部区域，在中央提示点附近成功放置地面镜。
2. 通过反向共享水平输入，让MirrorClone与Player分别从`X=-9`和`X=7`的下降口进入左右路线。
3. 左路角色踩住`Plate-Cross-A`，为右路角色持续打开`Door-Cross-A`。
4. 两名角色继续向下移动；左路角色踩住`Plate-Cross-C`，为右路Player持续打开`Door-Cross-C`。
5. Player穿过第二扇门，从右侧`Exit-A to FIRE_004`进入`FIRE_004`。

## 重置、失败与软锁

- Player死亡或手动重置：按通用房间重置流程清除镜子、MirrorClone和两个普通压力板的占用，并关闭对应普通门。
- MirrorClone死亡：只清除镜像并自动回收镜子；其压力板占用必须立即释放，对应门随即关闭。
- 两扇门均为持续占用控制，不锁存；占板角色离开或镜像被回收后，门恢复关闭。
- 当前Scene没有火球、投火者、喷发器或永久状态需要额外重置。
- 下降路线依赖手工地形与门位置保证角色不会被夹住；尚未通过PlayMode或人工试玩验证软锁路径。

## Scene与Prefab

- Scene：`Assets/Scenes/Levels/Fire/Fire_003.unity`
- 旧Editor构建器：`Assets/Editor/Fire003RoomBuilder.cs`；其对象数量、坐标和旧谜题组合尚未与当前手工Scene同步，不得直接重建Scene，否则会覆盖手工布局。
- 编辑用Tile Palette：`Assets/TilePalettes/Fire.prefab`
- 当前Terrain Tile：`Assets/Tiles/Graybox/Fire003Terrain.asset`
- `Fire003Terrain`使用`fire_limbo_blocky_brick_v1`进行地面砖块贴图优化，Collider与语义保持不变。
- 当前提示Tile：`Assets/Tiles/Graybox/Fire003MirrorHint.asset`
- 使用`PressurePlate2D`、`Door2D`与`RoomExit2D`通用Prefab。
- 标准Tilemap层齐全；`OneWayPlatform`、`SpecialMirrorWall`与`Hazard`层当前为空。

## 已知Scene未对齐项

- 返回入口`EntranceFromFIRE004`仍位于`(10.5,-17.08)`，但当前实际地形最低只到`Y=-2`；从`FIRE_004`返回时的出生与落脚位置尚未和手工灰盒对齐。
- 相机边界仍保留旧房间高度`Y=-21～15`，明显大于当前实际有Tile区域；纵向跟随时可能显示大面积空白。
- `Fire003RoomBuilder`仍会生成旧版的六压力板、五门、双投火者和永久门控布局，其验证数量也与当前Scene不一致。

以上项目仅记录当前静态差异，本次不反向修改Scene、Builder或通用机制。

## 验收标准

- 顶部入口、中央放镜提示、左右下降口和右侧出口关系清楚。
- Player与MirrorClone分别进入左右路线；左路持续占板时，右路对应门打开。
- `Plate-Cross-A`只控制`Door-Cross-A`，`Plate-Cross-C`只控制`Door-Cross-C`。
- Scene中不存在投火者、火球锁存、永久双板锁存、岩浆或周期喷发器。
- Player或MirrorClone离开压力板后，对应普通门恢复关闭；重置后两个压力板均无占用。
- 在正式验收前，需要先解决返回入口、相机边界和旧Builder与当前Scene不一致的问题。
