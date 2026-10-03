# 英语花体 r7 与自然度抽查

本轮按用户选定的推荐搭配，英文对白使用 **Great Vibes**，章节标题及共用 Batang 字库使用 **Berkshire Swash**。普通界面继续使用 Source Sans 3，简体和繁体载荷保持原样。版本号仍为 0.5.3，通过载荷哈希识别同版本更新。

对白增加独立的字体、材质和纯授权字体图集，由玩家本地原始资源模板生成，16 个确认的对白组件改为指向该字体。对白字号 48、字距 0、行距 -18；花体需要更大的字号来保持小写字母的可读性。标题保留已有组件字号，使用字形 scale 保持大写字母高度，连同字形边缘的采样 padding 一起缩放，避免挤字。

`花体方案.png` 是实际输出字形、材质与对白配置的隔离排版预览，背景是示意。测试框为 1180×170；游戏动态布局和实际游玩仍待玩家验收，不能视为游戏截图或所有对白排版的保证。

英语翻译此前已经完成 r4 全量优化，r5 独立复核关闭三项 P2。r5 的复核重点为修订范围及工程一致性，不等同于英语母语编辑的全剧情通读。本轮每隔 240 组抽读一组（45 组），并阅读三处发现问题的前后各两组，共 57 个不同对白组。大多数抽样句子已经自然、简洁；仍发现三处生硬表达并作窄范围润色，详见 `naturalness-edits.json`。因此可以确认明显直译已有大幅改善，但不承诺每句都完全没有机器翻译感。

三处修改保留韩文信息、专名与控制指令：

- “I don't expect understanding” 改为更口语化的 “I don't ask you to understand”。
- “Machiavelli departing ... is an ominous development” 改为两个完整、便于朗读的句子。
- “you are still the same, Mr. Scholar” 改为 “you haven't changed either, my learned friend”，保留原文带调侃的学者称呼。

`naturalness-samples.json` 保存本轮分布式抽样；`naturalness-edits.json` 保存精确地址、原文和改前改后。审阅包还保留 r3/r4/r5/r6 的来源与既有证据，可追溯此前全量文本优化。此抽查不是重新逐句审读全部 10,654 剧情组。

工程验证读取全部 22,507 个文本字段，并核对字体表、授权图集、16 个组件引用、21 个资源重建、15 个 Unity 原生 CRC、r6 同版本升级、三语切换及隔离原版还原。具体通过状态以 `final-verification.json` 为准。

源码和载荷已更新到当前工程；工作台从当前工程生成的安装包会包含本轮修改。未安装到真实游戏，未提交或发布 GitHub。第三方字体遵循 SIL OFL 1.1，安装包包含完整许可；审阅包不包含原始游戏资源或完整游戏图集。

解压安装包后运行 `Zephyr-Chinese-Patcher/ZephyrChinesePatcher.exe`，选择 English。校验解压内容运行 `python verification/verify_manifest.py`。完整审阅包含当前源码、载荷、字体源文件、验证脚本与证据。
