using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly:MelonInfo(typeof(ATT.Cosmetics.ApiCore),"CosmeticsAPI","0.2.1","caleb")]
[assembly:MelonGame("Alta","A Township Tale")]
namespace ATT.Cosmetics
{
 public static class CosmeticsApi
 {
  public const string Version="0.2.1";
  public static string PacksDirectory=>Path.Combine(MelonEnvironment.UserDataDirectory,"CosmeticsAPI","Packs");
  /// <summary>Call from Melon initialization on the main thread. The client loader is never called by the server.</summary>
  public static PackHandle RegisterPack(PackManifest manifest,Func<byte[]> clientBundleLoader)
  {
   ApiCore.CheckThread();return Registry.Queue(ManifestRules.Snapshot(manifest),clientBundleLoader);
  }
  public static PackHandle RegisterFolder(string folder)
  {
   ApiCore.CheckThread();folder=Path.GetFullPath(folder);
   var path=Path.Combine(folder,"pack.json");if(new FileInfo(path).Length>1024*1024)throw new InvalidDataException("Pack manifest exceeds 1 MiB");
   var manifest=JsonUtility.FromJson<PackManifest>(File.ReadAllText(path));ManifestRules.Validate(manifest);
   string bundlePath=Path.Combine(folder,manifest.bundleFile??"assets.bundle");
   return RegisterPack(manifest,()=>File.ReadAllBytes(bundlePath));
  }
  public static PackHandle RegisterEmbedded(Assembly assembly,string manifestResource,string bundleResource)
  {
   if(assembly==null)throw new ArgumentNullException(nameof(assembly));
   var manifest=JsonUtility.FromJson<PackManifest>(System.Text.Encoding.UTF8.GetString(ReadResource(assembly,manifestResource)));
   return RegisterPack(manifest,()=>ReadResource(assembly,bundleResource));
  }
  static byte[] ReadResource(Assembly assembly,string name)
  {
   using(var stream=assembly.GetManifestResourceStream(name))
   {if(stream==null)throw new FileNotFoundException("Missing cosmetic resource: "+name);using(var copy=new MemoryStream()){stream.CopyTo(copy);return copy.ToArray();}}
  }
  public static PackHandle[] GetPacks(){ApiCore.CheckThread();return Registry.Handles();}
 }
 public sealed class ApiCore:MelonMod
 {
  internal static int MainThread;
  internal static bool Started;
#if SERVER
  internal static bool IsServer=>true;
#else
  internal static bool IsServer=>false;
#endif
  float nextPump;
  internal static void CheckThread(){if(MainThread!=0&&Thread.CurrentThread.ManagedThreadId!=MainThread)throw new InvalidOperationException("CosmeticsAPI registration must run on the Unity main thread");}
  public override void OnInitializeMelon(){MainThread=Thread.CurrentThread.ManagedThreadId;LoggerInstance.Msg("COSMETICS_API_START "+CosmeticsApi.Version+" "+(IsServer?"server":"client"));}
  public override void OnLateInitializeMelon()
  {
   Directory.CreateDirectory(CosmeticsApi.PacksDirectory);
   foreach(var file in Directory.GetFiles(CosmeticsApi.PacksDirectory,"pack.json",SearchOption.AllDirectories).OrderBy(f=>f,StringComparer.Ordinal))
    try{CosmeticsApi.RegisterFolder(Path.GetDirectoryName(file));}catch(Exception e){LoggerInstance.Error("COSMETICS_PACK_REJECTED "+file+": "+e.Message);}
   Started=true;Registry.Pump();
  }
  public override void OnUpdate(){if(Started&&Time.realtimeSinceStartup>=nextPump){nextPump=Time.realtimeSinceStartup+.2f;Registry.Pump();}}
 }
}

