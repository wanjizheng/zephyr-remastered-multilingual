# 更新日志 / Changelog

## [Unreleased]

### 中文

- 修正希尔弗告别船员时的“傻老幺”台词：她是在拜托其他船员照顾刚才说话的年轻同伴。重写了她败给西拉诺后的追随承诺，以及再次提起承诺时的整句台词，明确表达“追随至死不渝”。已同步简体中文、繁體中文和 English。

### English

- Clarified Silver's farewell request: she asks the other crew members to look after the youngest member who had just spoken. Rewrote her vow after losing to Cyrano and her later reaffirmation to convey a lifelong commitment to follow him. Updated in Simplified Chinese, Traditional Chinese, and English.

## [0.4.1] - 2026-09-21

### 中文

- 修复英文版地图探索中 F9 自动推进快捷键提示在固定胶囊内换行、溢出的问题。提示改为简短的 **Auto**，功能不变。

### English

- Fixed the English F9 auto-advance shortcut label wrapping outside its fixed pill during field exploration. Its label is now the shorter **Auto**; functionality is unchanged.

## [0.4.0] - 2026-09-21

### 中文

- 新增繁體中文与英文翻译，并在安装器右上角用 CN／TW／EN 切换工具界面和要安装的语言包；简体中文的既有修改继续保留。**繁体只把简体字转为繁体字，没有按繁体口语习惯改写。英文因时间有限，由 GPT 根据简体译文中已确定的人物关系与其他信息直接进行 AI 翻译。**
- 检查并补齐繁体字形；英文界面与对话采用许可的 Source Sans 3 / Source Serif 4，并针对已发现的设置、按钮、任务文字问题做局部排版和字母间距调整。**英文的字体与界面尚未全面优化。**
- 修复三语言共享基础攻击错误附带人物姓名的问题；英文基础攻击改用不会在战斗菜单被截成相同末词的名称。将英文“读取”改为 Load。
- 感谢 [@ElysiumWorks](https://github.com/ElysiumWorks) 在 [Issue #1](https://github.com/wanjizheng/zephyr-remastered-zh-cn/issues/1) 指出技能名前缀人物姓名的问题。本版已修正这项反馈；该 issue 中的其他译名建议仍单独评估。
- 安装进度随所选界面语言变化；缩短安装临时与回滚路径，规避 Windows 默认路径长度导致准备资源结束后失败的问题。
- 移除手动本地恢复和强制恢复；恢复原版改用 Steam 验证。安装失败自动回滚，以及更新和切换语言所需的原版备份继续保留。
- 已通过隔离副本中的三语言安装与切换、英文资源完整重建、安装异常自动回滚、打包引擎验证。实际游戏显示和不同 Windows 10 路径仍欢迎玩家及时反馈任何问题。视频字幕暂未处理。

### English

- Added Traditional Chinese and English translations. The CN / TW / EN buttons at the top right select both the patcher's interface language and the game translation to install. All existing Simplified Chinese changes remain. **Traditional Chinese only converts Simplified characters; its wording was not adapted to regional speech. Due to time constraints, GPT translated the English version directly from the Simplified Chinese text, relying on the character relationships and other information established there.**
- Checked Traditional Chinese glyph coverage. English UI and dialogue use licensed Source Sans 3 / Source Serif 4 fonts. Specific reported layout and letter-spacing issues were adjusted, but **the English fonts and interface have not received comprehensive refinement**.
- Removed incorrect character-name prefixes from shared basic attacks in all three languages. English attack labels now stay distinct in the battle menu. Corrected the English Load menu label.
- Thanks to [@ElysiumWorks](https://github.com/ElysiumWorks) for reporting character-name prefixes on skills in [Issue #1](https://github.com/wanjizheng/zephyr-remastered-zh-cn/issues/1). This release fixes that issue; the separate character-name suggestion in the same report remains under review.
- Localized installation progress. Shortened temporary and rollback paths to address failures after resource preparation under Windows' default path-length limit.
- Removed manual local and forced restore. Use Steam verification to restore the original game. Automatic rollback on installation failure and the original-file backup needed for updates or language switching remain.
- Verified three-language installation and switching, a complete English resource rebuild, automatic rollback after an injected failure, and the packaged engine on isolated copies. Please report any problems promptly, especially in-game display or Windows 10 installation issues. Video subtitles remain outside the patch scope.

## [0.2.0] - 2026-09-21

### 中文

- 发布全新 Material Design 界面，显示 Steam Build、支持版本与备份状态；主程序和引擎分别打包依赖。
- 当时版本提供 Steam 验证和旧备份强制恢复，并改进 Steam 验证后的状态刷新。**v0.4.0 已移除旧备份手动恢复入口。**
- 修正原版模式提示、人物姓名及中文字形位置，并调整对话人物名样式。
- 发行包只保留运行文件与许可证；详细历史见 [v0.2.0 发布说明](https://github.com/wanjizheng/zephyr-remastered-zh-cn/releases/tag/v0.2.0)。

### English

- Introduced the Material Design interface with Steam build, supported-version, and backup status. Shipped the app and engine with their runtime dependencies.
- This historical release offered Steam verification and a forced restore from an old backup, plus improved status refresh after Steam verification. **The manual backup restore option was removed in v0.4.0.**
- Corrected original-mode prompts, character-name and Chinese glyph positioning, and dialogue speaker styling.
- Reduced the release ZIP to runtime files and license notices; see the [v0.2.0 release notes](https://github.com/wanjizheng/zephyr-remastered-zh-cn/releases/tag/v0.2.0) for details.

## [0.1.0] - 2026-09-20

### 中文

- 首个正式版本：简体中文剧情与界面翻译、朱雀仿宋字体、原版资源备份和签名更新检查。
- 用户验证可安装汉化并恢复原版；完善路径显示、使用说明和人物译名对照。

### English

- First stable release: Simplified Chinese story and UI localization, Zhuque Fangsong font, an original-file backup, and signed update checks.
- Player-tested installation and restore; improved path display, usage instructions, and character-name cross-references.

[0.4.1]: https://github.com/wanjizheng/zephyr-remastered-zh-cn/releases/tag/v0.4.1
[0.4.0]: https://github.com/wanjizheng/zephyr-remastered-zh-cn/releases/tag/v0.4.0
[0.2.0]: https://github.com/wanjizheng/zephyr-remastered-zh-cn/releases/tag/v0.2.0
[0.1.0]: https://github.com/wanjizheng/zephyr-remastered-zh-cn/releases/tag/v0.1.0
