using System;
using System.Collections.Generic;
using System.Linq;
namespace ATT.Cosmetics
{
 internal sealed class PackIdentityIndex
 {
  readonly HashSet<string> packs=new HashSet<string>(StringComparer.Ordinal);
  readonly Dictionary<uint,string> owners=new Dictionary<uint,string>();
  internal void Reserve(PackManifest manifest)
  {
   ManifestRules.Validate(manifest);
   if(packs.Contains(manifest.packId))throw new InvalidOperationException("Pack already registered: "+manifest.packId);
   var ids=manifest.cosmetics.Select(c=>CosmeticIds.FromSku(c.sku)).ToArray();
   foreach(var id in ids)if(owners.ContainsKey(id))throw new InvalidOperationException("Cosmetic ID "+id+" conflicts with pack "+owners[id]+". Change the NEW item's SKU before release; never renumber a saved item.");
   // All-or-nothing reservation: a rejected pack cannot consume its earlier IDs.
   packs.Add(manifest.packId);foreach(var id in ids)owners.Add(id,manifest.packId);
  }
  internal string OwnerOf(uint id){string owner;return owners.TryGetValue(id,out owner)?owner:null;}
 }
}
