using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace ATT.Cosmetics.Creator
{
 [CreateAssetMenu(fileName="Skin Palette",menuName="Cosmetics API/Skin Colour Palette")]
 public sealed class SkinPaletteAsset:ScriptableObject
 {
  public string packId="yourname.skin-palette",displayName="My Skin Palette",version="1.0.0";
  public int templateId=38247;
  public SkinPaletteEntry[] tones=new SkinPaletteEntry[0];
 }
 [Serializable] public sealed class SkinPaletteEntry
 {
  public string sku="yourname.skin-palette.tone",displayName="My Skin Tone";
  [ColorUsage(false,false)] public Color shadows=new Color(.70f,.48f,.38f);
  [ColorUsage(false,false)] public Color midtones=new Color(.82f,.62f,.48f);
  [ColorUsage(false,false)] public Color highlights=new Color(.90f,.74f,.60f);
 }
 public static class SkinPaletteTools
 {
  [MenuItem("Cosmetics API/Create Skin Tone Palettes")]
  public static void CreateSamples()
  {
   foreach(var name in new[]{"Natural","Fantasy"})
   {
    string folder="Assets/CosmeticsAPI/Samples/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();string path=folder+name+" Skin Palette.asset";
    var existing=AssetDatabase.LoadAssetAtPath<SkinPaletteAsset>(path);if(existing!=null){Selection.activeObject=existing;continue;}
    var manifest=JsonUtility.FromJson<PackManifest>(File.ReadAllText("Assets/CosmeticsAPI/Editor/"+name+"SkinPalette.json"));ManifestRules.Validate(manifest);
    var palette=ScriptableObject.CreateInstance<SkinPaletteAsset>();palette.packId=manifest.packId;palette.displayName=manifest.displayName;palette.version=manifest.version;
    palette.tones=manifest.cosmetics.Select(c=>new SkinPaletteEntry{sku=c.sku,displayName=c.displayName,shadows=Parse(c.skinTone.shadows),midtones=Parse(c.skinTone.midtones),highlights=Parse(c.skinTone.highlights)}).ToArray();
    AssetDatabase.CreateAsset(palette,path);Selection.activeObject=palette;
   }
   AssetDatabase.SaveAssets();
  }
  static Color Parse(string hex){Color color;if(!ColorUtility.TryParseHtmlString(hex,out color))throw new InvalidDataException("Invalid palette colour");return color;}
  public static void Export(SkinPaletteAsset palette,string output)
  {
   if(palette==null||palette.tones==null||palette.tones.Any(t=>t==null))throw new InvalidDataException("Palette needs valid tone entries");
   var pack=ScriptableObject.CreateInstance<CosmeticPackAsset>();
   try
   {
    pack.packId=palette.packId;pack.displayName=palette.displayName;pack.version=palette.version;
    pack.items=palette.tones.Select(t=>new AuthoringItem{sku=t.sku,displayName=t.displayName,templateId=palette.templateId,skinTone=true,skinShadows=t.shadows,skinMidtones=t.midtones,skinHighlights=t.highlights}).ToArray();
    CreatorExporter.Export(pack,output);
   }
   finally{Object.DestroyImmediate(pack);}
  }
 }
 [CustomEditor(typeof(SkinPaletteAsset))]
 public sealed class SkinPaletteInspector:Editor
 {
  public override void OnInspectorGUI()
  {
   EditorGUILayout.HelpBox("Native full-body skin tones. Edit shadow, midtone and highlight colours; these are shading stops, not transparency. Template 38247 is ATT Skin 01. Requires CosmeticsAPI 0.2.0 on client and server.",MessageType.Info);
   DrawDefaultInspector();
   if(GUILayout.Button("Export Client + Server Skin Palette"))
   {
    var palette=(SkinPaletteAsset)target;var output=EditorUtility.SaveFolderPanel("Export skin palette","Builds/CosmeticsAPI",palette.packId);
    if(!string.IsNullOrEmpty(output)){SkinPaletteTools.Export(palette,output);EditorUtility.RevealInFinder(output);}
   }
   EditorGUILayout.Space();EditorGUILayout.LabelField("Palette preview",EditorStyles.boldLabel);
   var p=(SkinPaletteAsset)target;if(p.tones==null)return;
   foreach(var tone in p.tones.Where(t=>t!=null))
   {
    var rect=GUILayoutUtility.GetRect(100,22);var bar=new Rect(rect.x,rect.y,90,18);
    for(int x=0;x<90;x++){float t=x/89f;EditorGUI.DrawRect(new Rect(bar.x+x,bar.y,1,bar.height),t<.5f?Color.Lerp(tone.shadows,tone.midtones,t*2):Color.Lerp(tone.midtones,tone.highlights,(t-.5f)*2));}
    EditorGUI.LabelField(new Rect(rect.x+100,rect.y,rect.width-100,22),tone.displayName);
   }
  }
 }
}
