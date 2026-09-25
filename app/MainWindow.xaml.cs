using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
namespace ZephyrPatcher;

public partial class MainWindow : Window
{
 bool busy,initialized,preview,dialogOpen; int generation; JsonElement? lastState;
 readonly System.Windows.Threading.DispatcherTimer steamTimer=new(){Interval=TimeSpan.FromSeconds(15)};
 SteamVerificationWatch? steamWatch;
 DateTimeOffset lastSteamCheck=DateTimeOffset.MinValue;
 const string IdleText="安装前自动备份；恢复原版请使用 Steam。存档不受影响。";
 string AppLanguage=>EnLanguage?.IsChecked==true?"en":TwLanguage?.IsChecked==true?"zh-Hant":"zh-Hans";
 string L(string text)=>UiText.Translate(text,AppLanguage);
 public MainWindow()
 {
  InitializeComponent();AppLanguageChanged(this,new RoutedEventArgs());Closing+=OnClosing;
  steamTimer.Tick+=async (_,_)=>await CheckSteamVerification();
  Activated+=async (_,_)=>await CheckSteamVerification();
  Closed+=(_,_)=>steamTimer.Stop();
 }
 void StopSteamWatch(){steamWatch=null;steamTimer.Stop();}
 void BeginSteamWatch()
 {
  steamWatch=new(GamePaths.Normalize(GamePath.Text),DateTimeOffset.UtcNow);
  lastSteamCheck=DateTimeOffset.MinValue;ShowSteamWaiting();steamTimer.Start();
 }
 void ShowSteamWaiting()
 {
  StateTitle.Text=AppLanguage=="en"?"Waiting for Steam verification":AppLanguage=="zh-Hant"?"等待 Steam 驗證並還原檔案":"等待 Steam 验证并恢复文件";
  StateDetail.Text=AppLanguage=="en"?"Complete verification in Steam. This tool checks again every 15 seconds.":AppLanguage=="zh-Hant"?"請在 Steam 完成驗證。工具每 15 秒自動檢查一次。":"请在 Steam 中完成验证。工具每 15 秒自动检测一次，返回此窗口也会重新检查。";
  ProgressText.Text=AppLanguage=="en"?"Checking the restore result automatically…":AppLanguage=="zh-Hant"?"正在自動檢查還原結果…":"正在自动检测恢复结果，无需一直点击“重新检测”。";
  SetIcon(PackIconKind.FolderSearchOutline,"#456080");
  InstallButton.IsEnabled=RestoreButton.IsEnabled=false;
  RecoverButton.Visibility=Visibility.Collapsed;
 }
 void ApplySteamState(JsonElement result)
 {
  if(steamWatch is null){ShowState(result);return;}
  string outcome=steamWatch.Evaluate(Value(result,"status"),SteamVerificationWatch.ReadFlags(steamWatch.GamePath),DateTimeOffset.UtcNow);
  ShowState(result);
  if(outcome=="waiting"){ShowSteamWaiting();return;}
  StopSteamWatch();
  ProgressText.Text=outcome=="original"?(AppLanguage=="en"?"Original files verified. You can install a patch again.":AppLanguage=="zh-Hant"?"原版檔案已驗證，可以重新安裝漢化。":"已自动检测到原版资源，文件校验通过，可以重新安装汉化。"):
   outcome=="finished"?(AppLanguage=="en"?"Steam finished processing files. The current state is shown above.":AppLanguage=="zh-Hant"?"Steam 已完成檔案處理；上方顯示重新驗證後的狀態。":"Steam 已结束文件处理；上方显示重新校验后的实际状态。"):
   (AppLanguage=="en"?"Automatic checking paused. Check Steam, then select Check again.":AppLanguage=="zh-Hant"?"自動檢查已暫停。請確認 Steam 結果後重新檢查。":"自动检测已暂停，上方显示当前文件状态。请确认 Steam 结果后点击“重新检测”。");
 }
 async Task CheckSteamVerification()
 {
  if(preview||steamWatch is null||busy||dialogOpen||DateTimeOffset.UtcNow-lastSteamCheck<TimeSpan.FromSeconds(3))return;
  if(!string.Equals(steamWatch.GamePath,GamePaths.Normalize(GamePath.Text),StringComparison.OrdinalIgnoreCase)){StopSteamWatch();return;}
  lastSteamCheck=DateTimeOffset.UtcNow;await Refresh();
 }
 public void PreparePreview(string state="original")
 {
  preview=true;GamePath.Text=@"D:\Games\Steam\steamapps\common\The Rhapsody of Zephyr Remastered";
  var previewState=state=="install-progress"?"original":state;
  using var doc=JsonDocument.Parse(JsonSerializer.Serialize(new{status=previewState,can_install=previewState=="original",backup_available=true,backup_files=19,backup_complete=true,game_build="25418345",installed_build=previewState=="unsupported_or_modified"?"25420000":"25418345",version=Updates.Current,installed_version=previewState=="installed"?Updates.Current:null}));
  ShowState(doc.RootElement.Clone());
  if(state=="steam-waiting"){BeginSteamWatch();steamTimer.Stop();}
  if(state=="install-progress"){Progress.Visibility=Visibility.Visible;Progress.IsIndeterminate=false;Progress.Maximum=19;Progress.Value=5;FeatureRow.Visibility=Visibility.Collapsed;ProgressText.Text=AppLanguage=="en"?"Preparing resources 5/19":AppLanguage=="zh-Hant"?"正在準備資源 5/19":"正在准备资源 5/19";}
 }
 public async void Initialize(){initialized=true;GamePath.Text=FindGame()??"";await Refresh();await CheckUpdates(false);}
 static string? FindGame()
 {
  var steam=Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath",null) as string;
  if(steam==null)return null;var roots=new List<string>{steam};var file=Path.Combine(steam,"steamapps","libraryfolders.vdf");
  if(File.Exists(file))foreach(Match m in Regex.Matches(File.ReadAllText(file),"\"path\"\\s+\"([^\"]+)\""))roots.Add(m.Groups[1].Value.Replace(@"\\",@"\"));
  return roots.Select(r=>GamePaths.Normalize(Path.Combine(r,"steamapps","common","The Rhapsody of Zephyr Remastered"))).FirstOrDefault(p=>File.Exists(Path.Combine(p,"ZephyrRemastered.exe")));
 }
 void OnClosing(object? sender,CancelEventArgs e){if(busy||dialogOpen){e.Cancel=true;ProgressText.Text=AppLanguage=="en"?"Wait for the operation to finish or close the dialog first.":AppLanguage=="zh-Hant"?"請等待操作完成，或先關閉提示視窗。":"请等待当前操作完成，或先关闭提示窗口。";}}
 void SetBusy(bool value)
 {
  busy=value;GamePath.IsEnabled=BrowseButton.IsEnabled=CnLanguage.IsEnabled=TwLanguage.IsEnabled=EnLanguage.IsEnabled=!value;InstallButton.IsEnabled=RestoreButton.IsEnabled=false;RecoverButton.IsEnabled=!value;
  Progress.IsIndeterminate=true;Progress.Visibility=value?Visibility.Visible:Visibility.Collapsed;FeatureRow.Visibility=value?Visibility.Collapsed:Visibility.Visible;
 }
 void SetIcon(PackIconKind kind,string color){StateIcon.Kind=kind;StateIcon.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));}
 async Task<JsonElement> RunEngine(string action)
 {
  GamePath.Text=GamePaths.Normalize(GamePath.Text);
  var start=new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"tools","ZephyrPatchEngine.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=System.Text.Encoding.UTF8,StandardErrorEncoding=System.Text.Encoding.UTF8};
  start.ArgumentList.Add(action);start.ArgumentList.Add("--game");start.ArgumentList.Add(GamePath.Text.Trim());start.ArgumentList.Add("--payload");start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"payload"));start.ArgumentList.Add("--language");start.ArgumentList.Add(AppLanguage);
  using var process=new Process{StartInfo=start};
  process.ErrorDataReceived+=(_,e)=>{if(!string.IsNullOrWhiteSpace(e.Data))Dispatcher.BeginInvoke(()=>
  {
   ProgressText.Text=e.Data;var m=Regex.Match(e.Data,@"(\d+)/(\d+)");
   if(m.Success){Progress.IsIndeterminate=false;Progress.Maximum=int.Parse(m.Groups[2].Value);Progress.Value=int.Parse(m.Groups[1].Value);}
  });};
  process.Start();process.BeginErrorReadLine();string output=await process.StandardOutput.ReadToEndAsync();await process.WaitForExitAsync();
  JsonElement result;
  try{using var json=JsonDocument.Parse(output.Trim());result=json.RootElement.Clone();}
  catch{throw new InvalidOperationException(AppLanguage=="en"?"Could not read the result. Check that the package was fully extracted and the processing tool was not blocked.":AppLanguage=="zh-Hant"?"無法讀取處理結果。請檢查壓縮包是否完整解壓，以及安全軟體是否封鎖處理程式。":"无法读取处理结果。请检查下载包是否完整，以及安全软件是否拦截了 tools 中的处理程序。");}
  if(!result.GetProperty("ok").GetBoolean())throw new InvalidOperationException(result.GetProperty("error").GetString());return result;
 }
 async Task Refresh()
 {
  if(preview||busy)return;
  if(string.IsNullOrWhiteSpace(GamePath.Text)){lastState=null;InstallButton.IsEnabled=RestoreButton.IsEnabled=false;StateTitle.Text=L("等待选择游戏目录");StateDetail.Text=L("请选择包含 ZephyrRemastered.exe 的文件夹。");return;}
  SetBusy(true);StateTitle.Text=L("正在检查游戏文件…");SetIcon(PackIconKind.FolderSearchOutline,"#456080");
  try{ApplySteamState(await RunEngine("status"));}
  catch(Exception ex)
  {
   lastState=null;
   if(steamWatch is not null)
   {
    if(steamWatch.Evaluate("unreadable",null,DateTimeOffset.UtcNow)=="timeout"){StopSteamWatch();StateTitle.Text=AppLanguage=="en"?"Automatic check paused":AppLanguage=="zh-Hant"?"自動檢查已暫停":"自动检测已暂停";StateDetail.Text=AppLanguage=="en"?"Resources cannot be read yet. Check Steam, then select Check again.":AppLanguage=="zh-Hant"?"目前無法讀取資源。請確認 Steam 結果後重新檢查。":"暂时无法读取资源，请确认 Steam 结果后点击“重新检测”。";}
    else{ShowSteamWaiting();StateDetail.Text=AppLanguage=="en"?"Steam may be replacing files. The check will retry shortly.":AppLanguage=="zh-Hant"?"Steam 可能正在替換檔案，稍後會自動重試。":"Steam 可能正在替换资源，暂时无法读取；稍后会自动重试。";}
   }
   else{StateTitle.Text=L("暂时不能操作");StateDetail.Text=ex.Message;BuildInfo.Text=AppLanguage=="en"?"Game build: unknown":AppLanguage=="zh-Hant"?"遊戲 Build：未識別":"游戏 Build：未识别";BackupInfo.Text="";SetIcon(PackIconKind.AlertCircleOutline,"#9B642A");}
  }
  finally{busy=false;GamePath.IsEnabled=BrowseButton.IsEnabled=CnLanguage.IsEnabled=TwLanguage.IsEnabled=EnLanguage.IsEnabled=true;Progress.Visibility=Visibility.Collapsed;FeatureRow.Visibility=Visibility.Visible;}
 }
 static bool Flag(JsonElement r,string key)=>r.TryGetProperty(key,out var x)&&x.ValueKind==JsonValueKind.True;
 static string Value(JsonElement r,string key,string fallback="未识别")=>r.TryGetProperty(key,out var x)&&x.ValueKind==JsonValueKind.String?x.GetString()??fallback:fallback;
 void ShowState(JsonElement r)
 {
  lastState=r;string state=Value(r,"status");
  (StateTitle.Text,StateDetail.Text)=state switch{
   "original"=>("原版游戏，可以安装汉化","文件校验通过。安装时自动保存原版备份，存档保持原样。"),
   "installed"=>("汉化已就绪","当前汉化包已安装。可自行进入游戏；视频字幕与原版美术字保持原样。"),
   "update_available"=>("发现可更新的汉化","会使用已保存的原版备份重新构建，保留存档。"),
   "recovery_required"=>("上次操作尚未完成","请关闭游戏，先修复上次中断的安装。"),
   "unmanaged_localization"=>("检测到已有汉化","文件与当前汉化一致，但未记录为本工具安装。可通过 Steam 验证恢复原版。"),
   _=>("游戏版本或文件发生变化","已暂停汉化。请先检查工具更新；若 Steam 更新了游戏，建议通过 Steam 验证完整性后使用适配版本。")};
  StateTitle.Text=L(StateTitle.Text);StateDetail.Text=L(StateDetail.Text);
  SetIcon(state is "installed" or "original"?PackIconKind.CheckCircleOutline:PackIconKind.AlertCircleOutline,state is "installed" or "original"?"#446D62":"#9B642A");
  BuildInfo.Text=AppLanguage=="en"?$"Game build: {Value(r,"installed_build")}   ·   Supported: {Value(r,"game_build")}":AppLanguage=="zh-Hant"?$"遊戲 Build：{Value(r,"installed_build")}   ·   支援 Build：{Value(r,"game_build")}":$"游戏 Build：{Value(r,"installed_build")}   ·   支持 Build：{Value(r,"game_build")}";
  BackupInfo.Text=Flag(r,"backup_available")?(AppLanguage=="en"?$"Original files for updates: {r.GetProperty("backup_files").GetInt32()}   ·   Patch: {Value(r,"version")}":AppLanguage=="zh-Hant"?$"更新用原版備份：{r.GetProperty("backup_files").GetInt32()} 個檔案   ·   漢化包：{Value(r,"version")}":$"更新用原版备份：{r.GetProperty("backup_files").GetInt32()} 个文件   ·   汉化包：{Value(r,"version")}"):(AppLanguage=="en"?$"Original files for updates: none   ·   Patch: {Value(r,"version")}":AppLanguage=="zh-Hant"?$"更新用原版備份：尚未建立   ·   漢化包：{Value(r,"version")}":$"更新用原版备份：尚未建立   ·   汉化包：{Value(r,"version")}");
  InstallButton.IsEnabled=Flag(r,"can_install")&&state!="installed";UpdateInstallButtonText();
  RestoreButton.IsEnabled=state!="recovery_required";RecoverButton.IsEnabled=true;RecoverButton.Visibility=state=="recovery_required"?Visibility.Visible:Visibility.Collapsed;
 }
 async Task<string> Dialog(string title,string detail,params (string Label,string Action,bool Enabled)[] actions)
 {
  if(dialogOpen)return "cancel";dialogOpen=true;
  var panel=new StackPanel{Width=440,Margin=new Thickness(28)};
  panel.Children.Add(new TextBlock{Text=L(title),FontSize=22,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,14)});
  panel.Children.Add(new TextBlock{Text=L(detail),TextWrapping=TextWrapping.Wrap,LineHeight=24,Foreground=new SolidColorBrush(Color.FromRgb(85,99,118)),Margin=new Thickness(0,0,0,18)});
  foreach(var (label,action,enabled) in actions)
  {
   bool primary=action=="yes";
   var button=new Button{Content=L(label),IsEnabled=enabled,HorizontalContentAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,4,0,0),Padding=new Thickness(12,8,12,8),Background=new SolidColorBrush(primary?Color.FromRgb(37,60,96):Color.FromRgb(238,242,247)),Foreground=new SolidColorBrush(primary?Colors.White:Color.FromRgb(38,54,75)),Command=DialogHost.CloseDialogCommand,CommandParameter=action};
   panel.Children.Add(button);
  }
  try{return (await Dialogs.ShowDialog(panel))?.ToString()??"cancel";}finally{dialogOpen=false;}
 }
 async Task Operate(string action)
 {
  if(busy||preview)return;SetBusy(true);ProgressText.Text=L("正在准备，请保持游戏关闭…");
  try{
   var result=await RunEngine(action);ShowState(result);
   ProgressText.Text=action=="install"?L("汉化已完成，校验通过。现在可以进入游戏。"):L("安装中断已恢复，文件校验通过。");
  }
  catch(Exception ex){StateTitle.Text=L("操作未完成");StateDetail.Text=ex.Message;await Dialog("操作未完成",ex.Message,("知道了","cancel",true));}
  finally{busy=false;GamePath.IsEnabled=BrowseButton.IsEnabled=CnLanguage.IsEnabled=TwLanguage.IsEnabled=EnLanguage.IsEnabled=true;Progress.Visibility=Visibility.Collapsed;FeatureRow.Visibility=Visibility.Visible;await Refresh();}
 }
 async void BrowseClick(object sender,RoutedEventArgs e){if(busy)return;var dialog=new OpenFolderDialog{Title=L("选择包含 ZephyrRemastered.exe 的游戏文件夹")};if(dialog.ShowDialog(this)==true){GamePath.Text=GamePaths.Normalize(dialog.FolderName);await Refresh();}}
 async void PathChanged(object sender,TextChangedEventArgs e){if(!initialized||busy)return;StopSteamWatch();int ticket=++generation;await Task.Delay(600);if(ticket==generation)await Refresh();}
 async void AppLanguageChanged(object sender,RoutedEventArgs e)
 {
  // The default CN icon is checked while XAML is still constructing later controls.
  if(MainHeading is null||CnLanguage is null||TwLanguage is null||EnLanguage is null)return;
  var lang=AppLanguage; bool hant=lang=="zh-Hant", english=lang=="en";
  if(ProofreaderButton is not null)ProofreaderButton.Content=english?"Dialogue review":hant?"對白校對":"对白校对";
  if(proofreader is not null)try{proofreader.ChangeContext(GamePath.Text,lang);}catch(Exception ex){MessageBox.Show(this,ex.Message);}
  Title=english?"The Rhapsody of Zephyr · Localization Patcher":hant?"西風狂詩曲 · 漢化工具":"西风狂诗曲 · 汉化工具";
  SidebarProject.Text=english?"Community localization":hant?"民間漢化計畫":"民间汉化计划";
  SidebarTitle.Text=english?"Rhapsody of Zephyr":hant?"西風狂詩曲":"西风狂诗曲";
  SidebarTitle.FontSize=english?23:33;
  SidebarMotto.Text=english?"Let the adventure\nbegin again.":hant?"讓記憶裡的冒險\n再次啟程。":"让记忆里的冒险\n再次启程。";
  SidebarCredit.Text=english?"Made with care and shared freely.\nThanks for supporting the official game.":hant?"為愛發電，免費分享。\n感謝每一位支持正版的冒險者。":"为爱发电，免费分享。\n感谢每一位支持正版的冒险者。";
  MainHeading.Text=english?"Return to a familiar world":hant?"中文，回到熟悉的世界":"中文，回到熟悉的世界";
  MainSubheading.Text=english?"Select the game folder to install the patch.":hant?"選擇遊戲目錄，即可安裝漢化。":"选择游戏目录，即可安装汉化。";
  SidebarEdition.Text=english?"Remastered localization patch":hant?"重製版多語言翻譯":"重制版多语言翻译";
  VersionBadge.Text=english?"v0.5.2":hant?"v0.5.2":"v0.5.2";
  GameDirectoryLabel.Text=english?"Game folder":hant?"遊戲目錄":"游戏目录";
  LanguageIconLabel.Text=english?"Install language":hant?"安裝語言":"安装语言";
  GamePath.ToolTip=english?"Select the folder containing ZephyrRemastered.exe":hant?"選擇包含 ZephyrRemastered.exe 的資料夾":"选择包含 ZephyrRemastered.exe 的文件夹";
  BrowseButton.ToolTip=english?"Browse for game folder":hant?"選擇遊戲資料夾":"选择游戏文件夹";
  FeatureText.Text=english?"Original art preserved    /    Zhuque font    /    Offline install":hant?"保留原版美術字    /    朱雀仿宋    /    離線安裝":"保留原版美术字    /    朱雀仿宋    /    离线安装";
  RecheckButton.Content=english?"Check again":hant?"重新檢查":"重新检测";
  UpdateButton.Content=english?"Check updates":hant?"檢查更新":"检查更新";
  FeedbackButton.Content=english?"Feedback":hant?"問題回報":"问题反馈";
  StoreButton.Content=english?"Support the official game":hant?"支持正版":"支持正版";
  RecoverButton.Content=english?"Repair interrupted install":hant?"修復中斷的安裝":"修复中断的安装";
  FooterVersion.Text=english?"v0.5.2 · Unofficial localization tool":hant?"v0.5.2 · 非官方漢化工具":"v0.5.2 · 非官方汉化工具";
  UpdateInstallButtonText();
  if(lastState is JsonElement current)ShowState(current);
  else{StateTitle.Text=L("等待选择游戏目录");StateDetail.Text=L("请选择包含 ZephyrRemastered.exe 的文件夹。");}
  ProgressText.Text=L(IdleText);
  if(initialized&&!busy)await Refresh();
 }
 void UpdateInstallButtonText()
 {
  var state=lastState is JsonElement r?Value(r,"status"):"";
  bool hant=AppLanguage=="zh-Hant";
  bool english=AppLanguage=="en";
  InstallButton.Content=state=="update_available"?(english?"Update patch":hant?"更新漢化":"更新汉化"):state=="installed"?(english?"Patch installed":hant?"已安裝漢化":"已安装汉化"):(english?"Install patch":hant?"安裝漢化":"安装汉化");
  RestoreButton.Content=english?"Restore via Steam":hant?"透過 Steam 還原":"通过 Steam 恢复";
 }
 async void InstallClick(object sender,RoutedEventArgs e)=>await Operate("install");
 async void RestoreClick(object sender,RoutedEventArgs e)
 {
  if(busy||lastState is not JsonElement)return;
  if(await Dialog("通过 Steam 恢复原版","请先保存并退出游戏。接下来将打开 Steam 验证 App 5099430 的文件；由 Steam 检查并重新下载需要的资源。本工具不会修改存档。\n若验证后仍有异常，可备份存档后卸载并重新安装游戏。",("打开 Steam 验证","yes",true),("取消","cancel",true))=="yes")
   try{Open("steam://validate/5099430");BeginSteamWatch();}catch{await Dialog("未能打开 Steam","请打开 Steam → 游戏属性 → 已安装文件 → 验证游戏文件的完整性。",("知道了","cancel",true));}
 }
 async void RecoverClick(object sender,RoutedEventArgs e)=>await Operate("recover");
 async void RecheckClick(object sender,RoutedEventArgs e){if(!busy){ProgressText.Text=L(IdleText);await Refresh();}}
 static void Open(string url)=>Process.Start(new ProcessStartInfo(url){UseShellExecute=true});
 void StoreClick(object sender,RoutedEventArgs e)=>Open("https://store.steampowered.com/app/5099430/");
 Dialogue.DialogueWindow? proofreader;
 void ProofreaderClick(object sender,RoutedEventArgs e)
 {
  if(proofreader is not null){proofreader.ChangeContext(GamePath.Text,AppLanguage);proofreader.Activate();return;}
  try{proofreader=new Dialogue.DialogueWindow(GamePath.Text,language:AppLanguage);proofreader.Closed+=(_,_)=>proofreader=null;proofreader.Show();}
  catch(Exception ex){MessageBox.Show(this,ex.Message,"校对窗口未能打开");}
 }
 void FeedbackClick(object sender,RoutedEventArgs e)=>Open("https://github.com/wanjizheng/zephyr-remastered-zh-cn/issues/new/choose");
 async Task CheckUpdates(bool manual)
 {
  if(preview)return;UpdateButton.IsEnabled=false;
  try{
   var update=await Updates.Check();UpdateStatus.Text=L(update!=null?"有新版本可下载":"未发现更新版本");
   if(manual&&update!=null){if(await Dialog("有新版本可下载","更新签名已验证。下载并完整解压新版程序后，再打开程序更新汉化。",("打开下载页","yes",true),("稍后","cancel",true))=="yes")Open(update);}
   else if(manual)await Dialog("暂未发现更新","当前已是最新正式版本。若游戏已更新而汉化尚未适配，请通过 Steam 验证原版文件，等待新的汉化包。",("知道了","cancel",true));
  }catch{UpdateStatus.Text=L("更新检查暂不可用");if(manual)await Dialog("暂时无法检查更新","请稍后重试，或到项目的 GitHub Releases 页面查看。离线安装不受影响。",("知道了","cancel",true));}
  finally{UpdateButton.IsEnabled=true;}
 }
 async void CheckUpdateClick(object sender,RoutedEventArgs e)=>await CheckUpdates(true);
}
