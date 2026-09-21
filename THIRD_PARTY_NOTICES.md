# 第三方字体与软件

工具代码的 MIT 许可证不替代以下组件的许可证。完整许可文本放在 `licenses/`；打包时随程序一起提供。

- **朱雀仿宋 / Zhuque Fangsong**，TrionesType / Zhejiang JadeFoci Technology Co. LTD，SIL Open Font License 1.1。来源：https://github.com/TrionesType/zhuque 。使用其独立生成的字形像素，未发布游戏原有图集。
- **Noto Sans CJK SC / Noto Serif CJK SC**，相关版权与 SIL OFL 1.1 见 NotoSans / NotoSerif LICENSE。来源：https://github.com/notofonts/noto-cjk 。少量已有补齐字形保留。
- **Source Sans 3 / Source Serif 4**，Adobe 及字体项目贡献者，SIL Open Font License 1.1。来源：https://github.com/adobe-fonts/source-sans 与 https://github.com/adobe-fonts/source-serif 。英文界面使用 Source Sans 3 字形，标题及已确认的剧情对话组件使用 Source Serif 4 字形；完整许可见 `licenses/SourceSans3-OFL.md` 与 `licenses/SourceSerif4-OFL.md`。
- **UnityPy**，K0lb3，MIT。https://github.com/K0lb3/UnityPy
- **Python**，Python Software Foundation 及各上游贡献者，PSF License 与附带组件许可。
- **.NET / WPF**，.NET Foundation、Microsoft 及贡献者，MIT 与随附第三方声明。
- **MaterialDesignThemes 5.3.2 / MaterialDesignColors 5.3.2**，James Willock 及贡献者，MIT；**Microsoft.Xaml.Behaviors.Wpf 1.1.77**，Microsoft 及贡献者，MIT。许可文本见 `licenses/material-design/`。主题库与其依赖随主程序单文件发布。
- **PyInstaller**，GPL 与允许封装应用发行的 bootloader exception；本应用源码不因此改用 GPL。完整例外见对应许可。
- Python 依赖包括 NumPy、Pillow、lz4、Brotli、texture2ddecoder、etcpak、astc-encoder-py、fmod_toolkit、fsspec、attrs、tpk_ar、archspec、pyfmodex、Spooky、setuptools、packaging；各自及内嵌压缩／纹理解码库的版权和许可见 `licenses/python/` 与打包运行时附带声明。

确切构建版本列在公开源码仓库的 `dependency-versions.json`（https://github.com/wanjizheng/zephyr-remastered-zh-cn）。运行依赖不包含游戏 FMOD 音频库，也没有从玩家游戏目录复制可执行代码进发行包。

字体字形数据是原字体派生的数据，遵循各自 OFL；请勿单独出售字体，衍生修改与命名遵守对应许可。本项目没有获得游戏原作的版权或商标授权。
