using System;
using System.Collections.Generic;

// Pure presentation state: USB connectivity, a foreground command, and its last
// result are independent. Poll replies never finish an in-flight apply command.
class UiState {
 public bool Ready,Usable,Active,ProbePending;
 public string Connection="unknown",Device="正在检测 USB 设备…",Operation="",Title="正在启动连接组件…",Detail="请连接并解锁手机。",Tone="info",LastResult="尚未执行定位修改";
 public long OperationId,ProbeId;public DateTime Started;public string NoticeTitle,NoticeText,NoticeTone;
 public bool Busy{get{return Operation!="";}}
 public bool CanApply{get{return Ready&&!Busy&&(Connection=="unknown"||Usable);}}
 public void Begin(string command,long id){
  if(command=="probe"){ProbePending=true;ProbeId=id;return;}
  Operation=command;OperationId=id;Started=DateTime.Now;Tone="working";
  Title=command=="apply"?"正在修改定位…":"正在恢复真实定位…";
  Detail=ProbePending?"正在等待手机检测完成，随后立即执行。":"正在连接手机，请保持数据线连接。";
  NoticeTitle=Title;NoticeText=Detail;NoticeTone="info";
 }
 public void Fail(string title,string detail){Operation="";Title=title;Detail=detail;Tone="error";NoticeTitle=title;NoticeText=detail;NoticeTone="error";LastResult=DateTime.Now.ToString("HH:mm:ss")+"  "+title;}
 static string S(Dictionary<string,object> d,string k,string fallback=""){return d.ContainsKey(k)?Convert.ToString(d[k]):fallback;}
 static bool B(Dictionary<string,object> d,string k){return d.ContainsKey(k)&&Convert.ToBoolean(d[k]);}
 public void Handle(Dictionary<string,object> d){
  NoticeTitle=null;string ev=S(d,"event"),message=S(d,"message"),command=S(d,"command");long id=0;if(d.ContainsKey("request_id")&&d["request_id"]!=null)long.TryParse(Convert.ToString(d["request_id"]),out id);
  if(ev=="ready"){Ready=true;Title="正在检测手机…";Detail="连接手机后会自动更新。";return;}
  if(ev=="device"){
   if(id==ProbeId)ProbePending=false;
   string previous=Connection;Connection=S(d,"connection","unknown");Usable=B(d,"usable");Active=B(d,"active");Device=message;
   if(!Busy&&(Tone=="info"||Tone=="disconnected")){
    if(Usable){Title=Active?"定位连接正在保持":"手机已连接，可以修改定位";Detail=Active?"手机应用显示可能需要刷新。":"选择地点后点击“修改定位”。";Tone=Active?"success":"info";}
    else {Title=Connection=="disconnected"?"等待连接手机":Connection=="multiple"?"请只连接一台手机":"等待手机就绪";Detail=message;Tone="disconnected";}
   }
   return;
  }
  if(ev=="session_lost"){
   Active=false;if(!Busy){Title=S(d,"reason")=="usb_disconnected"?"手机已断开":"定位会话已中断";Detail=message;Tone=S(d,"reason")=="usb_disconnected"?"disconnected":"error";NoticeTitle=Title;NoticeText=Detail;NoticeTone="error";}
   return;
  }
  if(ev=="status"){if(Busy&&id==OperationId)Detail=message;return;}
  if(ev=="error"&&command=="probe"){
   if(id!=ProbeId)return;ProbePending=false;Connection="unknown";Usable=false;Device="设备状态暂时无法确认";
   if(!Busy){Title="设备检测失败";Detail=message;Tone="disconnected";}return;
  }
  if(!Busy||id!=OperationId)return;
  if(ev=="applied"||ev=="restored"){
   Operation="";Active=ev=="applied";Connection="connected";Usable=true;Tone=Active?"success":"pending";
   Title=Active?"修改成功 · 手机已确认":"恢复指令已发送 · 待手机确认";Detail=message;LastResult=DateTime.Now.ToString("HH:mm:ss")+"  "+Title;
   NoticeTitle=Title;NoticeText=Active?"已应用保存的地点。工具将继续保持连接。":"请在手机地图点击当前位置，确认是否恢复。";NoticeTone=Active?"success":"pending";return;
  }
  if(ev=="error"){if(B(d,"session_ended"))Active=false;Fail("操作失败",message);}
 }
}
