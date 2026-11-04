using System.Threading;
using System.Threading.Tasks;
using Alta;
using Alta.Customization;
using Alta.Serialization;
using HarmonyLib;
namespace ATT.Cosmetics
{
 // Player streams can arrive before a queued pack's native donor finishes
 // loading. Known custom IDs must wait instead of becoming failed addresses.
 [HarmonyPatch(typeof(PlayerCosmeticWrapper),"GetItem",new[]{typeof(uint)})]
 static class DefinitionReadyPatch
 {
  static bool Prefix(uint hash,ref Task<ICustomizationDefinition> __result)
  {
   Task<ICustomizationDefinition> task;if(!Registry.Definitions.TryGet(hash,out task))return true;
   __result=task;return false;
  }
 }
 [HarmonyPatch(typeof(CustomizationWrapperSerializer),"SerializeStreamAsync")]
 static class AppearanceStreamPatch
 {
  static bool Prefix(ICustomizationWrapper wrapper,Stream stream,bool isApplying,CancellationToken cancellation,ref Task __result)
  {
   if(!(wrapper is PlayerCosmeticWrapper)||stream.IsWriting||!Registry.HasPacks)return true;
   __result=Read(wrapper,stream,isApplying,cancellation);return false;
  }
  static async Task Read(ICustomizationWrapper wrapper,Stream source,bool isApplying,CancellationToken cancellation)
  {
   cancellation.ThrowIfCancellationRequested();
   // ReadStreamAsync loads the full set and native body defaults, then calls
   // ApplyStreamItems, which clears and replaces the appearance. Clearing it
   // before those asynchronous loads leaves an invisible avatar on failure.
   var stream=(Stream)source.Clone();
   using(var session=new AddressableSession())
   {
    try
    {
     await wrapper.StartSession(session);
     cancellation.ThrowIfCancellationRequested();
     await CustomizationWrapperSerializer.ReadStreamAsync(wrapper,(StreamReader)stream,isApplying,cancellation);
    }
    finally{wrapper.StopSession(session);}
   }
  }
 }
}
