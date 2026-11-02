using System;
using System.IO;
using System.Linq;
using Alta.Customization;
using UnityEngine;
using Object=UnityEngine.Object;
namespace ATT.Cosmetics
{
 internal static class SkinToneRuntime
 {
  internal static CustomizationVisual Build(Entry entry,PlayerCosmeticDefinition donor,ICustomizationVisual original)
  {
   // Clone the native skin visual to preserve its primary slot, zero-bit
   // arguments, shader/blit path and normal native pickup behaviour.
   var originalVisual=original as CustomizationVisual;
   if(originalVisual==null||original.Parts.Length!=1||!(original.Parts[0] is CosmeticSkinColor)||!original.Parts[0].Slots.Contains((int)PlayerCustomizationSlot.SkinColor))throw new InvalidDataException("Skin tone template must be a native skin-colour cosmetic: "+entry.Spec.templateId);
   if(!(Registry.Field(donor.GetType(),"pickup").GetValue(donor) is SkinColorPickup))throw new InvalidDataException("Skin tone template lacks the native skin swatch pickup");
   var originalPart=(CosmeticSkinColor)original.Parts[0];
   if(originalPart.CreateDefaultArguments().Size!=0)throw new InvalidDataException("Unsupported native skin argument format");
   entry.Template=Object.Instantiate(originalVisual.gameObject);entry.Template.name=entry.Name+" template";entry.Template.SetActive(false);Object.DontDestroyOnLoad(entry.Template);
   var visual=entry.Template.GetComponent<CustomizationVisual>();var part=visual.Parts.Single() as CosmeticSkinColor;
   if(part==null)throw new InvalidDataException("Native skin part did not survive cloning");
   var color=ScriptableObject.CreateInstance<CosmeticColor>();entry.SkinColor=color;color.name=entry.Sku+" ramp";color.hideFlags=HideFlags.DontSave;Object.DontDestroyOnLoad(color);
   // These private colours are rendered locally from the saved cosmetic SKU.
   // They are not added to the global clothing-dye registry or serialized as dye IDs.
   Registry.Set(color,"color0",Parse(entry.Spec.skinTone.shadows,0));
   Registry.Set(color,"color1",Parse(entry.Spec.skinTone.midtones,.5f));
   Registry.Set(color,"color2",Parse(entry.Spec.skinTone.highlights,1));
   Registry.Set(part,"skinColor",color);Registry.Set(part,"defaultArguments",originalPart.CreateDefaultArguments().Clone());
   return visual;
  }
  static Color Parse(string hex,float stop)
  {Color color;if(!ColorUtility.TryParseHtmlString(hex,out color))throw new InvalidDataException("Invalid skin colour "+hex);color.a=stop;return color;}
 }
}
