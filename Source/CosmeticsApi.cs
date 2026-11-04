using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly:MelonInfo(typeof(ATT.Cosmetics.ApiCore),"CosmeticsAPI","0.2.2","caleb")]
[assembly:MelonGame("Alta","A Township Tale")]
namespace ATT.Cosmetics
{
 public static class CosmeticsApi
 {
  public const string Version="0.2.2";
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
  float nextPump,nextBootstrap;int bootstrapAttempts;bool bundledPrepared,updateScanned;
  readonly HashSet<string> discovered=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  internal static void CheckThread(){if(MainThread!=0&&Thread.CurrentThread.ManagedThreadId!=MainThread)throw new InvalidOperationException("CosmeticsAPI registration must run on the Unity main thread");}
  public override void OnInitializeMelon(){MainThread=Thread.CurrentThread.ManagedThreadId;LoggerInstance.Msg("COSMETICS_API_START "+CosmeticsApi.Version+" "+(IsServer?"server":"client"));Bootstrap("initialize");}
  public override void OnLateInitializeMelon(){Bootstrap("late");}
  void Bootstrap(string phase)
  {
   if(MainThread==0)MainThread=Thread.CurrentThread.ManagedThreadId;CheckThread();
   bootstrapAttempts++;nextBootstrap=Time.realtimeSinceStartup+1;
   if(!bundledPrepared)
   {
    try{bundledPrepared=BundledPacks.Install();}
    catch(Exception error){LoggerInstance.Error("COSMETICS_BOOTSTRAP_FAILED bundled content: "+error.Message);}
   }
   try
   {
    string path=CosmeticsApi.PacksDirectory;if(!Path.IsPathRooted(path))throw new InvalidDataException("CosmeticsAPI UserData directory must be absolute");
    Directory.CreateDirectory(path);var files=Directory.GetFiles(path,"pack.json",SearchOption.AllDirectories).OrderBy(f=>f,StringComparer.Ordinal).ToArray();
    int queued=0,rejected=0;
    LoggerInstance.Msg("COSMETICS_PACK_SCAN phase="+phase+" path="+path+" manifests="+files.Length);
    foreach(var file in files)
    {
     string fullPath=Path.GetFullPath(file);if(discovered.Contains(fullPath))continue;
     try{CosmeticsApi.RegisterFolder(Path.GetDirectoryName(file));discovered.Add(fullPath);queued++;}
     catch(Exception error){rejected++;LoggerInstance.Error("COSMETICS_PACK_REJECTED "+file+": "+error.Message);}
    }
    Started=true;LoggerInstance.Msg("COSMETICS_DISCOVERY_COMPLETE queued="+queued+" rejected="+rejected+" totalPacks="+CosmeticsApi.GetPacks().Length);
    if(files.Length==0&&!Registry.HasPacks)LoggerInstance.Warning("COSMETICS_NO_PACKS: no cosmetic manifests found in "+path+"; check bundled content settings and startup errors");
   }
   catch(Exception error){LoggerInstance.Error("COSMETICS_BOOTSTRAP_FAILED discovery: "+error.Message);}
  }
  public override void OnUpdate()
  {
   if(!updateScanned){updateScanned=true;Bootstrap("first-update");}
   else if((!Started||!bundledPrepared)&&bootstrapAttempts<5&&Time.realtimeSinceStartup>=nextBootstrap)Bootstrap("retry");
   if(Started&&Time.realtimeSinceStartup>=nextPump){nextPump=Time.realtimeSinceStartup+.2f;Registry.Pump();}
  }
 }
}

