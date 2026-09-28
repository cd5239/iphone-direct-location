using System;
using System.Collections.Generic;
static class UiStateTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static Dictionary<string,object> Msg(string ev,long id,string command,params object[] values){var d=new Dictionary<string,object>{{"event",ev},{"request_id",id},{"command",command},{"message","test"}};for(int i=0;i<values.Length;i+=2)d[(string)values[i]]=values[i+1];return d;}
 public static void Run(){
  var s=new UiState();s.Handle(Msg("ready",0,""));s.Begin("probe",1);Check(s.CanApply&&!s.Busy,"Background probe must not disable apply");
  s.Begin("apply",2);s.Handle(Msg("device",1,"probe","connection","connected","usable",true,"active",false));Check(s.Busy&&s.OperationId==2,"Late probe reply must not finish apply");
  s.Handle(Msg("status",2,"apply","message","sending"));Check(s.Detail=="sending"&&s.Tone=="working","Progress must be visible");
  s.Handle(Msg("applied",2,"apply"));Check(!s.Busy&&s.Active&&s.Tone=="success"&&s.NoticeTitle!=null,"Success must finish with reminder");
  string result=s.LastResult;s.Begin("probe",3);s.Handle(Msg("device",3,"probe","connection","connected","usable",true,"active",true));Check(s.LastResult==result&&s.Title.Contains("修改成功"),"Polling must preserve success result");
  s.Handle(Msg("session_lost",3,"probe","reason","session_lost"));Check(!s.Active&&s.Usable&&s.Connection=="connected"&&s.Title=="定位会话已中断","Tunnel loss is not USB unplug");
  s.Handle(Msg("device",4,"probe","connection","disconnected","usable",false,"active",false));s.Handle(Msg("session_lost",4,"probe","reason","usb_disconnected"));
  s.Handle(Msg("device",5,"probe","connection","connected","usable",true,"active",false));Check(s.Title=="手机已连接，可以修改定位"&&s.Tone=="info","Reconnect clears stale disconnected notice");
  s.Begin("apply",6);s.Handle(Msg("error",5,"probe"));Check(s.Busy,"Stale probe error must not cancel operation");
  s.Handle(Msg("error",6,"apply","session_ended",true));Check(!s.Busy&&s.Tone=="error"&&s.NoticeTone=="error","Failures must be visible");
  s.Begin("clear",7);s.Handle(Msg("restored",7,"clear"));Check(!s.Active&&!s.Busy&&s.Tone=="pending"&&s.Title.Contains("待手机确认"),"No-reply restore must not claim verified GPS");
  s.Begin("probe",8);s.Handle(Msg("device",8,"probe","connection","connected","usable",true,"active",false));Check(s.Tone=="pending","Probe must preserve pending GPS verification");
 }
}
