# 本地视频语言包（0.4.7）

已接入 060、083、109。维护源位于相邻项目的 `translation/full_localization_v1/media/video/`，文件名为 `060.zh-Hans.webm`、`060.zh-Hant.webm`、`060.en.webm`，另两段同理。无语言后缀的文件始终保留为原片。060 英文原本就是英文，英文补丁从原版 bundle 重建并保留该片，不追加重复数据。

## 安装与切换

界面已有 zh-Hans / zh-Hant / en 选择。各语言 `patch.json.gz` 包含自己的 `video_bundle` 操作，`videos/` 提供该语言片源。引擎从经过校验的原版 movie bundle 重建，追加目标片源并更新对应 VideoClip 的 offset / size；其余元数据、对象和原始数据流保留。catalog 的 CRC、文件大小和 catalog.hash 同步更新。movie bundle 纳入原有备份、事务回滚与语言切换机制；升级旧补丁时自动补备份该新增目标。

## 维护流程

1. 将审核通过的 WebM 按 `<编号>.<语言>.webm` 放入维护源目录。
2. 执行 `scripts/prepare_video_payloads.py`（参数见 `--help`），使用经哈希确认的原版备份和 movie bundle，在新的 work 目录重建候选。不会写入游戏安装。
3. 对 `crc.request` 列出的候选运行 Unity 原生 CRC 提取。现有维护工程的 `DialogueStyleR2Crc.Run` 支持环境变量 `ZEPHYR_CRC_ROOT` 指向 work。
4. 执行 `scripts/seal_video_payloads.py --work <work>`，读取 CRC 证据，核对内嵌片源及非目标对象，更新 catalog 和发行数据固定哈希，保留旧 payload。
5. 使用 `scripts/build.ps1` 构建。构建前会核对所有语言的片源与固定哈希；缺文件会停止，避免生成缺视频的安装包。安装程序不读取维护源目录，而是使用随包携带的各语言 payload。

## 分发边界

这些 WebM 含游戏画面与音轨，是本地使用资源，已用 .gitignore 排除。当前本地含视频构建会携带这些片源，不能继续宣称该构建“不含游戏视频资源”。公开源码中不提交视频，公开发行仍需单独决定视频资源的分发方式；本次没有上传或发布。

离线 bundle 数据、CRC 与片源校验不等于游戏内播放验收；最终播放仍需在游戏中确认。
