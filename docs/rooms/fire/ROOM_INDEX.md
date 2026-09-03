# 火之区域房间索引

本文档是火之区域房间编号和批准状态的权威索引。房间文档存在不代表该房间已经批准进入正式开发。

2026-08-31增量修复及逐房RV结果见[FIRE_REPAIR_REVIEW.md](FIRE_REPAIR_REVIEW.md)。本轮没有将任何房间升级为正式或全项通过；FIRE_002、004、009、014仍有设计冲突，FIRE_013已补写入口、台阶、门洞和出口修复，完整共享输入解法仍待运行验证。

## 状态定义

- 占位：只有编号或初步想法。
- 草案：已有设计，但尚未批准。
- 已批准：允许制作灰盒。
- 灰盒中：正在实现。
- 待试玩：灰盒完成，等待人工试玩验证。
- 正式：通过试玩并纳入区域流程。
- 废弃：停止使用。

## 当前房间

| 房间 | 状态 | 设计文档 | Unity Scene | 主要目标 |
|---|---|---|---|---|
| FIRE_001 | 待试玩 | `docs/rooms/fire/FIRE_001.md` | `Assets/Scenes/Levels/Fire/Fire_001.unity` | 只教学岩浆伤害 |
| FIRE_002 | 待试玩 | `docs/rooms/fire/FIRE_002.md` | `Assets/Scenes/Levels/Fire/Fire_002.unity` | 首次教学水平投火者与镜像替身 |
| FIRE_003 | 手工灰盒已同步，待运行时验证 | `docs/rooms/fire/FIRE_003.md` | `Assets/Scenes/Levels/Fire/Fire_003.unity` | 顶部放镜、纵向分路与两段持续占板交叉门 |
| FIRE_004 | 待试玩 | `docs/rooms/fire/FIRE_004.md` | `Assets/Scenes/Levels/Fire/Fire_004.unity` | 用镜像引导水平火球命中锁存板并打开机关门 |
| FIRE_005 | 待试玩 | `docs/rooms/fire/FIRE_005.md` | `Assets/Scenes/Levels/Fire/Fire_005.unity` | 三层横向折返；双投火者、中部锁存板、单格背板与右侧出口门 |
| FIRE_006 | 待试玩 | `docs/rooms/fire/FIRE_006.md` | `Assets/Scenes/Levels/Fire/Fire_006.unity` | 左侧攀爬并取得全游戏7个永久收藏品之一 |
| FIRE_007 | 待试玩 | `docs/rooms/fire/FIRE_007.md` | `Assets/Scenes/Levels/Fire/Fire_007.unity` | 永久锁存双压力板门控组灰盒 |
| FIRE_008 | 灰盒中 | `docs/rooms/fire/FIRE_008.md` | `Assets/Scenes/Levels/Fire/Fire_008.unity` | 三段地形错位与双压力板组合房 |
| FIRE_009 | 灰盒中 | `docs/rooms/fire/FIRE_009.md` | `Assets/Scenes/Levels/Fire/Fire_009.unity` | 水平投火者与镜像替身巩固房 |
| FIRE_010 | 灰盒中 | `docs/rooms/fire/FIRE_010.md` | `Assets/Scenes/Levels/Fire/Fire_010.unity` | 固定岩浆、镜像分路与单板门组合房 |
| FIRE_011 | 灰盒中 | `docs/rooms/fire/FIRE_011.md` | `Assets/Scenes/Levels/Fire/Fire_011.unity` | 周期喷发、镜像诱敌与单板门组合房 |
| FIRE_012 | 灰盒中 | `docs/rooms/fire/FIRE_012.md` | `Assets/Scenes/Levels/Fire/Fire_012.unity` | 投火者、镜像替身与门盾牌组合房 |
| FIRE_013 | 灰盒中 | `docs/rooms/fire/FIRE_013.md` | `Assets/Scenes/Levels/Fire/Fire_013.unity` | 双门分支枢纽 |
| FIRE_014 | 灰盒中 | `docs/rooms/fire/FIRE_014.md` | `Assets/Scenes/Levels/Fire/Fire_014.unity` | 双投火者与门盾牌挑战 |
| FIRE_015 | 灰盒中 | `docs/rooms/fire/FIRE_015.md` | `Assets/Scenes/Levels/Fire/Fire_015.unity` | 双喷发、岩浆、诱敌与单板门综合房 |
| FIRE_016 | 灰盒中 | `docs/rooms/fire/FIRE_016.md` | `Assets/Scenes/Levels/Fire/Fire_016.unity` | 双门接力与镜像替身 |
| FIRE_017 | 灰盒中 | `docs/rooms/fire/FIRE_017.md` | `Assets/Scenes/Levels/Fire/Fire_017.unity` | 周期升降岩浆与火区机制综合考验 |
| FIRE_018 | 灰盒中 | `docs/rooms/fire/FIRE_018.md` | `Assets/Scenes/Levels/Fire/Fire_018.unity` | 上下双通道；镜像与本体分别诱导火球命中双锁存板，AND开启上方门 |
| FIRE_019 | 灰盒中 | `docs/rooms/fire/FIRE_019.md` | `Assets/Scenes/Levels/Fire/Fire_019.unity` | 三通道接力；先放行巡逻投火者，再用MirrorClone在底层诱火锁存并打开FIRE_020出口 |
| FIRE_020 | 灰盒中 | `docs/rooms/fire/FIRE_020.md` | `Assets/Scenes/Levels/Fire/Fire_020.unity` | 火窗协作；镜像占板、Player穿越周期喷发诱火，锁存开启下层目标门 |

FIRE_001、FIRE_002、FIRE_004、FIRE_005、FIRE_006与FIRE_007当前处于待试玩状态；FIRE_003与FIRE_008至FIRE_017已由用户明确要求实现，当前处于灰盒中。FIRE_006本次只批准并实现空间灰盒，不包含核心解法、完成目标或出口。

FIRE_018已由用户确认与FIRE_017相连并进入灰盒实现；当前采用FIRE_017右侧出口连接FIRE_018左上入口。

FIRE_019已按用户确认补全底层火球锁存步骤，并与右侧FIRE_020建立双向连接。地图中的FIRE_018—FIRE_019仍表示规划连接，本次未修改FIRE_018 Scene。

FIRE_020已由用户确认进入可玩灰盒实现；右侧完成区只设置`FutureExitAnchor-FIRE021`，FIRE_021尚未批准，因此没有正式出口或场景连接。
