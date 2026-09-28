using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

class SavedLocation {
 public string Id=Guid.NewGuid().ToString("N"),Name,Longitude,Latitude,Source;
 public override string ToString(){return Name;}
 public void Validate(){
  double lon,lat;
  if(string.IsNullOrWhiteSpace(Name))throw new Exception("请填写地点名称。");
  if(!double.TryParse(Longitude,NumberStyles.Float,CultureInfo.InvariantCulture,out lon)||!double.TryParse(Latitude,NumberStyles.Float,CultureInfo.InvariantCulture,out lat)||double.IsNaN(lon)||double.IsNaN(lat)||double.IsInfinity(lon)||double.IsInfinity(lat)||Math.Abs(lon)>180||Math.Abs(lat)>90)throw new Exception("请填写有效经纬度。");
  if(Source!="BD09"&&Source!="GCJ02"&&Source!="WGS84")throw new Exception("坐标来源无效。");
 }
 public XElement Xml(){return new XElement("location",new XAttribute("id",Id),new XElement("name",Name),new XElement("longitude",Longitude),new XElement("latitude",Latitude),new XElement("source",Source));}
 public static SavedLocation Read(XElement x){var p=new SavedLocation{Id=(string)x.Attribute("id")??Guid.NewGuid().ToString("N"),Name=(string)x.Element("name"),Longitude=(string)x.Element("longitude"),Latitude=(string)x.Element("latitude"),Source=(string)x.Element("source")??"BD09"};p.Validate();return p;}
}
class LocationStore {
 public List<SavedLocation> Items=new List<SavedLocation>();public string Selected;readonly string root;
 public LocationStore(string directory){root=directory;}
 public SavedLocation Current{get{return Items.FirstOrDefault(x=>x.Id==Selected)??Items.FirstOrDefault();}}
 public void Load(){
  string p=Path.Combine(root,"locations.xml");
  if(File.Exists(p)){var x=XDocument.Load(p).Root;if(x.Name!="locations")throw new Exception("地点文件格式无效。");Items=x.Elements("location").Select(SavedLocation.Read).ToList();Selected=(string)x.Attribute("selected");if(Items.Select(a=>a.Id).Distinct().Count()!=Items.Count)throw new Exception("常用地点文件无效，请检查 locations.xml。");}
  else if(File.Exists(Path.Combine(root,"location.xml"))){Items.Add(SavedLocation.Read(XDocument.Load(Path.Combine(root,"location.xml")).Root));Selected=Items[0].Id;}
  Selected=Current==null?"":Current.Id;
 }
 public void Save(){
  foreach(var p in Items)p.Validate();
  Selected=Current==null?"":Current.Id;
  string file=Path.Combine(root,"locations.xml"),temp=file+".tmp";
  new XDocument(new XElement("locations",new XAttribute("selected",Selected),Items.Select(x=>x.Xml()))).Save(temp);
  if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);
 }
 public void DeleteCurrent(){
  var old=Current;if(old==null)return;
  int index=Items.IndexOf(old);string selected=Selected;Items.RemoveAt(index);
  Selected=Items.Count==0?"":Items[Math.Min(index,Items.Count-1)].Id;
  try{Save();}catch{Items.Insert(index,old);Selected=selected;throw;}
 }
}
