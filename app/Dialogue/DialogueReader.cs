using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ZephyrPatcher.Dialogue;

public sealed record ProbeResult(string Status, List<DialogueSample> Samples)
{
 public List<string> Diagnostics { get; init; } = [];
}

// Same SHA-gated pointer chain as experiments/dialogue-watcher-poc/read_snapshot.py.
// No debugger, process writes, remote calls, hooks or game-directory changes.
public sealed class DialogueReader : IDisposable
{
 public const string SupportedHash="ccdcca011a443114da40968ae486b3530efd9657cd41989667af052fb4a024a9";
 // Each offset is read from the SHA-gated ProcessFrame call to GetFieldLocalizedText
 // and the stored UI reference, not inferred from similar command names.
 public static (ulong Index,ulong Box,string BoxType)? Layout(string type)=>type switch
 {
  "NPCTalk_ThenObject" or "NPCTalk3_ThenObject" => (0x44,0x50,"UITextBoxBalloon"),
  "NPCTalk2_ThenObject" => (0x34,0x48,"UITextBoxBalloon"),
  "NPCTalk4_ThenObject" => (0x34,0x40,"UITextBoxBalloon"),
  "NPCTalkFace_ThenObject" or "NPCTalkFace3_ThenObject" => (0x44,0x38,"UITextBoxBottom"),
  "MPCTalk_ThenObject" or "MPCTalk3_ThenObject" => (0x40,0x50,"UITextBoxBalloon"),
  "MPCTalk2_ThenObject" or "MPCTalk4_ThenObject" => (0x30,0x48,"UITextBoxBalloon"),
  "MPCTalkFace_ThenObject" => (0x40,0x38,"UITextBoxBottom"),
  "SignTalk_ThenObject" => (0x40,0x58,"UITextBoxBalloon"),
  "SignTalk2_ThenObject" => (0x30,0x48,"UITextBoxBalloon"),
  _ => null
 };
 IntPtr handle; Process? process; ulong moduleBase; string verifiedHash="";
 readonly DialogueCatalog catalog=new();
 public void Dispose(){if(handle!=IntPtr.Zero){CloseHandle(handle);handle=IntPtr.Zero;}process?.Dispose();process=null;}
 public ProbeResult Poll(string gameDirectory)
 {
  var exe=Path.GetFullPath(Path.Combine(gameDirectory,"ZephyrRemastered.exe"));
  if(process is not null && process.HasExited)Dispose();
  if(process is null)
  {
   var all=Process.GetProcessesByName("ZephyrRemastered");
   var matches=new List<Process>();
   foreach(var candidate in all)
   {
    try{if(string.Equals(candidate.MainModule?.FileName,exe,StringComparison.OrdinalIgnoreCase))matches.Add(candidate);else candidate.Dispose();}
    catch{candidate.Dispose();foreach(var p in matches)p.Dispose();throw;}
   }
   if(matches.Count!=1){foreach(var p in matches)p.Dispose();return new(matches.Count==0?"等待游戏启动或进入场景":"发现多个游戏进程，请只保留一个",[]);}
   process=matches[0];
   try
   {
    var module=process.Modules.Cast<ProcessModule>().Single(m=>m.ModuleName=="GameAssembly.dll");
    using var file=File.OpenRead(module.FileName);
    verifiedHash=Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
    if(verifiedHash!=SupportedHash)throw new NotSupportedException("当前游戏版本尚未支持，已停止读取。请更新校对助手。");
    moduleBase=(ulong)module.BaseAddress;
    handle=OpenProcess(0x410,false,process.Id); // QUERY_INFORMATION | VM_READ
    if(handle==IntPtr.Zero)throw new IOException("无法以只读权限打开游戏进程。");
    if(!Read(moduleBase+0x767f10,8).SequenceEqual(Convert.FromHexString("488B81D8000000C3")))throw new NotSupportedException("运行中游戏的字段布局不匹配。");
   }
   catch{Dispose();throw;}
  }
  var cls=Q(moduleBase+0x5446d20);if(!Pointer(cls))return new("等待场景初始化",[]);
  var stat=Q(cls+0xb8);if(!Pointer(stat))return new("等待场景初始化",[]);
  var field=Q(stat);if(!Pointer(field))return new("等待进入游戏场景",[]);
  Expect(field,"Field");var fieldIdPointer=Q(field+0xd8);
  if(!Pointer(fieldIdPointer))return new("等待场景名称初始化",[]);
  var fieldId=String(fieldIdPointer);
  var runner=Q(field+0x18);if(!Pointer(runner))return new("等待剧情初始化",[]);
  Expect(runner,"EventRunner");var eventList=Q(runner+0x10);
  if(!Pointer(eventList))return new("等待剧情列表初始化",[]);
  var events=List(eventList);
  var config=Q(Q(Q(moduleBase+0x5497aa0)+0xb8)+8);Expect(config,"ZRUserConfig");
  var prefix=String(Q(config+0x28));
  var samples=new List<DialogueSample>();var unsupported=new HashSet<string>();var diagnostics=new List<string>();
  foreach(var ev in events)
  {
   Expect(ev,"EventInfo");if(Byte(ev+0x18)==0)continue;
   diagnostics.Add($"running_event=0x{ev:x}");
   var commands=List(Q(ev+0x30));
   foreach(var cmd in commands)
   {
    if(Byte(cmd+0x28)!=0 || I(cmd+0x2c)<=0)continue;
    var type=ClassName(cmd);var layout=Layout(type);
    if(layout is null){if(type.Contains("Talk",StringComparison.Ordinal))unsupported.Add(type);diagnostics.Add($"active_command={type}@0x{cmd:x}");continue;}
    var (indexOffset,boxOffset,boxType)=layout.Value;
    var index=I(cmd+indexOffset);if(index<0 || index>100000)throw new InvalidDataException("台词序号超出合理范围。");
    var box=Q(cmd+boxOffset);if(!Pointer(box)){diagnostics.Add($"missing_box={type}@0x{cmd:x}");continue;}Expect(box,boxType);
    if(String(Q(moduleBase+0x546d2c0))!="@k{0}/{1:D03}")throw new NotSupportedException("台词键格式已变化。");
    var body=Q(box+0x28);var speaker=Q(box+0x98);
    Expect(body,"TextMeshProUGUI");Expect(speaker,"TextMeshProUGUI");
    var bodyPtr=Q(body+0xe0);var namePtr=Q(speaker+0xe0);
    if(!Pointer(bodyPtr))continue;
    var text=String(bodyPtr);var name=Pointer(namePtr)?String(namePtr):"";
    // Best-effort consistency check: do not label a stale object as current after a switch.
    if(Byte(ev+0x18)==0 || Byte(cmd+0x28)!=0 || I(cmd+indexOffset)!=index || Q(cmd+boxOffset)!=box
      || Q(body+0xe0)!=bodyPtr || Q(speaker+0xe0)!=namePtr || Q(stat)!=field
      || String(Q(field+0xd8))!=fieldId || !List(Q(ev+0x30)).Contains(cmd)){diagnostics.Add($"changed_during_read={type}@0x{cmd:x}");continue;}
    if(string.IsNullOrEmpty(text)){diagnostics.Add($"empty_text={type}@0x{cmd:x}");continue;}
    samples.Add(new(process!.Id,verifiedHash,fieldId,$"@k{fieldId}/{index:D3}",prefix,type,
     $"0x{cmd:x}",$"0x{box:x}",name,text,DateTimeOffset.UtcNow));
   }
  }
  // UITextBoxBase.AddToTalkBoxStack/RemoveFromTalkBoxStack own this live list.
  // Some NPC explanations remain here after EventInfo commands have finished.
  var stackClass=Q(moduleBase+0x5471fd8);
  if(Pointer(stackClass))
  {
   var stackStatic=Q(stackClass+0xb8);
   if(Pointer(stackStatic))
   {
    var stack=Q(stackStatic);
    if(Pointer(stack))foreach(var box in List(stack))
    {
     var type=ClassName(box);
     if(type is not ("UITextBoxBalloon" or "UITextBoxBottom") || Byte(box+0x74)!=0
       || samples.Any(s=>s.BoxAddress==$"0x{box:x}"))continue;
     var body=Q(box+0x28);Expect(body,"TextMeshProUGUI");
     var ptr=Q(body+0xe0);if(!Pointer(ptr))continue;
     var text=String(ptr);var key=catalog.MatchVisibleKey(prefix,fieldId,text);
     if(key is null){diagnostics.Add($"visible_box_unmatched_or_ambiguous={type}@0x{box:x}");continue;}
     var speaker=Q(box+0x98);Expect(speaker,"TextMeshProUGUI");
     var namePtr=Q(speaker+0xe0);var name=Pointer(namePtr)?String(namePtr):"";
     if(Q(stat)!=field || String(Q(field+0xd8))!=fieldId || Q(stackStatic)!=stack
       || !List(stack).Contains(box) || Byte(box+0x74)!=0 || Q(box+0x28)!=body
       || Q(body+0xe0)!=ptr || Q(speaker+0xe0)!=namePtr)continue;
     samples.Add(new(process!.Id,verifiedHash,fieldId,key,prefix,"VisibleStack/"+type,
      "",$"0x{box:x}",name,text,DateTimeOffset.UtcNow));
     diagnostics.Add("key_origin=unique_full_visible_text_match_in_current_field_catalog");
    }
   }
  }
  return new(samples.Count>0?$"正在读取 · {fieldId}":unsupported.Count>0?"当前对白类型尚未支持："+string.Join(", ",unsupported):$"等待对白 · {fieldId}",samples){Diagnostics=diagnostics};
 }
 static bool Pointer(ulong p)=>p>=0x10000 && p<0x7fffffffffff;
 byte[] Read(ulong address,int count)
 {
  if(!Pointer(address)||count<0||count>65536)throw new InvalidDataException("无效的内存读取范围。");
  var data=new byte[count];
  if(!ReadProcessMemory(handle,(IntPtr)address,data,(nuint)count,out var read)||read!=(nuint)count)
   throw new IOException("场景正在切换或读取失败（"+Marshal.GetLastWin32Error()+"）。");
  return data;
 }
 ulong Q(ulong a)=>BitConverter.ToUInt64(Read(a,8));
 int I(ulong a)=>BitConverter.ToInt32(Read(a,4));
 byte Byte(ulong a)=>Read(a,1)[0];
 string ClassName(ulong a){var data=Read(Q(Q(a)+0x10),128);var end=Array.IndexOf(data,(byte)0);return Encoding.UTF8.GetString(data,0,end<0?data.Length:end);}
 void Expect(ulong a,string type){if(ClassName(a)!=type)throw new InvalidDataException("运行时对象类型不匹配："+type);}
 string String(ulong a){Expect(a,"String");var n=I(a+0x10);if(n<0||n>8192)throw new InvalidDataException("字符串长度异常。");return n==0?"":Encoding.Unicode.GetString(Read(a+0x14,n*2));}
 ulong[] List(ulong a)
 {
  Expect(a,"List`1");var n=I(a+0x18);if(n<0||n>2048)throw new InvalidDataException("列表长度异常。");
  if(n==0)return [];var arr=Q(a+0x10);if(Q(arr+0x18)<(ulong)n)throw new InvalidDataException("列表边界异常。");
  var data=Read(arr+0x20,n*8);return Enumerable.Range(0,n).Select(i=>BitConverter.ToUInt64(data,i*8)).ToArray();
 }
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool ReadProcessMemory(IntPtr h,IntPtr address,[Out]byte[] data,nuint size,out nuint read);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr h);
}
