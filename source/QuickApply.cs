using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
static class QuickApply {
 [STAThread] static void Main(){
  try{string root=AppDomain.CurrentDomain.BaseDirectory;Process.Start(new ProcessStartInfo(Path.Combine(root,"iPhone直连定位.exe"),"--apply-background"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=root});}
  catch(Exception ex){MessageBox.Show("无法启动直连定位，请确认工具文件夹完整。\n"+ex.Message,"iPhone 直连定位");}
 }
}
