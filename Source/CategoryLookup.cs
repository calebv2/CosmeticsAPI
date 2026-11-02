using System;
using System.Collections.Generic;
using System.Linq;

namespace ATT.Cosmetics
{
 // Resolve the canonical table ROW, not the independently loaded donor asset.
 // Kept free of Unity objects so the duplicate-asset case can be regression tested.
 internal static class CategoryLookup
 {
  internal static T Resolve<T>(IEnumerable<T> table,uint donor,int? donorType,Func<T,IEnumerable<uint>> ids,Func<T,int> type)
  {
   var rows=table.ToArray();
   var matches=rows.Where(row=>(ids(row)??Enumerable.Empty<uint>()).Contains(donor)).ToArray();
   if(matches.Length>1)throw new InvalidOperationException("Native donor is listed in multiple category rows: "+donor);
   if(matches.Length==1)
   {
    if(donorType.HasValue&&type(matches[0])!=donorType.Value)throw new InvalidOperationException("Native donor/category type mismatch: "+donor);
    return matches[0];
   }
   if(!donorType.HasValue)throw new InvalidOperationException("No native category metadata for donor "+donor);
   matches=rows.Where(row=>type(row)==donorType.Value).ToArray();
   if(matches.Length!=1)throw new InvalidOperationException("Expected one native category row for type "+donorType.Value+", found "+matches.Length+" (donor "+donor+")");
   return matches[0];
  }
 }
}

