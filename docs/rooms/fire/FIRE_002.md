# FIRE_002：狭道替身

## 状态

- 当前状态：待试玩
- 是否允许制作灰盒：是
- 主要目标：首次教学用MirrorClone吸引水平投火者

## 已批准设计

- 玩家从`FIRE_001`进入时出生在左上角，面向右。
- 上层安全路线向右通往一个清晰可见的下落口。
- 玩家跳下后落在下层通道右侧；目标平台和出口位于左侧。
- 投火者固定在下层通道右墙内的开放射击位，向左覆盖狭窄通道。
- 下层通道只有站立净高，Player和MirrorClone可以水平移动，但不能用跳跃越过火球。
- 玩家必须在安全下落区放置地面镜；Player向左时MirrorClone向右进入攻击带并成为更近目标，以替身承受火球。
- 本房间是水平投火者的首次正式教学房；`FIRE_009`改为后续巩固房。

## 教学目标

- 让玩家理解投火者会在水平攻击带内选择更近的合格目标。
- 让玩家主动利用镜像反向移动，把MirrorClone送向投火者并替Player承伤。
- 让玩家理解MirrorClone被火球击中后只触发镜像死亡与镜子回收，Player继续存活。
- 禁止通过跳跃、随机时机或像素级距离平局绕过核心教学。

## 房间布局

- 灰盒边界：`X=-11～11`、`Y=-6～7`。
- 上层地面：从左侧入口延伸到`X=0`附近的下落口。
- 下落口：位于房间中右侧，落点中心约`X=1`，处于投火者6-unit攻击半宽之外。
- 下层狭道：地面表面与天花板之间净高约`2 units`，满足站立移动但无法正常起跳。
- 建议放置区：下落点使用无碰撞青色Decoration提示，不改变地面语义。
- 投火者：固定在右墙开放射击位，约`(8.5, -4.5)`，初始朝左。
- 左侧平台和出口：Player向左通过狭道后抵达。

```text
┌──────────────────────────────┐
│ S───────────────┐            │
│                 ▼            │
│                              │
│ E─────── 狭窄左行通道 ─M  ←H │
└──────────────────────────────┘

S 来自FIRE_001的入口   M 建议放镜区   H 水平投火者   E 出口
```

## 对象与Prefab

| 实例或对象 | 实现 | 资产路径 | 本房配置 |
|---|---|---|---|
| Enemy-H1 | `HorizontalFireballEnemy2D` Prefab | `Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab` | 固定Scene敌人；`(8.5,-4.5)`；初始朝左 |
| Exit-A | `RoomExit2D` Prefab | `Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab` | 左下出口；连接`Fire_003/DEFAULT` |
| 静态地形 | `Terrain` Tilemap | 不适用 | 上层、下落井、狭道、左侧平台和右墙 |
| 建议放置区 | `Decoration` Tilemap | 不适用 | 无碰撞青色提示，不改变表面语义 |

Enemy-H1直接固定放置在Scene中，不使用Spawner，不新增房间专用运行时脚本。

## 入口与出口

- `DEFAULT`：左上角安全入口，用于直接打开Scene。
- `FROM_FIRE_001`：与左上角构图一致，供`FIRE_001`出口显式请求。
- 两个入口的Player Collider中心均为`(-9,4.92)`；上层地面表面为`Y=4`，保留`0.02 unit`生成净空。
- `Exit-A`：位于左下平台，目标为`Fire_003/DEFAULT`。

## 镜头配置

- 镜头模式：固定单屏。
- 使用全局默认比例：是；正交尺寸`7`，16:9下Player占屏高度约`12.9%`。
- 相机可显示边界：`X=-11～11`、`Y=-6～7`。
- 构图中心：`(0, 0.5)`。
- 必须同时可见：左上入口、下落口、下层建议放置区、狭道、投火者和左侧出口。
- 没有经过批准的镜头例外。

## 视觉分层实现

Scene在房间根下使用`EnvironmentVisuals`承载纯表现内容；现有`Grid`与`Gameplay`继续共同承担第6层玩法地形和角色，不重父级、不改变玩法生命周期。

| 功能层 | Scene根 | 水平跟随倍率 | 当前内容与排序 |
|---|---|---:|---|
| 1 色彩与雾背景 | `01 Color and Fog Backdrop` | `1.00` | 暗色火山洞穴底图，Order `-100` |
| 2 极远轮廓 | `02 Extreme Far Contours` | `0.95` | 顶部岩层剪影，Order `-82～-81` |
| 3 远景环境 | `03 Far Environment` | `0.85` | 不可玩背景腔体内的岩架，Order `-56～-55` |
| 4 中景环境 | `04 Mid Environment` | `0.65` | 低对比机械柱和管道，Order `-32～-31` |
| 5 后景动态雾 | `05 Rear Dynamic Fog (Reserved)` | `0.80` | 预留空根；未引入测试粒子或材质 |
| 6 玩法层 | 现有`Grid`与`Gameplay` | 不使用视差 | Terrain、Decoration、Player、Mirror、Clone、敌人与出口保持原规则 |
| 7 前景动态雾 | `07 Front Dynamic Fog (Reserved)` | `0.35` | 预留空根；避免未经批准的前景遮挡和高亮灰烬 |
| 8 前景遮挡 | `08 Foreground Occlusion` | `0.20` | 只占左右画面外缘的框景，Order `31～32` |

- 所有视差根均显式引用本房`Main Camera`，仅跟随水平方向；固定单屏模式下当前相机位移为零，但序列化配置保持与全局分层约定一致。
- 背景底图位于`(0,0.5)`并等比缩放`1.55`，覆盖16:9、正交尺寸`7`的完整实际视野。
- 使用的正式素材来自`Assets/Art/Fire/Backgrounds/`和`Assets/Art/Generated/Fire/`；不引用`test002-1` Scene、`Test002_1_*`材质或高亮熔岩Glow图形。
- Terrain仅在Scene实例上增加暗色渲染Tint以形成可玩剪影；Tile、Collider、`SurfaceSemantic2D`、`MirrorSurface2D`和青色放镜提示均不变。
- `EnvironmentVisuals`子树只包含Transform、`ParallaxLayer2D`和SpriteRenderer，不含Collider、Trigger、Rigidbody2D、ParticleSystem或玩法脚本。

## 预期解法

1. Player从左上入口向右，观察下方狭道和右墙投火者。
2. Player从下落口进入安全落点，此时仍位于攻击带外。
3. Player在青色建议区放置地面镜，MirrorClone与Player同位置生成。
4. Player向左移动，MirrorClone反向向右并先进入投火者攻击带。
5. MirrorClone比Player更接近投火者，吸引一次向左火球并承伤消失；镜子自动回收。
6. Player继续向左抵达平台和出口。

## 失败、重置与软锁

- Player被火球或敌人本体命中：执行完整房间重置，返回左上入口。
- MirrorClone被火球命中：只清理镜像并回收镜子，不重置Player、敌人阶段或房间。
- 手动重置：Player返回左上入口，镜子回手，MirrorClone消失，投火者恢复Watching并清理在途火球。
- 狭道不允许用跳跃躲避，但不得夹住站立Player或MirrorClone。
- MirrorClone过早死亡后Player仍可退回安全下落区重新放镜，不形成软锁。

## 灰盒实现记录

- 实装Scene：`Assets/Scenes/Levels/Fire/Fire_002.unity`。
- 使用标准Tilemap分层、统一Player生成器、RoomResetSystem及共享敌人/出口Prefab。
- Scene不序列化Player，不包含房间专用玩法脚本。
- `Fire002RoomBuilder`会以空Scene重建并覆盖同路径文件，现仅作为历史灰盒bootstrap保留；正式Scene及其增量视觉层为实例配置权威，不得用该Builder覆盖同步。
- 已加入Build Settings；当前状态为待试玩。

## 验收标准

- 从`FIRE_001`进入时Player出现在左上角并面向右。
- 下落前可以观察投火者和下层路线，落点不会立即触发无预警攻击。
- Player向左、MirrorClone向右时，MirrorClone稳定成为更近目标。
- 下层通道允许稳定水平移动，但没有足够空间完成跳跃躲避。
- MirrorClone中弹后Player继续存活，镜子回手；Player中弹则完整重置。
- 出口保持可达，不产生镜像死亡软锁。
