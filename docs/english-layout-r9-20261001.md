# 英语排版与界面字体 r9

本轮对应用户提供的五张实机截图：开场叙述与日记行间重叠、队伍姓名挤压称号、Gameplay / Controls 越出按钮，以及界面字体过粗且字距偏紧。用户已选择预览右侧的 Inter Regular。

## 修改

- 英文对话和章节保留 Libre Baskerville Regular（400）。独立对话字体及 16 条字体引用保持 r8 原样。
- 修正共享书籍字体的行高、升部、降部等度量：字体度量与字形使用同一单位，保留原字号单位及实际大写字高。原有字形、纹理和字形引用均保留。
- 开场保持居中，正文 48 号、自然字距、略增行距；正文及阴影副本一起调整。
- 日记自然字距、略增行距，字号上限 32，下限 20，较长内容自动缩小。
- 人物姓名限制单行，放大上限不超过原基础字号，最低 18；称号限制单行，14–20 号。长姓名按现有文字区自动缩小。
- 普通界面改用 Inter Regular（400，光学字号 14），使用 SDF3、Bitmap1、Bitmap9 三种与实际材质对应的字形，恢复负字距处的自然字距。
- Gameplay / Controls 采用首字母大写，限制单行并按按钮宽度自动缩小；三处窄列 Weapon Durability 同样限制单行并自动缩小。

全部 22,507 个英文翻译字段与 r8 一致，简体、繁体的载荷和对白目录保持一致。没有修改真实游戏，也未发布 GitHub。

## 验证与边界

`build-verification.json`：21 文件独立重建、15 个原生 Unity CRC/强制加载、22,507 字段回读、7 个字体对象与纹理回读、931 个文字组件设置回读。

`proof/verification.json`：308 个核心排版用例全部通过，包含全部 282 条日记、截图中的两段开场、五种长短姓名、称号、设置标题、11 个菜单/技能文字，以及 Bitmap 材质检查。没有行间字形重叠或越出文字区。Roberto 姓名单行，与称号的可见字形间距约 9.27 个画布单位。

另测量 852 个已修改界面的静态文字样本，对照 r8 没有新增越框。报告保留 64 项修订前已存在的范围诊断，包含开发占位文字、显式负边距和伸展布局参考尺寸；这些不能直接等同于真实游戏中的显示缺陷。完整测量和组件路径在 `proof/` 下。没有以这些占位样本代替游戏内验收。

排版采用实际载荷的字形、纹理、材质及组件设置，在隔离 Unity 画布中测试。字体对比图由真实字体文件绘制。它们均不是真实游戏截图；游戏脚本更新文字、动画、分辨率和实际背景的最终显示仍由游戏内验收确认。

`installer-verification.json`、`frozen-runtime-verification.json`：安装器内的 16 个载荷文件、三语目录与嵌入资源；r8 同版本升级；English → 简体 → 繁体 → English 切换；隔离原版还原识别。`final-verification.json` 汇总完成后的结果。

## 使用

解压安装包中的 `Zephyr-Chinese-Patcher`，运行 `ZephyrChinesePatcher.exe`，选择 English。版本仍为 0.5.3，更新通过载荷哈希识别。关闭游戏后安装，再重新启动游戏检查上述四个场景。

工作台使用 `Zephyr-Chinese-Patcher/payload/en` 当前载荷生成安装包，可包含 r9；此交付包也已经从当前源码和载荷重新构建。

## 审阅导航与复现

- `preview/界面字体对比.png`：用户选定的界面字体方案；`preview/verification.json` 为字体加载和文字适配检查。
- `layout-changes.json`、`font-changes.json`：确切组件和字体修改清单。
- `proof/`：原设置/新设置的实际字形排版、范围记录、UI 对照检查。`.alpha` 原游戏纹理和 Unity 资源文件不进入审阅 ZIP。
- `fonts/`、`font-sources.json`、`static-font-derivation.json`：官方字体、许可、来源哈希与更名的静态衍生字体。
- `evidence/en-patch.json.gz` 与 `evidence/glyphs.before.zip`：r8 英文输入快照。重做候选生成时先恢复这些输入及 `baseline.json` 所列源码状态；避免在 r9 载荷上重复追加。
- `setup.py`、`ClassicEnglishFont.cs`、`make_candidate.py`：生成授权字体及候选。共享书籍字体度量来自审阅包内 r8 的独立对话字体记录。
- `ClassicEnglishProof.cs`、`expand_ui_proof.py`、`verify_proof.py`：实际字形排版与对照检查。
- `build.py`、`check_installer.py`、`test_frozen.py`、`verify_final.py`、`package.py`：资源重建、安装器验证、隔离升级/三语切换及带逐文件校验清单的封装。
- 审阅 ZIP 的 `workspace/` 包含当前补丁工程、三语载荷、历轮英语源码/语料/证据及工作台重建依赖；不包含真实游戏资源。

解压后运行 `python verification/verify_manifest.py` 复核逐文件 SHA-256；ZIP 的旁置 `.sha256` 文件记录整个档案的哈希。
