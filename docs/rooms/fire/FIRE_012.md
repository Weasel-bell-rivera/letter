# FIRE_012：门也是盾牌

## 状态与连接

- 当前状态：灰盒制作中；用户已明确要求继续实现下一关。
- Scene：`Assets/Scenes/Levels/Fire/Fire_012.unity`。
- 世界图中仅连接`FIRE_011`，因此采用内部环路：从右侧下层进入，完成谜题后从右侧上层返回`FIRE_011`。
- `Entrance-DEFAULT`与`Exit-A`均连接`FIRE_011/DEFAULT`，本次不改变地图结构。

## 房间定位与机制

- 类型：水平投火者、镜像替身与单压力板门组合房。
- 洞察：同一扇门既是通路开关，也是MirrorClone消失后隔断下层射击路线的实体盾牌。
- 使用地面镜、MirrorClone反向移动、单压力板持续开门、水平投火者和通用重置。
- 不使用岩浆、喷发、双板锁存、Spawner、检查点、房间专用运行时代码或未批准火区机制。

## 布局

- Grid：`1×1 Unity unit`；房间与固定单屏相机边界：`X[-15,15]`、`Y[-7,7]`；正交尺寸`7`。

```text
┌────────────────────────────────────────────────────────────┐
│          左侧楼梯 ─────── 上层安全回程 ─────────── E →     │
│                                                            │
│        D ◀──── P/M ─────────────── [A]█  ◀◀◀ H            │
│                                                            │
│ ██████████████████████████████████████████████████████████ │
└────────────────────────────────────────────────────────────┘
```

- `Terrain`承担边界、地面、门上封墙、左侧楼梯、上层平台和止挡墙，使用Composite Collider与`StaticSolid`语义；其余标准Tilemap层保留。
- `Terrain`统一使用 `Assets/Tiles/Fire/Fire012SilverSandstoneGround.asset`；该Tile以 `Assets/Art/Fire/Tiles/fire012_silver_sandstone_ground_v1.png` 作为单个 `1×1 Unity unit` 银灰砂岩格子，不改变地形坐标、碰撞或表面语义。

## Prefab需求

| 实例ID | 通用Prefab路径 | 初始位置/状态 | 配置 |
|---|---|---|---|
| `Plate-A` | `Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab` | `(8.5,-1.7)`；弹起 | 通用持续占用 |
| `Door-A Shield` | `Assets/Prefabs/Gameplay/Doors/Door2D.prefab` | `(-4.5,-1)`；关闭 | 显式引用`Plate-A` |
| `Enemy-H1` | `Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab` | `(12.5,-1.5)`；向左 | 固定Scene敌人，不使用Spawner |
| `Exit-A` | `Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab` | `(9.5,3)` | 目标`Fire_011/DEFAULT` |

## 预期流程

1. Player放镜并向左，MirrorClone反向向右。
2. MirrorClone进入攻击范围并踩板开门，Player穿门进入左侧。
3. 火球击中MirrorClone后镜子自动回收，压力板释放，门关闭在Player身后。
4. Player从左侧楼梯进入上层安全回程，抵达右侧出口。

## 失败、重置与风险

- Player中弹执行完整重置；MirrorClone中弹只回收镜子并释放压力板。
- 不放镜时门关闭；出生点不能直接到达上层；门上方由静态Terrain封闭；门左侧可经楼梯离开，不产生软锁。
- Scene不序列化本地Player，动态玩法对象必须保持Prefab连接。
- 未运行PlayMode或人工试玩；攻击锁定时机、穿门容错和实际跳跃可达性仍需运行时确认。
