# 压力板剪影视觉接入

2026-08-31。范围：只替换通用压力板的表现；未修改门、房间Scene、玩法脚本或碰撞规则。

## 交付

- `Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab`
- `Assets/Art/Generated/Gameplay/PressurePlateSilhouetteSheet.png`及Unity生成的双Sprite导入配置。
- `Assets/Materials/Gameplay/PressurePlateSilhouette.mat`
- `Assets/Shaders/Gameplay/SilhouetteMatteSprite.shader`
- `docs/systems/GAMEPLAY_PREFAB_CATALOG.md`中压力板视觉来源说明。
- `unity-prefab-states.png`：Unity隔离预览渲染，从左至右为未按下、按下、永久锁存。预览手动选择Sprite/颜色，不代表真实踩踏或状态机测试。

图集为RGB，不具有真实Alpha。配套材质根据浅色底生成渲染透明度，保留轮廓及状态色；不要把该图直接交给默认Sprite材质。材质使用当前URP的`unity_SpriteColor`读取SpriteRenderer状态色，避免在Unity 6.5中丢失颜色。

## 验证与限制

- Unity实例：`W1@49ee0df8`，版本`6000.5.8f1`。
- Prefab已保存；没有通过本任务保存或重载任何房间Scene，也没有停止用户/其他操作发起的Play Mode。
- 接入时活动Scene为`Snow_004`；最终隔离渲染时活动Scene已由外部操作切换到`Fire_013`，渲染前后dirty均为false。预览没有向活动Scene添加对象。
- Collider序列化内容与原版本逐字一致：`1.25 × 0.3`、零offset、Trigger启用；Prefab GUID与默认Occupancy模式不变。
- 原压力板、门玩法脚本和门Prefab无本任务改动。重置、镜像清理和FireballLatch仍使用原有实现。
- 最终Shader检查无错误，Console按Silhouette过滤未返回Error；不代表全项目Console无错误或完整编译通过。
- 28个直接引用压力板的正式房间未发现其视觉字段覆盖；组合门控Prefab的PlateA、PlateB回读均继承新Sprite和材质。未逐房运行验证。
- RV-04：两状态共用底部锚点，隔离预览底部对齐；场景支撑关系未逐房重验。
- RV-08、RV-11：控制与生命周期逻辑未改，静态检查通过；实际踩踏、火球命中、死亡/重置和存档恢复未运行验证。
- RV-10：仅验证隔离渲染的三状态可读性；四区真实背景、特效遮挡及游戏镜头尺寸仍待场景验证。
- RV-12：基础Prefab GUID、组件引用及嵌套继承回读通过。其余房间验收项不属于本次资产替换范围，未重验。
- 未运行PlayMode自动测试、完整EditMode、全项目编译或人工试玩；没有把截图作为这些测试通过的证据。
- 工作区原有大量修改保留；Prefab目录文档中原有的其他修改也保留。

## 图片生成记录

使用内置image_gen工具，未使用CLI/API fallback。来源为本任务已展示的压力板预览，经状态一致性修正与去底请求生成；工具未实际输出Alpha，因此使用上述材质完成渲染去底。源PNG原样复制到项目，没有外部修改二进制像素。

最终图片编辑提示词：

> Precise background extraction, not a new illustration. Remove the entire white/gray checkerboard background from this sheet. Keep both existing dark pressure plates exactly unchanged and in the same positions. Output an RGBA PNG with a genuine transparent alpha channel. Every background pixel, including gaps beside the little neck beneath the left upper slab, must have alpha zero. Do NOT draw another checkerboard, white backdrop, or gray backdrop. The checkerboard in the input is unwanted baked-in pixels, not part of the objects. Do not simulate transparency with a pattern. Real alpha transparency is REQUIRED. Two separated dark objects only, antialiased transparent edges, no other changes.
