using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ATT.Cosmetics
{
 // Native avatar LOD reduction disables the renderers in the removed level.
 // Each remaining level therefore needs its own renderer instances.
 internal sealed class WearableLodBinding
 {
  readonly LODGroup group;
  readonly List<Renderer> tracked;
  readonly HashSet<Renderer> owned=new HashSet<Renderer>();
  bool released;
  WearableLodBinding(LODGroup group,List<Renderer> tracked){this.group=group;this.tracked=tracked;}
  internal static WearableLodBinding Attach(LODGroup group,List<Renderer> tracked,Func<int,Renderer[]> createLevel)
  {
   if(tracked==null)throw new ArgumentNullException(nameof(tracked));
   if(createLevel==null)throw new ArgumentNullException(nameof(createLevel));
   var binding=new WearableLodBinding(group,tracked);
   var levels=group==null?new LOD[0]:group.GetLODs();
   try
   {
    Renderer[] first=null;
    for(int i=0;i<Math.Max(1,levels.Length);i++)
    {
     var renderers=createLevel(i);
     if(renderers==null||renderers.Any(r=>r==null))throw new InvalidOperationException("Invalid wearable LOD renderers");
     foreach(var renderer in renderers)if(!binding.owned.Add(renderer))throw new InvalidOperationException("Wearable LODs must not share renderer instances");
     if(i==0)first=renderers;
     if(i<levels.Length)levels[i].renderers=(levels[i].renderers??new Renderer[0]).Concat(renderers).ToArray();
    }
    if(group!=null&&levels.Length>0)group.SetLODs(levels);
    // This native list represents the current first LOD, rather than every
    // renderer. CharacterDummy.ReduceLOD refreshes it when dropping a level.
    tracked.AddRange(first);
    return binding;
   }
   catch{binding.Release();throw;}
  }
  internal void Release()
  {
   if(released)return;released=true;
   tracked.RemoveAll(r=>owned.Contains(r));
   if(group==null)return;
   var levels=group.GetLODs();
   for(int i=0;i<levels.Length;i++)levels[i].renderers=(levels[i].renderers??new Renderer[0]).Where(r=>!owned.Contains(r)).ToArray();
   group.SetLODs(levels);
  }
 }
}
