# Improve the translation while you play

[中文教程](dialogue-review.md) · [Back to the README](../README.md) · [Download the latest release](https://github.com/wanjizheng/zephyr-remastered-zh-cn/releases/latest)

A more natural line, a clearer form of address, or a single corrected typo can make the next player's adventure better. You do not need to code or translate an entire chapter. Start with one line you notice while playing. Suggestions for English, Simplified Chinese, and Traditional Chinese are all welcome.

## 1. Open Dialogue review

Extract the complete latest-release ZIP and run `ZephyrChinesePatcher.exe`. Select the game folder and CN, TW, or EN in the patcher, then click **Dialogue review**. You can review dialogue with the game running. Installing a patch or changing the game's language still requires you to exit the game first.

CN, TW, and EN select the language you want to review. **Live game** shows text captured from the running game; **Language-pack text** shows the text from your selected translation pack. If they use different languages, check which translation you intend to edit.

## 2. Find a line worth improving

Let the dialogue finish appearing and pause briefly. The assistant attempts to capture and locate the line in **Latest 20 lines**. Select it to read the original on the right.

With **Follow live dialogue** enabled, the selection follows new dialogue. Selecting history or editing a suggestion turns following off so you can finish your work. Enable it again when ready. **Keep on top** can help when switching between windows.

## 3. Write a suggestion

Choose **Use original**, or enter your complete revised line under **Your suggested translation**. A short reason helps: “The speaker is addressing two people,” “The previous line establishes that she already knows,” or “This sounds too literal in English.”

You can report a problem without proposing a replacement: fill in **Reason / speaker issue**. Preserve the intended meaning, character relationships, and established names where possible. Explain any relevant context and mark spoilers; there is no need to paste a whole scene.

Edited entries stay under **Edited lines** and are not removed by the 20-line recent-history limit. Typewriter animation does not create a new entry for every fragment. Suggestions for different target languages remain separate.

**These are suggestions, not live edits to the game or its saves.** A maintainer reviews accepted changes and includes them in a later patch. Players see the revised translation after installing that update.

## 4. Export before closing

Click **Export suggestions** and save the ZIP. It contains your suggestions, the original wording, and version and location information that helps a maintainer find the right line. Only entries with a suggestion or note are exported.

**Records last only for the current review-window session. They are not automatically restored after you close it. Export first.** The “Edited” label does not mean your work has been uploaded or permanently saved.

## 5. Share your contribution

Click **Preview issue**, review the text, then choose **Copy and open GitHub**. Select the translation-suggestion form, paste the text, attach your exported ZIP, and submit it yourself. You can also open the [translation-suggestion form directly](https://github.com/wanjizheng/zephyr-remastered-zh-cn/issues/new?template=translation.yml). A GitHub account is needed to submit an issue, but not to use the assistant or export locally.

Group related suggestions by scene or issue when convenient. Check existing issues first and add useful context to an existing discussion rather than duplicating it. You may include your preferred attribution name. Alternative phrasing is welcome; acceptance depends on meaning, context, character voice, and consistency across the story.

Submission does not automatically upload game files or merge changes into the patch. Attach only the suggestion package and necessary screenshots, not game assets, executables, saves, or full logs containing personal information.

## Missing dialogue?

- **“Reading paused”**: wait for the scene to finish loading, then click **Resume**. Temporary read failures during a scene transition can stop polling in this version; a missing line does not necessarily mean an update was reverted.
- **The editor still shows an earlier line**: check **Follow live dialogue**, or select the new line from history. Editing intentionally keeps your selection fixed.
- **A complete line still does not appear**: not every dialogue path is covered. Identical text in multiple resource entries, installed text that differs from the bundled catalog, and fast skipping can prevent a match. Report the scene, patch version, and a screenshot through Issues instead of guessing a line ID.
- **Unsupported game version**: update the patch or report the version; do not force the reader to continue.

The assistant will keep improving. You do not have to be a professional translator to help: pointing out one line that did not read naturally is already a useful contribution.
