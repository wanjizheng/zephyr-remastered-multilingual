using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace ZephyrPatcher;
public partial class App : Application
{
 protected override async void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  if(e.Args.Contains("--proofreader") || e.Args.Contains("--proofreader-preview") || System.Reflection.Assembly.GetExecutingAssembly().GetName().Name=="ZephyrDialogueAssistant")
  {
   try
   {
    var preview=e.Args.Contains("--proofreader-preview");
    var gameArg=Array.IndexOf(e.Args,"--game");
    var gamePath=gameArg>=0 && gameArg+1<e.Args.Length?e.Args[gameArg+1]:@"D:\Games\Steam\steamapps\common\The Rhapsody of Zephyr Remastered";
    var langArg=Array.IndexOf(e.Args,"--language");
    var language=langArg>=0&&langArg+1<e.Args.Length?e.Args[langArg+1]:Dialogue.DialogueCatalog.InstalledLanguage(gamePath);
    if(!Dialogue.DialogueCatalog.Languages.Contains(language))language="zh-Hans";
    var proof=new Dialogue.DialogueWindow(gamePath,preview?Path.Combine(Path.GetTempPath(),"zephyr-ui-"+Guid.NewGuid().ToString("N")):null,!preview,language);
    MainWindow=proof;proof.Show();
    if(preview)
    {
     proof.LoadPreview(new(0,Dialogue.DialogueReader.SupportedHash,"fl05_s03","@kfl05_s03/004","text.re/art_","NPCTalk_ThenObject","preview","preview","希尔弗","我叫希尔弗。\n看来，我的手下受了你不少照顾。",DateTimeOffset.UtcNow));
     proof.ValidateEditorFlow();
     proof.UpdateLayout();await Task.Delay(250);
     var target=e.Args[Array.IndexOf(e.Args,"--proofreader-preview")+1];
     var bitmap=new RenderTargetBitmap((int)proof.ActualWidth,(int)proof.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(proof);
     var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(target))png.Save(file);
     proof.Close();Shutdown();
    }
   }
   catch(Exception ex){MessageBox.Show(ex.Message,"校对助手未能打开");Shutdown(1);}
   return;
  }
  var window=new MainWindow();MainWindow=window;
  if(e.Args.Length>=2 && e.Args[0]=="--render-preview")
  {
   window.PreparePreview(e.Args.Length>=3?e.Args[2]:"original");
   if(e.Args.Contains("ui-en")||e.Args.Contains("patch-en"))window.EnLanguage.IsChecked=true;
   if(e.Args.Contains("ui-zh-Hant")||e.Args.Contains("patch-zh-Hant"))window.TwLanguage.IsChecked=true;
   if(e.Args.Length>=3 && e.Args[2]=="install-progress")window.PreparePreview("install-progress");
   if(e.Args.Contains("small")){window.Width=940;window.Height=680;}
   window.Show();window.UpdateLayout();await Task.Delay(200);
   if(e.Args.Length>=3 && e.Args[2]=="install-progress")
   {
    var progressBottom=window.ProgressText.TranslatePoint(new Point(0,window.ProgressText.ActualHeight),window).Y;
    var footerTop=window.FooterVersion.TranslatePoint(new Point(0,0),window).Y;
    if(progressBottom+8>=footerTop)throw new InvalidOperationException($"Progress text overlaps footer: {progressBottom:F1} >= {footerTop:F1}");
   }
   if(e.Args.Contains("restore"))
   {
    window.RestoreButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await Task.Delay(650);
    if(!window.Dialogs.IsOpen)throw new InvalidOperationException("Restore dialog did not open");
   }
   var bounds=VisualTreeHelper.GetDescendantBounds(window);
   var bitmap=new RenderTargetBitmap((int)Math.Ceiling(bounds.Right),(int)Math.Ceiling(bounds.Bottom),96,96,PixelFormats.Pbgra32);bitmap.Render(window);
   var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(e.Args[1]))encoder.Save(file);
   if(window.Dialogs.IsOpen){MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute("cancel",window.Dialogs);await Task.Delay(100);}
   Shutdown();return;
  }
  window.Show();window.Initialize();
 }
}
