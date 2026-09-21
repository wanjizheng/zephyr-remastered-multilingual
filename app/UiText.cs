namespace ZephyrPatcher;

internal static class UiText
{
 static readonly Dictionary<string,(string Traditional,string English)> Items=new()
 {
  ["等待选择游戏目录"]=("等待選擇遊戲目錄","Select a game folder"),
  ["请选择包含 ZephyrRemastered.exe 的文件夹。"]=("請選擇包含 ZephyrRemastered.exe 的資料夾。","Select the folder containing ZephyrRemastered.exe."),
  ["正在检查游戏文件…"]=("正在檢查遊戲檔案…","Checking game files…"),
  ["暂时不能操作"]=("目前無法操作","Unable to continue"),
  ["原版游戏，可以安装汉化"]=("原版遊戲，可以安裝漢化","Original game ready to patch"),
  ["文件校验通过。安装时自动保存原版备份，存档保持原样。"]=("檔案驗證通過。安裝時會自動備份原版，存檔不受影響。","Files verified. The original files will be backed up; saves remain untouched."),
  ["汉化已就绪"]=("漢化已就緒","Patch installed"),
  ["当前汉化包已安装。可自行进入游戏；视频字幕与原版美术字保持原样。"]=("目前的漢化包已安裝。可以進入遊戲；影片字幕與原版美術字保持原樣。","The selected patch is installed. You can start the game."),
  ["发现可更新的汉化"]=("找到可切換的漢化","A different patch language is available"),
  ["会使用已保存的原版备份重新构建，保留存档。"]=("會使用已備份的原版檔案重新建立，保留存檔。","The patch will be rebuilt from the original backup. Saves remain untouched."),
  ["上次操作尚未完成"]=("上次操作尚未完成","Previous operation incomplete"),
  ["请关闭游戏，先修复上次中断的安装。"]=("請關閉遊戲，先修復上次中斷的安裝。","Close the game and repair the interrupted installation first."),
  ["检测到已有汉化"]=("偵測到已有漢化","Existing patch detected"),
  ["文件与当前汉化一致，但未记录为本工具安装。可通过 Steam 验证恢复原版。"]=("檔案與目前漢化相同，但沒有本工具的安裝記錄。可透過 Steam 驗證還原原版。","The files match this patch without an installation record. Use Steam verification to restore the original game."),
  ["游戏版本或文件发生变化"]=("遊戲版本或檔案已變更","Game version or files changed"),
  ["已暂停汉化。请先检查工具更新；若 Steam 更新了游戏，建议通过 Steam 验证完整性后使用适配版本。"]=("已暫停漢化。請先檢查工具更新；若 Steam 更新了遊戲，建議驗證檔案完整性。","Patching is paused. Check for updates and verify the game files in Steam."),
  ["安装前自动备份；恢复原版请使用 Steam。存档不受影响。"]=("安裝前自動備份；還原原版請使用 Steam。存檔不受影響。","Original files are kept for updates. Use Steam to restore the game; saves are untouched."),
  ["正在准备，请保持游戏关闭…"]=("正在準備，請保持遊戲關閉…","Preparing files. Keep the game closed…"),
  ["汉化已完成，校验通过。现在可以进入游戏。"]=("漢化已完成，驗證通過。現在可以進入遊戲。","Patch installed and verified. You can start the game."),
  ["安装中断已恢复，文件校验通过。"]=("安裝中斷已恢復，檔案驗證通過。","Interrupted installation recovered and files verified."),
  ["操作未完成"]=("操作未完成","Operation incomplete"),
  ["知道了"]=("知道了","OK"),
  ["取消"]=("取消","Cancel"),
  ["通过 Steam 恢复原版"]=("透過 Steam 還原原版","Restore original files through Steam"),
  ["打开 Steam 验证"]=("開啟 Steam 驗證","Open Steam verification"),
  ["未能打开 Steam"]=("無法開啟 Steam","Could not open Steam"),
  ["有新版本可下载"]=("有新版本可下載","An update is available"),
  ["未发现更新版本"]=("沒有找到更新版本","No update found"),
  ["更新检查暂不可用"]=("目前無法檢查更新","Update check unavailable"),
  ["暂未发现更新"]=("目前沒有找到更新","No update found"),
  ["暂时无法检查更新"]=("目前無法檢查更新","Cannot check for updates"),
  ["打开下载页"]=("開啟下載頁","Open download page"),
  ["稍后"]=("稍後","Later"),
  ["请先保存并退出游戏。接下来将打开 Steam 验证 App 5099430 的文件；由 Steam 检查并重新下载需要的资源。本工具不会修改存档。\n若验证后仍有异常，可备份存档后卸载并重新安装游戏。"]=("請先儲存並退出遊戲。接下來會開啟 Steam 驗證 App 5099430，由 Steam 重新下載所需資源。本工具不會修改存檔。\n若問題仍在，請先備份存檔再重新安裝遊戲。","Save and close the game. Steam will verify App 5099430 and download any required files. This tool does not change saves.\nIf problems remain, back up your saves before reinstalling the game."),
  ["请打开 Steam → 游戏属性 → 已安装文件 → 验证游戏文件的完整性。"]=("請開啟 Steam → 遊戲內容 → 已安裝檔案 → 驗證遊戲檔案的完整性。","In Steam, open the game's Properties → Installed Files → Verify integrity of game files."),
  ["更新签名已验证。下载并完整解压新版程序后，再打开程序更新汉化。"]=("更新簽章已驗證。下載並完整解壓新版程式後，再開啟程式更新漢化。","The update signature is verified. Download and fully extract the new version before updating the patch."),
  ["当前已是最新正式版本。若游戏已更新而汉化尚未适配，请通过 Steam 验证原版文件，等待新的汉化包。"]=("目前已是最新正式版。若遊戲已更新而漢化尚未適配，請透過 Steam 驗證原版檔案。","No newer release is available. If the game was updated, verify its files in Steam and wait for a compatible patch."),
  ["请稍后重试，或到项目的 GitHub Releases 页面查看。离线安装不受影响。"]=("請稍後重試，或到專案的 GitHub Releases 頁面查看。離線安裝不受影響。","Try again later or check the project's GitHub Releases page. Offline installation still works."),
  ["选择包含 ZephyrRemastered.exe 的游戏文件夹"]=("選擇包含 ZephyrRemastered.exe 的遊戲資料夾","Select the game folder containing ZephyrRemastered.exe"),
 };

 public static string Translate(string simplified,string? language)
  =>Items.TryGetValue(simplified,out var value)?language=="en"?value.English:language=="zh-Hant"?value.Traditional:simplified:simplified;
}
