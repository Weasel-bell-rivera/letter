# FIRE_022：夜间山路与洞口

> 证据归档说明（2026-09-15）：本文 `Captures/` 历史文件已移出 Git；归档位置及历史提交提取方法见 [Git 清理记录](../../maintenance/GIT_CLEANUP_2026-09-15.md#历史证据查找)。

## 状态与范围

- 灰盒中；用户2026-09-06明确要求参照FIRE_021房间大小创建FIRE_022，一条道路横穿场景，背景先设灰色。
- Scene：`Assets/Scenes/Levels/Fire/Fire_022.unity`，手工Scene为权威，无房间运行脚本。
- 依据：GAME_DESIGN、MIRROR_MECHANIC、LEVEL_BUILDING、LEVEL_GEOMETRY_SYSTEM、CAMERA_SYSTEM、PLAYER_PREFAB、RESET_SYSTEM与FIRE_REGION现行规则。灰色底为本次明确批准的基础表现。
- MAP登记独立房间，未批准邻接连接；不推断FIRE_021→FIRE_022，不设置出口，不改世界进度。
- 定位：基础场景制作，无教学谜题、通关判定或失败压力。无门、压力板、敌人、Spawner、拾取物及岩浆。仅复制尺寸和道路结构，已增量加入夜空、独立山体、路边装饰与右侧洞口视觉；洞口没有通行触发器。

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

- 第1层：Labnana蓝灰夜空图，使用无光照Sprite材质，白色Tint。背景中心(0,3)，60×18 units，覆盖X=-30..30、Y=-6..12，与初始相机中心对齐；水平倍率1使其在相机行程中保持完整覆盖。
- Sorting Order=-100；ParallaxLayer2D水平倍率1、纵向不跟随，显式引用Main Camera。无Collider、Trigger、语义或其他玩法组件。
- 第3层：Labnana绘制的灰色远山剪影，水平视差0.85。第6层：黑色Terrain和运行时角色。第4层：3座独立深灰中景山，水平视差0.65。第2层为稀疏星点和薄云，第5层为山间雾；贴地枯树、草石及洞口采用世界固定位置，各自可调。
- Camera灰色清屏作宽屏延伸；正交尺寸7，初始中心(0,3,-10)，仅水平跟随；显式偏移(0,0)、阻尼0.15，边界Rect(-10,-4,60,14)，Spawner和Reset均显式引用本房相机，序列化target为空。
- 16:9相机中心X范围约2.444..37.556，固定Y=3；入口按通用边界钳制。Player占屏约12.9%，不缩放角色。
- 必须可见当前Player、可站立边缘和镜子操作区域；不要求两端同时可见，不设计离屏双角色谜题。窄屏与完整行程构图待验证。

## 流程、死亡与恢复

1. 直接打开Scene，从DEFAULT生成Player，沿黑色道路移动，观察夜间山路与右侧洞口。
2. 通用跳跃与已解锁镜子能力可用，没有独立胜利条件或出口。
3. 手动重置及Player死亡沿用RESET_SYSTEM：回当前入口，镜子按解锁状态回手，MirrorClone清除；镜像单独死亡不重置Player。
4. 重新进入恢复静态初始场景，切场沿用通用清理规则，但本房没有切场出口。无新增存档状态。

## RV检查与未验证风险

| RV | 结果 | 证据或待验证范围 |
|---|---|---|
| RV-01 | 通过 | DEFAULT净空0.02；短时运行Player中心(-8,0.91)并正常支撑 |
| RV-02 | 不适用 | 无出口 |
| RV-03 | 通过（静态） | 60格平坦通道，上方无障碍；无跳跃关卡 |
| RV-04、05 | 不适用 | 无固定机关、敌人或门 |
| RV-06 | 不适用 | 无房间连接，本次仅独立登记 |
| RV-07 | 通过 | 240格；Composite单一路径，Bounds中心(20,-2)、Extents(30,2)，顶面Y=0；语义显式 |
| RV-08 | 不适用 | 无机关或Spawner |
| RV-09 | 静态空间通过，运行待验证 | 平台上方净空充足，无镜子规则覆盖；未操作镜子 |
| RV-10 | 初始画面通过，完整行程待验证 | 16:9入口相机(2.444,3)，背景完整覆盖视野；灰色底与黑色道路可辨；右端跟随及多宽高比未运行验证 |
| RV-11 | 引用检查通过，运行待验证 | Spawner、Reset、入口、CameraFollow显式引用；未触发死亡或手动重置 |
| RV-12 | 通过 | Scene、地图、索引、本文一致；复用现有Tile/Palette，无房间脚本及序列化Player |

- Unity 6000.5.8f1，W1@49ee0df8；Scene已保存并打开，短时Play Mode观察后已退出。Console Error查询为0。
- 运行截图：`Captures/Fire022/fire022-initial.png`；直接相机捕获不含HUD，颜色转换可能与Game View不同。
- 未运行PlayMode自动测试、完整EditMode、完整编译或人工试玩。镜子重复操作、手动/死亡重置、完整道路行走与多比例构图待验证。
- FIRE_021及既有工作区内容保留，本次新建FIRE_022与对应文档，仅追加地图和区域索引登记。


## 2026-09-06 Labnana远山剪影增量

- 用户要求远景增加山体剪影、使用Labnana绘制并参考FIRE_021设置；本房沿用已批准灰底，以火山山脊为独立装饰，不改变区域玩法。
- 现有FIRE_021素材主要为洞壁和垂直石柱，不能直接满足横向山脊轮廓，因此仅生成1张所需远景素材；首次返回假透明棋盘底，进行1次针对性修正，使用纯白技术底绘制轮廓后提取Alpha。
- 资源：`Assets/Art/Generated/Environment/Fire/OnDemand/fire022-distant-mountains/fire-far-volcanic-mountains.png`，Labnana `gemini-3-pro-image`，21:9、2K请求，实际3168×1344。白底反向亮度转为Alpha、清除浅色残余并柔化1.2px；RGB统一白色供材质Tint使用。真实Alpha为0..255，四周透明，完整山脊轮廓保留；无烘焙雾、前景或玩法布局。
- 导入：Single Sprite、PPU=100、中心Pivot、Bilinear、Clamp、无MipMap、无压缩、最大4096；由Unity生成新meta，未改旧资源GUID。
- 对象：`FIRE_022 Grey Road/EnvironmentVisuals/03 Far Mountain Silhouette`。中心(4,0)，等比缩放1.5151515，画布48×20.364 units；Order=-60、Default Sorting Layer，线性Tint(0.12743768,0.12743768,0.12743768,1)，复用`Fire018EnvironmentSilhouette.mat`。只有Transform、SpriteRenderer、ParallaxLayer2D，显式引用本房Main Camera，水平倍率0.85、纵向不跟随。
- 外轮廓为不等高宽山峰，内部统一平色，柔边；山脚与图像底边藏在黑色道路后，不形成可站立平台、镜面或危险反馈。排序在Terrain、MirrorClone(-10)、Player和镜子(20)后方。
- 检查发现原灰底中心X=20配合倍率1后，在16:9画面左边露出约2.444 units的清屏色带；本次仅将灰底中心X改为0，尺寸、Tint、相机参数不变。相机参考坐标为(0,3)，灰底始终相对视野覆盖±30 units。
- 16:9视野宽24.889、相机中心X=2.444..37.556；远景最低水平覆盖24.889+35.111×0.15=30.156 units，小于画布48。山脊有效Alpha宽约45.227 units，左中右构图均没有图像底边、侧边或透明接缝。固定Y视野为-4..10，山峰最高约6.9，其上由灰底自然留白。

### 本轮验证

| RV | 结果 | 证据与范围 |
|---|---|---|
| RV-07 | 通过（静态） | 新远山仅3个表现组件；Terrain、Collider与表面语义序列化块逐字未变 |
| RV-10 | 静态左中右构图通过；运行待验证 | 相机X=2.444、20、37.556，以脚本倍率计算各层位置后直接相机取图；山体柔边、道路分离、灰底接缝修正，无角色运行证据 |
| RV-12 | 通过（静态与资源回读） | 新增4个Scene序列化块、一个父子引用，只另改灰底Transform；所有原玩法、相机与Prefab块逐字未变，anchor唯一；Sprite与0.85倍率在Editor回读有效，本文同步 |
| RV-01～06、08、09、11 | 未受影响，本轮未重验 | 入口、道路、镜子、重置和连接配置未修改；不声称全房或解法通过 |

- Unity 6000.5.8f1，实例W1@49ee0df8。磁盘增量已落盘，临时Additive加载成功后完成回读/截图并卸载，保留用户原Fire021场景，没有保存其他Scene。Console Error查询0条；无运行时代码改动。
- 证据：`Captures/Fire022/mountains-left.png`、`mountains-middle.png`、`mountains-right.png`。直接相机捕获不含角色/HUD，颜色输出可能与Game View不同，只用于本轮轮廓和覆盖检查。
- 未进入Play Mode、未运行PlayMode自动测试、完整EditMode、完整编译或人工试玩；连续运行视差、角色/镜像实际对比、重置和多宽高比仍待验证。
- 原工作区已有Fire001～020、Fire021、Fire022、共享Palette及文档等修改；过程中出现其他任务的Player相关修改，均未触碰。本任务只写本房Scene/文档、新山脊图片与导入meta、三张截图。未运行任何Builder。

### 最终生成提示词

```text
Correction: prior output baked a checkerboard instead of transparency. For reliable alpha extraction produce SOLID PURE WHITE background RGB 255 255 255, absolutely NO checkerboard, grid, shadow or background pattern. Draw one completely flat BLACK silhouette of a wide volcanic mountain massif. Original art for a 2D side-scrolling puzzle platformer, LIMBO-inspired cinematic layered silhouettes without copying assets characters or layouts. One single connected distant mountain ridge, three unequal broad asymmetric summits with irregular organic shoulders and deep sweeping valleys, avoid regular triangles or flat ledges. Complete uncropped contour on all sides with white padding, mountain spans 92 percent width and 65 percent height. Interior 100 percent pure flat BLACK without internal shading, at least 95 percent one continuous value. Clean mildly soft unsharpened edges. Fire region volcanic massive quiet oppressive silhouette, no snow trees buildings glowing cracks or lava. Final integration is low contrast charcoal grey on a grey field; white background is a technical matte to be removed, not scene content. Outer-contour-led, near-flat silhouette cutout; not a grayscale rock rendering, material study, photo, digital sculpture, or full environment concept painting. No small cracks pores gravel brush noise granular shading PBR relief normal map appearance repeated striations edge chatter ambient occlusion bevel shading rim light backlit halo glossy highlight local volume modelling. No fog particles foreground gameplay terrain routes characters mirrors enemies doors plates hazards UI or feedback. No internal divisions. One subject one far depth role, not a full room image.
```


## 2026-09-06 三座独立近山

- 用户要求增加颜色更深、距离更近且可逐座调整的山。本轮Labnana Nano Banana Pro分别生成3张独立单山图片，未拼成山脉大图；原远山与灰底保留。
- 资源目录：`Assets/Art/Generated/Environment/Fire/OnDemand/fire022-near-mountains/`；文件为`fire-mid-mountain-broad.png`、`fire-mid-mountain-steep.png`、`fire-mid-mountain-shoulder.png`。每图实际2528×1696，独立完整山体、真实Alpha和透明留边；纯白技术底提取Alpha后柔化0.7px，RGB白色供Tint使用。单Sprite、PPU100、中心Pivot、Bilinear、Clamp、无MipMap、无压缩、最大4096，Unity生成meta。
- 在`EnvironmentVisuals`直接选择以下任意对象，即可独立调整Transform位置/尺寸和SpriteRenderer颜色；各对象独立配置ParallaxLayer2D，不受另一个山对象控制。

| 对象 | 中心XY | 画布宽 | 等比缩放 | Order |
|---|---|---:|---:|---:|
| 04 Near Mountain Broad | (-8,1.9691456) | 18 | 0.71202532 | -40 |
| 04 Near Mountain Steep | (6,1.6938291) | 15 | 0.59335443 | -39 |
| 04 Near Mountain Shoulder | (21,2.8662975) | 18 | 0.71202532 | -38 |

- 三山均为第4层中景、水平倍率0.65、显式Main Camera引用、无纵向跟随。线性Tint均为(0.06301002,0.06301002,0.06301002,1)，相当于约#474747，深于原远山约#646464；共享现有剪影材质。有效Alpha底部Y=-1，藏于道路后；山顶约4.95、4.38、6.73。仅Transform、SpriteRenderer、ParallaxLayer2D，不含碰撞或玩法组件。
- 美术检查：三张均为独立完整宽重山体，近乎纯色内部，软边无烘焙雾、纹理、辉光或棋盘底；形态分别为宽缓、陡侧、长坡。图间空隙自然露出原远山，不需要拼接贴图；所有排序在角色/镜子之后。
- RV-07、12通过静态差异和Unity组件回读：新增12个序列化块，仅修改已有EnvironmentVisuals子引用；全部原Terrain、碰撞、入口、远山、灰底、相机与重置块逐字不变，Scene可加载，Console Error=0，房间文档同步。
- RV-10：16:9相机X=2.444、20、37.556，按实际倍率做编辑态静态摆位取图；三座近山可区分于远山，道路边缘可见，无矩形底边或假平台。证据`Captures/Fire022/near-mountains-{left,middle,right}.png`，直接相机捕获颜色可能与Game View不同，不含运行时角色。
- RV-01～06、08、09、11未受影响且未重验。未进入Play Mode、未运行自动测试、完整编译或人工试玩；连续视差、角色/镜像对比、多比例与重置仍待运行验证。
- 本轮落盘后在确认非Play Mode、无dirty与Prefab Stage的情况下Reload；Fire022已在Unity打开，保留原工作区修改，未保存其他Scene或改公共代码。

### 单山生成提示词记录

**broad**

```text
Original decorative art for a 2D side scrolling puzzle platformer, LIMBO inspired cinematic layered silhouette without copying LIMBO assets characters or layouts. EXACTLY ONE isolated volcanic mountain, one single summit, NOT a mountain range, no second mountain, no connected peaks. Full uncropped silhouette with generous padding on all four sides. Solid PURE WHITE technical matte background for later alpha extraction, absolutely no checkerboard, no grey background, no cast shadow. Solid BLACK near-flat interior, at least 95 percent uniform value, no internal divisions. Designed organic asymmetric outer contour, clean slightly soft unsharpened edge. Charcoal volcanic fire region mood, quiet heavy form; intended midground module independently placed over grey distant mountains. Outer-contour-led, near-flat silhouette cutout; not a grayscale rock rendering, material study, photo, digital sculpture, or full environment concept painting. No cracks pores gravel brush noise granular shading PBR relief normal map appearance repeated striations edge chatter ambient occlusion bevel shading rim light backlit halo glossy highlight local volume modelling. No snow lava glow trees buildings fog particles foreground gameplay terrain routes characters mirrors enemies doors plates hazards UI or feedback. No flat ledges or climbable platforms. Single independent complete mountain only. One low broad mountain with one off-center rounded summit and a long sloping right shoulder; width about 1.7 times height, massive organic silhouette, not a triangle.
```

**steep**

```text
Original decorative art for a 2D side scrolling puzzle platformer, LIMBO inspired cinematic layered silhouette without copying LIMBO assets characters or layouts. EXACTLY ONE isolated volcanic mountain, one single summit, NOT a mountain range, no second mountain, no connected peaks. Full uncropped silhouette with generous padding on all four sides. Solid PURE WHITE technical matte background for later alpha extraction, absolutely no checkerboard, no grey background, no cast shadow. Solid BLACK near-flat interior, at least 95 percent uniform value, no internal divisions. Designed organic asymmetric outer contour, clean slightly soft unsharpened edge. Charcoal volcanic fire region mood, quiet heavy form; intended midground module independently placed over grey distant mountains. Outer-contour-led, near-flat silhouette cutout; not a grayscale rock rendering, material study, photo, digital sculpture, or full environment concept painting. No cracks pores gravel brush noise granular shading PBR relief normal map appearance repeated striations edge chatter ambient occlusion bevel shading rim light backlit halo glossy highlight local volume modelling. No snow lava glow trees buildings fog particles foreground gameplay terrain routes characters mirrors enemies doors plates hazards UI or feedback. No flat ledges or climbable platforms. Single independent complete mountain only. One taller asymmetric mountain with a single blunt summit leaning left, steep curved left flank and long convex right flank, width about 1.3 times height. No secondary peaks.
```

**shoulder**

```text
Original decorative art for a 2D side scrolling puzzle platformer, LIMBO inspired cinematic layered silhouette without copying LIMBO assets characters or layouts. EXACTLY ONE isolated volcanic mountain, one single summit, NOT a mountain range, no second mountain, no connected peaks. Full uncropped silhouette with generous padding on all four sides. Solid PURE WHITE technical matte background for later alpha extraction, absolutely no checkerboard, no grey background, no cast shadow. Solid BLACK near-flat interior, at least 95 percent uniform value, no internal divisions. Designed organic asymmetric outer contour, clean slightly soft unsharpened edge. Charcoal volcanic fire region mood, quiet heavy form; intended midground module independently placed over grey distant mountains. Outer-contour-led, near-flat silhouette cutout; not a grayscale rock rendering, material study, photo, digital sculpture, or full environment concept painting. No cracks pores gravel brush noise granular shading PBR relief normal map appearance repeated striations edge chatter ambient occlusion bevel shading rim light backlit halo glossy highlight local volume modelling. No snow lava glow trees buildings fog particles foreground gameplay terrain routes characters mirrors enemies doors plates hazards UI or feedback. No flat ledges or climbable platforms. Single independent complete mountain only. One medium height mountain with one rounded high summit on the right and a long gently concave descending left shoulder, width about 1.6 times height. No secondary peaks.
```



## 2026-09-06 夜间山路与洞口视觉批准

- 用户明确批准“夜间山路走向右侧洞穴”的视觉优化：蓝灰夜空、稀疏星点与薄云、远近山层次、山间雾、右侧独立洞顶和侧岩、少量石块与枯草、左侧枯树和入口暖灯。此处为本房已批准的露天夜景表现，不将蓝灰色自动迁移到其他火区房间。
- 当前没有正式Door2D或RoomExit2D，本轮仅制作洞口视觉与入口灯，预留门/出口落位；不决定目标房间、门控制源或开关方式，不新增切场触发器、Collider、危险及存档状态。
- 所有新绘图均指定Labnana gemini-3-pro-image；3张成功（night-sky、dead-tree、entrance-lantern），其余8项发生SSL EOF连接错误，停止对应生成分支。复用项目独立岩壁、悬岩、石块和草图，采用统一剪影材质抑制原内部纹理；各对象分别可调，不新增拼接山脉图。
- 目标Scene为手工权威；所有环境写入仅针对Fire022，保留其他房间与工作区已有修改。使用现有雾Shader和柔边遮罩，不新增运行时代码或呼吸灯组件。


### 夜景优化落盘结果与验收

- 新增50个独立表现对象：24个星点、2层薄云、2片山间雾、1片路面薄雾、1棵枯树、8丛草、5块石、3块洞口岩体、洞内暗部、入口暖色范围、灯晕和灯各1个。三座近山继续独立可调，调整为更深蓝灰色；原远山保留。
- 新图与独立复用遮罩位于`Assets/Art/Generated/Environment/Fire/OnDemand/fire022-night-approach/`。Labnana成功生成夜空、枯树、灯；洞口三块岩体与草、石使用现有图片的独立Alpha遮罩副本，未修改共享原图。枯树和灯已清除绿色技术底并裁切透明留边，所有对象分别引用Single Sprite。
- 星云水平视差0.95、山间雾0.80，保留远山0.85及近山0.65；树草石、洞口和灯与地面固定，不加视差。新物件只有Transform、SpriteRenderer及必要的ParallaxLayer2D，无Collider或玩法组件。
- 新增`Fire022HighCloud`、`Fire022ValleyMist`、`Fire022RoadHaze`三份独立材质，复用现有SoftDriftingFogSprite2D着色器的时间噪声。雾云保持低透明度，动态效果未获得连续帧证据。
- 洞口由Cave Roof、Cave Left Rock、Cave Right Rock独立组成，入口视觉约在X=46.5，灯位于(44.9,3.1)。仅表示预留入口，正式门、连接与交互仍未实现。

| 检查 | 结果与证据 |
|---|---|
| RV-07 | 静态通过：原Terrain、Collider、地面语义块与本轮前快照逐字一致；新增对象无碰撞 |
| RV-12 | 静态通过：50个独立对象，179个新序列化块，无原块删除；原块仅改变环境子引用、背景名字/尺寸/Renderer、4个山体颜色。8张图片与导入meta存在 |
| RV-10 | 待最终Unity画面验证：首轮直接相机截图暴露旧多Sprite引用和部分草树悬空，已修复为独立Single Sprite并校正贴地；最终左右构图已离线合成检查，不能代替Unity运行画面 |
| 其余RV | 未受本轮表现修改影响，本轮未重验，不声称全房解法或运行通过 |

- 证据目录：`Temp/W1VisualOptimize/Fire_022/night-pass/`。`before-*.png`和`after-v1-*.png`为Unity直接相机截图；`layout-*.png`为修正后的离线布局示意，雾仅近似模拟。保留v1为问题记录，不作为最终效果通过证据。
- 最终验证时Editor正运行其他房间Fire024，截图守卫返回STOP playing，未中断试玩、切换用户场景或保存其编辑。最终修正后的Unity渲染、角色/镜像对比、连续视差、雾动态、死亡重置及多比例仍待验证。
- 本轮未运行自动测试、完整编译或人工试玩。静态S暂不发布最终前后分数（最终Unity证据不足），动态D=N/A（无连续帧）；固定LIMBO静态基准87.5不用于推算缺失分数。

## 统一纵向跟随与背景视差（2026-09-15）

- 用户明确将FIRE_024的纵向相机跟随和降低背景视觉位移同步至FIRE_021～FIRE_030。本节取代本房历史“仅水平跟随/固定Y=3/无纵向视差”的配置描述。
- 相机followVertical=true、smoothTime=0、framingOffset=(0,0.56)、正交尺寸7；边界Rect(-10,-4,60,18)，保留本房水平宽度，纵向中心允许Y=3..7。初始中心Y=3，Player中心约到Y=2.44后纵向接管；低于接管线的短跳仍保持入口构图，落地回到下边界。
- 本房34个已有ParallaxLayer2D启用纵向跟随，保留各自cameraFollowFactor、水平设置、固定参考Pose和初始位置。屏幕纵向位移为原来的(1-factor)：factor=1时背景纵向固定在画面，.95时余5%，.85时余15%，.8时余20%，.65时余35%；现有子对象继承父节点，不重复添加组件。
- RV-07/12静态检查：视差子树未检测到2D碰撞体或Rigidbody2D，也无嵌套视差组件；本轮只改相机跟随/偏移/阻尼/边界高度及现有视差纵向开关，保留全部其他序列化块与Prefab引用。FIRE_024已有相同配置，无重复Scene改动。
- RV-10：按通用公式静态核对；各房跳跃舒适度、背景与地面衔接、上方覆盖、全行程遮挡和其他比例待运行验证。RV-09/11：目标仍仅为Player，沿用通用绑定、入口接管、死亡/重置和切场清理；本轮未运行验证镜像操作或重置表现。其他RV项未受影响、本次未重验。
- 本轮未切换Editor当前场景，未运行Play Mode、自动测试或人工试玩；修改为已保存的Scene文件，不声称各房已通过运行验收。
