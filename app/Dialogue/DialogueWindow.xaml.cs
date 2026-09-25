using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
namespace ZephyrPatcher.Dialogue;

public partial class DialogueWindow : Window
{
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(100)},saveTimer=new(){Interval=TimeSpan.FromMilliseconds(500)};
 readonly DialogueStore store;readonly DialogueIndex index=new();readonly DialogueCatalog catalog=new();readonly DialogueReader reader=new();
 DialogueEntry? selected;bool refreshing,loading,busy,closing,paused,resetReader;int errors,generation;
 DateTimeOffset retryAfter=DateTimeOffset.MinValue;
 string lastLive="",gamePath,language,runtimeLanguage;
 string T(string cn,string tw,string en)=>language=="en"?en:language=="zh-Hant"?tw:cn;
 string LanguageName=>language=="en"?"English":language=="zh-Hant"?"繁體中文":"简体中文";
 public DialogueWindow(string gamePath,string? dataDirectory=null,bool autoStart=true,string language="zh-Hans")
 {
  this.gamePath=gamePath;this.language=DialogueCatalog.Languages.Contains(language)?language:"zh-Hans";
  runtimeLanguage=DialogueCatalog.InstalledLanguage(gamePath);
  store=new(dataDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ZephyrChinesePatcher","Proofreader"),sessionOnly:true);
  InitializeComponent();ApplyLanguage();RefreshHistory();
  History.SelectionChanged+=(_,_)=>{if(!refreshing && History.SelectedItem is DialogueEntry e){refreshing=true;Modified.SelectedItem=null;refreshing=false;SelectEntry(e);}};
  Modified.SelectionChanged+=(_,_)=>{if(!refreshing && Modified.SelectedItem is DialogueEntry e){refreshing=true;History.SelectedItem=null;refreshing=false;SelectEntry(e);}};
  Suggestion.TextChanged+=Edited;Note.TextChanged+=Edited;
  Suggestion.GotKeyboardFocus+=(_,_)=>Follow.IsChecked=false;Note.GotKeyboardFocus+=(_,_)=>Follow.IsChecked=false;
  Modified.PreviewMouseDown+=(_,_)=>Follow.IsChecked=false;Modified.PreviewKeyDown+=(_,_)=>Follow.IsChecked=false;
  History.PreviewMouseDown+=(_,_)=>Follow.IsChecked=false;History.PreviewKeyDown+=(_,_)=>Follow.IsChecked=false;
  Follow.Checked+=(_,_)=>{if(store.Entries.LastOrDefault(e=>e.Identity==lastLive) is {} e)SelectListEntry(e);};
  Pin.Checked+=(_,_)=>Topmost=true;Pin.Unchecked+=(_,_)=>Topmost=false;
  saveTimer.Tick+=(_,_)=>SaveNow();timer.Tick+=async (_,_)=>await Poll();
  Loaded+=async (_,_)=>{if(autoStart){timer.Start();await Poll();}else Status.Text=T("界面预览 · 未连接游戏","介面預覽 · 未連接遊戲","Preview · game not connected");};
  Closing+=(_,e)=>{if(!SaveNow()){e.Cancel=true;MessageBox.Show(this,T("草稿保存失败，请检查磁盘。","草稿儲存失敗，請檢查磁碟。","Draft save failed. Check your disk before closing."));return;}closing=true;timer.Stop();saveTimer.Stop();if(!busy)reader.Dispose();};
  Closed+=(_,_)=>store.Dispose();
 }
 public void ChangeContext(string path,string locale)
 {
  if(!DialogueCatalog.Languages.Contains(locale)||(path==gamePath&&locale==language))return;
  if(!SaveNow())throw new IOException("Draft save failed");
  generation++;resetReader=path!=gamePath;gamePath=path;language=locale;runtimeLanguage=DialogueCatalog.InstalledLanguage(path);
  errors=0;retryAfter=DateTimeOffset.MinValue;
  if(resetReader&&!busy){reader.Dispose();resetReader=false;}
  lastLive="";SelectEntry(null);ApplyLanguage();RefreshHistory();
 }
 void ApplyLanguage()
 {
  Title=T("西风狂诗曲 · 对白校对","西風狂詩曲 · 對白校對","Rhapsody of Zephyr · Dialogue review");
  Heading.Text=T("众人拾柴火焰高，冒险途中认真打磨每一句话","眾人拾柴火焰高，冒險途中認真打磨每一句話","Many hands make light work. Together, refine every line of the adventure.");
  Subtitle.Text=T("西风狂诗曲重制版 / 翻译校对","西風狂詩曲重製版 / 翻譯校對","The Rhapsody of Zephyr Remastered / Translation review");
  LanguageBadge.Text=LanguageName;ScopeHint.Text=T("校对语言 · 跟随安装器选择","校對語言 · 跟隨安裝工具選擇","Review language · selected in the patcher");
  ModifiedHeading.Text=T("已修改的台词","已修改的臺詞","Edited lines");HistoryHeading.Text=T("最近 20 句","最近 20 句","Latest 20 lines");Follow.Content=T("跟随最新对白","跟隨最新對白","Follow live dialogue");
  OriginalLabel.Text=T("语言包原文","語言包原文","Language-pack text")+" · "+LanguageName;
  SuggestionLabel.Text=T("你的建议译文","你的建議譯文","Your suggested translation");NoteLabel.Text=T("修改原因 / 说话人问题（选填）","修改原因 / 說話人問題（選填）","Reason / speaker issue (optional)");
  CopyButton.Content=T("用原文起草","以原文起草","Use original");ClearButton.Content=T("清空建议","清空建議","Clear suggestion");
  ConfirmLanguageButton.Content=T("确认旧建议属于此语言","確認舊建議屬於此語言","Assign legacy language");
  ResourceDetails.Header=T("资源定位与版本详情","資源定位與版本詳情","Resource and version details");
  IssueButton.Content=T("预览提交","預覽提交","Preview issue");ExportButton.Content=T("导出修改包","匯出修改包","Export suggestions");Pin.Content=T("置顶","置頂","Keep on top");SetToggle();
  Saved.Text=T("仅本次会话","僅本次工作階段","This session only");
  FooterHint.Text=T("关闭后清空本次记录，请先导出已修改的台词。最近列表只保留 20 句；快速跳过可能漏句。","關閉後清空本次記錄，請先匯出已修改的臺詞。最近列表只保留 20 句；快速跳過可能漏句。","Closing clears this session. Export edited lines first. Only the latest 20 unedited lines are retained; fast skips may be missed.");
  Status.Text=T("等待对白","等待對白","Waiting for dialogue");Live.Text=T("在游戏中进入剧情即可开始。","在遊戲中進入劇情即可開始。","Enter a conversation in the game to begin.");
 }
 void RefreshHistory()
 {
  var keep=selected;refreshing=true;
  try{store.TrimRecent();History.Items.Clear();Modified.Items.Clear();
   foreach(var e in store.Changes().Where(e=>e.Language==language))Modified.Items.Add(e);
   foreach(var e in store.Recent().Where(e=>e.Language==language))History.Items.Add(e);
   Count.Text=History.Items.Count+" / 20";ModifiedCount.Text=Modified.Items.Count.ToString();
   if(keep is not null){if(Modified.Items.Contains(keep))Modified.SelectedItem=keep;else if(History.Items.Contains(keep))History.SelectedItem=keep;}
  }finally{refreshing=false;}

 }
 void RunSafe(Action action){try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,T("操作未完成","操作未完成","Action not completed"),MessageBoxButton.OK,MessageBoxImage.Warning);}}
 void SetToggle()=>ToggleButton.Content=paused?T("继续读取","繼續讀取","Resume"):T("暂停读取","暫停讀取","Pause");
 void Toggle(object sender,RoutedEventArgs e){paused=!paused;errors=0;retryAfter=DateTimeOffset.MinValue;SetToggle();Status.Text=paused?T("已暂停，仍可编辑和导出","已暫停，仍可編輯與匯出","Paused; editing and export remain available"):T("正在连接","正在連接","Connecting");}
 async Task Poll()
 {
  if(busy||closing||paused||DateTimeOffset.UtcNow<retryAfter)return;
  if(string.IsNullOrWhiteSpace(gamePath)){Status.Text=T("请先在安装器中选择游戏目录","請先在安裝工具中選擇遊戲目錄","Choose the game folder in the patcher first");return;}
  busy=true;var path=gamePath;var ticket=generation;
  try
  {
   var result=await Task.Run(()=>reader.Poll(path));if(closing||ticket!=generation)return;errors=0;retryAfter=DateTimeOffset.MinValue;if(paused)return;
   Status.Text=result.Samples.Count>0?T("游戏实况 · ","遊戲實況 · ","Live game · ")+result.Samples[0].FieldId:result.Status.Contains("尚未支持")?T("当前对白类型尚未支持","目前對白類型尚未支援","This dialogue type is not supported yet"):T("等待游戏对白","等待遊戲對白","Waiting for game dialogue");Status.ToolTip=result.Status;
   if(result.Samples.Count==0){lastLive="";Live.Text=T("历史记录已保留，可继续校对。","歷史記錄已保留，可繼續校對。","History is retained; you can continue reviewing.");return;}
   Live.Text=string.Join("\n",result.Samples.Select(s=>$"{s.Speaker}：{s.Text}"));foreach(var sample in result.Samples)AcceptSample(sample);
  }
  catch(Exception ex)
  {
   if(closing||ticket!=generation)return;
   lastLive="";Status.ToolTip=ex.Message;reader.Dispose();
   if(ex is NotSupportedException)
   {
    paused=true;SetToggle();Status.Text=T("游戏版本或读取布局不受支持，已停止读取：","遊戲版本或讀取配置不受支援，已停止讀取：","Unsupported game build or reader layout; reading stopped: ")+ex.Message;
   }
   else
   {
    errors=Math.Min(errors+1,4);
    var seconds=Math.Min(1<<Math.Max(0,errors-1),8);
    retryAfter=DateTimeOffset.UtcNow.AddSeconds(seconds);
    Status.Text=T($"读取暂不可用，{seconds} 秒后自动重试；草稿不受影响",$"讀取暫不可用，{seconds} 秒後自動重試；草稿不受影響",$"Reading unavailable; retrying in {seconds}s. Drafts are safe.");
   }
  }
  finally{busy=false;if(closing||resetReader){reader.Dispose();resetReader=false;}}
 }
 void AcceptSample(DialogueSample sample)
 {
  var before=store.Entries.Count;var line=catalog.Find(sample,language);
  var previous=store.Entries.FirstOrDefault(e=>e.Sample.Identity==sample.Identity&&e.Language==language&&e.PayloadHash==(line?.PayloadHash??""));
  var changed=previous?.Sample.RawText!=sample.RawText;
  var entry=store.Add(sample,index.Find(sample),language,line,runtimeLanguage);
  if(store.Entries.Count>before){RefreshHistory();SaveSoon();}
  else if(changed){SaveSoon();if(line is null){History.Items.Refresh();if(selected==entry)SelectEntry(entry);}}
  if(lastLive!=entry.Identity&&Follow.IsChecked==true){SelectListEntry(entry);}lastLive=entry.Identity;
 }
 void SelectListEntry(DialogueEntry entry){if(entry.HasSuggestion){Modified.SelectedItem=entry;Modified.ScrollIntoView(entry);}else{History.SelectedItem=entry;History.ScrollIntoView(entry);}SelectEntry(entry);}
 void SelectEntry(DialogueEntry? entry)
 {
  selected=entry;loading=true;
  try
  {
   Original.Text=entry is null?"":entry.OriginalRawText.Length>0?entry.OriginalText:T("该语言包未找到完整译文；以下仅为游戏片段：\n","此語言包未找到完整譯文；以下僅為遊戲片段：\n","No full target translation; captured game fragment only:\n")+entry.Sample.Text;
   Suggestion.Text=entry?.SuggestedText??"";Note.Text=entry?.Note??"";Suggestion.IsEnabled=Note.IsEnabled=entry is not null;EntryKey.Text=entry?.Sample.Key??"";ConfirmLanguageButton.Visibility=entry?.Language=="unknown"?Visibility.Visible:Visibility.Collapsed;
   if(entry is null){Details.Text="";return;}
   Details.Text=$"Language: {entry.Language}\nLanguage evidence: {entry.LanguageEvidence}\nInstalled language (record): {entry.RuntimeLanguage}\nKey: {entry.Sample.Key}\nField: {entry.Sample.FieldId}\nPack: {entry.CatalogVersion}\nPayload SHA-256: {entry.PayloadHash}\nOriginal SHA-256: {entry.OriginalTextHash}\n"+
    (entry.Resource is {} r?$"Bundle: {r.Bundle}\nObject: {r.ObjectName}\nPath ID: {r.ObjectPathId}\nField path: {r.FieldPath}\nRecord ID: {r.RecordId}":"Resource mapping unavailable")+
    (entry.LegacySuggestions.Count>0?"\nLegacy conflicts: "+System.Text.Json.JsonSerializer.Serialize(entry.LegacySuggestions,DialogueStore.JsonOptions):"");
  }
  finally{loading=false;}
 }
 void Edited(object sender,TextChangedEventArgs e){if(loading||selected is null)return;Follow.IsChecked=false;selected.SuggestedText=Suggestion.Text;selected.Note=Note.Text;selected.EditedAt=DateTimeOffset.UtcNow;if(!store.Entries.Contains(selected))store.Entries.Add(selected);RefreshHistory();SaveSoon();}
 void CopyOriginal(object sender,RoutedEventArgs e){if(selected is not null&&selected.OriginalRawText.Length>0)Suggestion.Text=selected.OriginalText;}
 void ClearSuggestion(object sender,RoutedEventArgs e){Suggestion.Clear();Note.Clear();}
 void ConfirmLegacyLanguage(object sender,RoutedEventArgs e)=>RunSafe(()=>
 {
  if(selected is null||selected.Language!="unknown")return;
  var line=catalog.Find(selected.Sample,language)??throw new InvalidOperationException(T("语言包中没有此条目","語言包中沒有此條目","Entry absent from language pack"));
  if(MessageBox.Show(this,T("将此旧建议标记为","將此舊建議標記為","Assign this legacy suggestion to")+" "+LanguageName+"?",Title,MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
  var old=selected;var resolved=store.Add(old.Sample,old.Resource,language,line,runtimeLanguage);
  if(resolved.HasSuggestion&&(resolved.SuggestedText!=old.SuggestedText||resolved.Note!=old.Note))resolved.LegacySuggestions.Add(new(old.SuggestedText,old.Note,old.EditedAt));
  else{resolved.SuggestedText=old.SuggestedText;resolved.Note=old.Note;resolved.EditedAt=old.EditedAt;}
  resolved.LegacySuggestions.AddRange(old.LegacySuggestions);resolved.LanguageEvidence="user_confirmed_legacy";store.Entries.Remove(old);RefreshHistory();History.SelectedItem=resolved;SaveSoon();
 });
 void SaveSoon(){Saved.Text=T("会话已更新","工作階段已更新","Session updated");if(!saveTimer.IsEnabled)saveTimer.Start();}
 bool SaveNow(){saveTimer.Stop();try{store.Save();Saved.Text=T("已修改","已修改","Edited")+" · "+store.Changes().Count;return true;}catch(Exception ex){Saved.Text=T("保存失败","儲存失敗","Save failed");Saved.ToolTip=ex.Message;return false;}}
 void ValidateExport()
 {
  if(!SaveNow())throw new IOException(T("请先解决草稿保存问题","請先解決草稿儲存問題","Resolve the draft save error first"));
  if(store.Changes().Count==0)throw new InvalidOperationException(T("还没有修改建议","尚無修改建議","No suggestions to export"));
  if(store.Changes().Any(x=>x.Language=="unknown"))throw new InvalidOperationException(T("旧建议有未确认语言的条目，请先选中并确认语言。","舊建議含未確認語言的項目，請先選取並確認語言。","Some legacy suggestions have no language. Select each and confirm its language first."));
 }
 void Export(object sender,RoutedEventArgs e)=>RunSafe(()=>
 {
  ValidateExport();var dialog=new SaveFileDialog{Title=T("导出修改建议","匯出修改建議","Export suggestions"),Filter="ZIP (*.zip)|*.zip",FileName="Zephyr-suggestions-"+string.Join("+",store.Changes().Select(x=>x.Language).Distinct())+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".zip"};
  if(dialog.ShowDialog(this)==true){store.Export(dialog.FileName);Saved.Text=T("已导出","已匯出","Exported");}
 });
 void PreviewIssue(object sender,RoutedEventArgs e)=>RunSafe(()=>
 {
  ValidateExport();var body=store.IssueMarkdown(store.Changes());
  var dialog=new Window{Title=T("预览提交 · 不会自动上传","預覽提交 · 不會自動上傳","Preview issue · no automatic upload"),Owner=this,Width=800,Height=640,WindowStartupLocation=WindowStartupLocation.CenterOwner};var dock=new DockPanel{Margin=new Thickness(18)};dialog.Content=dock;
  var button=new Button{Content=T("复制并打开 GitHub","複製並開啟 GitHub","Copy and open GitHub"),Margin=new Thickness(0,10,0,0)};DockPanel.SetDock(button,Dock.Bottom);dock.Children.Add(button);
  button.Click+=(_,_)=>RunSafe(()=>{Clipboard.SetText(body);Process.Start(new ProcessStartInfo("https://github.com/wanjizheng/zephyr-remastered-multilingual/issues/new"){UseShellExecute=true});MessageBox.Show(dialog,T("已复制。请粘贴正文、附上导出包，再在网页提交。","已複製。請貼上內文、附上匯出檔，再於網頁提交。","Copied. Paste into the issue, attach the export, then submit on GitHub."));});
  dock.Children.Add(new TextBox{Text=body,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});dialog.ShowDialog();
 });
 public void LoadPreview(DialogueSample sample){AcceptSample(sample);SelectListEntry(store.Entries.Last());Live.Text=sample.Speaker+"："+sample.Text;}
 public void ValidateEditorFlow()
 {
  var first=selected??throw new InvalidOperationException("Missing preview row");Suggestion.Text=T("我叫希尔弗。看来，你让我的手下吃了不少苦头。","我叫希爾弗。看來，你讓我的手下吃了不少苦頭。","I'm Silph. It seems you've given my men quite a hard time.");Note.Text=T("界面测试；非正式译文。","介面測試；非正式譯文。","UI test; not an approved translation.");
  if(Follow.IsChecked==true||selected!=first||first.SuggestedText!=Suggestion.Text)throw new InvalidOperationException("Draft focus protection failed");
  var n=store.Entries.Count;AcceptSample(first.Sample with{RawText="打字中"});if(store.Entries.Count!=n)throw new InvalidOperationException("Typewriter duplicate");
  AcceptSample(first.Sample with{Key="@kfl05_s03/005",RawText="Next"});if(selected!=first||Suggestion.Text!=first.SuggestedText)throw new InvalidOperationException("Incoming dialogue overwrote draft");
  History.SelectedIndex=History.Items.Count-1;if(Suggestion.Text!="")throw new InvalidOperationException("Draft leaked");SelectListEntry(first);if(Suggestion.Text!=first.SuggestedText)throw new InvalidOperationException("Draft restore failed");
  var initialLanguage=language;var alternate=language=="en"?"zh-Hans":"en";
  ChangeContext(gamePath,alternate);Follow.IsChecked=true;AcceptSample(first.Sample);
  if(selected?.Language!=alternate||Original.Text!=catalog.Find(first.Sample,alternate)?.Text||Suggestion.Text!="")throw new InvalidOperationException("Language switch leaked draft or showed wrong pack");
  ChangeContext(gamePath,initialLanguage);AcceptSample(first.Sample);SelectListEntry(first);
  if(Suggestion.Text!=first.SuggestedText)throw new InvalidOperationException("Language switch lost draft");
  if(!SaveNow())throw new IOException("UI session update failed");Directory.CreateDirectory(store.DirectoryPath);store.Export(Path.Combine(store.DirectoryPath,"ui-test-export.zip"));
 }
}
