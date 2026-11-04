using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
namespace ATT.Cosmetics
{
 internal enum BundledInstallResult {Installed,Unchanged,Preserved}
 internal sealed class BundledPackFiles
 {
  internal string PackId;
  internal IDictionary<string,byte[]> Files;
 }
 internal static class BundledPackInstaller
 {
  const string StateName=".bundled-state";
  sealed class Change {internal string Destination,Staged,Backup;internal bool Existed,Committed;}
  internal static BundledInstallResult InstallPack(string packsDirectory,BundledPackFiles pack)
  {
   if(string.IsNullOrWhiteSpace(packsDirectory)||!Path.IsPathRooted(packsDirectory))throw new InvalidDataException("An absolute cosmetic packs directory is required");
   if(pack==null||!Regex.IsMatch(pack.PackId??"","^[a-z0-9][a-z0-9._-]{2,95}\\z"))throw new InvalidDataException("Invalid bundled pack ID");
   if(pack.Files==null||!pack.Files.ContainsKey("pack.json")||pack.Files.Count>8)throw new InvalidDataException("Bundled pack needs a manifest");
   foreach(var file in pack.Files)if(!Regex.IsMatch(file.Key??"","^[a-z0-9][a-z0-9._-]{2,95}\\z")||file.Key==StateName||file.Value==null||file.Value.Length==0)throw new InvalidDataException("Invalid bundled content file");
   var packs=Path.GetFullPath(packsDirectory);var target=Path.GetFullPath(Path.Combine(packs,pack.PackId));
   if(!target.StartsWith(packs.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Bundled target escapes packs directory");
   if(IsReparse(target)||IsReparse(Path.Combine(target,StateName))||pack.Files.Keys.Any(name=>IsReparse(Path.Combine(target,name))))return BundledInstallResult.Preserved;
   var previous=ReadState(Path.Combine(target,StateName));
   var desired=pack.Files.ToDictionary(f=>f.Key,f=>ManifestRules.Sha256(f.Value),StringComparer.Ordinal);
   var payload=new Dictionary<string,byte[]>(StringComparer.Ordinal);
   foreach(var file in pack.Files)
   {
    var path=Path.Combine(target,file.Key);if(!File.Exists(path)){payload.Add(file.Key,file.Value);continue;}
    string existing=ManifestRules.Sha256(File.ReadAllBytes(path));if(existing==desired[file.Key])continue;
    string old;if(!previous.TryGetValue(file.Key,out old)||existing!=old)return BundledInstallResult.Preserved;
    payload.Add(file.Key,file.Value);
   }
   var state=Encoding.UTF8.GetBytes(string.Join("\n",desired.OrderBy(f=>f.Key,StringComparer.Ordinal).Select(f=>f.Key+"\t"+f.Value))+"\n");
   var statePath=Path.Combine(target,StateName);
   if(!File.Exists(statePath)||!File.ReadAllBytes(statePath).SequenceEqual(state))payload.Add(StateName,state);
   if(payload.Count==0)return BundledInstallResult.Unchanged;
   var parent=Directory.GetParent(packs);if(parent==null)throw new InvalidDataException("Packs directory needs a parent");
   Directory.CreateDirectory(parent.FullName);var staging=Path.Combine(parent.FullName,".bundled-install-"+Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(staging);var changes=new List<Change>();bool keepRecovery=false;
   try
   {
    // Prepare every byte before replacing any live pack file. Manifest is last
    // among content, so discovery cannot observe a new manifest with old assets.
    foreach(var file in payload.OrderBy(f=>f.Key==StateName?2:f.Key=="pack.json"?1:0).ThenBy(f=>f.Key,StringComparer.Ordinal))
    {
     var change=new Change{Destination=Path.Combine(target,file.Key),Staged=Path.Combine(staging,file.Key+".new"),Backup=Path.Combine(staging,file.Key+".old")};
     change.Existed=File.Exists(change.Destination);File.WriteAllBytes(change.Staged,file.Value);changes.Add(change);
    }
    Directory.CreateDirectory(target);
    foreach(var change in changes)
    {
     if(change.Existed)File.Replace(change.Staged,change.Destination,change.Backup);
     else File.Move(change.Staged,change.Destination);
     change.Committed=true;
    }
    return BundledInstallResult.Installed;
   }
   catch(Exception updateError)
   {
    var recoveryErrors=new List<Exception>();
    foreach(var change in Enumerable.Reverse(changes).Where(c=>c.Committed))
    {
     try
     {
      if(change.Existed)File.Replace(change.Backup,change.Destination,null);
      else File.Delete(change.Destination);
     }
     catch(Exception recoveryError){keepRecovery=true;recoveryErrors.Add(recoveryError);}
    }
    if(keepRecovery)throw new IOException("Bundled update failed; recovery files retained at "+staging,new AggregateException(new[]{updateError}.Concat(recoveryErrors)));
    throw;
   }
   finally
   {
    // This directory is flat and contains only the prepared files above.
    if(!keepRecovery){foreach(var path in Directory.GetFiles(staging))File.Delete(path);Directory.Delete(staging);}
   }
  }
  static bool IsReparse(string path)
  {
   try{return (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0;}
   catch(FileNotFoundException){return false;}
   catch(DirectoryNotFoundException){return false;}
  }
  static Dictionary<string,string> ReadState(string path)
  {
   var result=new Dictionary<string,string>(StringComparer.Ordinal);if(!File.Exists(path))return result;
   foreach(var line in File.ReadAllLines(path))
   {
    var parts=line.Split('\t');
    if(parts.Length!=2||!Regex.IsMatch(parts[0],"^[a-z0-9][a-z0-9._-]{2,95}\\z")||!Regex.IsMatch(parts[1],"^[a-f0-9]{64}\\z"))return new Dictionary<string,string>(StringComparer.Ordinal);
    if(result.ContainsKey(parts[0]))return new Dictionary<string,string>(StringComparer.Ordinal);
    result.Add(parts[0],parts[1]);
   }
   return result;
  }
 }
}
