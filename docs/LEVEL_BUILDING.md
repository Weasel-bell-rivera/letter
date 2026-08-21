# 关卡搭建规范

## 基础单位

- 1个标准地形格为1×1 Unity unit。
- 地面和墙壁优先使用Grid与Tilemap搭建。
- 玩家尺寸、跳跃高度和通道宽度必须使用同一套尺度。
- 玩家Collider宽度为：`0.8 Unity unit`。
- 玩家Collider高度为：`1.8 Unity units`。
- 普通通道的最小宽度为：`1.25 Unity units`。
- 普通通道的最小高度为：`2.25 Unity units`。
- 普通平台的最小站立宽度为：`1.25 Unity units`。
- 标准可靠跳跃距离：`3.5 Unity units`；这是关卡灰盒采用的保守值，不是物理极限值。
- 以完整助跑和标准跳跃计算的理论水平位移约为 `4.2 Unity units`；普通房间必须保留至少 `0.7 unit` 的落地余量。
- 不使用极薄Collider制造不可见障碍。

以上尺寸必须以 `docs/PLAYER_MOVEMENT.md` 中的移动参数和实际 Player Collider 为依据，通过Unity灰盒测试确定，不能仅凭视觉估算。

## 地面

- 地面阻挡玩家和明确配置为需要碰撞的动态物体。
- 地面是否阻挡镜像，以 `docs/COLLISION_RULES.md` 的已确认交互矩阵为准。
- 地面必须提供稳定的落地检测。
- 视觉边界应与Collider边界基本一致。

## 墙壁

- 普通墙壁不可穿越。
- 普通墙壁不能放置镜子。
- 特殊墙面可以根据镜子规则允许放置镜子。
- 特殊墙面必须具有统一的视觉标识和Collider配置。
