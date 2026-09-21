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
