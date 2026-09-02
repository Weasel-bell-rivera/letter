# 本地化系统

本文档是 W1 语言覆盖、字符串回退、语言偏好和字体覆盖的权威来源。

## 首发语言与选择规则（本任务已批准）

- 正式首发语言为英语 `en` 与简体中文 `zh-Hans`。
- 英语是源语言和最终回退语言。
- 首次启动仅在操作系统语言明确为简体中文时选择 `zh-Hans`；其他系统语言选择英语。
- 用户显式保存的受支持语言选择优先于操作系统语言。
- 不支持的语言标识不得改变当前选择；查不到目标语言字符串时回退英语，英语也缺失时显示稳定 key 并记录诊断。
- 语言偏好使用独立的本地键 `w1.locale`，不写入长期进度存档，也不耦合显示、音频设置模型。

## 数据与运行时接口

字符串表位于 `Assets/Localization/Resources/Localization/strings.<locale>.json`。所有玩家可见字符串使用稳定 key；两个正式语言表必须具有相同且非空的 key 集合。

`LocalizationService.Get(key)` 获取当前语言字符串；`SetLocale(locale)` 校验并保存选择；`LocaleChanged` 供已显示文本即时刷新；`Format(key, args)` 使用当前语言对应的明确文化信息格式化数字和参数，不能依赖进程当前文化。

当前覆盖清单包括原型运行提示、房间出口、暂停/退出菜单，以及显示、音频和无障碍设置项（文字大小、高对比度、减少动态效果与开关状态）。暂停设置页提供可聚焦的语言按钮，可在 `en` 与 `zh-Hans` 间切换并立即持久化；该偏好独立于设置页的“应用/取消”事务。`PauseSettingsPanel.SetLocale` 提供同一套可访问的编程接口。

## 字体契约

- 运行时字体必须作为项目资源随包分发，不得调用操作系统字体或假设设备安装了某字体。
- 唯一约定资源路径为 `Resources/Localization/W1UIFont`。
- 字体必须提供许可/来源记录，并验证所有正式字符串使用到的拉丁字符、标点与简体中文字符。
- `LocalizedFontProvider.Covers` 是逐字符静态/编辑器验证入口。缺失字体时运行时明确报错，世界空间出口文字停止渲染，不能静默绑定平台默认字体。
- 当前捆绑字体为 Noto Sans CJK SC Regular，来自官方 `notofonts/noto-cjk` 仓库的 `Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf`，按 SIL Open Font License 1.1 再分发。原始许可、来源路径与 SHA-256 记录在字体旁的 `OFL-1.1.txt` 和 `NOTICE.md`。

## 验证要求

- 低成本 EditMode 测试验证双表 key 对等、非空、语言归一化和回退。
- 当前两张正式字符串表已经过 HarfBuzz 字形扫描，Noto Sans CJK SC Regular 的缺失字形数为 `0`；新增或修改字符串后仍须重新扫描。
- 应在 `en`、`zh-Hans` 下运行检查截断、换行和焦点状态；多分辨率与无障碍验收由各自权威文档负责。
- PlayMode、完整 EditMode、完整编译、构建和全量重导入仍需当前任务的明确批准。
