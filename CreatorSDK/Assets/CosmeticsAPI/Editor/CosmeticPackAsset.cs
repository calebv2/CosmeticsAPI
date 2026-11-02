using System;
using UnityEngine;
namespace ATT.Cosmetics.Creator
{
 public enum GeometryMode {Skinned,Rigid}
 [CreateAssetMenu(fileName="Cosmetic Pack",menuName="Cosmetics API/Cosmetic Pack")]
 public sealed class CosmeticPackAsset:ScriptableObject
 {
  public string packId="yourname.yourpack";
  public string displayName="My Cosmetic Pack";
  public string version="1.0.0";
  public AuthoringItem[] items=new AuthoringItem[0];
 }
 [Serializable] public sealed class AuthoringItem
 {
  [Tooltip("Permanent saved identity, e.g. yourname.yourpack.shirt. Do not change after release.")]
  public string sku="yourname.yourpack.shirt";
  public string displayName="My Shirt";
  [Tooltip("Native cosmetic whose category/slots to inherit. Shirt: 28346; harness/belt: 56783; skirt: 15368.")]
  public int templateId=28346;
  public GameObject prefab;
  public GeometryMode geometry=GeometryMode.Skinned;
  [Tooltip("For rigid accessories, bind the whole model to this native bone. Position it on the reference rig first.")]
  public string rigidBone="chest_jnt";
  [Tooltip("An intentionally invisible cosmetic, such as a clear shirt. Leave prefab empty.")]
  public bool emptyVisual;
  [Tooltip("Enable for hats/face accessories to keep them out of the wearer's eyes.")]
  public bool hideInFirstPerson;
  [Tooltip("Use ATT's native skin-colour system. No model is needed; template 38247 is Skin 01.")]
  public bool skinTone;
  [ColorUsage(false,false)] public Color skinShadows=new Color(.70f,.48f,.38f);
  [ColorUsage(false,false)] public Color skinMidtones=new Color(.82f,.62f,.48f);
  [ColorUsage(false,false)] public Color skinHighlights=new Color(.90f,.74f,.60f);
 }
}
