# EARTH_006：轨面交接

## 状态与权威来源

- 当前状态：灰盒已创建，等待人工试玩与运行时调优。
- Unity Scene：`Assets/Scenes/Levels/Earth/Earth_006.unity`（已创建并登记Build Settings）。
- 地图连接：左`EARTH_007`，右`EARTH_010`。
- 入口与出口：保留唯一`DEFAULT`安全入口；每个已实现相邻来源使用`FROM_<来源房间ID>`入口，出口显式请求目标房的`FROM_<本房ID>`入口。
- `DEFAULT`入口位于左出口Trigger内侧且与其保持安全间距，直接从本Scene启动时不得立即触发离场。
- 世界进入顺序与土区解锁条件仍待确定。

## 房间定位

- 房间类型：组合。
- 主要目标：压低土块，使其上表面与往返移动平台的端点齐平并完成换乘。
- 预期洞察：重量可以改变移动路线的接驳高度。
- 失败压力：低到中；错误操作不会永久改变房间。
- 无惩罚重试：回收镜子后重新规划，或使用统一手动重置。

## 使用机制与排除项

- 使用压沉土块及本文Prefab表列出的既有通用机制。
- 不包含矿车、矿石、转辙器、动态Tilemap、永久锁定土块或房间专用玩法脚本。
- 不改变镜像输入、角色质量、移动参数、镜子放置或统一重置规则。

## 标准网格布局

- Grid：`1×1 Unity unit`。
- 概念可玩边界：`x=-12～12，y=-5～6`；灰盒前可以收紧，但不得改变谜题关系。
- 左侧入口、中央土块、右侧水平往返平台；土块未下沉时两者高度错开2格。
- 静态地形使用标准`Terrain` Tilemap；镜子放置点必须是安全`StaticSolid`。
- `Terrain` Tilemap使用`Assets/Tiles/Earth/Earth006/Earth006Terrain.asset`与纯色Sprite `Assets/Art/Earth/Terrain/Earth006TerrainSolid.png`呈现近黑暖褐剪影；单格内部不包含纹理、裂纹、渐变或烘焙明暗。该视觉替换不改变格子布局、碰撞或`StaticSolid`表面语义。
- 动态对象完整行程不得夹住角色、侵入出口或形成存活软锁。

## Prefab需求

- SinkingBlock-A：宽3、行程2、满沉重量1；MovingPlatform-A：水平4格往返。
- 压沉土块统一使用`Assets/Prefabs/Gameplay/Earth/SinkingEarthBlock2D.prefab`。
- 其他对象使用现有通用Prefab；实例只覆盖对应系统文档允许的参数。
- Player、镜子和MirrorClone不作为房间重复实例。
- 本房没有Spawner敌人；敌人出生点与生成配置不适用。

## 相机配置

- 固定单屏；土块完整行程、平台两端点和落点同时可见。
- 使用全局默认角色占屏比例，正交尺寸基准为`7`。
- 相机边界不得显示房外内容；MirrorClone参与时必须同时显示双方、操作对象和结果对象。
- 无构图例外，不修改Player尺寸。

## 环境视觉分层

- 使用`EARTH_006 Environment Visuals`纯表现根，所有子对象只包含`Transform`、`SpriteRenderer`和`ParallaxLayer2D`，不包含Collider、Trigger、Rigidbody2D或玩法语义。
- `01 Color and Fog Backdrop`：`cameraFollowFactor=1.00`，使用`earth-fog-light.png`建立暖褐雾光底。
- `02 Extreme Far Contours`：`cameraFollowFactor=0.95`，使用`earth-extreme-far-shaft.png`建立极远深井轮廓。
- `03 Far Environment`：`cameraFollowFactor=0.85`，使用`earth-far-strata-tunnel.png`建立低对比远景坑道。
- `04 Mid Environment`：`cameraFollowFactor=0.65`，使用承重岩柱、矿井支撑与层状岩体三个独立透明模块。
- `08 Foreground Occlusion`：`cameraFollowFactor=0.20`，使用左右独立近黑框景，限制在画面边缘，不承担地形、危险或镜子放置语义。
- 本次没有配置后部动态尘雾和前部落尘粒子；对应功能层保持省略，不把空层记录为已实现表现。
- 本房沿用全局默认水平视差，不启用纵向跟随；固定单屏构图下仍保留各层独立职责和显式Main Camera引用。
- 素材来自`Assets/Art/Generated/Environment/Earth/Candidates/earth-lowtexture-20260901/`，均为极少纹理的独立模块，不包含完整房间布局或可玩地形。

## 预期流程

1. 玩家先观察土块完整行程、目标高度和安全落点。
2. 玩家通过自身或MirrorClone重量改变土块位置。
3. 房间以位置、顶部标识和受影响通路直接反馈结果。
4. 状态不合适时离开土块等待恢复，或无惩罚重置。
5. 完成因果组后进入地图登记的相邻出口。

## 重置、死亡与边界

- 手动重置和Player死亡：MirrorClone清除、镜子回手；所有瞬时对象恢复初始状态。
- MirrorClone单独死亡或回收：只解除其重量和占用；土块按恢复速度继续，房间不整体重置。
- 重新进入：加载本房全部瞬时对象初始状态。
- 压沉土块不能放置镜子；重复左键不得改变机关。
- 所有落差提供安全回路或统一重置，不形成存活软锁。

## 验收与未验证风险

- 操作前可看清因果关系；镜子合法性可预判；土块全行程安全；解法不依赖帧率或盲跳。
- 已使用标准Tilemap骨架、通用压沉土块Prefab及本文列出的通用动态对象完成灰盒。
- 已完成Unity序列化保存、Builder内部结构校验和低成本静态检查；未运行PlayMode、完整EditMode或人工试玩。
- 已完成环境表现增量重构的静态相机截图检查；Gameplay Tilemap、入口出口、相机参数、压沉土块与移动平台配置未因环境分层改变。
- 尚需人工试玩校正实际Collider净空、跳跃节奏、动态表面换乘窗口和镜像路线可读性。
- 尚需Play Mode观察确认运行时Player与MirrorClone不会被左右前景遮挡，并确认固定镜头下不同画面比例没有透明接缝。
