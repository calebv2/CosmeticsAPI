using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace ATT.Cosmetics.Creator
{
 public static class CreatorExporter
 {
  const string Generated="Assets/CosmeticsAPI/Generated/";
  [Serializable] internal class RigData {public RigBone[] bones;}
  [Serializable] internal class RigBone {public string name;public int parent;public float[] bindpose;}
  internal static RigData Rig()=>JsonUtility.FromJson<RigData>(File.ReadAllText("Assets/CosmeticsAPI/Editor/NativeRig.json"));
  internal static Matrix4x4 Matrix(float[] a){var m=new Matrix4x4();for(int r=0;r<4;r++)for(int c=0;c<4;c++)m[r,c]=a[r*4+c];return m;}
  static Matrix4x4 Swap {get{var m=Matrix4x4.zero;m.m00=1;m.m12=1;m.m21=1;m.m33=1;return m;}}
  [MenuItem("Cosmetics API/Export Selected Pack")]
  public static void ExportSelected()
  {
   var pack=Selection.activeObject as CosmeticPackAsset;if(pack==null)throw new InvalidOperationException("Select a Cosmetic Pack asset in the Project window first");
   var output=EditorUtility.SaveFolderPanel("Export client and server pack folders","Builds/CosmeticsAPI",pack.packId);if(string.IsNullOrEmpty(output))return;
   Export(pack,output);EditorUtility.RevealInFinder(output);
  }
  public static void Export(CosmeticPackAsset asset,string output)
  {
   if(Application.unityVersion!="2020.3.23f1")throw new InvalidOperationException("Use Unity 2020.3.23f1, matching this ATT client");
   if(UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset!=null)throw new InvalidOperationException("Use a Built-in 3D project. This ATT exporter does not support URP/HDRP materials.");
   if(asset==null||asset.items==null||asset.items.Any(i=>i==null))throw new InvalidOperationException("Invalid pack entries");
   var manifest=new PackManifest{schemaVersion=asset.items.Any(i=>i.skinTone)?2:1,packId=asset.packId,displayName=asset.displayName,version=asset.version,bundleSha256=new string('0',64),cosmetics=asset.items.Select((i,n)=>new CosmeticDefinition{sku=i.sku,displayName=i.displayName,templateId=(uint)i.templateId,expectedId=CosmeticIds.FromSku(i.sku),asset=i.emptyVisual||i.skinTone?null:"item-"+n,emptyVisual=i.emptyVisual,hideInFirstPerson=i.hideInFirstPerson,isSkinTone=i.skinTone,skinTone=i.skinTone?new SkinToneDefinition{shadows=Hex(i.skinShadows),midtones=Hex(i.skinMidtones),highlights=Hex(i.skinHighlights)}:null}).ToArray()};
   ManifestRules.Validate(manifest);
   var rig=Rig();if(rig.bones.Length!=141||rig.bones.Select(b=>b.name).Distinct().Count()!=141)throw new InvalidDataException("Invalid SDK rig metadata");
   var nativeBinds=rig.bones.Select(b=>Matrix(b.bindpose)).ToArray();
   string generated=Generated+manifest.packId+"/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
   var paths=new List<string>();var keys=new List<string>();var report=new List<string>();
   for(int n=0;n<asset.items.Length;n++)
   {
    var item=asset.items[n];if(item.emptyVisual||item.skinTone){if(item.prefab!=null)throw new InvalidOperationException("Empty/skin-tone entries must not have a prefab: "+item.displayName);report.Add(item.displayName+": "+(item.skinTone?"native skin tone":"empty visual")+"; ID "+manifest.cosmetics[n].expectedId);continue;}
    if(item.prefab==null)throw new InvalidOperationException("Assign a prefab for "+item.displayName);
    if(item.prefab.transform.localPosition.sqrMagnitude>.000001f||Quaternion.Angle(item.prefab.transform.localRotation,Quaternion.identity)>.01f||(item.prefab.transform.localScale-Vector3.one).sqrMagnitude>.000001f)throw new InvalidOperationException("Prefab root must have position/rotation zero and scale one; place geometry beneath that root: "+item.displayName);
    var result=new GameObject(item.displayName);int count=0;
    try
    {
     var sources=item.geometry==GeometryMode.Skinned?item.prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Cast<Renderer>().ToArray():item.prefab.GetComponentsInChildren<MeshRenderer>().Cast<Renderer>().ToArray();
     if(sources.Length==0)throw new InvalidOperationException("No "+item.geometry+" renderers in "+item.displayName);
     foreach(var renderer in sources)
     {
      var skin=renderer as SkinnedMeshRenderer;var filter=renderer.GetComponent<MeshFilter>();var source=skin!=null?skin.sharedMesh:filter==null?null:filter.sharedMesh;
      if(source==null||source.vertexCount==0)throw new InvalidOperationException("Empty mesh on "+renderer.name);
      if(source.blendShapeCount!=0)throw new InvalidOperationException("Bake blend shapes before exporting: "+renderer.name);
      var toPack=item.prefab.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
      var geometry=Swap*toPack;var normalMatrix=geometry.inverse.transpose;
      var mesh=Object.Instantiate(source);mesh.name=item.displayName+" mesh "+count;
      mesh.vertices=source.vertices.Select(v=>geometry.MultiplyPoint3x4(v)).ToArray();mesh.normals=source.normals.Select(v=>normalMatrix.MultiplyVector(v).normalized).ToArray();
      float sign=geometry.determinant<0?-1:1;
      mesh.tangents=source.tangents.Select(t=>{var v=geometry.MultiplyVector(t).normalized;return new Vector4(v.x,v.y,v.z,t.w*sign);}).ToArray();
      if(sign<0)for(int s=0;s<mesh.subMeshCount;s++){var t=source.GetTriangles(s);for(int k=0;k<t.Length;k+=3){int a=t[k+1];t[k+1]=t[k+2];t[k+2]=a;}mesh.SetTriangles(t,s);}
      if(skin!=null)
      {
       if(skin.bones.Length!=source.bindposes.Length||source.boneWeights.Length!=source.vertexCount)throw new InvalidOperationException("Mesh needs valid skin weights: "+renderer.name);
       var boneMap=skin.bones.Select(b=>b==null?-1:Array.FindIndex(rig.bones,x=>x.name==b.name)).ToArray();
       for(int k=0;k<boneMap.Length;k++)
       {
        if(boneMap[k]<0)throw new InvalidOperationException("Unknown bone; use the SDK reference rig: "+renderer.name);
        var converted=Swap*source.bindposes[k]*toPack.inverse*Swap;float delta=0;
        for(int r=0;r<4;r++)for(int c=0;c<4;c++)delta=Mathf.Max(delta,Mathf.Abs(converted[r,c]-nativeBinds[boneMap[k]][r,c]));
        if(delta>.002f)throw new InvalidOperationException("Bind pose differs from ATT by "+delta+" on "+skin.bones[k].name+". Bind the garment to the SDK reference rig; moving the renderer alone does not rebind it.");
       }
       mesh.boneWeights=source.boneWeights.Select(w=>new BoneWeight{boneIndex0=Map(boneMap,w.boneIndex0),boneIndex1=Map(boneMap,w.boneIndex1),boneIndex2=Map(boneMap,w.boneIndex2),boneIndex3=Map(boneMap,w.boneIndex3),weight0=w.weight0,weight1=w.weight1,weight2=w.weight2,weight3=w.weight3}).ToArray();
      }
      else
      {
       int bone=Array.FindIndex(rig.bones,b=>b.name==item.rigidBone);if(bone<0)throw new InvalidOperationException("Unknown rigid attachment bone: "+item.rigidBone);
       mesh.boneWeights=Enumerable.Repeat(new BoneWeight{boneIndex0=bone,weight0=1},mesh.vertexCount).ToArray();
      }
      mesh.bindposes=nativeBinds;if(mesh.colors.Length==0)mesh.colors=Enumerable.Repeat(Color.white,mesh.vertexCount).ToArray();if(mesh.normals.Length==0)mesh.RecalculateNormals();mesh.RecalculateBounds();ValidateMesh(mesh);
      var materials=renderer.sharedMaterials;if(materials.Length!=mesh.subMeshCount||materials.Any(m=>m==null||m.shader==null||!m.shader.isSupported))throw new InvalidOperationException("Missing/unsupported material on "+renderer.name);
      foreach(var material in materials)if(material.shader.name!="Standard"&&!material.shader.name.StartsWith("CosmeticsAPI/")&&!material.shader.name.StartsWith("Cosmetics/"))Debug.LogWarning("Custom shader requires a both-eye headset check: "+material.shader.name);
      var path=generated+"item-"+n+"-"+count+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,path);
      var child=new GameObject(renderer.name);child.transform.SetParent(result.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterials=materials;count++;
     }
     var prefab=generated+"item-"+n+".prefab";PrefabUtility.SaveAsPrefabAsset(result,prefab);paths.Add(prefab);keys.Add("item-"+n);report.Add(item.displayName+": "+count+" meshes; ID "+manifest.cosmetics[n].expectedId);
    }
    finally{Object.DestroyImmediate(result);}
   }
   string client=Path.Combine(output,"Client","UserData","CosmeticsAPI","Packs",manifest.packId),server=Path.Combine(output,"Server","UserData","CosmeticsAPI","Packs",manifest.packId);
   Directory.CreateDirectory(client);Directory.CreateDirectory(server);
   if(paths.Count>0)
   {
    File.WriteAllLines(generated+"bones.txt",rig.bones.Select(b=>b.name));AssetDatabase.ImportAsset(generated+"bones.txt");paths.Add(generated+"bones.txt");keys.Add("bones");AssetDatabase.SaveAssets();
    // Both settings matter even though the authoring project runs without a headset.
    PlayerSettings.virtualRealitySupported=true;PlayerSettings.stereoRenderingPath=StereoRenderingPath.Instancing;PlayerSettings.stripUnusedMeshComponents=false;
    string temp=Path.Combine(output,"BuildCache");Directory.CreateDirectory(temp);
    var build=BuildPipeline.BuildAssetBundles(temp,new[]{new AssetBundleBuild{assetBundleName=manifest.packId+".bundle",assetNames=paths.ToArray(),addressableNames=keys.ToArray()}},BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.StrictMode|BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.StandaloneWindows64);
    if(build==null)throw new InvalidOperationException("Unity bundle build failed");
    var bytes=File.ReadAllBytes(Path.Combine(temp,manifest.packId+".bundle"));var bundle=AssetBundle.LoadFromMemory(bytes);if(bundle==null)throw new InvalidDataException("Exported bundle cannot reload");
    try{foreach(var key in keys.Where(k=>k!="bones")){var prefab=bundle.LoadAsset<GameObject>(key);if(prefab==null)throw new InvalidDataException("Missing exported prefab "+key);foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())ValidateMesh(filter.sharedMesh);}}
    finally{bundle.Unload(true);}
    manifest.bundleSha256=ManifestRules.Sha256(bytes);File.WriteAllBytes(Path.Combine(client,manifest.bundleFile),bytes);
   }
   else{manifest.bundleFile=null;manifest.bundleSha256=null;}
   ManifestRules.Validate(manifest);var json=JsonUtility.ToJson(manifest,true);File.WriteAllText(Path.Combine(client,"pack.json"),json);File.WriteAllText(Path.Combine(server,"pack.json"),json);
   File.WriteAllLines(Path.Combine(output,"Export-Report.txt"),report.Concat(new[]{paths.Count>0?"PASS: exported bundle reloads with skin weights intact. Install CosmeticsAPI on client/server and the matching pack folders. Custom shader behavior still needs a headset test.":"PASS: native-only pack exported with matching client/server manifests; no model bundle is required. Install CosmeticsAPI 0.2.0 for skin tones. Live game swatch/application/save tests remain."}));
   Debug.Log("COSMETICS_API_EXPORT_PASS "+manifest.packId+" -> "+output);
  }
  internal static string Hex(Color color)
  {
   foreach(float v in new[]{color.r,color.g,color.b})if(float.IsNaN(v)||float.IsInfinity(v)||v<0||v>1)throw new InvalidDataException("Skin colours must use finite RGB values between 0 and 1");
   return "#"+ColorUtility.ToHtmlStringRGB(color);
  }
  static int Map(int[] map,int index){if(index<0||index>=map.Length)throw new InvalidOperationException("Skin weight references an invalid bone");return map[index];}
  internal static void ValidateMesh(Mesh mesh)
  {
   if(mesh==null||mesh.bindposes.Length!=141||mesh.boneWeights.Length!=mesh.vertexCount)throw new InvalidDataException("Skin channels were stripped from the bundle");
   foreach(var w in mesh.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f||w.boneIndex0<0||w.boneIndex0>=141||w.boneIndex1<0||w.boneIndex1>=141||w.boneIndex2<0||w.boneIndex2>=141||w.boneIndex3<0||w.boneIndex3>=141)throw new InvalidDataException("Invalid skin weights on "+mesh.name);
  }
 }
}
