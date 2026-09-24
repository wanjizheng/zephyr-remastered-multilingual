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

本地 WebM 保留用于维护并由 .gitignore 排除。发行前 `scripts/package_payloads.py` 将片源封装为 AES-256-GCM `.zvenc`，验证解密内容 SHA-256，更新语言清单及固定哈希。工作台和 build.ps1 均执行此步骤，并仅复制固定哈希清单中的输入，不递归拷贝维护用视频。重复构建复用已封装文件。

安装时在内存中解密并直接写入 Unity 数据流，不生成临时 WebM，不需要 AI 或重新编码。生成的游戏资源包与加密前一致。加密密钥随引擎提供，不是发行签名私钥；此方式不保证无法提取。

对外说明：视频资源以加密形式封装，安装时在本地还原。不能宣称不携带游戏资源。本次没有上传或发布。

离线 bundle 数据、CRC 与片源校验不等于游戏内播放验收；最终播放仍需在游戏中确认。
