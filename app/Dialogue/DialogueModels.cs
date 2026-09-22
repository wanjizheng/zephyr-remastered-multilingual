using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZephyrPatcher.Dialogue;

public sealed record ResourceLocation(string Bundle, string SerializedFile, string ObjectPathId,
 string ObjectName, string FieldPath, string RecordId, string SourceHash);

public sealed record DialogueSample(int Pid, string GameHash, string FieldId, string Key, string Prefix,
 string CommandType, string CommandAddress, string BoxAddress, string Speaker, string RawText, DateTimeOffset CapturedAt)
{
 public string KeyOrigin => CommandType.StartsWith("VisibleStack/",StringComparison.Ordinal)
  ? "unique_full_visible_text_match_in_current_field_catalog" : "active_command_index";
 public string Text => Regex.Replace(RawText, @"</?(?:color|size|b|i|u|s|alpha|font|mark|voffset|cspace|mspace)(?:=[^>]*)?>", "");
 public string Identity => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
  JsonSerializer.Serialize(new[]{GameHash,Prefix,FieldId,Key}))));
}

public sealed class DialogueEntry
{
 public DialogueSample Sample { get; set; } = null!;
 public ResourceLocation? Resource { get; set; }
 public string Language { get; set; } = "unknown";
 public string LanguageEvidence { get; set; } = "unspecified_legacy";
 public string RuntimeLanguage { get; set; } = "unknown";
 public string RuntimeLanguageEvidence { get; set; } = "installer_state_not_runtime_proof";
 public string CatalogVersion { get; set; } = "";
 public string PayloadHash { get; set; } = "";
 public string OriginalRawText { get; set; } = "";
 public string OriginalText => OriginalRawText.Length>0?GameText.Visible(OriginalRawText):Sample.Text;
 public string OriginalTextHash => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(OriginalRawText))).ToLowerInvariant();
 public string Identity => Sample.Identity+"|"+Language+"|"+PayloadHash;
 public List<LegacySuggestion> LegacySuggestions { get; set; } = [];
 public bool NeedsReview => Language=="unknown" || OriginalRawText.Length==0 || LegacySuggestions.Count>0;
 public string SuggestedText { get; set; } = "";
 public string Note { get; set; } = "";
 public DateTimeOffset? EditedAt { get; set; }
 public bool HasSuggestion => !string.IsNullOrWhiteSpace(SuggestedText) || !string.IsNullOrWhiteSpace(Note);
 public override string ToString() => $"{(HasSuggestion ? "● " : "")}{Sample.Key}\n{OriginalText.Replace('\n',' ')}";
}
public sealed record LegacySuggestion(string Text,string Note,DateTimeOffset? EditedAt);

public sealed class DialogueStore : IDisposable
{
 public const int SchemaVersion=2;
 public static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true};
 public string DirectoryPath { get; }
 public List<DialogueEntry> Entries { get; } = [];
 readonly FileStream? directoryLock;
 readonly bool sessionOnly;
 public void Dispose()=>directoryLock?.Dispose();
 public DialogueStore(string directory,bool sessionOnly=false)
 {
  DirectoryPath=directory;this.sessionOnly=sessionOnly;
  if(sessionOnly)return; // Never load or write earlier sessions.
  Directory.CreateDirectory(directory);
  directoryLock=new FileStream(Path.Combine(directory,"session.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
  try
  {
  var path=Path.Combine(directory,"drafts.json");
  if(File.Exists(path))
  {
   var document=JsonSerializer.Deserialize<DraftDocument>(File.ReadAllText(path),JsonOptions)
     ?? throw new InvalidDataException("草稿文件为空，未覆盖原文件。");
   if(document.SchemaVersion is not (1 or SchemaVersion) || document.Entries is null || document.Entries.Any(x=>x.Sample is null))
    throw new InvalidDataException("草稿版本或内容不受支持，未覆盖原文件。");
   if(document.SchemaVersion==1)
   {
    File.Copy(path,Path.Combine(directory,"drafts.schema1-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff")+".json"),false);
    var catalog=new DialogueCatalog();
    foreach(var group in document.Entries.GroupBy(x=>x.Sample.Identity))
    {
     var best=group.MaxBy(x=>x.Sample.Text.Length)!;var line=catalog.MatchLegacy(best.Sample);
     var merged=Add(best.Sample,best.Resource,line?.Language??"unknown",line);
     merged.LanguageEvidence=line is null?"unresolved_legacy":"unique_exact_catalog_text_match";
     var edits=group.Where(x=>x.HasSuggestion).Select(x=>new LegacySuggestion(x.SuggestedText,x.Note,x.EditedAt)).DistinctBy(x=>(x.Text,x.Note)).ToList();
     if(edits.Count>0){merged.SuggestedText=edits[0].Text;merged.Note=edits[0].Note;merged.EditedAt=edits[0].EditedAt;}
     if(edits.Count>1)merged.LegacySuggestions=edits;
    }
    Save(); // Persist migration even when the game is not running.
   }
   else Entries.AddRange(document.Entries);
  }
  }
  catch{directoryLock.Dispose();throw;}
 }
 public DialogueEntry Add(DialogueSample sample, ResourceLocation? resource,string language="unknown",CatalogLine? line=null,string runtimeLanguage="unknown")
 {
  var id=sample.Identity+"|"+language+"|"+(line?.PayloadHash??"");
  var found=Entries.FirstOrDefault(e=>e.Identity==id);
  if(found is not null){if(sample.Text.Length>=found.Sample.Text.Length)found.Sample=sample;return found;}
  var entry=new DialogueEntry{Sample=sample,Resource=resource,Language=language,LanguageEvidence="installer_selection",
   RuntimeLanguage=runtimeLanguage,CatalogVersion=line?.Version??"",PayloadHash=line?.PayloadHash??"",OriginalRawText=line?.RawText??""};Entries.Add(entry);return entry;
 }
 public void Save()
 {
  if(sessionOnly)return;
  var path=Path.Combine(DirectoryPath,"drafts.json");var temp=path+".tmp";
  var data=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new DraftDocument(SchemaVersion,Entries),JsonOptions));
  using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
  {stream.Write(data);stream.Flush(true);}
  if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
 }
 public List<DialogueEntry> Changes()=>Entries.Where(e=>e.HasSuggestion).ToList();
 public void TrimRecent(){var unedited=Entries.Where(e=>!e.HasSuggestion).ToList();foreach(var e in unedited.Take(Math.Max(0,unedited.Count-20)))Entries.Remove(e);}
 public IEnumerable<DialogueEntry> Recent()=>Entries.Where(e=>!e.HasSuggestion).TakeLast(20).Reverse();
 public string IssueMarkdown(IEnumerable<DialogueEntry> entries)
 {
  var rows=entries.ToList();
  var text=new StringBuilder("## 翻译修改建议\n\n来自对白校对助手；建议尚未应用到游戏。\n\n");
  foreach(var e in rows)
  {
   text.AppendLine($"### {e.Sample.Key} [{e.Language}]\n");
   text.AppendLine($"- target_language: {e.Language}\n- language_evidence: {e.LanguageEvidence}\n- payload_version: {e.CatalogVersion}\n- payload_sha256: {e.PayloadHash}\n- original_text_sha256: {e.OriginalTextHash}\n- needs_review: {e.NeedsReview}\n");
   text.AppendLine($"- 场景：{e.Sample.FieldId}\n- 资源分支：{e.Sample.Prefix}\n- 显示姓名：{e.Sample.Speaker}");
   if(e.Resource is { } r)text.AppendLine($"- 资源包：{r.Bundle}\n- 对象：{r.ObjectName}\n- Path ID：{r.ObjectPathId}\n- 字段：{r.FieldPath}\n- record_id：{r.RecordId}");
   else text.AppendLine("- 资源映射：未匹配；仅有运行时定位，不可自动应用。");
   text.AppendLine($"- GameAssembly SHA-256：{e.Sample.GameHash}\n- 捕获时间：{e.Sample.CapturedAt:O}\n");
   text.AppendLine("语言包原文：\n"+Quote(e.OriginalText)+"\n\n建议文本：\n"+Quote(e.SuggestedText)+"\n\n说明：\n"+Quote(e.Note)+"\n");
  }
  text.AppendLine("请附上导出的 JSON（或 ZIP），用于精确定位。资源坐标来自随附索引，未逐条核验本机安装包；应用前需核对版本与原文。\n建议仅是可见正文；姓名、停顿和控制标记须由维护者另行处理。");
  return text.ToString();
 }
 static string Quote(string text)=>string.Join('\n',text.Replace("\r","").Split('\n').Select(s=>"> "+s));
 public void Export(string path)
 {
  var entries=Changes();if(entries.Count==0)throw new InvalidOperationException("还没有建议译文或说明。");
  if(entries.Any(x=>!DialogueCatalog.Languages.Contains(x.Language)))throw new InvalidOperationException("Target language must be confirmed before export.");
  var document=new {schema_version=SchemaVersion,target_languages=entries.Select(x=>x.Language).Distinct().ToArray(),exported_at=DateTimeOffset.UtcNow,
   suggestion_scope="visible_body_only_preserve_control_tokens_on_review",resource_mapping="bundled_registry_not_live_asset_verification",entries};
  var files=new Dictionary<string,byte[]>{
   ["translation-suggestions.json"]=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(document,JsonOptions)),
   ["github-issue.md"]=Encoding.UTF8.GetBytes(IssueMarkdown(entries))};
  files["SHA256SUMS.txt"]=Encoding.UTF8.GetBytes(string.Join('\n',files.Select(f=>$"{Convert.ToHexString(SHA256.HashData(f.Value)).ToLowerInvariant()}  {f.Key}"))+"\n");
  var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try
  {
   using(var zip=ZipFile.Open(temp,ZipArchiveMode.Create))
    foreach(var f in files){using var stream=zip.CreateEntry(f.Key).Open();stream.Write(f.Value);}
   using(var check=ZipFile.OpenRead(temp))
    foreach(var f in files){using var stream=check.GetEntry(f.Key)!.Open();if(!SHA256.HashData(stream).SequenceEqual(SHA256.HashData(f.Value)))throw new IOException("导出校验失败。");}
   File.Move(temp,path,true);
  }
  finally{if(File.Exists(temp))File.Delete(temp);}
 }
 public sealed record DraftDocument(int SchemaVersion,List<DialogueEntry> Entries);
}

public sealed class DialogueIndex
{
 readonly Dictionary<string,ResourceLocation> rows;
 public DialogueIndex()
 {
  using var stream=typeof(DialogueIndex).Assembly.GetManifestResourceStream("ZephyrPatcher.DialogueIndex.json")
   ?? throw new FileNotFoundException("对白资源索引缺失。");
  rows=JsonSerializer.Deserialize<Dictionary<string,ResourceLocation>>(stream) ?? throw new InvalidDataException("对白资源索引无效。");
 }
 public ResourceLocation? Find(DialogueSample s)=>rows.GetValueOrDefault(s.Prefix+"|"+s.Key);
}
