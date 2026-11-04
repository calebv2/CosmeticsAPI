using System;
using System.Collections.Generic;
using System.Linq;
using Alta.Customization;
using UnityEngine;
using Object=UnityEngine.Object;
namespace ATT.Cosmetics
{
 public sealed class WearablePart:CustomizationVisualPart
 {
  internal Entry Entry;
  protected override bool IsShownInFirstPerson=>!Entry.Spec.hideInFirstPerson;
  sealed class Attachment {internal GameObject Root;internal WearableLodBinding Binding;}
  readonly Dictionary<ICustomizationHandler,Attachment> attached=new Dictionary<ICustomizationHandler,Attachment>();
  public override CustomizationArguments CreateDefaultArguments()=>new CustomizationArguments();
  protected override void Attach(ICustomizationHandler handler,CustomizationArguments arguments)
  {
   if(ApiCore.IsServer||handler.IsDestroyed)return;Detach(handler);
   // Clothes.Renderers lists HEAD renderers first; using one would hide this
   // whole outfit in first person because head renderers become ShadowsOnly.
   var clothes=handler.CharacterDummy.Clothes;
   var bodyRenderers=(SkinnedMeshRenderer[])Registry.Field(clothes.GetType(),"opaqueClothes").GetValue(clothes);
   var reference=bodyRenderers.FirstOrDefault(r=>r!=null&&r.bones.Length==141);
   if(reference==null)throw new InvalidOperationException("No native 141-bone clothing renderer on "+handler.Name);
   var bones=reference.bones;for(int i=0;i<bones.Length;i++)if(bones[i]==null||bones[i].name!=Entry.Pack.BoneNames[i])throw new InvalidOperationException("Native bone order mismatch at "+i);
   var root=new GameObject(Entry.Name);root.transform.SetParent(reference.transform.parent,false);root.transform.localPosition=reference.transform.localPosition;root.transform.localRotation=reference.transform.localRotation;root.transform.localScale=reference.transform.localScale;
   var attachment=new Attachment{Root=root};attached[handler]=attachment;var renderers=new List<SkinnedMeshRenderer>();
   try
   {
   attachment.Binding=WearableLodBinding.Attach(handler.CharacterDummy.LodGroup,handler.CharacterDummy.Renderers,level=>
   {
    var lodRoot=new GameObject("LOD "+level);lodRoot.transform.SetParent(root.transform,false);
    var current=new List<Renderer>();
    foreach(var source in Entry.Model.GetComponentsInChildren<MeshFilter>())
    {
     var child=new GameObject(source.name);child.layer=reference.gameObject.layer;child.transform.SetParent(lodRoot.transform,false);
     var renderer=child.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=source.sharedMesh;renderer.sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;renderer.bones=bones;renderer.rootBone=reference.rootBone;
     renderer.localBounds=reference.localBounds;renderer.updateWhenOffscreen=false;renderer.quality=SkinQuality.Bone4;renderer.shadowCastingMode=reference.shadowCastingMode;renderer.receiveShadows=true;renderer.probeAnchor=reference.probeAnchor;
     current.Add(renderer);renderers.Add(renderer);
    }
    return current.ToArray();
   });
   var state=root.AddComponent<WearableVisibility>();state.Reference=reference;state.Renderers=renderers.ToArray();state.Binding=attachment.Binding;
   state.Released=()=>{Attachment active;if(attached.TryGetValue(handler,out active)&&ReferenceEquals(active,attachment))attached.Remove(handler);};
   }
   catch{Detach(handler);throw;}
  }
  protected override void Detach(ICustomizationHandler handler)
  {
   Attachment attachment;if(!attached.TryGetValue(handler,out attachment))return;attached.Remove(handler);
   attachment.Binding?.Release();if(attachment.Root==null)return;
   attachment.Root.SetActive(false);Object.Destroy(attachment.Root);
  }
  internal void Release(ICustomizationHandler handler)=>Detach(handler);
  protected override void ChangeArguments(ICustomizationHandler handler,CustomizationArguments arguments){}
 }
 public sealed class WearableVisibility:MonoBehaviour
 {
  internal SkinnedMeshRenderer Reference;internal SkinnedMeshRenderer[] Renderers;
  internal WearableLodBinding Binding;internal Action Released;
  void LateUpdate(){if(Reference==null){Destroy(gameObject);return;}foreach(var renderer in Renderers)if(renderer!=null){renderer.shadowCastingMode=Reference.shadowCastingMode;renderer.gameObject.layer=Reference.gameObject.layer;}}
  void OnDestroy(){Binding?.Release();Released?.Invoke();}
 }
}
