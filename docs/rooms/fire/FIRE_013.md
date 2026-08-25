# FIRE_013：双门抉择

- 状态：灰盒中；用户已批准实现当前地图中可落实的后续房间。
- Scene：`Assets/Scenes/Levels/Fire/Fire_013.unity`。
- 连接：`FIRE_009`、`FIRE_014`、`FIRE_015`，不改变`docs/maps/MAP.md`。
- 定位：分支枢纽与双门练习；固定单屏，Grid `1×1`，边界`X[-15,15] Y[-7,7]`，正交尺寸`7`。

## 布局与解法

```text
FIRE_014 ◀ E ─ D-A ─── [B] ─ M/P ─ [A] ─── D-B ─ E ▶ FIRE_015
```

Player与MirrorClone分居镜面两侧：右侧`Plate-A`持续开启左门，左侧`Plate-B`持续开启右门。玩家根据目标分支选择站位与镜子放置时机；中央短阶通向返回`FIRE_009`的上层出口。

## Prefab与重置

- `Plate-A (4,-1.7)`控制`Door-A (-6,-1.5)`；`Plate-B (-4,-1.7)`控制`Door-B (6,-1.5)`；两块板都位于两门之间，避免闭环软锁。
- 两门、两板和三个`RoomExit2D`均使用现有通用Prefab；无敌人、Spawner、岩浆或房间脚本。
- Player死亡、手动重置和重新进入时恢复两门关闭、两板弹起；MirrorClone消失立即释放占用。
- 风险：未人工试玩，双分支的到达容错待验证。
