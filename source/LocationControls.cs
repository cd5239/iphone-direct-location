using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

partial class Controller {
 LocationStore locations;ComboBox favorites;Button addLocation,deleteLocation;TextBlock placeStatus;bool loadingPlaces,locationLoadFailed;
 void InitLocationControls(){
  favorites=Get<ComboBox>("Favorites");addLocation=Get<Button>("AddLocation");deleteLocation=Get<Button>("DeleteLocation");placeStatus=Get<TextBlock>("PlaceStatus");locations=new LocationStore(App.Root);
  try{locations.Load();RefreshPlaces();FillLocation(locations.Current);}
  catch{locationLoadFailed=true;placeStatus.Text="地点文件读取失败，已禁止覆盖保存。请检查 locations.xml 或备份文件。";}
  favorites.SelectionChanged+=(s,e)=>{if(loadingPlaces||favorites.SelectedItem==null)return;var p=(SavedLocation)favorites.SelectedItem;string old=locations.Selected;try{locations.Selected=p.Id;locations.Save();FillLocation(p);placeStatus.Text="已切换地点，下次一键修改也会使用它。点击修改后才会应用到手机。";}catch(Exception ex){locations.Selected=old;RefreshPlaces();toast.Show("切换失败",ex.Message,"error");}};
  addLocation.Click+=(s,e)=>{try{SavePlace(true);placeStatus.Text="已添加常用地点，可在列表中切换。";toast.Show("已添加常用地点","已设为下次一键修改使用的地点。","success");}catch(Exception ex){toast.Show("添加失败",ex.Message,"error");}};
  deleteLocation.Click+=(s,e)=>{
   if(state.Busy||locationLoadFailed||locations.Current==null)return;
   string name=locations.Current.Name;
   if(MessageBox.Show(Window,"确定删除常用地点“"+name+"”？\n只删除收藏，不改变手机当前定位。","删除常用地点",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)return;
   try{locations.DeleteCurrent();RefreshPlaces();FillLocation(locations.Current);Render();placeStatus.Text=locations.Current==null?"暂无常用地点，请填写名称和坐标后保存。":"已删除并切换地点；手机当前定位未改变。";toast.Show("地点已删除",name,"success");}
   catch(Exception ex){toast.Show("删除失败",ex.Message,"error");}
  };

 }
 SavedLocation EditedLocation(){var p=new SavedLocation{Name=place.Text.Trim(),Longitude=longitude.Text.Trim(),Latitude=latitude.Text.Trim(),Source=SourceValue};p.Validate();return p;}
 void SetSource(string value){for(int i=0;i<source.Items.Count;i++)if(Convert.ToString(((ComboBoxItem)source.Items[i]).Tag)==value)source.SelectedIndex=i;}
 void FillLocation(SavedLocation p){if(p==null){place.Text=longitude.Text=latitude.Text="";placeStatus.Text="暂无常用地点，请填写名称和坐标后保存。";return;}place.Text=p.Name;longitude.Text=p.Longitude;latitude.Text=p.Latitude;SetSource(p.Source);}
 void RefreshPlaces(){loadingPlaces=true;favorites.ItemsSource=null;favorites.ItemsSource=locations.Items;favorites.SelectedItem=locations.Current;loadingPlaces=false;}
 void SavePlace(bool add){
  if(locationLoadFailed)throw new Exception("地点文件读取失败，不能覆盖。请先修复 locations.xml。");
  var p=EditedLocation();var old=locations.Current;string selected=locations.Selected;int index=old==null?-1:locations.Items.IndexOf(old);
  if(!add&&index>=0){p.Id=old.Id;locations.Items[index]=p;}else locations.Items.Add(p);
  locations.Selected=p.Id;
  try{locations.Save();}catch{if(!add&&index>=0)locations.Items[index]=old;else locations.Items.Remove(p);locations.Selected=selected;throw;}
  RefreshPlaces();Render();
 }
 void RenderLocationControls(){favorites.IsEnabled=addLocation.IsEnabled=!state.Busy&&!locationLoadFailed;deleteLocation.IsEnabled=!state.Busy&&!locationLoadFailed&&locations.Current!=null;}
}
