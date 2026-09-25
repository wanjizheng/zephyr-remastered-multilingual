<div align="center">

# The Rhapsody of Zephyr Remastered

**An unofficial, free localization patch**

Simplified Chinese · Traditional Chinese · English

[**Download the latest release**](https://github.com/wanjizheng/zephyr-remastered-multilingual/releases/latest) · [**Improve the translation**](docs/dialogue-review.en.md) · [Support the game on Steam](https://store.steampowered.com/app/5099430/)

[简体中文](README.md) · [繁體中文](README.zh-Hant.md) · English

</div>

## A note from the creator

I loved the original Rhapsody of Zephyr growing up. The 1998 game and the old Chinese edition left me with memories I wanted to revisit in the remaster—and share with more players.

This project started as a fresh Chinese translation based on the remaster's Korean resources, with revised dialogue, terminology, and character names. It has since grown to include Traditional Chinese and English, along with text embedded in three important story cinematics. I hope it helps you follow the story and enjoy returning to this world.

## Three key cinematics, now readable in your language

**v0.5.3 covers the embedded text in all three featured story cinematics:** map labels and battle information, tactical briefing subtitles, and the scrolling speech. All three are covered for Simplified Chinese, Traditional Chinese, and English. The map clip that was already in English retains its original English text.

<p align="center"><a href="docs/screenshots/cinematics/109-en.jpg"><img src="docs/screenshots/cinematics/109-en.jpg" width="800" alt="English scrolling text in the flag-and-speech cinematic"></a></p>

<sub>A frame extracted from the video used in this release. Contains a brief story scene. [See the other examples](README.md#关键动画也能读懂了).</sub>

This release also includes:

- Localized prompts and Yes/No buttons when starting a new game after completing a playthrough, in all three languages.
- Accumulated dialogue, item, skill, and character-name display corrections since the previous public release.
- An encoding compatibility fix for the speech cinematic; in-game playback has been confirmed for Simplified Chinese.
- **Dialogue review**, available to players of all three languages.

[Full v0.5.3 release notes](changelogs/v0.5.3.md) · [Changelog](CHANGELOG.md)

## Get started

You need **Windows 10 or 11, x64**, and a legally installed Steam copy of the game. This release targets Steam App **5099430**, Build **25418345**; the patcher checks the supported resource hashes.

1. Download **`Zephyr-Chinese-Patcher-*.zip`** from the [latest release](https://github.com/wanjizheng/zephyr-remastered-multilingual/releases/latest). Choose the installer ZIP rather than GitHub's “Source code” archives.
2. Extract the entire ZIP and run `ZephyrChinesePatcher.exe`. Keep `tools`, `payload`, and the license files beside it. No separate .NET, Python, or Unity installation is needed.
3. Save and fully exit the game. Select **EN**, choose the folder containing `ZephyrRemastered.exe`, and install.
4. Start the game after installation finishes. To change languages, exit the game, select **CN**, **TW**, or **EN**, and install again. To restore the original files, use the patcher's **Steam verification** button.

The language buttons change both the patcher's interface and the installed game translation. Saved games are not modified. For upgrades, extract the complete new package instead of mixing old program files with new language data.

<details>
<summary><strong>See the patcher interface</strong></summary>

<p align="center"><img src="docs/interface-preview-en.png" width="640" alt="English patcher interface, with example path and status"></p>

</details>

## Help improve the dialogue

**One thoughtful correction is enough to contribute.** Open **Dialogue review** from the patcher while playing, find a supported line, and suggest a more natural wording—or simply explain the problem.

**Notice a line → suggest an improvement → export → submit → accepted changes reach a future update.**

Captured entries include line and version information. You can revisit the latest 20 lines, while edited entries are kept separately. Suggestions for English, Simplified Chinese, and Traditional Chinese are all welcome.

[**Read the tutorial**](docs/dialogue-review.en.md) · [**Submit a suggestion**](https://github.com/wanjizheng/zephyr-remastered-multilingual/issues/new?template=translation.yml)

> **Export before closing.** Review records last only for the current window session. Suggestions do not immediately change the game or upload themselves. Some lines cannot yet be located; screenshots and context are welcome too.

## Scope and feedback

The English translation was produced with AI assistance from the established Simplified Chinese text and continues to receive corrections. Traditional Chinese is based on character conversion and has not been comprehensively adapted to regional wording. Not every branch or line has received independent human review.

The cinematic completion statement covers the three specified clips. Original artistic lettering and other unlisted clips are outside that claim. Resource reconstruction has been checked for all three languages; English and Traditional Chinese in-game video playback and layout still need player feedback.

Please [report problems](https://github.com/wanjizheng/zephyr-remastered-multilingual/issues) with the tool version, game build, scene, reproduction steps, and a screenshot. Include surrounding dialogue for translation issues. Do not upload full game resources, saves, account information, or logs containing personal paths.

<details>
<summary><strong>Updates, disk space, and installation issues</strong></summary>

- The patcher checks for new releases and verifies signed update metadata. Updates are downloaded and installed by you; there is no silent update.
- If Steam updates the game or another mod changes supported files, installation stops until compatible originals are available. Steam verification can remove the translation; reinstall a compatible patch afterward.
- Keep at least 5 GB free on both the system drive and the game drive for backups and temporary reconstruction. The package is larger than earlier releases because it includes localized cinematic data.
- Failed installations attempt automatic rollback. After an interruption, reopen the patcher and use its repair option. Original backups and transaction records are under `%LOCALAPPDATA%\ZephyrChinesePatcher\`.
- Windows may show a reputation warning because the executable is not Authenticode-signed. Download from this repository's Releases and check the published SHA-256. Update-metadata signatures are a separate mechanism.

</details>

## Credits and licenses

This fan project is free and unaffiliated with the developer or publisher. It requires a purchased game and does not provide the game itself or bypass purchase checks. Please [support the official Steam release](https://store.steampowered.com/app/5099430/).

The tool does not automatically upload game files or personal information. Current packages include encrypted localized video resources, reconstructed locally during installation. Encryption does not change ownership of the underlying game content. See [video packaging details](docs/video-localization.md).

Original tool code is licensed under [MIT](LICENSE); fonts and dependencies have their own [license notices](THIRD_PARTY_NOTICES.md). These licenses do not grant rights to the game, its artwork, story, or trademarks.
