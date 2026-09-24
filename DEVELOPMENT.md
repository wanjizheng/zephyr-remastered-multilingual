# 开发与发行

0.4.7 本地视频接入流程见 [视频语言包](docs/video-localization.md)。视频输入被 Git 忽略，但本地构建必须完整包含对应语言的视频 payload，并通过构建前哈希核对。

维护者需要 Windows、.NET 10 SDK、Python 3.14；玩家不需要这些工具。建议创建独立 Python 虚拟环境后安装 `requirements.txt`。完整传递依赖版本见 `dependency-versions.json`。

```powershell
python -m venv C:\Build\zephyr-venv
C:\Build\zephyr-venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
pwsh -File scripts/build.ps1 -OutputDirectory C:\Build\zephyr-release-001
```

构建输出必须在仓库之外。`app/bin`、`app/obj` 是临时编译结果，不提交、不打进源码审阅包。主程序通过 .NET 原生 PublishSingleFile 与 IncludeNativeLibrariesForSelfExtract 将托管和原生运行库打包，保留 zh-Hans 资源，不做 WPF 裁剪；无需引入 Fody。Python 引擎仍放在 tools 内，payload 与许可证必须完整保留。图标为本项目原创几何羽笔，可用 scripts/create-icon.ps1 重建。第一版尚未承诺不同 SDK/传递依赖下的逐字节可重复 EXE 构建。

## 测试

`tests/integration.py --fixture <原版测试副本> --work <新空目录>` 会再复制一份资源进行安装、恢复、故障和路径保护测试。fixture 必须包含受支持的 EXE、19 个目标文件和所需 `.resS` 依赖；官方文件只能由维护者本地提供，不能提交或上传到 CI。

测试内仅对其新建副本跳过进程占用检测，生产安装没有此开关。发行引擎支持只读 `verify --game <原版目录> --payload <payload目录>`，在工具工作区重建所有资源并检查精确输出哈希，不修改游戏。另应验证游戏运行时安装被拒绝、界面显示、签名校验、公开文件内容与包内哈希。

## 数据更新

当前 `payload/zh-Hans/`、`payload/zh-Hant/`、`payload/en/` 是经过核验的三语言发行数据；`engine/trusted_payload.py` 分别固定各语言两个文件的 SHA-256。不能只改 pin 来跳过一次失败。

新译文版本需从维护者私有的原版／候选工作副本重新导出叶字段操作和许可字体字形块，重新检查所有原版哈希、最终输出 SHA、Unity 原生 CRC、catalog。将原版字形转换为本地复制引用，新字形须有逐块许可来源证明。不要把压缩 bundle 的粗粒度二进制差分当成“没有官方资源”的证明。内部游戏素材和生成工具工作区不应加入仓库。

升级游戏支持版本需要独立重建和回归，不能修改旧版哈希冒充兼容。工具版本由 `app/Updates.cs`、项目版本与 UI 显示同步维护；资源数据版本由 `engine/main.py` 和 payload 对应维护。v0.4.0 各语言包含 19 个目标资源，原版 bitmap 字体采用原图本地复制与许可 alpha 字形叠加重建；所有输出必须与已接受候选逐字节相同。

当前版本使用 MaterialDesignThemes/Colors 5.3.2 与 Microsoft.Xaml.Behaviors.Wpf 1.1.77；WPF 依赖整合入自包含 EXE。Python 引擎采用 PyInstaller onefile，位于 tools/ZephyrPatchEngine.exe；首启解压有少量耗时。保留 payload、许可证及 tools 目录。不会把测试游戏、官方资源、签名私钥放入发行包。

v0.4.0 仅通过 Steam 验证恢复原版，不提供手动旧备份或强制恢复入口。工具保留用于更新和切换语言的原版备份；安装事务支持中断后的自动回滚。游戏 EXE 与 Steam appmanifest 不由工具改写。Steam 恢复通过 steam://validate/5099430 交由 Steam 处理，工具不能预先宣称其完成。

## 发布签名

私钥保存在仓库外，并单独做安全备份。仓库内只有 `release-public.pem` 和 `app/ReleaseKey.cs` 公钥。打包后执行：

```powershell
pwsh -File scripts/sign-release.ps1 -Archive C:\Build\Zephyr-Chinese-Patcher-0.1.0-beta.1.zip -Version 0.1.0-beta.1 -PrivateKeyPath C:\Private\release-private.pem -OutputDirectory C:\Build\release-metadata
```

GitHub tag 使用 `v` 前缀；该 tag 的 Release 附上程序 ZIP、`release.json`、`release.sig`、SHA-256 清单。程序从固定仓库读取发布记录，用 RSA-PSS/SHA-256 核验元数据再提示更新。程序不自动执行下载内容；用户需按发布清单核对手动下载的 ZIP。元数据签名不等于对 EXE 的 Authenticode 签名。

发布前扫描仓库及 ZIP，排除官方资源、原文全集、私钥、令牌、游戏备份、存档和个人路径；核对字体和所有运行依赖许可证。Release 不上传测试 fixture。

v0.1.0 为首个正式工具版本，用户已确认 beta.4 可正常安装汉化和恢复原版。保留已验证的引擎与资源数据版本 beta.1，不因工具转正而重写数据版本；相同汉化数据无需重复安装。
