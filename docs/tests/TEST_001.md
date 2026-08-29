# TEST_001：Tilemap与MCP测试场景

## 定位

`TEST_001`是专用于Unity编辑器、Unity MCP和Tilemap结构检查的测试场景，不是正式游戏房间。

- Scene：`Assets/Scenes/Tests/Test_001.unity`
- 不登记到`docs/maps/MAP.md`、区域`ROOM_INDEX.md`或世界进度。
- 不作为任何区域玩法规则、房间连接、难度曲线或正式美术的验收证据。
- 不创建正式入口、出口、存档节点或房间进度。

## 当前覆盖

场景保留用于检查以下编辑器结构的测试对象：

- 标准`Grid`与多个Tilemap层。
- `Terrain`、`FrozenGround`、`FreezingGround`、`OneWayPlatform`、`Hazard`和装饰层。
- `MirrorSurface2D`与`SurfaceSemantic2D`等显式表面组件。
- 用于验证场景查询和可视化检查的相机与照明对象。
- `SpecialMirrorWall`测试层；该对象只用于跨系统编辑器测试，不代表雪区允许特殊墙面镜。

## 使用限制

- 测试场景可以组合不同区域或系统的对象，但不得据此修改正式区域规则。
- 正式房间不得复制本场景布局或通过对象名称推断玩法语义。
- 修改测试对象时仍须保留`.meta`、GUID和显式组件配置。
- 如果测试需要改变镜子、镜像、碰撞或表面语义的正式行为，必须先更新对应权威文档和相关测试。
- 本场景默认不加入正式Build Settings；如需临时加入，验证结束后必须移除。

## 验证边界

本场景适合验证Hierarchy查询、Tilemap层识别、组件回读、Scene View截图和MCP增量编辑。它不能证明正式房间的连接、重置、相机构图、运行时生成或谜题流程正确。
