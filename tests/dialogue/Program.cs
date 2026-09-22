using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using ZephyrPatcher.Dialogue;

if(args.Length>0 && args[0]=="--watch")
{
 using var reader=new DialogueReader();
 using var output=new StreamWriter(args[2],append:false,new System.Text.UTF8Encoding(false)){AutoFlush=true};
 var end=DateTimeOffset.UtcNow.AddSeconds(Math.Clamp(int.Parse(args[3]),1,600));int errors=0;
 while(DateTimeOffset.UtcNow<end)
 {
  try
  {
   var result=reader.Poll(args[1]);errors=0;
   output.WriteLine(JsonSerializer.Serialize(new{at=DateTimeOffset.UtcNow,result}));
  }
  catch(Exception ex)
  {
   output.WriteLine(JsonSerializer.Serialize(new{at=DateTimeOffset.UtcNow,error=ex.Message}));
   if(++errors>=2 || ex is NotSupportedException){Environment.ExitCode=2;break;}
  }
  Thread.Sleep(100);
 }
 Console.WriteLine("Read-only trace saved: "+args[2]);return;
}
if(args.Length>0 && args[0]=="--live")
{
 using var reader=new DialogueReader();
 var result=reader.Poll(args[1]);
 var json=JsonSerializer.Serialize(result,DialogueStore.JsonOptions);
 File.WriteAllText(args[2],json);Console.WriteLine(json);
 if(result.Samples.Count==0)Environment.ExitCode=2;
 return;
}
int assertions=0;
void Check(bool value,string name){if(!value)throw new Exception(name);assertions++;Console.WriteLine("PASS "+name);}
var root=Path.Combine(Path.GetTempPath(),"zephyr-dialogue-tests-"+Guid.NewGuid().ToString("N"));
var sample=new DialogueSample(1,DialogueReader.SupportedHash,"fl05_s03","@kfl05_s03/004","text.re/art_","NPCTalk_ThenObject","0x1","0x2","希尔弗","<color=#443B2EFF>同一句。\n第二行",DateTimeOffset.UtcNow);
var index=new DialogueIndex();var catalog=new DialogueCatalog();
var explanation=sample with{FieldId="fltka01",Key="@kfltka01/117"};
var explanationText=catalog.Find(explanation,"zh-Hant")!.Text;
Check(catalog.MatchVisibleKey(sample.Prefix,"fltka01","<color=#443B2EFF>"+explanationText)==explanation.Key,"live explanation resolves by full visible text");
Check(catalog.MatchVisibleKey(sample.Prefix,"fltka01",explanationText[..20]) is null,"partial explanation never guesses a key");
Check(catalog.MatchVisibleKey(sample.Prefix,"other",explanationText) is null,"visible matching cannot cross scenes");
Check(catalog.MatchVisibleKey("unknown","fltka01",explanationText) is null,"visible matching cannot cross resource branches");
var duplicateLines=new[]{new KeyValuePair<string,string>("p|@kf/001","same"),new KeyValuePair<string,string>("p|@kf/002","same")};
Check(DialogueCatalog.UniqueVisibleKey(duplicateLines,"p","f","same") is null,"ambiguous complete text rejected");
Check(DialogueCatalog.UniqueVisibleKey(new[]{duplicateLines[0],duplicateLines[0]},"p","f","same")=="@kf/001","same key in multiple languages is not ambiguity");
Check((explanation with{CommandType="VisibleStack/UITextBoxBalloon"}).KeyOrigin.StartsWith("unique_full"),"export distinguishes matched keys from command indexes");
var cn=catalog.Find(sample,"zh-Hans")!;var en=catalog.Find(sample,"en")!;var tw=catalog.Find(sample,"zh-Hant")!;
Check(cn.Text!=en.Text && cn.Text!=tw.Text,"three real language-pack texts differ");
Check(cn.PayloadHash!=en.PayloadHash,"language pack hashes are distinct");
Check(catalog.Find(sample with{Key="@kunknown/999"},"en") is null,"missing target text does not borrow runtime language");
Check(index.Find(sample)?.ObjectPathId=="7384774387832602567","RE resource mapping");
Check(index.Find(sample)?.FieldPath=="/TextList/4/Text","correct resource paragraph");
Check(index.Find(sample with{Prefix="unknown"}) is null,"unknown prefix fails closed");
using(var store=new DialogueStore(root))
{
 var first=store.Add(sample,index.Find(sample),"zh-Hans",cn);first.SuggestedText="建议 A\n换行与 `标点`";first.Note="理由";
 Check(ReferenceEquals(first,store.Add(sample with{Pid=5,CommandAddress="0x9"},index.Find(sample),"zh-Hans",cn)),"restart reuses matching draft");
 var second=store.Add(sample with{Key="@kfl05_s03/005"},null);
 Check(!ReferenceEquals(first,second),"identical text with different key stays separate");
 Check(second.SuggestedText=="","draft does not leak to another key");
 for(var i=1;i<=80;i++)store.Add(sample with{RawText=new string('字',i)},index.Find(sample),"zh-Hans",cn);
 Check(store.Entries.Count==2,"80 typewriter fragments make only one record");
 Check(first.SuggestedText.StartsWith("建议 A"),"typewriter updates preserve edits");
 Check(first.OriginalText==cn.Text,"full original comes from target pack");
 var english=store.Add(sample,index.Find(sample),"en",en);
 Check(english!=first && english.OriginalText==en.Text && english.SuggestedText=="","same key different language isolated");
 var revised=store.Add(sample,index.Find(sample),"zh-Hans",cn with{PayloadHash="new-pack-hash"});
 Check(revised!=first,"changed language pack preserves separate review");
 store.Save();first.Note="第二次保存";store.Save();
 Check(File.Exists(Path.Combine(root,"drafts.json.bak")),"atomic save retains backup");
 bool locked=false;try{using var other=new DialogueStore(root);}catch(IOException){locked=true;}
 Check(locked,"second instance cannot overwrite drafts");
 var zipPath=Path.Combine(root,"suggestions.zip");store.Export(zipPath);
 using var zip=ZipFile.OpenRead(zipPath);
 using var jsonStream=zip.GetEntry("translation-suggestions.json")!.Open();using var document=JsonDocument.Parse(jsonStream);
 var entries=document.RootElement.GetProperty("entries");
 Check(entries.GetArrayLength()==1,"export only edited entries");
 Check(document.RootElement.GetProperty("schema_version").GetInt32()==2,"schema 2 export");
 Check(entries[0].GetProperty("Language").GetString()=="zh-Hans","per-entry language exported");
 Check(entries[0].GetProperty("PayloadHash").GetString()==cn.PayloadHash,"exact payload fingerprint exported");
 Check(entries[0].GetProperty("SuggestedText").GetString()==first.SuggestedText,"Unicode and newlines roundtrip");
 Check(entries[0].GetProperty("Resource").GetProperty("ObjectPathId").GetString()=="7384774387832602567","64-bit Path ID exported as string");
 using var manifestReader=new StreamReader(zip.GetEntry("SHA256SUMS.txt")!.Open());
 foreach(var line in manifestReader.ReadToEnd().Split('\n',StringSplitOptions.RemoveEmptyEntries))
 {var parts=line.Split("  ",2);using var stream=zip.GetEntry(parts[1])!.Open();Check(Convert.ToHexString(SHA256.HashData(stream)).Equals(parts[0],StringComparison.OrdinalIgnoreCase),"export hash "+parts[1]);}
 Check(store.IssueMarkdown(store.Changes()).Contains("/TextList/4/Text") && store.IssueMarkdown(store.Changes()).Contains("target_language: zh-Hans"),"Issue includes language and locator");
}
using(var restored=new DialogueStore(root))Check(restored.Changes().Single().Note=="第二次保存","drafts survive reopen");
var broken=Path.Combine(root,"broken");Directory.CreateDirectory(broken);File.WriteAllText(Path.Combine(broken,"drafts.json"),"bad json");
bool rejected=false;try{using var store=new DialogueStore(broken);}catch(JsonException){rejected=true;}
Check(rejected && File.ReadAllText(Path.Combine(broken,"drafts.json"))=="bad json","corrupt drafts not overwritten");
var legacyDir=Path.Combine(root,"legacy");Directory.CreateDirectory(legacyDir);
var legacySample=sample with{RawText=cn.Text};
var legacyRows=new[]{new DialogueEntry{Sample=legacySample with{RawText=cn.Text[..4]},SuggestedText="old suggestion"},
 new DialogueEntry{Sample=legacySample},new DialogueEntry{Sample=legacySample,SuggestedText="conflicting suggestion"}};
var legacyJson=JsonSerializer.Serialize(new DialogueStore.DraftDocument(1,legacyRows.ToList()),DialogueStore.JsonOptions);
File.WriteAllText(Path.Combine(legacyDir,"drafts.json"),legacyJson);
using(var migrated=new DialogueStore(legacyDir))
{
 Check(migrated.Entries.Count==1,"legacy typewriter duplicates coalesced");
 using(var migration=JsonDocument.Parse(File.ReadAllText(Path.Combine(legacyDir,"drafts.json"))))Check(migration.RootElement.GetProperty("SchemaVersion").GetInt32()==2,"migration persisted without a running game");
 Check(migrated.Entries[0].Language=="zh-Hans","legacy language resolved only by unique full-text match");
 Check(migrated.Entries[0].LegacySuggestions.Count==2,"conflicting legacy edits retained");
 Check(File.ReadAllText(Directory.GetFiles(legacyDir,"drafts.schema1-*.json").Single())==legacyJson,"original legacy backup byte content preserved");
}
using(var unresolved=new DialogueStore(Path.Combine(root,"unresolved")))
{
 unresolved.Add(sample,null).SuggestedText="unknown language";
 bool blocked=false;try{unresolved.Export(Path.Combine(root,"unknown.zip"));}catch(InvalidOperationException){blocked=true;}
 Check(blocked,"unknown language export blocked");
}
if(args.Length>0 && args[0]=="--legacy-zip")
{
 using var zip=ZipFile.OpenRead(args[1]);using var stream=zip.GetEntry("translation-suggestions.json")!.Open();using var doc=JsonDocument.Parse(stream);
 var entries=JsonSerializer.Deserialize<List<DialogueEntry>>(doc.RootElement.GetProperty("entries"))!;
 var dir=Path.Combine(root,"user-export");Directory.CreateDirectory(dir);
 File.WriteAllText(Path.Combine(dir,"drafts.json"),JsonSerializer.Serialize(new DialogueStore.DraftDocument(1,entries)));
 using var converted=new DialogueStore(dir);
 Check(converted.Changes().Count==entries.Count(x=>x.HasSuggestion),"user ZIP suggestions preserved");
 Check(converted.Changes().All(x=>x.Language=="zh-Hans"),"user ZIP uniquely matches simplified Chinese pack");
 Console.WriteLine("User ZIP migrated in isolated test directory; original untouched.");
}
var sessionPath=Path.Combine(root,"session-only");
using(var session=new DialogueStore(sessionPath,sessionOnly:true))
{
 for(int i=0;i<60;i++)
 {
  var row=session.Add(sample with{Key=$"@test/{i:D3}"},null,"zh-Hans",cn);
  if(i<30)row.SuggestedText="edited "+i;
  session.TrimRecent();
 }
 Check(session.Changes().Count==30,"edited history unlimited above 20");
 Check(session.Recent().Count()==20,"unedited history limited to 20");
 Check(session.Recent().First().Sample.Key=="@test/059","newest unedited first");
 Check(session.Recent().Last().Sample.Key=="@test/040","oldest unedited evicted");
 session.Save();Check(!Directory.Exists(sessionPath),"session creates no persistent drafts");
 var exportPath=Path.Combine(root,"session-export.zip");session.Export(exportPath);
 using var archive=ZipFile.OpenRead(exportPath);using var content=archive.GetEntry("translation-suggestions.json")!.Open();
 using var document=JsonDocument.Parse(content);
 Check(document.RootElement.GetProperty("entries").GetArrayLength()==30,"session export excludes recent unedited rows");
}
using(var reopened=new DialogueStore(sessionPath,sessionOnly:true))Check(reopened.Entries.Count==0,"new session starts empty");
using(var oldHistory=new DialogueStore(root,sessionOnly:true))Check(oldHistory.Entries.Count==0,"existing persistent history ignored in session mode");
Console.WriteLine($"{assertions} assertions passed; artifacts: {root}");
