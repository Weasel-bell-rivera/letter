# FIRE_024：小温泉山洞

## 已确认视觉方向（2026-09-06）

- 用户补充纠正：温泉必须是自然剪影风格，不能像澡堂、浴缸或高起的圆形砌石池；使用岩石间低矮、不规则的浅水面和克制水光，由蒸汽传达温泉特征。

- 用户明确：山洞里有几个小温泉和蒸汽，还有岩石交织；温泉及交错岩石仅作背景景观，不影响行走。
- 本次增量视觉优化使用三个独立背景温泉、洞壁与局部薄蒸汽；无温泉Collider、Trigger、伤害、恢复或镜子规则。
- 60格纯黑道路、入口、相机、重置与无出口范围保持不变。原灰底为制作基线，原“无山体”指无室外山体，不限制本次已确认的洞穴岩壁。

## 状态与范围

- 灰盒中；用户2026-09-06明确要求再制作类似FIRE_023的FIRE_024；复用当前无山体的60格道路基础场景。
- Scene：`Assets/Scenes/Levels/Fire/Fire_024.unity`，手工Scene为权威，无房间运行脚本。
- 依据：GAME_DESIGN、MIRROR_MECHANIC、LEVEL_BUILDING、LEVEL_GEOMETRY_SYSTEM、CAMERA_SYSTEM、PLAYER_PREFAB、RESET_SYSTEM与FIRE_REGION现行规则。灰色底为本次明确批准的基础表现。
- MAP登记独立房间，未批准邻接连接；不推断FIRE_023→FIRE_024，不设置出口，不改世界进度。
- 定位：基础场景制作，无教学谜题、通关判定或失败压力。无门、压力板、敌人、Spawner、拾取物及岩浆。保留道路，背景已按本次确认改为温泉洞穴；不含室外山体。

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

- 第1层：纯灰色#808080底，使用纯白方块Sprite及现有无光照剪影材质，Tint取灰色的线性值。背景中心(0,3)，60×18 units，覆盖X=-30..30、Y=-6..12，与初始相机中心对齐；水平倍率1使其在相机行程中保持完整覆盖。
- Sorting Order=-100；ParallaxLayer2D水平倍率1、纵向不跟随，显式引用Main Camera。无Collider、Trigger、语义或其他玩法组件。
- 第1层追加宽幅柔光`01 Soft Grey Light`，中心(4,2)、60×24、factor=1、Order=-90，使用本房`Fire024SoftLight.mat`。
- 第2层`02 Distant Cave Silhouette`：中心(5,4)、60×20、factor=.95、Order=-85；第4层`04 Cave Vault`：中心(10,4)、85×20、factor=.65、Order=-40。二者复用`cave-ceiling-sidewalls.png`，保持无碰撞。
- 三个背景温泉中心分别为(-1,.9)、(9,.72)、(21,.95)，缩放分别为.28、.22、.30，水平factor=.65，Order=-37；使用`Assets/Art/Generated/Fire/Fire024HotSpring/natural-spring.png`与URP默认Sprite Unlit材质。它们是背景景观，无实体、Trigger或其他玩法组件。
- 第5层宽幅后雾：中心(5,3)、80×16、factor=.8、Order=-30。每个温泉还包含局部蒸汽子对象，世界尺寸5×5、中心高于温泉1.8、Order=-28，继承温泉视差，不重复挂Parallax。蒸汽复用`SoftDriftingFogSprite2D`，密度.5、Opacity=.65、速度(.006,-.045)、软边.35；不改变全局随机、时间或物理。
- 第7层高空前雾：中心(12,9)、90×8、factor=.35、Order=25，最低Y=5，不覆盖道路角色操作区。
- 第6层保持黑色Terrain与运行时角色；第3、8层无内容，省略。所有Parallax显式引用本房Main Camera、仅水平跟随。
- Camera灰色清屏作宽屏延伸；正交尺寸7，初始中心(0,3,-10)，仅水平跟随；显式偏移(0,0)、阻尼0.15，边界Rect(-10,-4,60,14)，Spawner和Reset均显式引用本房相机，序列化target为空。
- 16:9相机中心X范围约2.444..37.556，固定Y=3；入口按通用边界钳制。Player占屏约12.9%，不缩放角色。
- 必须可见当前Player、可站立边缘和镜子操作区域；不要求两端同时可见，不设计离屏双角色谜题。窄屏与完整行程构图待验证。

## 流程、死亡与恢复

1. 直接打开Scene，从DEFAULT生成Player，沿黑色道路移动，观察背景温泉、洞顶与局部蒸汽。
2. 通用跳跃与已解锁镜子能力可用，没有独立胜利条件或出口。
3. 手动重置及Player死亡沿用RESET_SYSTEM：回当前入口，镜子按解锁状态回手，MirrorClone清除；镜像单独死亡不重置Player。
4. 重新进入恢复静态初始场景，切场沿用通用清理规则，但本房没有切场出口。无新增存档状态。

## 制作范围与验证

- 使用FIRE_023已保存的基础Scene创建独立Scene，复用现有Tile、材质和Sprite，不改源房间或共享资源。
- FIRE_023先前提出的黑暗洞穴与微光是表现方向，源Scene尚未实现；本次先创建相同道路基础，不把灰底声称为已完成洞穴美术。
- RV-01：静态入口净空0.02，运行出生待验证；RV-03：60格连续平面静态通过；RV-07：240格Terrain及显式StaticSolid，Editor回读Composite单路径，Bounds中心(20,-2)、Extents(30,2)通过。
- RV-02、04、05、06、08：无出口、机关、门、敌人和Spawner，不适用。
- RV-09：静态净空充足，镜子重复操作及双角色运动待验证；RV-10：灰底与相机配置继承，实际构图待验证；RV-11：通用入口/重置/相机引用继承，死亡与手动重置待验证。
- RV-12：Scene独立GUID、文档/地图/索引一致，已检查序列化：除房间根名称外逐字等同源Scene，anchor唯一。不继承源房间运行通过结论。
- 未运行自动测试、完整编译或人工试玩。

- Unity 6000.5.8f1（W1@49ee0df8）已打开并保存Fire_024，当前无dirty状态。回读240格、1个背景Sprite、Spawner相机引用有效。未进入Play Mode或运行截图，运行时出生、视差、重置仍待验证。


## 2026-09-06 温泉洞穴优化验收

- 原灰底制作与旧验证记录保留为历史基线；本节与上方分层描述为当前视觉事实。未调整地图连接、世界进度、共用Prefab或Shader，也未运行Builder。
- 本轮只增加11个表现对象与4个本房材质（`Assets/Materials/Regions/Fire/Fire024*.mat`）、一个温泉RGBA Sprite；原有所有玩法、相机、Tilemap、Collider、入口、Spawner和Reset序列化块逐字不变，仅环境父Transform新增子引用。
- 新Sprite经Labnana `gemini-3-pro-image`生成，绿色色键#00FF00；后处理验证透明区、主体、四角及无色键残边均通过，生成一次，无重试。导入保留Unity生成GUID。
- RV-07、RV-12：增量静态检查通过，anchor唯一；RV-10：1280×720默认入口运行画面通过，完整横向视差行程、其他比例与镜子/镜像画面待验证。RV-01、03、09、11未受影响且本次未重验；RV-02、04、05、06、08按无出口/机关/敌人范围不适用。不代表全房验收通过。
- 静态S暂定51.5→63.5/100；固定LIMBO基准87.5，仅对入口静态表现比较；动态D=N/A，缺少等条件连续视频。温泉与人物的视角统一、洞顶造型和完整行程遮挡仍可进一步完善。
- Scene已写盘并同步Unity 6000.5.8f1，退出本轮Play Mode后Fire_024 active、isDirty=false；最新Console查询Error/Warning为0。未运行自动测试、完整编译或完整人工试玩；死亡、重置、镜子重复操作与完整横向运动仍未验证。
- 完整分项评分、回退清单、素材验证、静态对比与取证限制见`Temp/W1VisualOptimize/Fire_024/20260906/report.md`；前后图为`before.png`与`final-left.png`。临时定位相机的center/right诊断图不代表实际视差行程，不用于验收通过结论。


## 交错岩石增量（2026-09-06）

- 按用户补充增加三组交错岩柱，每组两块，分别位于温泉之间及后方：中心(3.5,4)/(5.3,3.6)、(14.5,4.4)/(17,3.5)、(28,4)/(30,3.8)；Z旋转分别-19/24、18/-25、-17/22度。
- 对象`04 Interwoven Cave Rock 1..6`复用已有forked/heavy/slender洞柱Sprite及剪影材质；Order=-39，在温泉-37和蒸汽-28之后，明度.14/.20。水平视差.65、无纵向跟随，显式引用本房相机；不新增碰撞、Trigger、玩法脚本或公共资源修改。
- RV-07、RV-12：静态通过，新增24个序列化块，原有块仅EnvironmentVisuals子引用变化，anchor唯一。温泉、蒸汽、道路、入口、相机和重置配置不变。RV-10：待验证实际岩石轮廓、三组交织关系和全行程角色背景对比；其他RV项未受影响且本轮未重验，原不适用项不变。
- 文件已保存到磁盘。Editor已由其他操作切换至Fire_027，本轮不切换、不保存该Scene，没有重新进入Play Mode或拍摄岩石增量截图；上一轮63.5静态分不自动沿用于本轮，当前新增岩石未评分。未运行自动测试、完整编译或人工试玩。
- 增量前快照`Temp/W1VisualOptimize/Fire_024/20260906/before-rocks.unity`；新增对象及参数清单`rocks-manifest.json`。回退只移除此清单的六个对象及对应父引用，不覆盖后续编辑。


## 自然剪影温泉修正（2026-09-06）

- 移除三个温泉对旧圆环浴池素材的引用，换成`natural-spring.png`：低矮不规则水面、两侧近黑天然岩石剪影、极少灰色水光，无高起池壁、圆形石环或砌石。第二处视觉中心从Y=1.35降至.72；蒸汽随父对象一起移动，其他温泉/蒸汽参数不变。
- 同次取图发现前轮旋转岩柱顶部有裁切直边：六根交错岩柱Y缩放统一为.65，Order由-39改为-41，让其顶部与底部分别延伸到洞顶后方和道路后方。温泉Order=-37、角色公共排序不变。
- 新素材使用Labnana `gemini-3-pro-image`、2K、3:2、#00FF00色键，生成一次，无重试；RGBA验证有效，透明比例.799773、不透明比例.198601、四角透明比例.987847、残留色键0。原图及后处理图在本次Temp/generated目录，已导入Single Sprite/PPU100，保留Unity生成GUID。
- RV-07/12静态检查：玩法、地形、入口、相机、碰撞、镜子和Reset块均未改变；本轮仅更改三个Sprite引用、第二处视觉位置及六根岩柱表现缩放/排序。RV-10：实际入口1280×720运行画面已检查，浅水面、蒸汽、岩石与Player可见，顶部矩形裁边已被洞顶遮住；横向完整行程、镜像和其他比例仍待验证。其他RV项未受影响未重验，不声称全房验收通过。
- 最新截图`Temp/W1VisualOptimize/Fire_024/20260906/silhouette-spring-accepted.png`。已退出Play Mode；Console Error/Warning查询0条。未运行自动测试、完整编译或人工试玩；不沿用旧温泉画面的评分声称本轮得分提升。
- 回退清单见`natural-spring-manifest.json`，此前Scene快照为`before-natural-spring.unity`；岩柱裁边修正前快照为`before-rock-crop-fix.unity`。旧素材未删除，但Fire_024不再引用。
