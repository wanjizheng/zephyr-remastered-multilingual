# 对白校对助手 v4 · 说明对话漏检修复

## 开始使用

打开包内 `Zephyr-Chinese-Patcher/ZephyrChinesePatcher.exe`，在安装器选择 CN / TW / EN 和游戏目录，然后点击“对白校对”（英文为 Dialogue review）。校对窗口直接继承目录与语言，不再重复显示目录选择区。安装器切换语言时，已打开的校对窗口同步切换，原语言草稿保留。

根目录 `ZephyrDialogueAssistant.exe` 是独立测试入口；默认参考安装记录选择语言，没有记录时默认简体。可用 `--language en`、`--language zh-Hant`、`--language zh-Hans` 和 `--game <目录>` 指定上下文。每个校对窗口仅维护本次会话，不加载旧草稿。关闭前请导出；重新打开为空。

## 说明对话栈补充读取（2026-09-22）

实机场景 fltka01 的 @kfltka01/117（“剛才那位已經解釋過前面的內容了嗎？”）在截图停留时，没有运行中的 EventInfo 命令，原探针因而漏检。新增读取 UITextBoxBase 维护的对话框栈，已在同一个游戏进程成功读出完整正文。

只处理栈内未关闭的 UITextBoxBalloon / UITextBoxBottom，先保留已有命令索引结果，再补充未捕获的窗口。补充路径按资源分支、当前场景、完整可见正文，在嵌入的三语言素材包中唯一匹配 key；拒绝片段、不同场景、不同分支及多 key 歧义。不会猜测命令地址。导出的 Sample.KeyOrigin 区分 active_command_index 和 unique_full_visible_text_match_in_current_field_catalog。

这条补充路径需等待文字完整显示；若安装文本与内置素材包不同，或有多条相同正文，则不关联。没有宣称所有对话类型或快速跳句均已覆盖。仍然仅以只读权限访问游戏，不写游戏或存档。

## 本轮修复

- 同一场景/key、目标语言及语言包指纹只保留一条记录。打字动画只更新运行时捕获，不追加历史行。
- 右侧“语言包原文”来自所选语言素材包的完整译文，不依赖游戏已经显示多少字。三语言内容由安装器现有、通过哈希校验的 patch.json.gz 操作生成，没有重新机器翻译。
- 顶部“游戏实况”仍展示真实内存文本，可能与所选校对语言不同。不会把运行时中文标记为英文原文。
- 缺少对应语言条目时明确显示缺失及运行时片段，不冒充目标语言译文。资源分支、key 与对象坐标仍保留。
- 编辑时固定选中句；切句、切换语言均不覆盖已有建议。历史列表按当前语言筛选，左上无限保留本次已修改条目，左下仅保留最近 20 条未修改台词；两个列表独立滚动。
- 界面支持简体、繁体、英文，采用安装器深蓝/金色体系、双栏布局、可展开资源详情。游戏原文不会被 UI 翻译器二次翻译。

## 本次会话

窗口不读写历史草稿；关闭后清空。旧磁盘备份保留但不会被加载或带入导出。只导出有建议译文或修改说明的条目。切换语言保留当前会话中其他语言的修改。

## 导出与提交

修改只保存在本机，不改游戏、不重打包译文。导出 ZIP 含结构化 JSON、Issue 正文与 SHA-256 清单；支持同包包含多语言建议，各条独立标记。

schema_version=2，顶层 `target_languages`，每条包含：

- `Language`：目标语言（zh-Hans / zh-Hant / en）。
- `LanguageEvidence`：安装器选择、旧版唯一匹配或用户确认。
- `CatalogVersion`、`PayloadHash`：目标语言包版本和原始 patch.json.gz 的 SHA-256。
- `OriginalRawText`、`OriginalText`、`OriginalTextHash`：目标原始条目、常用显示标记处理后的正文、原始条目 SHA-256。
- `Sample`：实际游戏捕获的文本、姓名、FieldId、Key、Prefix、GameHash、时间及定位证据。
- `RuntimeLanguage`：安装记录中的语言，来源是安装记录，不代表运行内存已经证明其语言。
- `Resource`：Bundle、SerializedFile、ObjectPathId（字符串）、ObjectName、FieldPath、RecordId、SourceHash。
- `SuggestedText`、`Note`、`EditedAt`、`NeedsReview`、`LegacySuggestions`。

维护者应使用目标语言 + 资源 key/坐标 + 语言包/原文哈希匹配，不能仅凭 key 或猜测语言直接应用。建议范围为可见正文，`[姓名]`、`/n`、`/25d` 等控制语法应另行审查保留。

“预览提交”只复制内容并打开 GitHub 网页，需用户自行附加导出文件并提交。Issue Template 已在源码更新，未发布；没有自动上传或创建 Issue。

## 验证边界

本轮实机补验 NPCTalk3_ThenObject（fl05_s03/010 与 /013），保留此前 NPCTalkFace3_ThenObject / NPCTalk_ThenObject 的验证。其他已加入的直接对话框引用变体仅完成静态调用链核对，尚待各自实机验收。没有直接对话框引用的类型仍显示未支持，不猜测共享窗口归属。GameAssembly 哈希不匹配停止。读取权限仅查询与 ReadProcessMemory，无注入、断点或游戏写入。

当前为 100 毫秒轮询，快速跳过可能漏句；本轮修复不等于逐事件无遗漏采集。资源坐标来自索引，需在应用译文时再次核对安装资源。本地生成的语言包目录不是游戏显示内容的实时证明。

## 构建

需要 Windows x64 / .NET 10 SDK，玩家无需安装 Python 或 .NET。

```
dotnet run --project tests/dialogue/DialogueTests.csproj -c Release
dotnet run --project tests/UpdaterTests.csproj -c Release
dotnet publish app/ZephyrPatcher.csproj -c Release -r win-x64 --self-contained true -o C:/Build/ZephyrPatcher
```

独立入口添加 `-p:AssemblyName=ZephyrDialogueAssistant`。完整安装器的 tools/payload 使用本项目既有受信任文件，本轮没有重新生成语言包或安装引擎。

定位索引仍可用 build_dialogue_index.py 生成；三语言校对文本用 `python scripts/build_dialogue_catalogs.py --payload <安装器的payload目录>` 重建。程序中已嵌入生成结果。

`--proofreader-preview <PNG> --language <语言>` 在隔离临时目录验证编辑固定、打字去重、跨语言切换与草稿恢复，不改用户草稿。
