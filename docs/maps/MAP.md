# 世界房间连通图

- 连线仅表示两个房间之间存在通路，不代表强制访问顺序。
- 房间在图中的左右、上下位置仅用于表达连通关系，不代表先后、难度或推荐路线。
- Center是教学区，`CENTER_001`是新游戏起点。
- 本文件只定义世界结构和房间连接，不定义房间内部布局，也不直接对应Unity Tilemap；正式房间内部的静态地形与空间布局必须按 `docs/LEVEL_DESIGN.md` 和 `docs/systems/LEVEL_GEOMETRY_SYSTEM.md` 使用标准Grid与Tilemap设计、制作。

```text
                                      [WIND_001]                                              [SNOW_001]
                                           │                                                       │
                                      [WIND_002]                                              [SNOW_002]
                                           │                                                       │
          [WIND_006]─[WIND_005]       [WIND_003]                                              [SNOW_003]
                         │                 │                                                       │
                    [WIND_007]        [WIND_004]─[WIND_008]                                 [SNOW_004]
                         │                 │          │                                            │
[WIND_012]─[WIND_011]─[WIND_010]─[WIND_009]─[WIND_013]─[WIND_014]     [SNOW_007]─[SNOW_006]─[SNOW_005]
                                                           │                 │          │
                                                      [WIND_015]─[WIND_016]─[CENTER_001]─[CENTER_002]─[CENTER_003]─[SNOW_008]─[SNOW_009]
                                                           │                                       │          │
                                                      [WIND_017]                              [SNOW_011]─[SNOW_010] [SNOW_012]
                                                           │                                       │          │
                                                [WIND_019]─[WIND_018]                        [SNOW_013]─[SNOW_014]─[SNOW_015]
                                                           │                                                                │
                                                [EARTH_002]─[EARTH_001]                                            │
                                                           │                                                        │
                                                      [EARTH_003]      [CENTER_004]─[CENTER_005]─[CENTER_006]─[CENTER_007]─[CENTER_008]
                                                           │                                                   │          │
                                                      [EARTH_004]                                         [CENTER_009] [FIRE_001]
                                                           │                                                              │
                                                      [EARTH_005]                                                [FIRE_003]─[FIRE_002]
                                                           │                                                              │
                               [EARTH_009]─[EARTH_008]─[EARTH_007]─[EARTH_006]─[EARTH_010]                        [FIRE_004]
                                                           │                   │                                          │
                               [EARTH_013]─[EARTH_012]─[EARTH_011]             [EARTH_014]                   [FIRE_006]─[FIRE_005]
                                                           │                   │                                          │
                                                      [EARTH_015]         [EARTH_016]                                [FIRE_007]
                                                           │                                                              │
                                                      [EARTH_017]                                                 [FIRE_008]
                                                           │                                                              │
                                                      [EARTH_018]                 [FIRE_012]─[FIRE_011]─[FIRE_010]─[FIRE_009]─[FIRE_013]─[FIRE_014]
                                                                                                                       │
                                                                                                                  [FIRE_015]
                                                                                                                       │
                                                                                                                  [FIRE_016]
                                                                                                                       │
                                                                                                                  [FIRE_017]─[FIRE_018]─[FIRE_019]
```
