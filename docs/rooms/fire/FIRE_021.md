# FIRE_021：橙色远景基础房间

> 证据归档说明（2026-09-15）：本文 `Captures/` 历史文件已移出 Git；归档位置及历史提交提取方法见 [Git 清理记录](../../maintenance/GIT_CLEANUP_2026-09-15.md#历史证据查找)。

## 状态与批准范围

- 状态：灰盒中；用户于 2026-09-05 明确要求创建约20格地面，随后要求延长至50格，并于2026-09-06延长至60格、黑色Tile、最远处橙色背景。
- Scene：`Assets/Scenes/Levels/Fire/Fire_021.unity`；Scene为实例配置权威，无房间专用运行脚本。
- 依据：`FIRE_REGION.md`、`LEVEL_GEOMETRY_SYSTEM.md`、`CAMERA_SYSTEM.md`、`PLAYER_PREFAB.md`、`RESET_SYSTEM.md`。
- 地图：`docs/maps/MAP.md`登记独立房间。无已批准邻接连接，不添加出口，不改变FIRE_020的预留锚点、世界进度或解锁条件。
- 定位：基础场景搭建，供后续增量制作；本次没有谜题、通关目标或教学洞察。无敌人、Spawner、门、压力板、拾取物或岩浆。

## 网格、Tile与入口

- Grid为1×1 unit。Terrain：Cell X=-10..49、Y=-4..-1，共60列×4行、240格；可站立顶面Y=0，连续长度60 units。厚度覆盖画面底部。
- 地面使用纯白方块Sprite的独立纯黑Tile `Assets/Tiles/Fire/Fire021BlackTerrain.asset`，Alpha=1；Tilemap着色也为黑色，复用无光照剪影材质。
- Terrain配置Static Rigidbody2D、TilemapCollider2D、CompositeCollider2D；显式StaticSolid、安全、地面镜放置语义。
- 编辑用Palette：`Assets/TilePalettes/Fire.prefab`；只在空格补入黑色Tile，不重排既有内容。
- DEFAULT入口为(-8,0.92)，朝右；Player碰撞盒底部距地面0.02，水平支撑完整。
- 通过通用RoomPlayerSpawner2D及Player Prefab Registry生成玩家，不序列化Player，不覆盖移动与镜子规则。
- 地面两端开放，无隐藏阻挡；走出边缘可通过通用手动重置回入口。当前不配置新危险或自动坠落重置规则。

```text
  P
############################################################  顶面 Y=0
############################################################
############################################################
############################################################
X=-10                                                     X=49
```

## 背景与相机

- 使用第1层最远颜色底、第2层极远洞壁、第3层模糊洞穴石柱、第4层焦木中景、第5层后方动态烟雾与第6层玩法及贴地装饰；第7、8层暂无独立内容，省略。草丛属于贴地路边表现，放在角色后方，不冒充第8层前景遮挡。
- 最远层为以橙色 #CE793A 为目标的纯色Sprite（剪影Shader顶点Tint使用其线性值约0.617/0.191/0.042），Sorting Order=-100，水平cameraFollowFactor=1，纵向不跟随，显式引用本房相机。背景无碰撞、危险或玩法语义。
- 背景中心(20,3)、尺寸60×18 units，编辑态覆盖X=-10..50、Y=-6..12，与60格地面同宽。背景铺满画面，Camera clear color使用相同橙色作为宽屏延伸。
- 相机采用通用水平跟随与显式房间边界；地面长60格，16:9下使用水平跟随，到两端按完整视野边界钳制。
- 正交尺寸7；初始中心(0,3,-10)，Y固定3，构图偏移(0,0)，阻尼0.15秒；显式显示边界Rect(-10,-4,60,14)。16:9相机半宽约12.444，水平中心范围约2.444..37.556，Y=-4..10；入口时自动钳制到左侧构图。
- Spawner与Reset显式引用本房CameraFollow2D，Scene中target为空；不改变Player尺寸。
- 必须可见：入口Player、当前操作处的地面与镜子；不要求60格平台两端同时可见，不设置需要离屏双角色协作的谜题。常规角色比例约12.9%。

## 流程与恢复

1. 直接打开Scene运行，从DEFAULT生成Player，观察橙色底与连续黑色地面。
2. 使用通用移动、跳跃及已解锁的镜子能力；无独立通关判定和出口。
3. 手动重置回当前入口，镜子按解锁状态回手、MirrorClone清除。Player死亡沿用同一通用恢复路径；MirrorClone单独死亡不重置Player。
4. 重新打开房间使用DEFAULT与静态初始状态；没有新增永久状态或切场行为。

## RV验收记录

| 检查项 | 结果 | 证据/限制 |
|---|---|---|
| RV-01 入口安全 | 通过 | DEFAULT底部净空0.02；运行生成后Player中心(-8,0.91)，正常支撑 |
| RV-02 入口出口分离 | 不适用 | 没有出口 |
| RV-03 通道净空 | 通过（静态） | 60格连续平面，上方无顶板或障碍；不含跳跃关卡 |
| RV-04 支撑安装 | 不适用 | 无固定机关和敌人；Player支撑见RV-01 |
| RV-05 门洞阻挡 | 不适用 | 无门或隐藏阻挡 |
| RV-06 出口连接 | 不适用 | 独立基础房间，未批准连接，不加入切场出口 |
| RV-07 地形语义 | 通过 | 240格；Composite单一路径，Bounds中心(20,-2)、Extents(30,2)，顶面Y=0；纯黑Sprite与显式StaticSolid一致 |
| RV-08 机关引用 | 不适用 | 无机关、敌人或Spawner |
| RV-09 镜子空间 | 通过（静态），运行待验证 | 平台上方净空足够，无房间镜像规则覆盖；未验证放置、回收和双角色运动 |
| RV-10 信息可见性 | 静态覆盖检查通过，运行待验证 | 60格相机边界已同步；背景收至60格并居中X=20、水平跟随倍率1；编辑态与地面齐宽，16:9运行画面待复核；延长后的跟随和端部构图未运行检查 |
| RV-11 重置恢复 | 通过（引用静态检查），运行待验证 | Spawner与Reset引用入口及本房CameraFollow2D；无自定义规则；未执行死亡/手动重置试玩 |
| RV-12 资源文档 | 通过 | Scene、独立黑色Tile、Palette空格(10,0)、地图登记、索引及本文一致；无序列化Player、出口或房间运行脚本 |

## 实施与验证

- Unity 6000.5.8f1，实例W1@49ee0df8；新场景已保存并在Editor打开，Play Mode已退出，无未保存改动。
- 初次创建Tile后立即填格未生成数据；重新通过SetTilesBlock填充后，回读80格和完整连续碰撞通过。
- 进入Play Mode做短时运行观察与截图，Player生成正常，无Console Error；出现2条memoryless depth load/store渲染警告。
- 运行截图：`Captures/Fire021/fire021-preview.png`（直接相机捕获，不含HUD/Gizmo；该捕获方式的颜色转换与Game View可能不同）。
- 未运行PlayMode自动测试、完整EditMode测试、完整编译或人工试玩；镜子重复操作、端部坠落后的手动恢复、死亡恢复及多宽高比构图仍待验证。
- 用户此前Fire_001—Fire_020纯黑迁移改动保留；本次只新增FIRE_021并补充Palette、地图及索引。

## 50格增量修改

- 保留左端、入口、地面顶面及厚度，向右新增Cell X=10..39、Y=-4..-1的120格，合计200格。
- 相机显示边界宽度20改为50；黑色Tile、橙色背景、倍率、相机尺寸、入口与玩法组件引用保持不变。
- RV-03、RV-07、RV-12：通过本轮填格、碰撞边界与序列化差异检查；RV-10仅静态覆盖检查，运行待验证。其他RV项未受影响，本轮未重验。
- 前述20格版本运行截图仅为历史证据；本轮未进入Play Mode、未执行自动测试或人工试玩，水平跟随、边缘停止及重置后的构图待运行验证。

### 背景延伸修正

- 上轮延长地面时遗漏背景尺寸与中心同步，本轮只修改最远橙色底的Transform：中心X从0到15，宽从40到80，高18不变。
- RV-10静态检查：编辑态背景X=-25..55覆盖地面X=-10..40；运行时水平倍率1保持相对相机偏移，16:9左右端视野均在背景范围内。未重新进入Play Mode，实际运行画面仍待验证。其他对象和玩法配置未改。


## 2026-09-05 Labnana 枯木与路边素材落位

### 授权、资产与层级

- 用户明确要求把本任务生成的草、枯树、倒树与横向断木加入FIRE_021；焦木植被是本房已授权的局部视觉内容，不批准新的燃烧、破坏、碰撞或区域玩法。
- 原始素材全部由Labnana Nano Banana Pro生成。导入9张独立PNG到`Assets/Art/Generated/Fire/Fire021Vegetation/`：草、原始直立枯树、倒树、5款不同枯树和横向断木；保留原始白底文件在任务输出目录。
- 导入时以反向亮度生成真实Alpha，清除白底与极浅噪点，裁到有效轮廓并保留8px透明边距；黑色RGB，100 PPU、底中Pivot、FullRect、Bilinear、Clamp、无MipMap、无压缩。Unity生成并保留所有meta/GUID。
- 复用`Fire018EnvironmentSilhouette.mat`，通过实例暖暗Tint让装饰与纯黑地面、角色区分；未新增Shader、材质、Sorting Layer或运行时代码。
- `EnvironmentVisuals/04 Charred Woodland`：6棵直立枯树与1棵倒树，使用通用`ParallaxLayer2D`，水平倍率0.65，纵向不跟随，显式引用本房Main Camera。
- `EnvironmentVisuals/06 Roadside Visuals`：6段草丛与1段横向断木，无视差。这里仅包含Transform与SpriteRenderer，所有新内容均无Collider、Trigger、Rigidbody、表面语义、事件或重置参与。

### 初始摆位（世界单位）

| 对象 | 资源文件（PNG） | X / 底部Y | 高度或尺寸 | Sorting Order |
|---|---|---|---|---|
| 原始枯树 | standing-dead-tree | -9 / -0.18 | 高8.2，等比 | -40 |
| 高瘦枯树 | dead-tree-01-tall-slender | -3 / -0.16 | 高9.4，等比 | -39 |
| 矮壮枯树 | dead-tree-02-short-wide | 3 / -0.12 | 高4.8，等比 | -38 |
| 迎风枯树 | dead-tree-03-windswept | 8 / -0.12 | 高7.5，等比 | -37 |
| 双干巨树 | dead-tree-04-forked-giant-full | 14 / -0.18 | 高9.1，等比 | -40 |
| 小型断顶树 | dead-tree-05-small-broken | 20 / -0.08 | 高3.1，等比 | -36 |
| 倒树 | fallen-broken-tree | 4 / -0.1 | 6×1.9 | -35 |
| 草丛1—6 | roadside-grass | X=-5.8、1.8、9.2、18、27、35.5；Y=-0.08 | 宽4.7—6.5，高0.38—0.6，隔段翻转 | -18 |
| 横向路面断木 | horizontal-broken-log-platform | 19 / -2.1 | 12×2.1 | 1 |

- 树、倒树、草均排序在MirrorClone(-10)、Player(10)和镜子(20)之后。枯树使用暖暗色降低对比；草丛最高约Y=0.52。
- 横向断木画布Bounds为X=13..25、Y=-2.1..0；上沿贴合既有Terrain的Y=0，主体位于地面内部，只是路面侧面的独立外观。实际支撑仍由200格Terrain提供，不是新增悬空木桥、独立平台或可破坏木头。实例Tint为线性(0.075,0.028,0.012)，保持深色断口可辨认。
- 地形、入口、相机参数、Spawner、Reset、镜子与镜像规则保持不变。此期间另一轮已记录的背景延伸修正（中心X=15、宽80）保留，未回退。

### 本轮增量验证

| RV项 | 本轮结果 | 证据与限制 |
|---|---|---|
| RV-01、03 | 配置未变，未重验完整运动 | 原有入口与Terrain序列化块不变；运行从DEFAULT正常生成，另两处仅为取景摆位 |
| RV-02、04、05、06、08 | 不适用 | 无出口、固定机关、敌人、门或Spawner；装饰不承担安装或交互职责 |
| RV-07 | 通过（静态） | 200格，Composite单一路径，Bounds中心(15,-2)、Extents(25,2)；新增14个SpriteRenderer均无玩法组件，断木不越过地面顶面 |
| RV-09 | 配置未变，运行待验证 | 镜子组件、语义与空间不变；树草均在MirrorClone之后，未实际操作镜子或验证双角色对比 |
| RV-10 | 取景检查通过，连续过程待验证 | Play Mode观察入口X=-8、中段X=15与右端X=38；另在X=19复查断木最终颜色。Player与地面可见，背景未露底，枯树横向视差可见；不代表行走、镜子或完整关卡解法通过 |
| RV-11 | 配置未变，运行待验证 | 新增对象不参与重置或存档；未实际触发死亡、手动重置或切场 |
| RV-12 | 通过（静态） | 9张Sprite导入成功、引用有效；场景为手工权威，未发现或运行Fire021 Builder；新视觉结构与本文同步 |

- 运行证据：`Captures/Fire021/Vegetation/vegetation-runtime-left.png`、`vegetation-runtime-middle.png`、`vegetation-runtime-right.png`、`vegetation-runtime-log-final.png`。前三张早于横向断木提亮，最终断木外观以最后一张为准。直接相机捕获不含HUD，颜色转换可能与Game View不同。
- Unity 6000.5.8f1，W1@49ee0df8。新增结构已保存；两次短时Play Mode取景后均退出。最终Console查询无Error或Warning。
- 未运行PlayMode自动测试、完整EditMode、完整项目编译或人工试玩；连续行走中的枝干背景对比、镜像与镜子可读性、重置和其他宽高比仍待验证。
- 任务开始前已有Fire_001—020、Palette、地图、区域文档、索引、FIRE_021初始文件和Captures改动均保留。本轮仅新增植被资产、修改FIRE_021视觉与本文，并写入本轮证据。

## 2026-09-06 延长至60格与背景收窄

- 背景80格是上一轮额外预留，现按用户反馈收为60格；中心(20,3)，世界边界X=-10..50，与地面同宽。颜色、高度18和最远层倍率1不变。
- 地面保留左端与入口，右侧新增Cell X=40..49、Y=-4..-1的40格；现为60×4、240格，相机边界Rect(-10,-4,60,14)。
- RV-03、RV-07、RV-12静态检查通过：Composite单路径，Bounds中心(20,-2)、Extents(30,2)，对象ID与非目标组件保持不变。树、草、断木、入口及玩法引用未改。
- RV-10：编辑态背景与地面范围一致；16:9入口相机中心2.444时背景左缘与视野左缘均为-10，倍率1保持覆盖关系；实际相机跟随和端部画面未进入Play Mode重验。RV-11及其余未受影响项本轮未重验。未运行自动测试或人工试玩。


## 2026-09-06 两段断木落地与矩形碰撞（当前配置）

- 用户明确要求Sprite断木配比图像小一圈的矩形碰撞并放到地面；本节替代上述两段断木的纯装饰配置，其他树草保持原状。按本次明确请求，这两件静态对象使用SpriteRenderer与独立BoxCollider2D，不新增Tilemap或运行时生成行为。
- `Fallen Tree Behind Road`与`Horizontal Broken Log Road Facing`均移到`Gameplay`直接子级，解除中景视差继承；保持原Sprite与尺寸，纯黑Tint，Order=-11，绘制在MirrorClone、Player与镜子之后。
- 矩形四边相对Sprite画布Bounds各内缩0.1 unit；倒树碰撞X=1.1..6.9、Y=0..1.7（5.8×1.7），横向断木碰撞X=13.1..24.9、Y=0..1.9（11.8×1.9）。两者Collider底边均为Y=0；Sprite底部Y=-0.1，轻微埋入地面。矩形不逐枝条或透明孔洞拟合，属于用户要求的简化碰撞。
- 两者使用非Trigger静态Collider，与Terrain相同Layer；显式StaticSolid、安全、Ground镜面语义，沿用通用站立、碰撞、镜子放置及重置规则。无移动、破坏、存档或房间专用行为；未改动240格Terrain、入口、背景、相机及其他对象。
- RV-04、07、12：组件和Bounds回读通过；矩形内缩尺寸及贴地位置已核对并保存。RV-03、09、10、11：新增障碍后的跳跃、双角色操作、画面与恢复行为未运行验证；两处上表面仅1.7/1.9格高，但不据此宣称路线验证通过。RV-01、02、05、06、08未受影响，本轮未重验。
- 按用户要求仅做直接相关的组件与位置检查；未进入Play Mode、未运行自动测试或人工试玩。


## 2026-09-06 模糊石柱预览

- 按用户要求直接使用已生成的模糊版本，导入`Assets/Art/Generated/Fire/Fire021Vegetation/lava-cave-pillar-blurred.png`。将白底转为连续Alpha，保留图片自带的柔化边缘；不新增模糊Shader。
- 新增`EnvironmentVisuals/03 Far Cave Pillar/Blurred Lava Cave Pillar`，中心(4,6.7)，画布高18、等比缩放，上下端伸出当前视野/藏入地面，避免平直裁切边暴露。Sorting Order=-60，线性Tint(0.28,0.083,0.022)，复用已有剪影材质。
- 第3层水平视差0.85，显式引用Main Camera；仅有Transform、SpriteRenderer和父级ParallaxLayer2D，无碰撞或玩法组件，位于树木与角色之后。
- RV-07、12：直接组件与资源回读通过；RV-10：编辑态预览可见模糊轮廓，未进入Play Mode检查全行程。其余RV条目未受本轮影响，未重验；原地形、断木碰撞与相机配置不变。
- 预览：`Captures/Fire021/Vegetation/blurred-pillar-preview-entry.png`。未运行自动测试或人工试玩，场景已保存。


## 2026-09-06 三种模糊石柱增量

- 用户要求增加不同形态的模糊柱子；Labnana Nano Banana Pro生成弯曲细柱、厚重层岩柱和带孔双支柱3张独立图片，转换白底为连续Alpha，保留模糊轮廓。资源为`Assets/Art/Generated/Fire/Fire021Vegetation/cave-pillar-blurred-{slender,heavy,forked}.png`。
- 全部加入现有`03 Far Cave Pillar`，共4根石柱，沿用水平倍率0.85与显式Main Camera引用，无新Shader、材质或玩法组件。
- 细柱：中心(-8,6.6)、画布高17、Order=-63、线性Tint(0.38,0.114,0.029)；厚柱：中心(12,8.1)、画布高20、Order=-62、Tint(0.34,0.102,0.027)、水平翻转；双支柱：中心(20,7.1)、画布高18、Order=-61、Tint(0.40,0.124,0.031)。三者等比缩放，位于原石柱及树木后方，底部藏入地面、顶部超出画面。
- RV-07、12直接回读通过：新增对象各仅Transform与SpriteRenderer，整个石柱层Collider数为0，3张资源引用有效，Scene已保存。RV-10只完成编辑态局部预览，证据`Captures/Fire021/Vegetation/blurred-pillars-variants-preview.png`；未验证运行全行程遮挡与视差构图。其他RV项未受影响且未重验。
- 保留当前地形、断木碰撞、树草、入口、背景和相机配置。未进入Play Mode、未运行自动测试或人工试玩。


## 2026-09-06 中景洞顶与侧壁预览

- 按用户要求导入Labnana生成的洞顶与侧壁组合模块，资源`Assets/Art/Generated/Fire/Fire021Vegetation/cave-ceiling-sidewalls.png`。白色开口转为真实Alpha，暗部统一为剪影，保留柔边；这是独立框景模块，不包含地面或可玩布局。
- 对象`EnvironmentVisuals/04 Charred Woodland/Cave Ceiling and Sidewalls`，中心(5,3)、尺寸40×18、Order=-45、线性Tint(0.18,0.065,0.025)。继承已有中景水平视差0.65，位于远景石柱之前、树木与角色之后；不修改共享材质或相机。
- 仅新增Transform与SpriteRenderer，无Collider、Trigger、表面语义或玩法脚本。原地形、断木与全部玩法配置不变。
- RV-07、12：直接组件、资源与Bounds回读通过，场景已保存。RV-10：完成一张编辑态入口方向预览`Captures/Fire021/Vegetation/cave-ceiling-sidewalls-preview.png`；实际全行程视差、角色与镜像背景对比待运行验证。其他RV项未受影响，本轮未重验。
- 未进入Play Mode，未运行自动测试或人工试玩。


## 2026-09-06 右端框景覆盖修正

- 用户反馈右侧直线截断。当前Play Mode相机X≈37.556，中景实际偏移≈22.822，原框景右边界约47.822，小于房间右边界50，导致露出图片竖边。
- 仅调整`Cave Ceiling and Sidewalls`局部中心X=5→8、宽40→46，高18与Y=3不变；初始左边界仍为-15，右边界从25延伸至31。当前运行右边界约53.822，超出视野右侧约3.822格，下沿Y=-6继续覆盖地面以下。
- 仅外部增量写入该Transform的position.x及scale.x，并同步当前运行中的同一视觉对象；用户正在Play Mode且Scene dirty，未退出运行、未保存运行态、未丢弃未保存编辑。磁盘修改已保存；退出Play Mode后的Editor Reload按UNITY_MCP_WORKFLOW处理，不能用旧内存版本覆盖磁盘修正。
- RV-10右端运行截图检查通过：竖直图片截断不再露出，证据`Captures/Fire021/Vegetation/right-wall-coverage-fixed.png`。RV-07、12：无新增组件或玩法改动，仅一个表现Transform更新。其他RV项未受影响且未重验；未运行自动测试或人工试玩。


## 2026-09-06 极远洞壁首版

- Labnana生成独立极远景模块`Assets/Art/Generated/Fire/Fire021Vegetation/extreme-far-lava-cave-wall.png`：模糊暗红褐色洞室与宽裂隙，局部琥珀开口，无地面、玩法布局或可接触岩浆。
- 新对象`EnvironmentVisuals/02 Extreme Far Cave Wall`，初始中心(2.444444,3)、42×18、Order=-90、白Tint与Alpha=0.35；位于橙色底之后的渲染顺序（即覆盖橙色底）、全部石柱之前的景深位置。复用URP包内Sprite-Unlit-Default材质保留图片颜色，无新Shader。
- 水平视差0.95，显式Main Camera引用，无纵向视差或碰撞。16:9视野宽24.889，最大相机行程35.111，最低水平覆盖约26.645，图片宽42有余量；Y=-6..12覆盖相机Y=-4..10。
- 增量写入磁盘4个新序列化块与EnvironmentVisuals一个子引用，并添加实时预览对象；没有保存用户运行态或主动停止Play Mode。若Editor尚有dirty状态，需按UNITY_MCP_WORKFLOW合并/重载，保留用户编辑，不能用旧内存覆盖磁盘。
- RV-07、12：新增对象无Collider且资源可加载，静态结构检查通过；RV-10仅当前镜头预览`Captures/Fire021/Vegetation/extreme-far-cave-wall-preview.png`，未验证全行程与双角色可见性。其他RV未受影响且未重验。未运行自动测试或人工试玩。


## 2026-09-06 后方烟雾与洞穴熔光

- 新增 `EnvironmentVisuals/05 Rear Smoke`，水平视差0.80、显式引用本房相机、无纵向跟随。两个子Sprite位于岩层前、树木与玩法后：
  - `Rear Smoke Lower Left`：中心(-2,4)，尺寸22×4.5，Order=-44，Opacity=0.22，UV横移速度+0.009/秒。
  - `Rear Smoke Upper Right`：中心(14,7)，尺寸23×5，Order=-43，Opacity=0.17，UV横移速度-0.006/秒。
- 烟带使用真实透明的柔边椭圆遮罩 `soft-atmosphere-mask.png`；从已有LayeredFog2D派生独立通用 `SoftDriftingFogSprite2D.shader`，增加纹理Alpha与Sprite顶点色支持，不改变既有共享Shader。两个独立Fire021材质均为低透明度暗红褐色，噪声横向流动，柔边与稀疏密度保留空隙。
- 复用 `AmbientSpritePulse2D`：烟带22/29秒周期，Alpha相对变化±9%、尺度±2.5%；组件已有减少动态效果策略。Shader内部噪声仍使用_Time，不受该组件动态策略控制。
- 新增 `EnvironmentVisuals/03 Far Cave Pillar/Distant Crevice Ember Glow`：中心(9,5)，尺寸11×8，Order=-70，继承远景0.85视差，位于石柱后。复用同一柔边遮罩与URP Sprite-Unlit-Default，线性颜色(0.72,0.19,0.032)、Alpha=0.19；26秒周期、Alpha相对变化±5.5%，无缩放。光斑最低Y=1，无Light2D、Bloom或连续水平边界，不照亮普通路面。
- 三个Sprite均无Collider、Rigidbody、表面语义、危险或玩法事件。只新增15个序列化块，以及EnvironmentVisuals/03层两个父Transform的子引用；与本轮开始磁盘快照相比，全部原对象块和玩法配置保持不变。用户此前移动树木/断木等修改均保留，未按历史坐标恢复。
- RV-07、12：无装饰碰撞、资源有效、ShaderHasError=false、Console无Error；目标Scene保存成功。RV-10：按相机X=2.444、20、37.556静态摆位并计算各层视差，完成左中右截图；本轮新增雾与光没有矩形硬边或水平岩浆线。右端取景的既有背景左侧可见一处直立色带边界，属于原背景覆盖/接缝问题，本轮未改。
- 证据：`Captures/Fire021/Vegetation/rear-atmosphere-{left,middle,right}.png`。这是编辑态静态取景，未进入Play Mode、未运行自动测试或人工试玩；连续漂移、双角色对比、重置及其他宽高比仍待运行验证。RV-01～06、08、09、11未受本轮影响且未重验。
- 操作时用户已切换Fire_022，因此临时以Additive打开Fire_021，单独保存后移除临时加载，保留用户当前场景；没有保存Fire_022或其他场景。


## 2026-09-06 洞顶纯黑调整

- 按用户要求，将共用洞顶与侧壁图的 `Cave Ceiling and Sidewalls` Tint改为(0,0,0,1)，连带两侧框景统一纯黑；Sprite、尺寸、视差和碰撞配置不变。Sorting Order从-45改为-42，放在两条后方烟带(-44/-43)之前、树木之后，避免烟雾染亮洞顶。
- RV-07、12：仅该SpriteRenderer颜色与排序两个字段变更，无玩法修改。RV-10：渲染排序静态检查，未重新运行全行程或人工试玩，透明柔边仍会与背景混合；其余RV项未受影响且未重验。


## 2026-09-06 角色路线背景雾光试版

- 按用户要求，在 `EnvironmentVisuals/05 Rear Smoke` 下添加 `Route Backlight Left`、`Route Backlight Center`、`Route Backlight Right` 三个静态柔边Sprite，继承水平视差0.80，复用 `soft-atmosphere-mask.png` 与URP Sprite-Unlit-Default；无新图片、材质、Shader或脚本。
- 初始中心依次(-7,2.1)、(6,2.7)、(19,2.1)，均24×7 units；线性暖褐色(0.55,0.24,0.105)，Alpha依次0.28/0.24/0.28。宽椭圆渐变交错衔接，避免窄亮线；静态亮度保证路线对比稳定。
- Order=-20，覆盖背景树与低处侧壁，位于路边草(-18)、断木(-11)、MirrorClone(-10)、Player(10)、镜子(20)及Terrain(0)后方。雾光底缘藏入黑色地面，顶部柔淡；上方洞顶主体保持纯黑，低垂侧壁可能受到雾光覆盖。无实际灯光、Collider、危险、镜面或重置语义。
- RV-07、12静态通过：仅新增9个序列化块和既有后雾父Transform的3个子引用，无删除或其他已有对象变化。RV-10完成相机X=2.444/20/37.556三处编辑态视差摆位取景，截图 `Captures/Fire021/Vegetation/route-backlight-{left,middle,right}.png`；背景下部提亮，没有新增矩形硬边或窄水平光线。之前记录的背景左侧直立色带未处理。
- 未进入Play Mode、运行自动测试或人工试玩；截图不含玩家，实际移动中的Player/MirrorClone轮廓及跳跃至高处时的对比仍待运行验证，不能视为完整可读性通过。RV-01～06、08、09、11未受本轮影响且未重验。
- 操作开始用户在Fire_023；以Additive临时加载Fire_021、单独保存并恢复用户当前场景，未保存其他Scene。

## 2026-09-09 修复停步后的屏幕回跳

- 用户报告与FIRE_023相同的问题。本房CameraFollow2D原smoothTime=0.15，现改为0，直接跟随已插值的Player位置，避免松键后镜头继续追赶产生人物后退的观感。房间阻尼可配置，依据CAMERA_SYSTEM“相机配置与绑定职责”。
- 仅修改本房相机阻尼；60格地面、Rect(-10,-4,60,14)、正交尺寸7、入口接管、装饰视差、玩家移动与镜像规则保持。
- 在X=20附近完成四轮短按右移/松键运行观察；物理位置倒退0次，四轮屏幕回移均为0。记录`Captures/Fire021Motion/tap-fixed.csv`。RV-10验证限于此触发方式，不声明其他阻挡或全行程已全部排除；其他RV未受影响且未重验。
- 编辑前目标Scene非dirty、无Prefab Stage、非Play Mode；在Editor保存阻尼修改后进入短时观察，观察结束已退出，未保存运行状态。未运行自动测试或完整人工试玩。

## 统一纵向跟随与背景视差（2026-09-15）

- 用户明确将FIRE_024的纵向相机跟随和降低背景视觉位移同步至FIRE_021～FIRE_030。本节取代本房历史“仅水平跟随/固定Y=3/无纵向视差”的配置描述。
- 相机followVertical=true、smoothTime=0、framingOffset=(0,0.56)、正交尺寸7；边界Rect(-10,-4,60,18)，保留本房水平宽度，纵向中心允许Y=3..7。初始中心Y=3，Player中心约到Y=2.44后纵向接管；低于接管线的短跳仍保持入口构图，落地回到下边界。
- 本房5个已有ParallaxLayer2D启用纵向跟随，保留各自cameraFollowFactor、水平设置、固定参考Pose和初始位置。屏幕纵向位移为原来的(1-factor)：factor=1时背景纵向固定在画面，.95时余5%，.85时余15%，.8时余20%，.65时余35%；现有子对象继承父节点，不重复添加组件。
- RV-07/12静态检查：视差子树未检测到2D碰撞体或Rigidbody2D，也无嵌套视差组件；本轮只改相机跟随/偏移/阻尼/边界高度及现有视差纵向开关，保留全部其他序列化块与Prefab引用。FIRE_024已有相同配置，无重复Scene改动。
- RV-10：按通用公式静态核对；各房跳跃舒适度、背景与地面衔接、上方覆盖、全行程遮挡和其他比例待运行验证。RV-09/11：目标仍仅为Player，沿用通用绑定、入口接管、死亡/重置和切场清理；本轮未运行验证镜像操作或重置表现。其他RV项未受影响、本次未重验。
- 本轮未切换Editor当前场景，未运行Play Mode、自动测试或人工试玩；修改为已保存的Scene文件，不声称各房已通过运行验收。
