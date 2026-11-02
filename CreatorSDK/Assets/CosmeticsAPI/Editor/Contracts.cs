using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ATT.Cosmetics
{
 [Serializable] public sealed class PackManifest
 {
  public int schemaVersion=1;
  public string packId,displayName,version;
  public string bundleFile="assets.bundle",bundleSha256;
  public CosmeticDefinition[] cosmetics;
 }
 [Serializable] public sealed class CosmeticDefinition
 {
  public string sku,displayName,asset;
  public uint templateId,expectedId;
  public bool emptyVisual,hideInFirstPerson,isSkinTone;
  public SkinToneDefinition skinTone;
 }
 [Serializable] public sealed class SkinToneDefinition
 {
  public string shadows,midtones,highlights;
 }
 public enum PackStatus {Queued,Ready,Failed}
 public sealed class PackHandle
 {
  public string PackId {get;internal set;}
  public string Version {get;internal set;}
  public PackStatus Status {get;internal set;}
  public string Error {get;internal set;}
 }
 public static class CosmeticIds
 {
  // ATT's deterministic alternating DJB2 hash, compressed to its 16-bit ID space.
  // Never auto-renumber a collision: doing so changes saved character identities.
  public static uint FromSku(string sku)
  {
   if(string.IsNullOrEmpty(sku))throw new ArgumentException("SKU is required");
   unchecked{int a=5381,b=5381;for(int i=0;i<sku.Length;i++)if((i&1)==0)a=((a<<5)+a)^sku[i];else b=((b<<5)+b)^sku[i];return (uint)(a+b*1566083941)&65535;}
  }
 }
 public static class ManifestRules
 {
  static readonly Regex Token=new Regex("^[a-z0-9][a-z0-9._-]{2,95}\\z",RegexOptions.CultureInvariant);
  public static void Validate(PackManifest manifest)
  {
   if(manifest==null||(manifest.schemaVersion!=1&&manifest.schemaVersion!=2))throw new InvalidDataException("Expected cosmetic pack schemaVersion 1 or 2");
   if(!Token.IsMatch(manifest.packId??""))throw new InvalidDataException("packId needs 3-96 lowercase letters, digits, dots, underscores or hyphens");
   Text(manifest.displayName,"pack displayName",96);Text(manifest.version,"version",32);
   if(manifest.cosmetics==null||manifest.cosmetics.Length==0||manifest.cosmetics.Length>128)throw new InvalidDataException("A pack needs 1-128 cosmetics");
   if(manifest.cosmetics.Any(c=>c==null))throw new InvalidDataException("Null cosmetic entry");
   bool models=manifest.cosmetics.Any(NeedsModel);
   if(models)
   {
    if(!Token.IsMatch(manifest.bundleFile??"")||manifest.bundleFile=="..")throw new InvalidDataException("bundleFile must be a simple filename inside the pack folder");
    if(!Regex.IsMatch(manifest.bundleSha256??"","^[a-fA-F0-9]{64}\\z"))throw new InvalidDataException("bundleSha256 must be the exported bundle SHA-256");
   }
   var ids=new HashSet<uint>();var skus=new HashSet<string>(StringComparer.Ordinal);
   foreach(var c in manifest.cosmetics)
   {
    if(!Token.IsMatch(c.sku??""))throw new InvalidDataException("Invalid SKU: use a stable lowercase creator.pack.item name");
    Text(c.displayName,"cosmetic displayName",96);
    if(c.templateId==0||c.templateId>65535)throw new InvalidDataException("templateId must identify a native ATT cosmetic (1-65535)");
    // Unity's JSON serializer creates empty nested objects even when an older
    // manifest omits the field. Use an explicit discriminator, not nullness.
    if(c.isSkinTone)
    {
     if(manifest.schemaVersion<2)throw new InvalidDataException("Skin tones require schemaVersion 2 and CosmeticsAPI 0.2.0 or newer");
     if(c.emptyVisual||!string.IsNullOrEmpty(c.asset)||c.hideInFirstPerson)throw new InvalidDataException("Skin tones use the native whole-body colour system; no model, empty visual, or first-person hiding is allowed");
     if(c.skinTone==null)throw new InvalidDataException("Skin-tone RGB ramp is required");
     foreach(var hex in new[]{c.skinTone.shadows,c.skinTone.midtones,c.skinTone.highlights})if(!Regex.IsMatch(hex??"","^#[0-9a-fA-F]{6}\\z"))throw new InvalidDataException("Skin-tone shadows, midtones and highlights must use #RRGGBB colours");
    }
    else if(c.skinTone!=null&&new[]{c.skinTone.shadows,c.skinTone.midtones,c.skinTone.highlights}.Any(s=>!string.IsNullOrEmpty(s)))throw new InvalidDataException("Set isSkinTone=true when providing a skin-tone ramp");
    if(c.emptyVisual&&!string.IsNullOrEmpty(c.asset))throw new InvalidDataException("An empty visual must not name a model asset");
    if(NeedsModel(c)&&!Token.IsMatch(c.asset??""))throw new InvalidDataException("A visible cosmetic needs an exported prefab asset key");
    uint id=CosmeticIds.FromSku(c.sku);
    if(id==0||!ids.Add(id)||!skus.Add(c.sku))throw new InvalidDataException("Zero/duplicate/colliding cosmetic ID for "+c.sku+". Choose a new SKU before first release.");
    if(c.expectedId!=0&&c.expectedId!=id)throw new InvalidDataException("Saved expectedId does not match SKU for "+c.sku);
    if(c.templateId==id)throw new InvalidDataException("Cosmetic ID collides with its native template: "+c.sku);
   }
  }
  public static bool NeedsModel(CosmeticDefinition item)=>!item.emptyVisual&&!item.isSkinTone;
  static void Text(string text,string field,int max){if(string.IsNullOrWhiteSpace(text)||text.Length>max||text.Any(char.IsControl))throw new InvalidDataException("Invalid "+field);}
  public static string Sha256(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  public static void VerifyBundle(PackManifest manifest,byte[] bytes)
  {
   if(bytes==null||bytes.Length==0||!string.Equals(Sha256(bytes),manifest.bundleSha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Bundle SHA-256 mismatch for "+manifest.packId+"; install matching pack.json and assets.bundle");
  }
  internal static PackManifest Snapshot(PackManifest p)
  {
   Validate(p);return new PackManifest{schemaVersion=p.schemaVersion,packId=p.packId,displayName=p.displayName,version=p.version,bundleFile=p.bundleFile,bundleSha256=p.bundleSha256,cosmetics=p.cosmetics.Select(c=>new CosmeticDefinition{sku=c.sku,displayName=c.displayName,asset=c.asset,templateId=c.templateId,expectedId=c.expectedId,emptyVisual=c.emptyVisual,hideInFirstPerson=c.hideInFirstPerson,isSkinTone=c.isSkinTone,skinTone=!c.isSkinTone?null:new SkinToneDefinition{shadows=c.skinTone.shadows,midtones=c.skinTone.midtones,highlights=c.skinTone.highlights}}).ToArray()};
  }
 }
}
