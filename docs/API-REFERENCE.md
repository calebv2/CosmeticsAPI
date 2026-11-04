# C# API

Namespace: `ATT.Cosmetics`. Assembly: `CosmeticsAPI.dll`. Reference the client or server build appropriate to your mod. Register on the Unity main thread, normally from your Melon mod's `OnInitializeMelon`. Declare `[assembly: MelonAdditionalDependencies("CosmeticsAPI")]`.

From 0.2.2, the runtime DLLs create the bundled Wayfarer and skin-palette files automatically. Folder discovery runs during initialization and later startup hooks, with duplicate prevention. Register additional creations with unique Pack IDs/SKUs; do not register the bundled sample IDs a second time. Existing edited pack files are preserved by the bundled installer.

The zero-code folder workflow is preferred for ordinary content packs. Use C# registration when your existing mod should carry the manifest and graphics as embedded resources. The API does not require an extra per-pack mod DLL.

```csharp
PackHandle registration = CosmeticsApi.RegisterEmbedded(
    typeof(MyCosmeticPack).Assembly,
    "MyPack.Manifest",
    "MyPack.Assets");
```

Embed the exporter-produced `pack.json` under `MyPack.Manifest` and `assets.bundle` under `MyPack.Assets`. The server can omit the graphics resource; the server never calls the asset loader. See `Examples/EmbeddedPackMod.cs` for a complete minimal Melon class.

## Methods

| Method | Purpose |
|---|---|
| `CosmeticsApi.RegisterFolder(string folder)` | Read a folder's `pack.json` and queue its local bundle. Startup discovery already calls this for folders beneath `UserData/CosmeticsAPI/Packs`. |
| `CosmeticsApi.RegisterEmbedded(Assembly assembly, string manifestResource, string bundleResource)` | Queue an embedded pack. Client bundle bytes are loaded lazily after native templates are ready. |
| `CosmeticsApi.RegisterPack(PackManifest manifest, Func<byte[]> clientBundleLoader)` | Lower-level registration; the API deep-copies the manifest. The callback must return the bundle whose SHA-256 is declared in the manifest. It is never called by the server. |
| `CosmeticsApi.GetPacks()` | Snapshot of registration handles for diagnostics. |
| `CosmeticIds.FromSku(string sku)` | Compute ATT's stable 16-bit ID. A hash may collide, so a computed value is not a reservation or proof of availability. |
| `ManifestRules.Validate(PackManifest manifest)` | Validate the schema, identities, filenames, and declared assets before queueing. |

`PackHandle` exposes `PackId`, `Version`, `Status` (`Queued`, `Ready`, or `Failed`) and `Error`. Queuing does not mean native registration has finished. The API waits for the catalog and donor definitions, then commits a whole pack. Failed registrations are not hot-retried; correct the files and restart.

## Manifest schemas 1 and 2

`PackManifest` contains `schemaVersion`, `packId`, `displayName`, `version`, `bundleFile`, `bundleSha256`, and `cosmetics`.

Each `CosmeticDefinition` contains:

- `sku`: permanent lowercase identity string.
- `displayName`: wardrobe/pickup label.
- `templateId`: native cosmetic donor ID for category and dressing slots.
- `asset`: address of the exported model prefab in the bundle.
- `expectedId`: optional pinned hash; the exporter fills this to detect accidental SKU changes.
- `emptyVisual`: true for an intentionally invisible selection; no model asset is allowed.
- `hideInFirstPerson`: hide from the wearer, suitable for head/face accessories.
- `isSkinTone`: true for a native skin-colour selection; requires schema 2 and API 0.2.0.
- `skinTone`: a `SkinToneDefinition` containing `shadows`, `midtones`, and `highlights` as `#RRGGBB` strings. Leave absent for clothing.

For skin entries, use template 38247 (Skin 01), no model asset, `emptyVisual=false`, and `hideInFirstPerson=false`. The API clones the native `CosmeticSkinColor` visual and supplies a private `CosmeticColor` ramp. Native swatch rendering, slot 311, body/hand texture packing, and zero-size customization arguments remain in use. Each tone is saved/networked using its cosmetic ID. These private ramps are not added to the native clothing-dye registry.

Set `isSkinTone=true` explicitly. Unity's JSON loader can create an empty nested `skinTone` object even when the field is absent; empty objects without this flag are treated as regular clothing. Supplying actual colour values without the flag is rejected.

```csharp
var palette = new PackManifest {
    schemaVersion = 2,
    packId = "yourname.skin", displayName = "My Skin Tones", version = "1.0.0",
    cosmetics = new[] { new CosmeticDefinition {
        sku = "yourname.skin.olive", displayName = "Olive",
        templateId = 38247, isSkinTone = true,
        skinTone = new SkinToneDefinition {
            shadows = "#6B6048", midtones = "#857456", highlights = "#A49270"
        }
    }}
};
PackHandle handle = CosmeticsApi.RegisterPack(palette, null);
```

This example SKU still needs collision validation against your installation. Skin-only packs may pass a null bundle loader; neither client nor server calls it. Mixed packs still need a valid model bundle on clients. `ManifestRules.NeedsModel(item)` exposes that decision. Schema-1 clothing behavior remains supported.

The SDK creates all bundle addresses and hashes. For visible items, bundles must include a `bones` TextAsset containing the ordered 141 native bone names and asset-only prefabs with identity transforms, MeshFilters, MeshRenderers, valid materials, and complete native skin weights/bind poses. No behaviour scripts are instantiated from content bundles.

## Registration behavior

IDs are reserved across queued packs before loading. Native catalog/prefab IDs are checked before commit. A collision is an error, never an instruction to overwrite another item or generate a new runtime ID. Per-pack Addressables provider IDs are unique. A failed commit rolls back only that pack's catalog changes; previously ready packs remain registered.

Native categories are resolved using the template's catalog membership and category type, not Unity object identity. Availability/ownership patches apply only to successfully registered custom IDs. Native cosmetic ownership is left to the game.

The initial API has no hot-unload, pack download, multiplayer version handshake, stat system, native colour-brush binding, or general body-fitting service. The server and every viewing client must install matching manifests/assets. Remove old standalone Wayfarer/ClearShirt mods when migrating those same identities to the API pack.
