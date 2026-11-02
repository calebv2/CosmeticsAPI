using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace ATT.Cosmetics.Creator
{
 public static class CreatorSamples
 {
  const string Folder="Assets/CosmeticsAPI/Samples/";
  [MenuItem("Cosmetics API/Create Reference Rig")]
  public static void CreateRig()
  {
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var data=CreatorExporter.Rig();var root=new GameObject("ATT authoring rig (Y up, Z forward)");
   var swap=Matrix4x4.zero;swap.m00=1;swap.m12=1;swap.m21=1;swap.m33=1;
   var binds=data.bones.Select(b=>swap*CreatorExporter.Matrix(b.bindpose)*swap).ToArray();
   var bones=data.bones.Select(b=>new GameObject(b.name).transform).ToArray();
   for(int i=0;i<bones.Length;i++)
   {
    int parent=data.bones[i].parent;bones[i].SetParent(parent<0?root.transform:bones[parent],false);var local=parent<0?binds[i].inverse:binds[parent]*binds[i].inverse;
    bones[i].localPosition=local.GetColumn(3);bones[i].localRotation=local.rotation;bones[i].localScale=local.lossyScale;
   }
   var saved=PrefabUtility.SaveAsPrefabAsset(root,Folder+"ATT Reference Rig.prefab");Object.DestroyImmediate(root);Selection.activeObject=saved;
   Debug.Log("Created a bone-only reference rig. Bind your mesh to these rest-pose bones; no game meshes are included.");
  }
  [MenuItem("Cosmetics API/Create Starter Accessory Pack")]
  public static void CreateStarter()
  {
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();CreateRig();
   string configPath=Folder+"Starter Pack.asset";if(AssetDatabase.LoadAssetAtPath<CosmeticPackAsset>(configPath)!=null){Selection.activeObject=AssetDatabase.LoadAssetAtPath<CosmeticPackAsset>(configPath);return;}
   var root=new GameObject("Starter brooch - authoring coordinates");
   var brooch=GameObject.CreatePrimitive(PrimitiveType.Sphere);brooch.name="Copper oval brooch";brooch.transform.SetParent(root.transform,false);brooch.transform.localPosition=new Vector3(0,1.34f,.155f);brooch.transform.localScale=new Vector3(.07f,.09f,.016f);Object.DestroyImmediate(brooch.GetComponent<Collider>());
   var mesh=Object.Instantiate(brooch.GetComponent<MeshFilter>().sharedMesh);mesh.name="Starter brooch";AssetDatabase.CreateAsset(mesh,Folder+"Starter Brooch Mesh.asset");brooch.GetComponent<MeshFilter>().sharedMesh=mesh;
   var material=new Material(Shader.Find("CosmeticsAPI/Creator Lit")){color=new Color(.62f,.27f,.11f)};material.SetFloat("_Metallic",.6f);AssetDatabase.CreateAsset(material,Folder+"Starter Copper.mat");brooch.GetComponent<Renderer>().sharedMaterial=material;
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"Starter Brooch.prefab");Object.DestroyImmediate(root);
   var pack=ScriptableObject.CreateInstance<CosmeticPackAsset>();pack.packId="example.creator-brooch";pack.displayName="Creator Starter Brooch";pack.version="1.0.0";
   pack.items=new[]{new AuthoringItem{sku="example.creator-brooch.oval-1",displayName="Creator Test Brooch",templateId=56783,prefab=prefab,geometry=GeometryMode.Rigid,rigidBone="chest_jnt"}};
   AssetDatabase.CreateAsset(pack,configPath);AssetDatabase.SaveAssets();Selection.activeObject=pack;
  }
 }
 [CustomEditor(typeof(CosmeticPackAsset))]
 public sealed class CosmeticPackInspector:Editor
 {
  public override void OnInspectorGUI()
  {
   EditorGUILayout.HelpBox("Pick a native template for the wardrobe category. Use a permanent SKU. Skinned clothing must use the ATT reference bind pose. Rigid accessories only need a target bone and placement on that rig.",MessageType.Info);
   DrawDefaultInspector();EditorGUILayout.Space();
   if(GUILayout.Button("Export Client + Server Pack")){Selection.activeObject=target;CreatorExporter.ExportSelected();}
  }
 }
}

