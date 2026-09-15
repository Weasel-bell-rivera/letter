# Git 清理与候选资源引用审计（2026-09-15）

## 本次执行

- 将 `Captures/`、`output/`、`TempAudioCandidates/` 共 285 个已跟踪文件移出 Git 索引，并加入根目录 `.gitignore`。
- 原始目录整体移动到仓库同级 `W1-local-archive/2026-09-15-100111/`，保持目录结构；逐文件 SHA-256 核对通过。此操作清理项目工作目录，不释放归档所在磁盘的空间。
- 音效候选映射及许可证记录保留在 [audio-candidate-provenance](audio-candidate-provenance/README.md)；原始压缩包、图片生成清单、提示词、运行截图和观察记录保留在上述仓库外归档。
- 未提交、未推送、未重写 Git 历史；历史仓库体积不会因此立即缩小。
- 后续获用户明确授权，已将 53 个无引用候选文件及对应 53 个 `.meta` 成对移出项目并移除 Git 跟踪；17 个有引用资源保持原样。已有场景、脚本和玩家攀爬资源修改保留。

## 历史证据查找

房间文档中原来的 `Captures/...` 路径表示当时的证据位置。清理后可在上述归档下使用同一路径查找；新克隆不会包含这份本机归档。
全部原先已跟踪内容也可从清理前提交 `5ee6c7d9dc6bd97a4efc3b3aca0b6724675d34d9` 提取，例如在仓库根目录执行：

```bash
git show 5ee6c7d9dc6bd97a4efc3b3aca0b6724675d34d9:Captures/Fire023WaterHazard/runtime-observations.txt
```

## 检查范围与结论

- 检查已跟踪的 Candidates 目录内容和 Branding 图标，共 70 个实际文件（不把 `.meta` 及文件夹计入素材数量）。
- 扫描 Assets、Packages、ProjectSettings、docs、scripts 的 1741 个当前磁盘文本文件，查找 `.meta` GUID、完整路径和名称；自身 `.meta` 的 GUID 定义不计为外部引用。
- 检查 C# 中 Resources.Load、AssetDatabase.LoadAssetAtPath、FindAssets、Addressables、AssetBundle、目录枚举等入口；没有发现加载这些未引用候选的动态加载路径。候选文件不在 Resources 或 StreamingAssets，未配置 AssetBundle 名称。
- 17 个文件有明确项目引用，约 41.15 MiB：当前应用图标 1 张、敌人图 8 张、Earth_006 使用的环境图 8 张。应保留。
- 初次扫描的 53 个无引用文件约 134.22 MiB；后续经磁盘复查、Unity 依赖检查及用户授权，已全部清理并归档。
- 初次审计仅使用磁盘静态证据；后续清理前增加了 Unity AssetDatabase 和已加载场景内存依赖检查，详见下节。未运行编译、自动测试或人工试玩。

逐项证据见 [JSON 清单](ASSET_CANDIDATE_AUDIT_2026-09-15.json)。其中 `references` 是项目引用，`mentions` 是文档、导入元数据或序列化名称的文本提及，后者不等于加载引用。

## 候选素材清理执行记录

- 范围严格限定为初次清单中 53 个无引用文件，与原始 `.meta` 成对移动，共 106 个文件。
- 仓库外归档：`../W1-local-archive/2026-09-15-130046-unused-assets/`，内部保留完整 `Assets/...` 路径与 GUID；每个文件均经 SHA-256 核对。
- 清理前重新扫描当前磁盘的 1547 个项目文本文件，包括新出现的攀爬资源及未提交修改，没有发现目标 GUID 或代码路径引用。
- Unity 对其余 902 个资源执行递归 `AssetDatabase.GetDependencies` 查询，目标命中 0；对已加载场景根对象执行 `EditorUtility.CollectDependencies`，目标命中 0。
- Editor 当前打开 `Fire_024`，检查时没有未保存编辑，未处于 Play Mode 或编译状态；没有 Prefab Stage。未保存、重载或切换任何 Scene。
- 17 个有引用素材及其 `.meta` 均保留；没有给 Assets 候选目录添加整体忽略规则，后续正式使用的资源仍可正常提交。
- 项目内不再保留这 53 个文件；历史文档中的旧路径可以在上述归档内查找。归档保留制作原图、提示词与清单的追溯价值。
- 文件清单和校验值见 [清理清单](ASSET_CANDIDATE_CLEANUP_2026-09-15.json)。所有清理改动已暂存，尚未提交或推送。

## 有明确引用：保留

| 文件 | 大小 MiB | 引用示例 |
|---|---:|---|
| `Assets/Art/Branding/MagicMirrorAppIconSource-v9-r1.png` | 4.29 | `ProjectSettings/ProjectSettings.asset:304`；`ProjectSettings/ProjectSettings.asset:309` |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/freezable_patrol_walk_a.png` | 3.67 | `Assets/Editor/SnowPrerequisiteBuilder.cs:16`；`Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab:116` |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/freezable_patrol_walk_b.png` | 3.90 | `Assets/Editor/SnowPrerequisiteBuilder.cs:18`；`Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab:142` |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/furnace_toad.png` | 3.76 | `Assets/Editor/HorizontalFireballEnemyBuilder.cs:12`；`Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab:84` |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/horizontal_fireball.png` | 3.37 | `Assets/Editor/HorizontalFireballEnemyBuilder.cs:16`；`Assets/Prefabs/Gameplay/Enemies/Projectiles/HorizontalFireballProjectile2D.prefab:235` |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/vertical_wall_patrol.png` | 3.33 | `Assets/Prefabs/Gameplay/Enemies/VerticalWallPatrolEnemy2D.prefab:202` |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/wind_ray_fly_a.png` | 3.38 | `Assets/Editor/WindRayEnemyBuilder.cs:16`；`Assets/Animations/Enemies/WindRay/WindRayFly.anim:26` |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/wind_ray_fly_b.png` | 3.60 | `Assets/Editor/WindRayEnemyBuilder.cs:18`；`Assets/Animations/Enemies/WindRay/WindRayFly.anim:28` |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/wind_ray_rest.png` | 3.55 | `Assets/Editor/WindRayEnemyBuilder.cs:14`；`Assets/Animations/Enemies/WindRay/WindRayFly.anim:24` |
| `Assets/Art/Generated/Environment/Earth/Candidates/earth-lowtexture-20260901/earth-extreme-far-shaft.png` | 1.28 | `Assets/Scenes/Levels/Earth/Earth_006.unity:3842` |
| `Assets/Art/Generated/Environment/Earth/Candidates/earth-lowtexture-20260901/earth-far-strata-tunnel.png` | 1.28 | `Assets/Scenes/Levels/Earth/Earth_006.unity:579` |
| `Assets/Art/Generated/Environment/Earth/Candidates/earth-lowtexture-20260901/earth-fog-light.png` | 1.07 | `Assets/Scenes/Levels/Earth/Earth_006.unity:4252` |
| `Assets/Art/Generated/Environment/Earth/Candidates/earth-lowtexture-20260901/earth-foreground-left.png` | 0.78 | `Assets/Scenes/Levels/Earth/Earth_006.unity:1022` |
| `Assets/Art/Generated/Environment/Earth/Candidates/earth-lowtexture-20260901/earth-foreground-right.png` | 0.78 | `Assets/Scenes/Levels/Earth/Earth_006.unity:930` |
| `Assets/Art/Generated/Environment/Earth/Candidates/earth-lowtexture-20260901/earth-mid-layered-rock.png` | 0.73 | `Assets/Scenes/Levels/Earth/Earth_006.unity:1381` |
| `Assets/Art/Generated/Environment/Earth/Candidates/earth-lowtexture-20260901/earth-mid-load-pillar.png` | 1.26 | `Assets/Scenes/Levels/Earth/Earth_006.unity:3587` |
| `Assets/Art/Generated/Environment/Earth/Candidates/earth-lowtexture-20260901/earth-mid-timber-support.png` | 1.11 | `Assets/Scenes/Levels/Earth/Earth_006.unity:3294` |

## 已清理：53 个无引用候选文件

| 文件 | 大小 MiB |
|---|---:|
| `Assets/Art/Branding/MagicMirrorAppIconSource-v2.png` | 1.45 |
| `Assets/Art/Branding/MagicMirrorAppIconSource-v3.png` | 1.39 |
| `Assets/Art/Branding/MagicMirrorAppIconSource-v4.png` | 0.82 |
| `Assets/Art/Branding/MagicMirrorAppIconSource-v5.png` | 0.58 |
| `Assets/Art/Branding/MagicMirrorAppIconSource-v6-r1.png` | 3.50 |
| `Assets/Art/Branding/MagicMirrorAppIconSource-v6.png` | 3.72 |
| `Assets/Art/Branding/MagicMirrorAppIconSource-v7.png` | 3.55 |
| `Assets/Art/Branding/MagicMirrorAppIconSource-v8-r1.png` | 3.52 |
| `Assets/Art/Branding/MagicMirrorAppIconSource-v8.png` | 3.32 |
| `Assets/Art/Branding/MagicMirrorAppIconSource-v9.png` | 3.71 |
| `Assets/Art/Branding/MagicMirrorAppIconSource.png` | 2.06 |
| `Assets/Art/Characters/Player/Candidates/boy-silhouette-actions-v1/icon-matched-walk-cycle-8frame-reference.png` | 1.27 |
| `Assets/Art/Characters/Player/Candidates/boy-silhouette-actions-v1/icon-matched-walk-cycle-reference.png` | 1.03 |
| `Assets/Art/Characters/Player/Candidates/boy-silhouette-actions-v1/natural-walk-cycle-reference.png` | 3.77 |
| `Assets/Art/Characters/Player/Candidates/boy-silhouette-actions-v1/player-and-clone-action-reference.png` | 3.96 |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/freezable_patrol_walk_a-chroma-source.png` | 3.31 |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/freezable_patrol_walk_b-chroma-source.png` | 2.71 |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/freezable_patrol_walk_b-retry-chroma-source.png` | 3.48 |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/furnace_toad-chroma-source.png` | 3.37 |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/horizontal_fireball-chroma-source.png` | 3.09 |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/vertical_wall_patrol-chroma-source.png` | 3.05 |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/wind_ray_fly_a-chroma-source.png` | 3.07 |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/wind_ray_fly_b-chroma-source.png` | 3.27 |
| `Assets/Art/Generated/Enemies/Candidates/enemy-silhouette-labnana-20260902/wind_ray_rest-chroma-source.png` | 3.23 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/recovered-alpha/rejected/snow-extreme-far-mountains.png` | 4.59 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/recovered-alpha/rejected/snow-foreground-right.png` | 3.87 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/recovered-alpha/snow-far-ridge.png` | 4.00 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/recovered-alpha/snow-fog-light.png` | 2.17 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/recovered-alpha/snow-foreground-left.png` | 3.90 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/recovered-alpha/snow-mid-ice-wall.png` | 4.13 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/recovered-alpha/snow-mid-sparse-trees.png` | 4.43 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/recovered-alpha/snow-mid-wind-rock.png` | 4.01 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/snow-extreme-far-mountains.png` | 3.98 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/snow-far-ridge.png` | 3.60 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/snow-fog-light-r1.png` | 2.17 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/snow-fog-light.png` | 2.33 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/snow-foreground-left.png` | 3.54 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/snow-foreground-right.png` | 3.49 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/snow-mid-ice-wall.png` | 3.70 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/snow-mid-sparse-trees.png` | 3.88 |
| `Assets/Art/Generated/Environment/Snow/Candidates/labnana-pro-20260902-set-01/snow-mid-wind-rock.png` | 3.56 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/alpha-audit-initial.json` | 0.00 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/alpha-audit.json` | 0.01 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/manifest.json` | 0.00 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/prompts.json` | 0.01 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/wind-extreme-far-mountains.png` | 0.54 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/wind-far-cloud-sea.png` | 1.15 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/wind-fog-light.png` | 1.11 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/wind-foreground-left.png` | 0.84 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/wind-foreground-right.png` | 0.68 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/wind-mid-floating-rock.png` | 1.08 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/wind-mid-weathered-pillar.png` | 0.84 |
| `Assets/Art/Generated/Environment/Wind/Candidates/wind-silhouettes-20260901-a/wind-mid-weathered-wall.png` | 0.42 |
