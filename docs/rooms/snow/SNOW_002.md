# SNOW_002：待命名

## 状态

- 当前状态：灰盒中（仅横向`FrozenGround`冰面原型）
- 是否允许制作灰盒：是，但仅限本文已确认并已实现的横向冰面原型
- 升级条件：补齐入口、出口、房间目标和完整布局并获得用户批准

## 地图登记

- 地图编号：`SNOW_002`
- 地图来源：`docs/maps/MAP.md`
- 所属区域：雪之区域
- 相邻房间与连接方向：以 `docs/maps/MAP.md` 为准

## Unity资源

- 计划Scene：`Assets/Scenes/Levels/Snow/Snow_002.unity`
- 当前状态：已创建最小可运行冰面原型
- 编辑器构建器：`Assets/Editor/Snow002RoomBuilder.cs`
- Scene已加入`ProjectSettings/EditorBuildSettings.asset`

## 已确认的静态地形要求

- 在房间内实现一条从左侧连续延伸到右侧的水平冰面。
- 冰面属于静态、安全、不可破坏的寒冰地面，使用标准`FrozenGround` Tilemap制作，不得放入普通`Terrain`或无碰撞装饰层。
- 冰面的可见边界、实体碰撞边界和`FrozenGround`表面语义边界必须一致。
- 相邻Tile使用`TilemapCollider2D`、`CompositeCollider2D`和Static `Rigidbody2D`合并为连续碰撞，不能让Player或MirrorClone在接缝处卡顿、弹起或意外离地。
- 该冰面沿用雪区统一规则；不得在本房单独覆盖寒冰加速度、减速度、镜子放置、敌人冻结或重置行为。
- 当前原型使用`24`个连续的`1×1` Tile，覆盖`x=-12～11`、`y=-3`，厚度为一格；这些数值只用于最小可运行验证，不批准为最终房间尺寸。
- 冰面与正式入口、出口的距离仍须在完整房间布局获批时确定。

## 寒冰Tile图片

用户指定的图片路径按原文记录为：

```text
C:\Users\Xingliang\Downloads\kenney\_new-platformer-pack-1.1\Sprites\Tiles\Default\terrain\_snow\_block.png
```

当前机器上该精确路径不存在；已确认的同一图片实际来源为：

```text
C:\Users\Xingliang\Downloads\kenney_new-platformer-pack-1.1\Sprites\Tiles\Default\terrain_snow_block.png
```

Unity项目内使用的图片资源路径为：

```text
Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_snow_block.png
```

- Scene和Tile资产不得运行时依赖`Downloads`目录。
- 使用上述项目内Sprite创建或复用标准`FrozenGround` Tile资产；冻结与低摩擦语义来自`FrozenGround` Tilemap及统一表面语义组件，不得根据图片名或路径推断。
- 该图片用于本房横向冰面的`FrozenGround`玩法层；普通地面与纯装饰冰雪应使用可明确区分的表现。

## 当前原型实现

- 标准层级为`Room → Grid → FrozenGround`、`Gameplay → Entrances`和`RoomSystems`。
- `FrozenGround`配置`Tilemap`、`TilemapRenderer`、`TilemapCollider2D`、`CompositeCollider2D`、Static `Rigidbody2D`、`SurfaceSemantic2D`和`MirrorSurface2D`。
- 24个Tile通过一次`SetTilesBlock`写入，并将碰撞烘焙为有效Composite几何。
- 原型入口`PrototypeEntrance`位于`(-9.5, -1.08)`，用于验证Player能够稳定落在冰面上。
- 原型包含Player、镜子系统、跟随相机和`RoomResetSystem`，用于验证放置、回收和手动重置；这些对象没有引入房间专用运行时脚本。
- 房间中央包含一个`MovingPlatform2D.prefab`实例，锚点为`(0, 0)`，沿本地`x=-2～2`水平往返，速度`2`，端点等待`0.35秒`，初始相位`0.5`。
- 当前原型没有正式房间出口、敌人、门、压力板、危险物或其他谜题对象。
- 当前没有实现寒冰低摩擦参数覆盖；在雪区加速度与减速度确认前，Player和MirrorClone继续使用现有默认移动参数。

## Prefab需求

| 实例 | 通用Prefab | 资产路径 | 本房配置 |
|---|---|---|---|
| `MovingPlatform-Center` | `MovingPlatform2D` | `Assets/Prefabs/Gameplay/Platforms/MovingPlatform2D.prefab` | 锚点`(0, 0)`；水平端点`(-2, 0)`与`(2, 0)`；速度`2`；等待`0.35秒`；初始相位`0.5` |

## 自动验证

- `Assets/Tests/EditMode/Snow002AssetTests.cs`检查24个Tile无空缺、Sprite引用、静态Composite碰撞、`FrozenGround`语义、镜子放置表面、Player、重置系统和Build Settings登记。
- `Assets/Tests/PlayMode/Snow002PlayModeTests.cs`检查Player稳定落地、冰面放置镜子、回收镜子、手动重置到入口以及速度清零。
- Unity `6000.5.7f1`隔离工程验证结果：EditMode `3/3`通过；PlayMode `1/1`通过。

## 待设计

- 房间名称：待确定
- 房间作用：待确定
- 入口位置：待确定
- 出口位置：待确定
- 教学或解谜目标：待确定
- 使用机制：已确定使用一条从左到右连续铺设的静态`FrozenGround`冰面；其他机制待确定，不得引入未批准机制
- 初始状态：待确定
- 预期洞察与解法：待确定
- 重置方式：待确定
- 软锁与逃课检查：待确定
- 预计完成时间：待确定
- 验收标准：待确定

## 实施限制

- 当前实现授权仅覆盖上述横向冰面最小原型，不代表完整房间布局或玩法设计已经获批。
- 正式设计前必须阅读 `docs/LEVEL_DESIGN.md`、对应区域文档和相关系统文档。
- 在剩余房间设计获得确认前，不得增加正式入口、出口、谜题机关、敌人、危险物或其他未批准内容。

