using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace ATT.Cosmetics
{
 internal static class PickupDisplay
 {
  // The native Cosmetic Pickup prefab uses a 0.175m display box around its
  // existing 0.075m-radius grab trigger. Display geometry adds no colliders.
  internal const float MaximumSize=.175f;
  static readonly Quaternion NativeRotation=Quaternion.Euler(-90,0,0);
  internal static GameObject Build(GameObject pickup,GameObject model,bool emptyVisual,Material tokenMaterial=null)
  {
   if(pickup==null)throw new ArgumentNullException(nameof(pickup));
   GameObject display;
   if(emptyVisual)display=TransparentShirt(pickup,tokenMaterial);
   else
   {
    if(model==null)throw new ArgumentNullException(nameof(model));
    display=Object.Instantiate(model,pickup.transform,false);display.name="Cosmetic display";
    var bounds=LocalMeshBounds(display);
    float longest=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
    if(longest<.00001f){Object.Destroy(display);throw new InvalidOperationException("Cosmetic display has empty bounds");}
    float scale=MaximumSize/longest;
    display.transform.localRotation=NativeRotation;
    display.transform.localScale=Vector3.one*scale;
    display.transform.localPosition=NativeRotation*(-bounds.center*scale);
   }
   foreach(var transform in display.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=pickup.layer;
   foreach(var renderer in display.GetComponentsInChildren<Renderer>(true))
   {renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
   // Exported model packs are collider-free. Defensively disable any display
   // collider without changing the pickup's native grab/hover interaction.
   foreach(var collider in display.GetComponentsInChildren<Collider>(true)){collider.enabled=false;Object.Destroy(collider);}
   return display;
  }
  static Bounds LocalMeshBounds(GameObject root)
  {
   bool first=true;Bounds result=default(Bounds);
   foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
   {
    if(filter.sharedMesh==null)continue;
    var b=filter.sharedMesh.bounds;var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
    for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
    {
     var point=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
     if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);
    }
   }
   return result;
  }
  static GameObject TransparentShirt(GameObject pickup,Material source)
  {
   if(source==null)throw new ArgumentNullException(nameof(source),"Empty cosmetic displays need the native pickup material");
   var root=new GameObject("Transparent shirt selection");root.transform.SetParent(pickup.transform,false);
   var owned=root.AddComponent<PickupDisplayResources>();
   var pale=Tint(source,new Color(.72f,.77f,.78f));var dark=Tint(source,new Color(.27f,.33f,.35f));var edge=Tint(source,new Color(.10f,.15f,.17f));
   owned.Materials=new[]{pale,dark,edge};
   var vertices=new List<Vector3>();var indices=new[]{new List<int>(),new List<int>(),new List<int>()};
   // A checkerboard is the familiar transparency cue. The shirt silhouette is
   // drawn on both sides so the selection remains identifiable when held.
   for(int x=0;x<4;x++)for(int y=0;y<4;y++)
   {
    float x0=-.068f+x*.034f,y0=-.068f+y*.034f;
    Quad(vertices,indices[(x+y)&1],new Vector3(x0,y0,-.002f),new Vector3(x0,y0+.034f,-.002f),new Vector3(x0+.034f,y0+.034f,-.002f),new Vector3(x0+.034f,y0,-.002f));
    Quad(vertices,indices[(x+y)&1],new Vector3(x0,y0,.002f),new Vector3(x0+.034f,y0,.002f),new Vector3(x0+.034f,y0+.034f,.002f),new Vector3(x0,y0+.034f,.002f));
   }
   var outline=new[]{new Vector2(-.023f,.050f),new Vector2(-.012f,.050f),new Vector2(-.008f,.039f),new Vector2(.008f,.039f),new Vector2(.012f,.050f),new Vector2(.023f,.050f),new Vector2(.053f,.025f),new Vector2(.038f,.008f),new Vector2(.027f,.018f),new Vector2(.027f,-.049f),new Vector2(-.027f,-.049f),new Vector2(-.027f,.018f),new Vector2(-.038f,.008f),new Vector2(-.053f,.025f)};
   for(int i=0;i<outline.Length;i++)for(int side=-1;side<=1;side+=2)
   {
    var a=outline[i];var b=outline[(i+1)%outline.Length];var direction=(b-a).normalized;var n=new Vector2(-direction.y,direction.x)*.0025f;
    if(side<0)Quad(vertices,indices[2],new Vector3(a.x+n.x,a.y+n.y,-.003f),new Vector3(b.x+n.x,b.y+n.y,-.003f),new Vector3(b.x-n.x,b.y-n.y,-.003f),new Vector3(a.x-n.x,a.y-n.y,-.003f));
    else Quad(vertices,indices[2],new Vector3(a.x-n.x,a.y-n.y,.003f),new Vector3(b.x-n.x,b.y-n.y,.003f),new Vector3(b.x+n.x,b.y+n.y,.003f),new Vector3(a.x+n.x,a.y+n.y,.003f));
   }
   var mesh=new Mesh{name="Transparent shirt icon"};mesh.SetVertices(vertices);mesh.subMeshCount=3;
   for(int i=0;i<3;i++)mesh.SetTriangles(indices[i],i);
   mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Meshes=new[]{mesh};
   root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=owned.Materials;
   return root;
  }
  static void Quad(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
  {int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);indices.Add(start);indices.Add(start+1);indices.Add(start+2);indices.Add(start);indices.Add(start+2);indices.Add(start+3);}
  static Material Tint(Material source,Color color)
  {
   var material=new Material(source){name="Cosmetic display swatch",hideFlags=HideFlags.DontSave,enableInstancing=true};
   if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",null);
   if(material.HasProperty("_BumpMap"))material.SetTexture("_BumpMap",null);
   if(material.HasProperty("_Color"))material.SetColor("_Color",color);
   if(material.HasProperty("_Mode"))material.SetFloat("_Mode",0);
   if(material.HasProperty("_SrcBlend"))material.SetFloat("_SrcBlend",(float)BlendMode.One);
   if(material.HasProperty("_DstBlend"))material.SetFloat("_DstBlend",(float)BlendMode.Zero);
   if(material.HasProperty("_ZWrite"))material.SetFloat("_ZWrite",1);
   material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.SetOverrideTag("RenderType","Opaque");material.renderQueue=(int)RenderQueue.Geometry;
   foreach(var prefix in new[]{"_ColorA","_ColorB"})for(int i=1;i<=3;i++)if(material.HasProperty(prefix+i))
   {var shade=color*Mathf.Lerp(.7f,1.15f,(i-1)*.5f);shade.a=(i-1)*.5f;material.SetColor(prefix+i,shade);}
   return material;
  }
 }
 [ExecuteAlways]
 public sealed class PickupDisplayResources:MonoBehaviour
 {
  internal Material[] Materials;internal Mesh[] Meshes;
  void OnDestroy()
  {if(Materials!=null)foreach(var material in Materials)if(material!=null)Release(material);if(Meshes!=null)foreach(var mesh in Meshes)if(mesh!=null)Release(mesh);}
  static void Release(Object asset){if(Application.isPlaying)Destroy(asset);else DestroyImmediate(asset);}
 }
}
