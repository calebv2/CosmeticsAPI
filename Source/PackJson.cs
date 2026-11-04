using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace ATT.Cosmetics
{
 internal static class PackJson
 {
  sealed class Settings {public bool installBundledPacks=true;}
  internal static PackManifest ReadManifest(string json)
  {
   var manifest=Read<PackManifest>(json);ManifestRules.Validate(manifest);return manifest;
  }
  internal static bool ReadInstallBundledPacks(string json)=>Read<Settings>(json).installBundledPacks;
  static T Read<T>(string json) where T:class
  {
   if(string.IsNullOrWhiteSpace(json)||json.Length>1024*1024)throw new InvalidDataException("Cosmetics JSON must contain an object no larger than 1 MiB");
   if(json[0]=='\uFEFF')json=json.Substring(1);
   // Use the game's managed JSON library. Unity's runtime serializer can omit
   // arrays of classes loaded from mod assemblies. A fresh serializer also
   // isolates pack parsing from global JSON defaults installed by other mods.
   var serializer=JsonSerializer.Create(new JsonSerializerSettings{
    TypeNameHandling=TypeNameHandling.None,MetadataPropertyHandling=MetadataPropertyHandling.Ignore,
    ObjectCreationHandling=ObjectCreationHandling.Replace,MissingMemberHandling=MissingMemberHandling.Ignore,
    ContractResolver=new DefaultContractResolver{IgnoreSerializableAttribute=true},
    Culture=CultureInfo.InvariantCulture,DateParseHandling=DateParseHandling.None,MaxDepth=16,CheckAdditionalContent=true});
   try
   {
    using(var text=new StringReader(json))
    using(var reader=new JsonTextReader(text){MaxDepth=16,DateParseHandling=DateParseHandling.None})
    {
     var result=serializer.Deserialize<T>(reader);if(result==null)throw new InvalidDataException("Cosmetics JSON must contain an object");return result;
    }
   }
   catch(JsonException error){throw new InvalidDataException("Invalid cosmetics JSON: "+error.Message,error);}
  }
 }
}
