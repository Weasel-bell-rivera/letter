# Unity level rules

本目录中的工作必须遵守项目根 `AGENTS.md`、`docs/LEVEL_DESIGN.md`、对应区域文档和对应房间文档。

- 每个正式 `.unity` Scene代表一个房间。
- 每个房间Scene必须具有对应的 `docs/rooms/` 设计记录。
- 只有状态为“已批准”或“灰盒中”的房间允许制作或修改灰盒。
- 通用玩法逻辑不得写死在具体Scene中；门、压力板、复活点、危险物和移动对象优先使用通用Prefab。
- 新房间先使用基础Sprite和Collider验证玩法，再加入正式美术。
- 不得删除或破坏Unity `.meta` 文件。
