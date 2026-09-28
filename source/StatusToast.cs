using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

class StatusToast {
 Window window;DispatcherTimer timer;
 public void Show(string title,string message,string tone){
  Close();Brush color=(Brush)new BrushConverter().ConvertFromString(tone=="success"?"#168350":tone=="error"?"#C63B3B":tone=="pending"?"#996600":"#2563EB");
  window=new Window{Width=360,SizeToContent=SizeToContent.Height,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,ShowActivated=false,Topmost=true,Background=Brushes.White};
  var content=new StackPanel{Margin=new Thickness(18)};
  var header=new DockPanel();var close=new Button{Content="×",Width=24,Height=24,Padding=new Thickness(0),Background=Brushes.Transparent,BorderThickness=new Thickness(0)};close.Click+=(s,e)=>Close();DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);
  header.Children.Add(new TextBlock{Text=title,Foreground=color,FontSize=17,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap});content.Children.Add(header);
  content.Children.Add(new TextBlock{Text=message,FontSize=13,Foreground=Brushes.DimGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)});
  window.Content=new Border{BorderBrush=color,BorderThickness=new Thickness(2),Child=content};window.FontFamily=new FontFamily("Microsoft YaHei UI");
  window.Loaded+=(s,e)=>{window.Left=SystemParameters.WorkArea.Right-window.ActualWidth-18;window.Top=SystemParameters.WorkArea.Bottom-window.ActualHeight-18;};
  window.Show();timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(tone=="error"?10:5)};timer.Tick+=(s,e)=>Close();timer.Start();
 }
 public void Close(){if(timer!=null){timer.Stop();timer=null;}if(window!=null){window.Close();window=null;}}
}
