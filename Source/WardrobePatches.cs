using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alta.Customization;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object=UnityEngine.Object;
namespace ATT.Cosmetics
{
 internal sealed class WardrobeLocator:IResourceLocator
 {
  readonly Pack pack;readonly string provider;internal WardrobeLocator(Pack p,string id){pack=p;provider=id;}
  public string LocatorId=>"ATT.Cosmetics."+pack.Manifest.packId;
  public IEnumerable<object> Keys=>pack.Entries.SelectMany(e=>new object[]{e.Id.ToString(),e.Sku});
  public bool Locate(object key,Type type,out IList<IResourceLocation> locations)
  {
   var entry=pack.Entries.FirstOrDefault(e=>Equals(key,e.Id.ToString())||Equals(key,e.Sku));
   if(entry!=null&&(type==null||type.IsAssignableFrom(typeof(PlayerCosmeticDefinition)))){locations=new[]{new ResourceLocationBase(entry.Id.ToString(),entry.Id.ToString(),provider,typeof(PlayerCosmeticDefinition))};return true;}
   locations=null;return false;
  }
 }
 internal sealed class WardrobeProvider:ResourceProviderBase
 {
  readonly Pack pack;internal WardrobeProvider(Pack p){pack=p;}
  public override string ProviderId=>"ATT.Cosmetics.Provider."+pack.Manifest.packId;
  public override Type GetDefaultType(IResourceLocation location)=>typeof(PlayerCosmeticDefinition);
  public override bool CanProvide(Type type,IResourceLocation location)=>pack.Entries.Any(e=>e.Id.ToString()==location.InternalId)&&(type==null||type.IsAssignableFrom(typeof(PlayerCosmeticDefinition)));
  public override void Provide(ProvideHandle handle)
  {var entry=pack.Entries.FirstOrDefault(e=>e.Id.ToString()==handle.Location.InternalId);bool ready=pack.Handle.Status==PackStatus.Ready&&entry?.Definition!=null;handle.Complete(entry?.Definition,ready,ready?null:new InvalidOperationException("Cosmetic pack is not ready"));}
 }
 [HarmonyPatch(typeof(PurchasedList),"IsOwned")] static class OwnedPatch {static bool Prefix(uint item,ref bool __result){if(Registry.Find(item)==null)return true;__result=true;return false;}}
 [HarmonyPatch(typeof(PurchasedList),"IsAvailable")] static class AvailablePatch {static bool Prefix(uint item,ref bool __result){if(Registry.Find(item)==null)return true;__result=true;return false;}}
 [HarmonyPatch(typeof(PurchasedList),"LoadAllOwned")] static class OwnedListPatch {static void Postfix(Func<uint,bool> filter,ref IEnumerable<uint> __result){__result=Registry.Append(__result,filter);}}
 [HarmonyPatch(typeof(PurchasedList),"LoadAllAvailable")] static class AvailableListPatch {static void Postfix(Func<uint,bool> filter,ref IEnumerable<uint> __result){__result=Registry.Append(__result,filter);}}
 [HarmonyPatch(typeof(CustomizationWrapper),"GetConflicts")] static class ConflictsPatch {static void Postfix(CustomizationWrapper __instance,ICustomizationVisual item,ref IEnumerable<uint> __result){__result=Registry.Conflicts(__instance,item,__result);}}
 [HarmonyPatch(typeof(EditableSlot),"IsApplicable")] static class ApplicabilityPatch
 {static bool Prefix(ICustomizationDefinition item,PlayerCustomizationSlotSet ___slots,CustomizationWrapper ___activeWrapper,ref bool __result){var entry=item==null?null:Registry.Find(item.Hash);if(entry==null)return true;__result=___activeWrapper is PlayerCosmeticWrapper&&___slots!=null&&___slots.Slots.Any(entry.Slots.Contains);return false;}}
 [HarmonyPatch] static class RemoveEmptyVisualPatch
 {
  static MethodBase TargetMethod()=>AccessTools.Method(typeof(CustomizationWrapper),"Remove",new[]{typeof(int),typeof(CustomizationArguments[]).MakeByRefType(),typeof(bool)});
  static void Postfix(CustomizationWrapper __instance,int slot,ref CustomizationArguments[] arguments,bool isPushingHistory,ref uint __result)
  {
   if(__result!=0||!(__instance is PlayerCosmeticWrapper))return;
   var entry=Registry.Entries.FirstOrDefault(e=>e.Spec.emptyVisual&&e.Slots.Contains(slot)&&__instance.Items.Contains(e.Id));
   if(entry==null)return;__instance.Remove(entry.Id,out arguments,isPushingHistory);__result=entry.Id;
  }
 }
 [HarmonyPatch(typeof(CustomizationVisualManager),"Clear")] static class ClearVisualsPatch
 {static void Prefix(CustomizationVisualManager __instance,Dictionary<uint,List<CustomizationSlotEntry>> ___items){foreach(var entry in Registry.Entries)if(entry.Part!=null&&___items.ContainsKey(entry.Id))foreach(var handler in __instance.Handlers.ToArray())entry.Part.Release(handler);}}
 [HarmonyPatch(typeof(PlayerCosmeticDefinition),"SpawnPickup")] static class PickupPatch
 {
  static void Postfix(PlayerCosmeticDefinition __instance,CustomizationPickup __result)
  {
   var entry=Registry.Find(__instance.Hash);if(ApiCore.IsServer||entry==null||__result==null)return;
   if(entry.Model!=null)
   {
    var model=Object.Instantiate(entry.Model,__result.transform,false);var filters=model.GetComponentsInChildren<MeshFilter>();var bounds=filters[0].sharedMesh.bounds;foreach(var f in filters.Skip(1))bounds.Encapsulate(f.sharedMesh.bounds);
    float scale=__instance.Category.PickupScale;model.transform.localRotation=Quaternion.Euler(-90,0,0);model.transform.localScale=Vector3.one*scale;model.transform.localPosition=model.transform.localRotation*(-bounds.center*scale);
   }
   var label=new GameObject("Cosmetic name");label.transform.SetParent(__result.transform,false);label.transform.localPosition=new Vector3(0,-.12f,-.02f);
   var text=label.AddComponent<TextMesh>();text.text=entry.Name.Replace(" ","\n");text.font=Resources.GetBuiltinResource<Font>("Arial.ttf");text.GetComponent<Renderer>().sharedMaterial=text.font.material;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=32;text.characterSize=.014f;text.color=Color.white;
  }
 }
}
