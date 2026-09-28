using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using Forms=System.Windows.Forms;

partial class Controller {
 public Window Window;
 TextBox place,longitude,latitude;ComboBox source;TextBlock status,device,deviceHeading,statusHeading,lastResult,elapsed;Border deviceCard,resultCard;ProgressBar progress;Button apply,restore,save,refresh;
 Process engine;JavaScriptSerializer json=new JavaScriptSerializer();Forms.NotifyIcon tray;UiState state=new UiState();StatusToast toast=new StatusToast();bool exiting,autoApply;long nextId;DateTime lastProbe=DateTime.MinValue,launched,probeStarted;
 DispatcherTimer timer;EventWaitHandle applySignal,showSignal;RegisteredWaitHandle applyWait,showWait;
 public Controller(bool background){
  autoApply=background;Window=App.Load();Window.Title="iPhone 直连定位 · 2.4";Window.Icon=System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(Path.Combine(App.Root,"source","app.ico")));Window.MaxHeight=SystemParameters.WorkArea.Height-30;
  place=Get<TextBox>("Place");longitude=Get<TextBox>("Longitude");latitude=Get<TextBox>("Latitude");source=Get<ComboBox>("Source");status=Get<TextBlock>("Status");device=Get<TextBlock>("Device");
  deviceHeading=Get<TextBlock>("DeviceHeading");statusHeading=Get<TextBlock>("StatusHeading");lastResult=Get<TextBlock>("LastResult");elapsed=Get<TextBlock>("Elapsed");progress=Get<ProgressBar>("Progress");deviceCard=Get<Border>("DeviceCard");resultCard=Get<Border>("ResultCard");
  apply=Get<Button>("Apply");restore=Get<Button>("Restore");save=Get<Button>("Save");refresh=Get<Button>("Refresh");InitLocationControls();
  tray=new Forms.NotifyIcon{Text="iPhone 直连定位 · 正在检测",Icon=new System.Drawing.Icon(Path.Combine(App.Root,"source","app.ico")),Visible=true};
  var menu=new Forms.ContextMenuStrip();menu.Items.Add("打开定位窗口",null,(s,e)=>Window.Dispatcher.BeginInvoke(new Action(Show)));
  menu.Items.Add("恢复真实定位",null,(s,e)=>Window.Dispatcher.BeginInvoke(new Action(()=>Send("clear"))));menu.Items.Add("退出并结束定位",null,(s,e)=>Window.Dispatcher.BeginInvoke(new Action(Exit)));tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>Show();
  applySignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\iPhoneDirectLocation_Apply");showSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\iPhoneDirectLocation_Show");
  applyWait=ThreadPool.RegisterWaitForSingleObject(applySignal,(o,t)=>Window.Dispatcher.BeginInvoke(new Action(()=>Send("apply"))),null,-1,false);showWait=ThreadPool.RegisterWaitForSingleObject(showSignal,(o,t)=>Window.Dispatcher.BeginInvoke(new Action(Show)),null,-1,false);
  apply.Click+=(s,e)=>Send("apply");restore.Click+=(s,e)=>Send("clear");refresh.Click+=(s,e)=>{if(!state.Ready&&!state.Busy)RestartEngine();else Send("probe");};
  save.Click+=(s,e)=>{try{SaveSettings();toast.Show("地点已保存","下次一键修改会使用此地点。","success");}catch(Exception ex){Fail("保存失败",ex.Message);}};
  Window.Closing+=(s,e)=>{if(exiting)return;e.Cancel=true;if(state.Active||state.Busy){Window.Hide();toast.Show("工具仍在托盘运行","右键托盘可恢复真实定位或退出。","info");}else Exit();};
  Window.Loaded+=(s,e)=>StartEngine();timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};timer.Tick+=(s,e)=>Tick();timer.Start();Render();
 }
 T Get<T>(string name){return (T)Window.FindName(name);}
 public void StartHidden(){StartEngine();}
 void Show(){Window.Show();Window.WindowState=WindowState.Normal;Window.Activate();if(state.Ready&&!state.Busy&&!state.ProbePending)Send("probe");}
 string SourceValue{get{return Convert.ToString(((ComboBoxItem)source.SelectedItem).Tag);}}
 static double Number(string text,double limit){double n;if(!double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out n)||double.IsNaN(n)||double.IsInfinity(n)||Math.Abs(n)>limit)throw new Exception("请填写有效经纬度：经度 -180～180，纬度 -90～90。");return n;}
 void SaveSettings(){SavePlace(false);}
 void StartEngine(){
  if(engine!=null)return;
  try{
   var p=new ProcessStartInfo(Path.Combine(App.Root,"engine","LocationEngine.exe"),"--state-dir \""+Path.Combine(App.Root,"state")+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=new UTF8Encoding(false),StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=App.Root};
   var current=new Process{StartInfo=p,EnableRaisingEvents=true};engine=current;launched=DateTime.Now;
   current.OutputDataReceived+=(s,e)=>{string line=e.Data;if(line!=null)Window.Dispatcher.BeginInvoke(new Action(()=>{if(engine==current)Receive(line);}));};current.ErrorDataReceived+=(s,e)=>{};
   current.Exited+=(s,e)=>Window.Dispatcher.BeginInvoke(new Action(()=>{if(engine!=current||exiting)return;state.Ready=false;state.Active=false;state.ProbePending=false;state.Connection="unknown";state.Usable=false;state.Device="连接组件已退出";engine=null;Fail("连接组件已退出","点击“重新检测手机”可重启连接组件。");}));
   current.Start();current.BeginOutputReadLine();current.BeginErrorReadLine();Render();
  }catch(Exception ex){engine=null;state.Ready=false;Fail("连接组件启动失败",ex.Message);}
 }
 void RestartEngine(){
  if(state.Busy)return;var old=engine;engine=null;if(old!=null)try{if(!old.HasExited)old.Kill();old.Dispose();}catch{}
  state.Ready=false;state.Active=false;state.ProbePending=false;state.Connection="unknown";state.Usable=false;state.Title="正在重新检测手机…";state.Tone="info";StartEngine();
 }
 void Send(string command){
  if(exiting)return;
  if(state.Busy){if(command!="probe")toast.Show(state.Title,"当前操作尚未完成，请等待结果。","info");return;}
  if(!state.Ready){if(command!="probe")Fail("连接组件尚未就绪","请稍后重试，或点击“重新检测手机”。");return;}
  if(command=="probe"&&state.ProbePending)return;
  try{
   var value=new Dictionary<string,object>{{"command",command},{"request_id",++nextId}};
   if(command=="apply"){SaveSettings();value["longitude"]=Number(longitude.Text,180);value["latitude"]=Number(latitude.Text,90);value["source"]=SourceValue;}
   state.Begin(command,nextId);if(command=="probe"){lastProbe=probeStarted=DateTime.Now;}Render();Notify();
   engine.StandardInput.WriteLine(json.Serialize(value));engine.StandardInput.Flush();
  }catch(Exception ex){if(command=="probe")state.ProbePending=false;Fail("操作未能开始",ex.Message);}
 }
 void Receive(string line){
  if(exiting)return;
  Dictionary<string,object> msg;
  try{msg=json.Deserialize<Dictionary<string,object>>(line);if(!msg.ContainsKey("event"))return;}
  catch{if(line.TrimStart().StartsWith("{"))Fail("连接反馈解析失败","请点击重新检测手机重试。");return;}
  state.Handle(msg);Render();Notify();
  if(Convert.ToString(msg["event"])=="ready"){Send("probe");if(autoApply){autoApply=false;Send("apply");}}
  else if(Convert.ToString(msg["event"])=="applied"||Convert.ToString(msg["event"])=="restored")lastProbe=DateTime.MinValue;
 }
 void Fail(string title,string detail){state.Fail(title,detail);Render();Notify();}
 void Notify(){if(state.NoticeTitle==null)return;toast.Show(state.NoticeTitle,state.NoticeText,state.NoticeTone);state.NoticeTitle=null;}
 static Brush Brush(string hex){return (Brush)new BrushConverter().ConvertFromString(hex);}
 void Render(){
  device.Text=state.Device;
  deviceHeading.Text=state.Connection=="connected"?(state.Usable?(state.Active?"● 手机已连接 · 定位运行中":"● 手机已连接"):"● USB 已连接 · 等待解锁或信任"):state.Connection=="disconnected"?"○ 手机未连接":state.Connection=="multiple"?"● 已连接多台设备":"● 正在检测 / 状态待确认";
  deviceHeading.Foreground=Brush(state.Usable?"#137347":"#7B5B13");deviceCard.Background=Brush(state.Usable?"#E7F5ED":"#FFF4DB");
  statusHeading.Text=state.Title;status.Text=state.Detail;lastResult.Text="最近结果："+state.LastResult;
  resultCard.Background=Brush(state.Tone=="success"?"#E7F5ED":state.Tone=="error"?"#FFF0F0":(state.Tone=="disconnected"||state.Tone=="pending")?"#FFF4DB":"#E7EFFB");
  statusHeading.Foreground=Brush(state.Tone=="success"?"#137347":state.Tone=="error"?"#B83232":"#284B78");
  progress.Visibility=elapsed.Visibility=state.Busy?Visibility.Visible:Visibility.Collapsed;
  apply.Content=state.Operation=="apply"?"正在修改…":state.Busy?"请等待当前操作":!state.Ready?"正在准备连接…":state.Connection!="unknown"&&!state.Usable?"等待手机就绪":"修改定位";
  apply.IsEnabled=state.CanApply;restore.IsEnabled=state.Ready&&!state.Busy&&(state.Active||state.Usable);refresh.IsEnabled=!state.Busy&&!state.ProbePending;
  save.IsEnabled=place.IsEnabled=longitude.IsEnabled=latitude.IsEnabled=source.IsEnabled=!state.Busy;RenderLocationControls();
  tray.Text="iPhone 直连定位 · "+(state.Busy?"正在操作":state.Active?"定位运行中":state.Usable?"手机已连接":"等待手机");
 }
 void Tick(){
  if(exiting)return;
  if(state.Busy){double seconds=(DateTime.Now-state.Started).TotalSeconds;elapsed.Text="已等待 "+((int)seconds)+" 秒 · 请保持 USB 连接";if(seconds>270){state.Active=false;Fail("操作超时，未确认成功","连接组件未及时响应。请重新检测后再试。");RestartEngine();}}
  else if(state.Ready&&!state.ProbePending&&(DateTime.Now-lastProbe).TotalSeconds>=3)Send("probe");
  if(state.ProbePending&&(DateTime.Now-probeStarted).TotalSeconds>25&&!state.Busy){state.ProbePending=false;state.Ready=false;Fail("设备检测超时","点击“重新检测手机”重试。");}
  if(!state.Ready&&engine!=null&&(DateTime.Now-launched).TotalSeconds>25&&!state.Busy){var old=engine;engine=null;try{old.Kill();}catch{}Fail("连接组件启动超时","点击“重新检测手机”重试。");}
 }
 async void Exit(){
  if(exiting)return;exiting=true;timer.Stop();toast.Close();
  if(engine!=null)try{if(!engine.HasExited){engine.StandardInput.WriteLine("{\"command\":\"shutdown\"}");engine.StandardInput.Flush();engine.StandardInput.Close();if(!await Task.Run(()=>engine.WaitForExit(12000))&&!engine.HasExited)engine.Kill();}}catch{}
  applyWait.Unregister(null);showWait.Unregister(null);applySignal.Dispose();showSignal.Dispose();tray.Visible=false;tray.Dispose();Window.Close();Application.Current.Shutdown();
 }
}
