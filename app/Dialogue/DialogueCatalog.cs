using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZephyrPatcher.Dialogue;

public sealed record CatalogLine(string Language,string Version,string PayloadHash,string RawText)
{
 public string Text=>GameText.Visible(RawText);
 public string Hash=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(RawText))).ToLowerInvariant();
}
public static class GameText
{
 public static string Visible(string raw)
 {
  var s=Regex.Replace(raw,@"</?(?:color|size|b|i|u|s|alpha|font|mark|voffset|cspace|mspace)(?:=[^>]*)?>", "");
  s=Regex.Replace(s,@"^\[[^\]\r\n]*\](?:/n)?","");
  s=Regex.Replace(s,@"/\d+d","").Replace("/n","\n");
  return s.Replace("\r\n","\n");
 }
}
public sealed class DialogueCatalog
{
 public sealed record Pack(string Version,string PayloadHash,Dictionary<string,string> Lines);
 readonly Dictionary<string,Pack> packs;
 public string? MatchVisibleKey(string prefix,string fieldId,string text)=>UniqueVisibleKey(
  packs.Values.SelectMany(p=>p.Lines),prefix,fieldId,text);
 public static string? UniqueVisibleKey(IEnumerable<KeyValuePair<string,string>> lines,string prefix,string fieldId,string text)
 {
  var visible=GameText.Visible(text);
  if(string.IsNullOrWhiteSpace(visible))return null;
  var scope=prefix+"|@k"+fieldId+"/";
  var keys=lines.Where(x=>x.Key.StartsWith(scope,StringComparison.Ordinal) && GameText.Visible(x.Value)==visible)
   .Select(x=>x.Key[(prefix.Length+1)..]).Distinct(StringComparer.Ordinal).Take(2).ToArray();
  return keys.Length==1?keys[0]:null;
 }
 public static readonly string[] Languages=["zh-Hans","zh-Hant","en"];
 public DialogueCatalog()
 {
  using var stream=typeof(DialogueCatalog).Assembly.GetManifestResourceStream("ZephyrPatcher.DialogueCatalogs.json")??throw new FileNotFoundException("Missing dialogue catalogs");
  packs=JsonSerializer.Deserialize<Dictionary<string,Pack>>(stream)!;
 }
 public CatalogLine? Find(DialogueSample sample,string language)=>packs.TryGetValue(language,out var pack) && pack.Lines.TryGetValue(sample.Prefix+"|"+sample.Key,out var text)?new(language,pack.Version,pack.PayloadHash,text):null;
 public CatalogLine? MatchLegacy(DialogueSample sample)
 {
  var matches=Languages.Select(l=>Find(sample,l)).Where(x=>x is not null && x.Text.Trim()==sample.Text.Trim()).ToArray();
  return matches.Length==1?matches[0]:null; // Ambiguous or partial text never guesses a language.
 }
 public static string InstalledLanguage(string gameDirectory)
 {
  try
  {
   var normalized=Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameDirectory)).ToLowerInvariant();
   var key=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant()[..24];
   var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ZephyrChinesePatcher",key,"state.json");
   using var json=JsonDocument.Parse(File.ReadAllText(path));
   var lang=json.RootElement.GetProperty("language").GetString();return Languages.Contains(lang)?lang!:"unknown";
  }
  catch{return "unknown";}
 }
}
