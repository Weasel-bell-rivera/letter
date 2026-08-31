# SNOW_005：开门放敌

## 状态

- 当前状态：已接入雪区分层环境；保留既有灰盒玩法布局
- 是否允许制作灰盒：是
- 灰盒历史入口：`Assets/Editor/SnowRegionRoomsBuilder.cs`。本房环境以当前 Scene 为准；该 Builder 会重建房间，不能用于保留手工环境的日常更新。

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
- 入口：保留唯一`DEFAULT`安全入口，并为每个已实现相邻来源配置`FROM_<来源房间ID>`入口
- 出口：严格指向`docs/maps/MAP.md`登记的相邻雪区房间，并显式请求目标房的`FROM_<本房ID>`入口
- 相机：有边界的Player跟随，正交尺寸`7`，双轴跟随，平滑时间`0.15秒`；显式显示边界为`X[-13,13]`、`Y[-7,7]`，对应方向不大于完整视野时按边界居中
- 重置：Player死亡或手动重置恢复入口、镜子、门、压力板、敌人、冻结状态、临时胡萝卜/雪人和周期落雪初始相位
- 验证：已通过构建器静态配置校验与Unity Console检查；未运行PlayMode或人工试玩
- 运行时风险：实际跳跃容错、冰面停点、敌人通过门的时机及落雪窗口仍需后续试玩微调

## 实施限制

- 当前灰盒只组合已记录的雪区和通用系统规则，不允许在Scene中覆盖共享数值或增加房间专用运行时规则。

## 雪区环境接入（2026-08-31）

- 使用用户选定的八张独立图片，资源目录为 `Assets/Art/Snow/Environment/Silhouettes20260831/`。原始 PNG 未修改；雾光底为不透明 RGB，其余七张保留真实 Alpha。
- 环境根节点为 `Snow005_Environment`，与原有 Gameplay、Grid 和 RoomSystems 分离。环境只包含 Transform、SpriteRenderer 和通用 `ParallaxLayer2D`，没有 Collider、Rigidbody、表面语义、危险或重置逻辑。
- 所有图片保持原始比例，采用 100 PPU、Bilinear、Clamp、无 Mipmap、无压缩、Full Rect，不生成物理形状。复用项目现有 Sprite 材质，不修改共享渲染配置。
- 采用左上柔光、雾白和低饱和蓝灰。远山降低对比；疏林根部移出视野并降低透明度；中景岩与冰壁只在下方边缘提供体积；深海军蓝前景只占底角，不构成两侧外墙或顶墙。
- `Terrain` 的 24 个既有格子换用 `Assets/Tiles/Snow/SnowSolidTerrain.asset`，复用纯白 Sprite，以均匀深蓝灰着色。Tile 的 Grid Collider、Transform、Flags 与旧 Tile 一致；没有增删或移动地形格。
- 制作 Palette 为 `Assets/TilePalettes/Snow.prefab`，已包含该纯色 Tile。共享旧 Tile 未修改，其他房间不因此换色。

| 景深职责 | 图片 | Default 排序 | 相机跟随系数 |
|---|---|---:|---:|
| 雾光底 | `01-fog-light-base.png` | -120 | 1.00 |
| 极远雪山 | `02-extreme-distance-mountains.png` | -110 | 0.95 |
| 远景山脊 | `03-distant-ridge.png` | -100 | 0.85 |
| 中景岩、冰壁、疏林 | `04`、`05`、`06` 对应图片 | -76 至 -74 | 0.65 |
| 后雾 | 空层，现有图片的雾化已足够，不叠加粒子 | — | 0.80 |
| 玩法 | 原有 Grid 和 Gameplay，不移入环境根节点 | 保留原值 | 无视差 |
| 前雾 | 空层，避免遮挡角色、门和缺口 | — | 0.35 |
| 底角前景 | `07-left-foreground-rock-snow.png`、`08-right-foreground-rock-snow.png` | 30、31 | 0.20 |

所有视差组件显式引用本房 Main Camera，仅跟随 X，不跟随 Y。原有正交尺寸、相机边界和跟随参数保持不变。16:9 视野为约 `24.89 × 14`，水平相机总行程约 `1.11`；30 units 宽的不透明底图覆盖全行程并保留余量。

### 验证与已知限制

- 对照改动前 Scene，原有对象 ID、玩法 Transform、Collider、Rigidbody、Prefab 覆盖、入口出口、镜子与重置组件保持不变。除地形视觉缓存、环境根列表及 Unity 自动补写的零值字段外，原有序列化块逐块一致；Decoration Renderer 继续禁用。
- 已进行运行画面观察和 1920×1080 取图。基线取了左、中、右机位；最终画面在现有试玩中取中心机位，不暂停或移动用户的相机，因此并非严格相同的敌人巡逻时刻。没有运行 PlayMode 自动测试、完整 EditMode 测试、批量编译或完整人工试玩。
- 本轮 Console 末次读取未发现 Error/Warning；这不代表全房玩法验证通过。角色、门、压力板、敌人及出口标签仍使用既有彩色表现，和雪区环境尚未完全统一。
- **既有玩法待核查项：**文档和历史 Builder 预期存在 FrozenGround，但本次开始时 Scene 中该 Tilemap 实际为空，`X[3,8)`、`Y[-3,-2)` 是没有地形的缺口。本次保留该事实，不用冰壁背景伪装可行走冰面，不擅自补地形或改变冻结机制。需另行检查关卡可解性。
- 本轮证据及评分位于 `Temp/W1VisualOptimize/Snow_005/20260831/`，不作为正式美术导入 Assets；评分只针对当前房间静态画面，动态评分为 N/A。
