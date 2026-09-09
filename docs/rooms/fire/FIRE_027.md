# FIRE_027：少量岩浆的洞穴

## 状态与范围

- 灰盒中；用户2026-09-06明确要求按编号再创建7个类似FIRE_024的房间，本房属于该批次；复用当前无山体的60格道路基础场景。
- Scene：`Assets/Scenes/Levels/Fire/Fire_027.unity`，手工Scene为权威，无房间运行脚本。
- 依据：GAME_DESIGN、MIRROR_MECHANIC、LEVEL_BUILDING、LEVEL_GEOMETRY_SYSTEM、CAMERA_SYSTEM、PLAYER_PREFAB、RESET_SYSTEM与FIRE_REGION现行规则。灰色底为本次明确批准的基础表现。
- MAP登记独立房间，未批准邻接连接；不按编号推断房间连接，不设置出口，不改世界进度。
- 定位：基础场景制作，无教学谜题、通关判定或失败压力。无门、压力板、敌人、Spawner、拾取物及可接触岩浆危险。用户2026-09-06批准优化为少量岩浆的洞穴，当前加入纯表现洞壁、岩柱、远景岩浆渗流与薄雾。

## 网格、入口与通用对象

- Grid：1×1 unit；Terrain为Cell X=-10..49、Y=-4..-1，60列×4行，共240格；顶面Y=0，形成一条60 units长连续黑色道路。
- 复用`Assets/Tiles/Fire/Fire021BlackTerrain.asset`纯黑Tile，已存在于`Assets/TilePalettes/Fire.prefab`，不重复创建或改共享Tile。
- Terrain使用Static Rigidbody2D、TilemapCollider2D与CompositeCollider2D，显式StaticSolid、安全、地面镜放置语义。
- DEFAULT入口(-8,0.92)，朝右；标准0.8×1.8 Player碰撞盒底部净空0.02，无周边阻挡。
- 通用RoomPlayerSpawner2D经PlayerPrefabRegistry生成Player；RoomResetSystem绑定入口与本房CameraFollow2D；不序列化Player、不改镜像或移动规则。
- 两端开放，可手动重置回入口；没有新增坠落危险或自动坠落重置机制。固定敌人、出生点、Spawner与其他动态Prefab需求均不适用。

```text
  P
############################################################  Y=0顶面
############################################################
############################################################
############################################################
X=-10                                                     X=49
```

## 背景、分层与相机

- 第1层：暖暗褐色底，线性Tint=(0.095,0.071,0.06,1)，原背景覆盖与倍率1保持。
- 第2层：远洞顶，中心(3,4)、44×20，倍率0.95、Order=-85。
- 第3层：两根雾化岩柱，Order=-70；两处低亮暗红背景岩浆渗流，中心(4,4)、(17,4.5)，画布1×5.5及0.65×4.5、Order=-65；均倍率0.85。岩浆不在可接触玩法层，不附带伤害或Trigger。
- 第4层：洞顶侧壁，中心(10,4)、65×20，倍率0.65、Order=-50、近黑暖色。
- 第5层：后部暖雾，中心(5,3)、80×22，倍率0.8、Order=-30；复用Fire024RearHaze材质并通过Sprite Tint降低亮度与浓度。
- 第6层：原黑色Terrain与运行时角色，不使用视差。
- 第7层：高处稀薄漂尘雾，中心(12,9)、90×12，倍率0.35、Order=25；复用Fire024HighHaze材质，Sprite Alpha=0.25。运动和遮挡待验证。
- 第8层省略。新增对象只有Transform、SpriteRenderer与ParallaxLayer2D，均显式引用本房相机，水平跟随、纵向不跟随。
- 复用Fire021Vegetation的cave-ceiling-sidewalls、cave-pillar-blurred-heavy/slender、soft-atmosphere-mask，以及Regions/Fire/Background/lava_stream。SolidSilhouette材质只取图片Alpha统一轮廓；未修改共享素材或材质。
- Camera灰色清屏作宽屏延伸；正交尺寸7，初始中心(0,3,-10)，仅水平跟随；显式偏移(0,0)、阻尼0.15，边界Rect(-10,-4,60,14)，Spawner和Reset均显式引用本房相机，序列化target为空。
- 16:9相机中心X范围约2.444..37.556，固定Y=3；入口按通用边界钳制。Player占屏约12.9%，不缩放角色。
- 必须可见当前Player、可站立边缘和镜子操作区域；不要求两端同时可见，不设计离屏双角色谜题。窄屏与完整行程构图待验证。

## 流程、死亡与恢复

1. 直接打开Scene，从DEFAULT生成Player，沿黑色道路移动，观察洞穴与少量远景熔光。
2. 通用跳跃与已解锁镜子能力可用，没有独立胜利条件或出口。
3. 手动重置及Player死亡沿用RESET_SYSTEM：回当前入口，镜子按解锁状态回手，MirrorClone清除；镜像单独死亡不重置Player。
4. 重新进入恢复静态初始场景，切场沿用通用清理规则，但本房没有切场出口。无新增存档状态。

## 制作与验证记录

- 以FIRE_024已保存的Scene作为本批基础模板，每房独立Scene GUID，复用现有Tile/材质/Sprite；不复制共享美术、不新增脚本、不改源房间。
- RV-01：静态入口净空0.02，运行待验证；RV-03：60格连续道路、上方无障碍，静态通过。
- RV-07：240格Terrain及显式StaticSolid；Composite配置与源模板一致；没有危险或装饰碰撞。静态通过。
- RV-02、04、05、06、08：无出口、固定机关、门、敌人或Spawner，不适用。
- RV-09：静态净空充足；镜子重复操作、回收和双角色运动未验证。
- RV-10：灰底倍率与相机边界引用完整，画面覆盖静态配置继承；本房初始画面与完整行程未运行验证。
- RV-11：入口、Spawner、Reset及相机引用继承，死亡和手动重置未运行验证。
- RV-12：落盘后检查独立GUID、唯一anchor、房间名、Tile数量、内部引用及与源模板的差异，结果另记。
- 没有新增房间出口或世界进度；编号顺序不代表连通。当前只有道路基础，不声称完成洞穴微光美术。
- 不继承模板的运行验收结论；未运行Play Mode、自动测试、完整编译或人工试玩。

- 本房落盘静态结果：240格、60×4道路；Scene GUID与其余新房间不同；anchor唯一且本地fileID引用闭合；除根房间名称外逐字等同FIRE_024模板。RV-12检查通过，其他运行待验证项保持未验证。

## 2026-09-06 视觉增量与验证

- Scene磁盘已写入，手工Scene为权威，未运行Builder；保留任务开始前已有的未跟踪Scene内容。
- `Temp/W1VisualOptimize/Fire_027/20260906/`保存before.unity与change-manifest.json。原有序列化块仅EnvironmentVisuals子列表及背景Tint变化，新增8个纯表现对象；原玩法、240格Terrain、碰撞/语义、入口、相机和Reset块逐字未变。anchor唯一、本地引用闭合；新增素材GUID均存在。
- RV-07：新增装饰无Collider、Trigger、危险或镜面语义，原地形保持，静态通过。RV-12：本轮序列化引用静态通过；原Light2D脚本来自URP包，不属于Assets内资源。
- RV-10：待验证。Editor被其他操作切回FIRE_024，隔离预览混入其画面且MCP断线；before.png已判定无效，不作为本房证据。完整行程、角色和镜像遮挡、窄屏构图未验。
- RV-01、03、09、11：配置未受影响，本轮未重验；出生、镜子操作、死亡及重置保留运行风险。RV-02、04、05、06、08：无对应出口或机关，不适用。
- 静态基线S=N/A，最佳S=N/A，固定LIMBO基准87.5；动态D=N/A。无有效同条件前后证据，不宣称提升或视觉验收通过，只实施一轮洞穴表现，未继续盲调。
- Editor同步、Console与运行画面待验证；交付为磁盘状态。未运行自动测试、完整编译或人工试玩。此前灰底制作与验收记录是历史状态。
