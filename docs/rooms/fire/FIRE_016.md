# FIRE_016：双门接力

- 状态：灰盒中；Scene：`Assets/Scenes/Levels/Fire/Fire_016.unity`。
- 地图连接：`FIRE_015—FIRE_016—FIRE_017`；本房位于链路中段。
- 固定单屏，Grid `1×1`，边界`X[-15,15] Y[-7,7]`，正交尺寸`7`。

```text
FIRE_017 ◀ E ─ D-A ─ [B] ─ P/M ─ [A] ─ D-B ─ E ▶ FIRE_015  ← H
```

两块中央压力板交叉控制两侧门：右板开左门，左板开右门。Player和MirrorClone必须同时分居两侧维持目标门，右侧投火者提供镜像替身压力。两门上方均由静态Terrain封闭。

- `Plate-A (3.5,-1.7)`控制`Door-A (-8,-1.5)`；`Plate-B (-3.5,-1.7)`控制`Door-B (8,-1.5)`。
- 固定投火者`(12.5,-1.5)`向左；出口分别连接`Fire_017/DEFAULT`与`Fire_015/DEFAULT`。
- 使用标准Tilemap和现有Prefab；无Spawner、房间脚本或未批准机制。
- Player中弹完整重置；MirrorClone中弹只释放占板并回收镜子。未人工试玩，双门切换容错待验证。
