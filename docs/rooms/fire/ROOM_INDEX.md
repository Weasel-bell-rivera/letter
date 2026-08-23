# 火之区域房间索引

本文档是火之区域房间编号和批准状态的权威索引。房间文档存在不代表该房间已经批准进入正式开发。

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
| FIRE_002 | 已批准 | `docs/rooms/fire/FIRE_002.md` | `Assets/Scenes/Levels/Fire/Fire_002.unity` | 教学镜像与岩浆 |
| FIRE_003 | 已批准 | `docs/rooms/fire/FIRE_003.md` | `Assets/Scenes/Levels/Fire/Fire_003.unity` | 只教学周期喷发 |
| FIRE_004 | 已批准 | `docs/rooms/fire/FIRE_004.md` | `Assets/Scenes/Levels/Fire/Fire_004.unity` | 教学镜像、压力板和门 |
| FIRE_005 | 已批准 | `docs/rooms/fire/FIRE_005.md` | `Assets/Scenes/Levels/Fire/Fire_005.unity` | 组合FIRE_001至FIRE_004 |
| FIRE_006 | 待试玩 | `docs/rooms/fire/FIRE_006.md` | `Assets/Scenes/Levels/Fire/Fire_006.unity` | 上下双层空间灰盒；T为FIRE_005出生点 |
| FIRE_007 | 待试玩 | `docs/rooms/fire/FIRE_007.md` | `Assets/Scenes/Levels/Fire/Fire_007.unity` | 永久锁存双压力板门控组灰盒 |
| FIRE_008 | 灰盒中 | `docs/rooms/fire/FIRE_008.md` | `Assets/Scenes/Levels/Fire/Fire_008.unity` | 三段地形错位与双压力板组合房 |
| FIRE_009 | 占位 | `docs/rooms/fire/FIRE_009.md` | 尚未创建 | 待设计 |
| FIRE_010 | 占位 | `docs/rooms/fire/FIRE_010.md` | 尚未创建 | 待设计 |
| FIRE_011 | 占位 | `docs/rooms/fire/FIRE_011.md` | 尚未创建 | 待设计 |
| FIRE_012 | 草案 | `docs/rooms/fire/FIRE_012.md` | `Assets/Scenes/Levels/Fire/Fire_012.unity` | 镜像机制组合房 |
| FIRE_013 | 占位 | `docs/rooms/fire/FIRE_013.md` | 尚未创建 | 待设计 |
| FIRE_014 | 占位 | `docs/rooms/fire/FIRE_014.md` | 尚未创建 | 待设计 |
| FIRE_015 | 占位 | `docs/rooms/fire/FIRE_015.md` | 尚未创建 | 待设计 |

FIRE_001、FIRE_006与FIRE_007当前处于待试玩状态；FIRE_002至FIRE_005已获批准，可以制作灰盒；FIRE_008已由用户明确要求实现，当前处于灰盒中；FIRE_012仍为草案，在明确批准前不得实现。FIRE_006本次只批准并实现空间灰盒，不包含核心解法、完成目标或出口。
