using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Alta;
using Alta.Customization;
using Alta.Global;
using Alta.Networking;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object=UnityEngine.Object;
namespace ATT.Cosmetics
{
 internal sealed class Pack
 {
  internal PackManifest Manifest;internal Func<byte[]> Loader;internal PackHandle Handle;
  internal Entry[] Entries;internal AssetBundle Bundle;internal string[] BoneNames;
  internal DateTime Started;internal WardrobeLocator Locator;internal WardrobeProvider Provider;
  internal readonly Dictionary<uint,AsyncOperationHandle<PlayerCosmeticDefinition>> Loads=new Dictionary<uint,AsyncOperationHandle<PlayerCosmeticDefinition>>();
 }
 internal sealed class Entry
 {
  internal Pack Pack;internal CosmeticDefinition Spec;internal uint Id;
  internal string Name=>Spec.displayName;
  internal string Sku=>Spec.sku;
  internal PlayerCosmeticDefinition Definition;internal PlayerCosmeticCategory Category;
  internal PurchasableMap.CosmeticCategory CategoryRow;internal HashSet<int> Slots;
  internal GameObject Template,Model;internal WearablePart Part;
  internal CosmeticColor SkinColor;
 }
 internal static class Registry
 {
  static readonly List<Pack> packs=new List<Pack>();
  static readonly Dictionary<uint,Entry> ready=new Dictionary<uint,Entry>();
  static readonly PackIdentityIndex identities=new PackIdentityIndex();
  internal static readonly DefinitionReadiness<ICustomizationDefinition> Definitions=new DefinitionReadiness<ICustomizationDefinition>();
  static bool pumping;static Task catalog;static PurchasableMap map;
  internal static IEnumerable<Entry> Entries=>ready.Values;
  internal static bool Ready=>ready.Count!=0;
  internal static bool HasPacks=>packs.Count!=0;
  internal static Entry Find(uint id){Entry e;return ready.TryGetValue(id,out e)?e:null;}
  internal static PackHandle[] Handles()=>packs.Select(p=>p.Handle).ToArray();
  internal static FieldInfo Field(Type type,string name){for(;type!=null;type=type.BaseType){var f=type.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(f!=null)return f;}throw new MissingFieldException(name);}
  internal static void Set(object value,string name,object item)=>Field(value.GetType(),name).SetValue(value,item);
  static PlayerCustomizationSlotSet Slots(IEnumerable<int> ids){var set=new PlayerCustomizationSlotSet();Set(set,"slots",ids.Distinct().ToArray());return set;}
  internal static PackHandle Queue(PackManifest manifest,Func<byte[]> loader)
  {
   if(packs.Any(p=>p.Manifest.packId==manifest.packId))throw new InvalidOperationException("Pack already registered: "+manifest.packId);
   if(!ApiCore.IsServer&&manifest.cosmetics.Any(ManifestRules.NeedsModel)&&loader==null)throw new ArgumentNullException(nameof(loader));
   var pack=new Pack{Manifest=manifest,Loader=loader,Handle=new PackHandle{PackId=manifest.packId,Version=manifest.version,Status=PackStatus.Queued}};
   pack.Entries=manifest.cosmetics.Select(c=>new Entry{Pack=pack,Spec=c,Id=CosmeticIds.FromSku(c.sku)}).ToArray();
   identities.Reserve(manifest);
   foreach(var entry in pack.Entries)Definitions.Declare(entry.Id);
   packs.Add(pack);MelonLogger.Msg("COSMETICS_PACK_QUEUED "+manifest.packId+" version="+manifest.version);return pack.Handle;
  }
  internal static void Pump()
  {
   if(!ApiCore.Started||pumping)return;pumping=true;
   try
   {
    foreach(var pack in packs.Where(p=>p.Handle.Status==PackStatus.Queued).ToArray())
    {
     try
     {
      if(pack.Started==default(DateTime))pack.Started=DateTime.UtcNow;
      if((DateTime.UtcNow-pack.Started).TotalSeconds>180)throw new TimeoutException("Native cosmetic catalog/templates did not become ready within 180 seconds; restart after fixing the catalog error");
      TryRegister(pack);
     }
     catch(Exception e){Fail(pack,e);}
    }
   }
   finally{pumping=false;}
  }
  static void TryRegister(Pack p)
  {
   map=GlobalSettings<PurchasableMap>.Instance;if(map==null)return;
   if(catalog==null)catalog=Purchasable.CheckCatalogAsync();if(!catalog.IsCompleted)return;
   if(catalog.IsFaulted||catalog.IsCanceled)throw new InvalidOperationException("Native cosmetic catalog failed",catalog.Exception);
   var donors=new Dictionary<uint,PlayerCosmeticDefinition>();
   foreach(var id in p.Entries.Select(e=>e.Spec.templateId).Distinct())
   {
    var donor=Resources.FindObjectsOfTypeAll<PlayerCosmeticDefinition>().FirstOrDefault(d=>d.Hash==id);
    if(donor==null&&Purchasable.LoadedPurchasables!=null&&Purchasable.LoadedPurchasables.ContainsKey(id))donor=Purchasable.LoadedPurchasables[id] as PlayerCosmeticDefinition;
    if(donor==null)
    {
     AsyncOperationHandle<PlayerCosmeticDefinition> handle;
     if(!p.Loads.TryGetValue(id,out handle)){p.Loads.Add(id,Addressables.LoadAssetAsync<PlayerCosmeticDefinition>(id.ToString()));return;}
     if(!handle.IsDone)return;if(handle.Status!=AsyncOperationStatus.Succeeded||handle.Result==null)throw new InvalidDataException("Native template "+id+" could not load",handle.OperationException);
     donor=handle.Result;
    }
    if(Find(id)!=null)throw new InvalidDataException("Templates must be native cosmetics, not another custom pack");donors.Add(id,donor);
   }
   var existing=(uint[])Field(map.GetType(),"cosmeticDefinitions").GetValue(map);
   var prefabMap=typeof(PrefabManager).GetField("prefabMap",BindingFlags.Static|BindingFlags.NonPublic)?.GetValue(null) as IDictionary;
   foreach(var e in p.Entries)
   {
    if(e.Id!=HashHelper.HashCompressed(e.Sku))throw new InvalidOperationException("Native hash algorithm differs from this API version");
    if(existing.Contains(e.Id)||map.AllItems.Contains(e.Id.ToString())||Resources.FindObjectsOfTypeAll<Purchasable>().Any(x=>x.Hash==e.Id)||(Purchasable.LoadedPurchasables?.ContainsKey(e.Id)??false)||(prefabMap!=null&&prefabMap.Contains(e.Id)))throw new InvalidOperationException("Cosmetic ID collision: "+e.Sku+" -> "+e.Id);
    if((e.Id==63925&&MelonMod.RegisteredMelons.Any(m=>m.Info.Name=="ClearShirt"))||
     (new uint[]{63925,63704,52837,15096}.Contains(e.Id)&&MelonMod.RegisteredMelons.Any(m=>m.Info.Name=="WayfarerWardrobe")))throw new InvalidOperationException("Remove legacy WayfarerWardrobe.dll/ClearShirt.dll when installing the API Wayfarer pack; saved IDs are preserved");
   }
   if(!ApiCore.IsServer&&p.Entries.Any(e=>ManifestRules.NeedsModel(e.Spec)))LoadAssets(p);
   var categories=(PurchasableMap.CosmeticCategory[])Field(map.GetType(),"categories").GetValue(map);
   foreach(var e in p.Entries)
   {
    var donor=donors[e.Spec.templateId];var donorVisual=donor.GetVisual();
    e.CategoryRow=CategoryLookup.Resolve(categories.Where(c=>c!=null&&c.Category!=null),e.Spec.templateId,donor.Category==null?(int?)null:(int)donor.Category.Type,c=>c.itemHashes,c=>(int)c.Category.Type);
    e.Category=e.CategoryRow.Category as PlayerCosmeticCategory;if(e.Category==null)throw new InvalidDataException("Template is not in a player cosmetic category");
    e.Slots=new HashSet<int>(donorVisual.Parts.SelectMany(part=>part.Slots).Concat(donorVisual.PrimarySlots));
    if(e.Slots.Count==0)throw new InvalidDataException("Template has no wardrobe slots: "+e.Spec.templateId);
    CustomizationVisual visual;
    if(e.Spec.isSkinTone)visual=SkinToneRuntime.Build(e,donor,donorVisual);
    else
    {
    if(donorVisual.Parts.Any(part=>part is CosmeticSkinColor))throw new InvalidDataException("A native skin template requires a skinTone entry");
    e.Template=new GameObject(e.Name+" template");e.Template.SetActive(false);Object.DontDestroyOnLoad(e.Template);
    visual=e.Template.AddComponent<CustomizationVisual>();
    if(!e.Spec.emptyVisual)
    {
     e.Part=e.Template.AddComponent<WearablePart>();e.Part.Entry=e;
     Set(e.Part,"slots",Slots(e.Slots));Set(e.Part,"isPrimary",false);Set(e.Part,"priority",0);
     Set(visual,"parts",new CustomizationVisualPart[]{e.Part});Set(visual,"additionalPrimary",Slots(donorVisual.PrimarySlots));
    }
    else{Set(visual,"parts",new CustomizationVisualPart[0]);Set(visual,"additionalPrimary",Slots(new int[0]));}
    Set(visual,"disableSlots",Slots(new int[0]));
    }
    e.Definition=Object.Instantiate(donor);e.Definition.name=e.Name;e.Definition.SetDisplayName(e.Name);e.Definition.SetSku(e.Sku);e.Definition.SetHash(e.Id);
    Set(e.Definition,"isLocked",false);Set(e.Definition,"overrideHash",0u);Set(e.Definition,"category",e.Category);Set(e.Definition,"visualPrefab",visual);e.Definition.hideFlags=HideFlags.DontSave;Object.DontDestroyOnLoad(e.Definition);e.Definition.GetVisual();
   }
   // Prepare and validate everything before touching global catalog state.
   var originals=new Dictionary<FieldInfo,object>();
   Action<string,object> change=(name,value)=>{var f=Field(map.GetType(),name);originals.Add(f,f.GetValue(map));f.SetValue(map,value);};
   try
   {
    p.Provider=new WardrobeProvider(p);p.Locator=new WardrobeLocator(p,p.Provider.ProviderId);Addressables.ResourceManager.ResourceProviders.Add(p.Provider);Addressables.AddResourceLocator(p.Locator);
    change("cosmeticDefinitions",existing.Concat(p.Entries.Select(e=>e.Id)).ToArray());
    change("categories",categories.Select(c=>c==null?null:new PurchasableMap.CosmeticCategory{Category=c.Category,itemHashes=(c.itemHashes??new uint[0]).Concat(p.Entries.Where(e=>ReferenceEquals(e.CategoryRow,c)).Select(e=>e.Id)).Distinct().ToArray()}).ToArray());
    change("allItems",map.AllItems.Concat(p.Entries.Select(e=>e.Id.ToString())).Distinct().ToArray());change("mappedCosmeticDefinitions",null);change("categoryMap",null);
    foreach(var e in p.Entries){if(Purchasable.LoadedPurchasables!=null)Purchasable.LoadedPurchasables[e.Id]=e.Definition;ready.Add(e.Id,e);}
    p.Handle.Status=PackStatus.Ready;p.Loader=null;
   }
   catch
   {
    foreach(var old in originals)old.Key.SetValue(map,old.Value);
    foreach(var e in p.Entries){ready.Remove(e.Id);if(e.Definition!=null&&Purchasable.LoadedPurchasables!=null&&Purchasable.LoadedPurchasables.ContainsKey(e.Id)&&Purchasable.LoadedPurchasables[e.Id]==e.Definition)Purchasable.LoadedPurchasables.Remove(e.Id);}
    throw;
   }
   foreach(var entry in p.Entries)Definitions.Complete(entry.Id,entry.Definition);
   MelonLogger.Msg("COSMETICS_PACK_READY "+p.Manifest.packId+" version="+p.Manifest.version+" bundle="+p.Manifest.bundleSha256);
   foreach(var e in p.Entries)MelonLogger.Msg("COSMETICS_REGISTERED id="+e.Id+" name="+e.Name+" category="+e.Category.name);
  }
  static void LoadAssets(Pack p)
  {
   var bytes=p.Loader();ManifestRules.VerifyBundle(p.Manifest,bytes);p.Bundle=AssetBundle.LoadFromMemory(bytes);if(p.Bundle==null)throw new InvalidDataException("Unity rejected asset bundle for "+p.Manifest.packId);
   var names=p.Bundle.LoadAsset<TextAsset>("bones");if(names==null)throw new InvalidDataException("Bundle is missing bones manifest");
   p.BoneNames=names.text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
   if(p.BoneNames.Length!=141||p.BoneNames.Distinct().Count()!=141)throw new InvalidDataException("Expected 141 unique ATT bone names");
   foreach(var e in p.Entries.Where(e=>ManifestRules.NeedsModel(e.Spec)))
   {
    e.Model=p.Bundle.LoadAsset<GameObject>(e.Spec.asset);if(e.Model==null)throw new InvalidDataException("Missing prefab "+e.Spec.asset);
    var filters=e.Model.GetComponentsInChildren<MeshFilter>(true);if(filters.Length==0)throw new InvalidDataException("Cosmetic has no meshes: "+e.Name);
    if(e.Model.GetComponentsInChildren<Component>(true).Any(c=>c==null||!(c is Transform||c is MeshFilter||c is MeshRenderer)))throw new InvalidDataException("Export an asset-only prefab: "+e.Name);
    foreach(var filter in filters)
    {
     for(var t=filter.transform;t!=null;t=t==e.Model.transform?null:t.parent)
      if(t.localPosition.sqrMagnitude>.0000001f||Quaternion.Angle(t.localRotation,Quaternion.identity)>.01f||(t.localScale-Vector3.one).sqrMagnitude>.0000001f)throw new InvalidDataException("Exported mesh transforms must be identity: "+e.Name);
     var mesh=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();
     if(mesh==null||mesh.vertexCount==0||mesh.bindposes.Length!=141||mesh.boneWeights.Length!=mesh.vertexCount)throw new InvalidDataException("Missing mesh skin weights: "+e.Name+". Re-export with the Creator SDK.");
     foreach(var w in mesh.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f||w.boneIndex0<0||w.boneIndex0>=141||w.boneIndex1<0||w.boneIndex1>=141||w.boneIndex2<0||w.boneIndex2>=141||w.boneIndex3<0||w.boneIndex3>=141)throw new InvalidDataException("Invalid skin weights on "+mesh.name);
     if(renderer==null||renderer.sharedMaterials.Length!=mesh.subMeshCount||renderer.sharedMaterials.Any(m=>m==null||m.shader==null||!m.shader.isSupported))throw new InvalidDataException("Missing/unsupported materials on "+e.Name);
    }
   }
  }
  static void Fail(Pack p,Exception error)
  {
   p.Handle.Status=PackStatus.Failed;p.Handle.Error=error.Message;p.Loader=null;
   if(p.Locator!=null)Addressables.RemoveResourceLocator(p.Locator);if(p.Provider!=null)Addressables.ResourceManager.ResourceProviders.Remove(p.Provider);
   foreach(var e in p.Entries){Definitions.Fail(e.Id,error);if(e.Definition!=null)Object.Destroy(e.Definition);if(e.Template!=null)Object.Destroy(e.Template);if(e.SkinColor!=null)Object.Destroy(e.SkinColor);}
   foreach(var h in p.Loads.Values)if(h.IsValid())Addressables.Release(h);p.Loads.Clear();
   if(p.Bundle!=null)p.Bundle.Unload(true);
   MelonLogger.Error("COSMETICS_PACK_FAILED "+p.Manifest.packId+": "+error);
  }
  internal static IEnumerable<uint> Append(IEnumerable<uint> original,Func<uint,bool> filter)
  {var ids=(original??Enumerable.Empty<uint>()).ToList();foreach(var e in Entries)if(!ids.Contains(e.Id)&&(filter==null||filter(e.Id)))ids.Add(e.Id);return ids;}
  internal static IEnumerable<uint> Conflicts(CustomizationWrapper wrapper,ICustomizationVisual visual,IEnumerable<uint> original)
  {
   var result=original.ToList();if(!Ready||!(wrapper is PlayerCosmeticWrapper)||visual?.Definition==null)return result;
   var selected=Find(visual.Definition.Hash);foreach(var id in wrapper.Items.ToArray())if(id!=visual.Definition.Hash&&(selected!=null||Find(id)!=null)&&map.GetItemCategory(id)?.Type==visual.Definition.Category.Type)result.Add(id);
   return result.Where(id=>id!=visual.Definition.Hash).Distinct().ToArray();
  }
 }
}
