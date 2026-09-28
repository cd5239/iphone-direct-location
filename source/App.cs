using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using Forms=System.Windows.Forms;

static class App {
    public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
    [STAThread] static void Main(string[] args){
        try {
            if(args.Length==2&&args[0]=="--test-locations"){try{LocationTests.Run(args[1]);File.WriteAllText(Path.Combine(args[1],"result.txt"),"PASS: migration, multiple locations, selection persistence, update, invalid input, backup");}catch(Exception ex){File.WriteAllText(Path.Combine(args[1],"result.txt"),"FAIL: "+ex);Environment.ExitCode=1;}return;}
            if(args.Length==2&&args[0]=="--test-state"){try{UiStateTests.Run();File.WriteAllText(args[1],"PASS: probe/click race, progress, success notification, stale results, USB reconnect, tunnel interruption, failure notification.");}catch(Exception ex){File.WriteAllText(args[1],"FAIL: "+ex);Environment.ExitCode=1;}return;}
            if(args.Length==2&&args[0]=="--render") {Render(args[1]);return;}
            bool fresh;using(var mutex=new Mutex(true,"Local\\iPhoneDirectLocation_v2",out fresh)) {
                if(!fresh){try{using(var signal=EventWaitHandle.OpenExisting(args.Contains("--apply-background")?"Local\\iPhoneDirectLocation_Apply":"Local\\iPhoneDirectLocation_Show"))signal.Set();}catch{MessageBox.Show("工具正在启动，请稍后重试。","iPhone 直连定位");}return;}
                var application=new Application();application.ShutdownMode=ShutdownMode.OnExplicitShutdown;
                bool background=args.Contains("--apply-background");var controller=new Controller(background);application.MainWindow=controller.Window;
                if(background)controller.StartHidden();else controller.Window.Show();application.Run();
            }
        }catch(Exception e){MessageBox.Show(e.Message,"iPhone 直连定位");}
    }
    public static Window Load(){using(var f=File.OpenRead(Path.Combine(Root,"source","MainWindow.xaml")))return (Window)XamlReader.Load(f);}
    static void Render(string directory){
        Directory.CreateDirectory(directory);foreach(int dpi in new[]{96,144,192}){
            var w=Load();var content=(FrameworkElement)w.Content;content.Measure(new Size(520,double.PositiveInfinity));var s=content.DesiredSize;
            content.Arrange(new Rect(0,0,520,s.Height));content.UpdateLayout();
            var b=new RenderTargetBitmap((int)Math.Ceiling(520*dpi/96.0),(int)Math.Ceiling(s.Height*dpi/96.0),dpi,dpi,PixelFormats.Pbgra32);b.Render(content);
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(b));using(var file=File.Create(Path.Combine(directory,"layout-"+dpi+".png")))png.Save(file);
            if(s.Height>900||s.Height<400)throw new Exception("Unexpected layout height: "+s.Height);
        }
    }
}
