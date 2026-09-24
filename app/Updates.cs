using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
namespace ZephyrPatcher;
public static class Updates
{
 public const string Current="0.4.8";
 public static int Compare(string a,string b)
 {
  static (int[] Core,string[] Pre) Parse(string s){var p=s.TrimStart('v').Split('-',2);var core=p[0].Split('.').Select(int.Parse).ToArray();if(core.Length!=3)throw new FormatException();return(core,p.Length==2?p[1].Split('.'):[]);}
  var x=Parse(a);var y=Parse(b);for(int i=0;i<3;i++){int d=x.Core[i].CompareTo(y.Core[i]);if(d!=0)return d;}
  if(x.Pre.Length==0||y.Pre.Length==0)return (x.Pre.Length==0?1:0).CompareTo(y.Pre.Length==0?1:0);
  for(int i=0;i<Math.Min(x.Pre.Length,y.Pre.Length);i++){bool nx=int.TryParse(x.Pre[i],out int ix),ny=int.TryParse(y.Pre[i],out int iy);int d=nx&&ny?ix.CompareTo(iy):nx?-1:ny?1:string.CompareOrdinal(x.Pre[i],y.Pre[i]);if(d!=0)return d;}return x.Pre.Length.CompareTo(y.Pre.Length);
 }
 public static async Task<string?> Check()
 {
  using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(12)};http.DefaultRequestHeaders.UserAgent.ParseAdd("ZephyrChinesePatcher/0.1");
  var json=await http.GetStringAsync("https://api.github.com/repos/wanjizheng/zephyr-remastered-zh-cn/releases?per_page=20");using var doc=JsonDocument.Parse(json);
  foreach(var release in doc.RootElement.EnumerateArray())
  {
   if(release.GetProperty("draft").GetBoolean())continue;string tag=release.GetProperty("tag_name").GetString()!;
   if(!Current.Contains('-')&&release.GetProperty("prerelease").GetBoolean())continue;
   if(Compare(tag,Current)<=0)continue;
   // Fixed owner/repository and signed metadata; never run a remote executable.
   string root="https://github.com/wanjizheng/zephyr-remastered-zh-cn/releases/download/"+Uri.EscapeDataString(tag)+"/";
   byte[] metadata=await http.GetByteArrayAsync(root+"release.json"),signature=await http.GetByteArrayAsync(root+"release.sig");
   VerifyMetadata(metadata,signature,tag,ReleaseKey.PublicPem);
   return "https://github.com/wanjizheng/zephyr-remastered-zh-cn/releases/tag/"+Uri.EscapeDataString(tag);
  }
  return null;
 }
 internal static void VerifyMetadata(byte[] metadata,byte[] signature,string tag,string publicPem)
 {
  if(metadata.Length>65536||signature.Length>4096)throw new InvalidDataException();
  using var rsa=RSA.Create();rsa.ImportFromPem(publicPem);
  if(!rsa.VerifyData(metadata,signature,HashAlgorithmName.SHA256,RSASignaturePadding.Pss))throw new CryptographicException("更新签名无效");
  using var manifest=JsonDocument.Parse(metadata);
  if(manifest.RootElement.GetProperty("version").GetString()!=tag.TrimStart('v'))throw new InvalidDataException();
 }
}
