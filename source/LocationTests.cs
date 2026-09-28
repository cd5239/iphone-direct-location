using System;
using System.IO;
using System.Xml.Linq;
static class LocationTests {
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 public static void Run(string root){
  Directory.CreateDirectory(root);
  var original=new SavedLocation{Name="原地点",Longitude="116.40",Latitude="39.90",Source="BD09"};new XDocument(original.Xml()).Save(Path.Combine(root,"location.xml"));
  var store=new LocationStore(root);store.Load();Check(store.Items.Count==1&&store.Current.Name=="原地点","Migration");store.Save();
  var second=new SavedLocation{Name="测试第二地点",Longitude="121.47",Latitude="31.23",Source="GCJ02"};store.Items.Add(second);store.Selected=second.Id;store.Save();
  var reload=new LocationStore(root);reload.Load();Check(reload.Items.Count==2&&reload.Current.Name==second.Name&&reload.Current.Source=="GCJ02","Selection survives restart");
  reload.Selected=reload.Items[0].Id;reload.Save();reload=new LocationStore(root);reload.Load();Check(reload.Current.Name=="原地点"&&reload.Items[1].Longitude=="121.47","Switch preserves other location");
  reload.Current.Name="更新名称";reload.Save();reload=new LocationStore(root);reload.Load();Check(reload.Items.Count==2&&reload.Current.Name=="更新名称","Update without duplicate");
  string before=File.ReadAllText(Path.Combine(root,"locations.xml"));reload.Current.Longitude="NaN";bool failed=false;try{reload.Save();}catch{failed=true;}Check(failed&&File.ReadAllText(Path.Combine(root,"locations.xml"))==before,"Invalid input must preserve saved file");
  Check(File.Exists(Path.Combine(root,"locations.xml.bak")),"Backup");
  reload=new LocationStore(root);reload.Load();reload.DeleteCurrent();reload=new LocationStore(root);reload.Load();Check(reload.Items.Count==1&&reload.Current.Name==second.Name,"Delete and select remaining");
  reload.DeleteCurrent();reload=new LocationStore(root);reload.Load();Check(reload.Items.Count==0&&reload.Current==null,"Last deletion persists and does not remigrate legacy file");
  reload.DeleteCurrent();Check(reload.Items.Count==0,"Empty delete safe");
  reload.Items.Add(original);reload.Selected=original.Id;reload.Save();reload=new LocationStore(root);reload.Load();Check(reload.Items.Count==1,"Can add after deleting all");
  string selected=reload.Selected;Directory.CreateDirectory(Path.Combine(root,"locations.xml.tmp"));bool rejected=false;try{reload.DeleteCurrent();}catch{rejected=true;}Check(rejected&&reload.Items.Count==1&&reload.Selected==selected,"Failed delete rolls back memory");
 }
}
