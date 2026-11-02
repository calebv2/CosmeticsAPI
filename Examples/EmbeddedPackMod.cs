// Optional C# integration. Most creators only need the Unity exporter and pack folder.
// Embed the exported client pack.json as MyPack.Manifest and assets.bundle as MyPack.Assets.
// Server builds embed the identical manifest; their asset loader is never invoked.
using ATT.Cosmetics;
using MelonLoader;
[assembly:MelonInfo(typeof(MyCosmeticPack),"MyCosmeticPack","1.0.0","YourName")]
[assembly:MelonGame("Alta","A Township Tale")]
[assembly:MelonAdditionalDependencies("CosmeticsAPI")]
public sealed class MyCosmeticPack:MelonMod
{
 PackHandle pack;
 public override void OnInitializeMelon()
 {
  pack=CosmeticsApi.RegisterEmbedded(typeof(MyCosmeticPack).Assembly,"MyPack.Manifest","MyPack.Assets");
 }
 // PackHandle.Status becomes Ready after native templates and the catalog load.
 // A failed pack exposes PackHandle.Error and prints COSMETICS_PACK_FAILED.
 public PackHandle Registration=>pack;
}
