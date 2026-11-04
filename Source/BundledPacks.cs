using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MelonLoader;
namespace ATT.Cosmetics
{
 internal static class BundledPacks
 {
  internal static bool Install()
  {
   string packs=CosmeticsApi.PacksDirectory;
   if(!Path.IsPathRooted(packs))throw new InvalidDataException("CosmeticsAPI UserData path must be absolute");
   var settingsPath=Path.Combine(Directory.GetParent(packs).FullName,"settings.json");Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
   if(!File.Exists(settingsPath))File.WriteAllText(settingsPath,"{\n  \"installBundledPacks\": true\n}\n");
   if(!PackJson.ReadInstallBundledPacks(File.ReadAllText(settingsPath))){MelonLogger.Msg("COSMETICS_BUNDLED_DISABLED "+settingsPath);return true;}
   bool succeeded=true;
   foreach(var name in new[]{"Wayfarer","Natural","Fantasy"})
   {
    try
    {
     var json=Read("ATT.Cosmetics.Bundled."+name+".Manifest");var manifest=PackJson.ReadManifest(Encoding.UTF8.GetString(json));
     var files=new Dictionary<string,byte[]>{{"pack.json",json}};
     if(!ApiCore.IsServer&&manifest.cosmetics.Any(ManifestRules.NeedsModel))
     {
      var bundle=Read("ATT.Cosmetics.Bundled."+name+".Assets");ManifestRules.VerifyBundle(manifest,bundle);files.Add(manifest.bundleFile,bundle);
     }
     var result=BundledPackInstaller.InstallPack(packs,new BundledPackFiles{PackId=manifest.packId,Files=files});
     MelonLogger.Msg("COSMETICS_BUNDLED_PACK "+manifest.packId+" "+result+" cosmetics="+manifest.cosmetics.Length);
     if(result==BundledInstallResult.Preserved)MelonLogger.Warning("COSMETICS_BUNDLED_PRESERVED "+manifest.packId+": existing edited or independently installed content retained; restore the bundled pack folder to receive its default files");
    }
    catch(Exception error){succeeded=false;MelonLogger.Error("COSMETICS_BUNDLED_FAILED "+name+": "+error.Message);}
   }
   return succeeded;
  }
  static byte[] Read(string name)
  {
   using(var stream=typeof(BundledPacks).Assembly.GetManifestResourceStream(name))
   {if(stream==null)throw new FileNotFoundException("Missing bundled cosmetic resource: "+name);using(var copy=new MemoryStream()){stream.CopyTo(copy);return copy.ToArray();}}
  }
 }
}
