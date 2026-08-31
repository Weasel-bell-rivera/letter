# 雪区剪影素材

使用内置 image_gen 逐张独立生成。此目录仅保留 8 张 PNG；不拼接展示板，不包含 Unity Scene 修改。

## 文件

- 01-fog-light-base.png
- 02-extreme-distance-mountains.png
- 03-distant-ridge.png
- 04-midground-wind-rock.png
- 05-midground-ice-wall.png
- 06-midground-sparse-grove.png
- 07-left-foreground-rock-snow.png
- 08-right-foreground-rock-snow.png

## 验证与尚未达标项

- 已通过 PNG 解码完整性检查。01 为不透明 RGB；02–08 均为 RGBA，具有真实零 Alpha 空白与半透明像素，不是棋盘格假透明。
- 全套较早期版本减少细碎表面纹理，以轮廓和大明暗面为主。
- 05、07、08 的轮廓外围仍有柔光和低 Alpha 残留。清理过程中生成了烘焙棋盘格的 RGB 图片，这些修订图未被采用或放进交付目录。
- 部分素材的极低 Alpha 像素触及画布边缘，尚未完全满足四周干净透明余量。
- 01 实际为 1672×941，接近但并非数学上精确的16:9。没有擅自裁切或拉伸。
- 仅检查独立图片，未进行 Unity 导入、组合叠加、排序和运行截图。没有修改玩法、房间或资源导入设置。

详细像素尺寸与 Alpha 检查见 alpha-check.json；逐张完整生成提示词与原始来源见 manifest.json。

## 未采用的清边尝试

Remove all glow, halo, haze and shadow OUTSIDE this object's silhouette. The background must be genuinely transparent, not black, white or checkerboard. Preserve only the complete solid object including its snow/ice and its current simple shadow-silhouette style and colors. Do not add any texture or detail. Zoom out slightly so the ENTIRE outer shape is uncut and surrounded on ALL FOUR sides by completely empty transparent padding. Keep its left/right placement and original canvas orientation. Export actual RGBA PNG with alpha=0 outside the cleaned silhouette, not an illustration of transparency.

第二次去背指令：Make the background transparent. Remove the checkerboard. Keep the object unchanged. Transparent PNG.

这些尝试没有得到全面优于已保存素材且满足全部透明要求的结果，因此保留了已验证真实 Alpha 的版本。

