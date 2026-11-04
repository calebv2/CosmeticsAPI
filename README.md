# CosmeticsAPI

Custom cosmetics and Unity creator tools for **A Township Tale on PC**.

**[Download v0.2.3](https://github.com/calebv2/CosmeticsAPI/releases/tag/v0.2.3)** · [Creator guide](docs/CREATOR-GUIDE.md) · [C# API](docs/API-REFERENCE.md)

## Install

1. Close the game and stop the server.
2. Download `CosmeticsAPI-Runtime-0.2.3.zip`.
3. Copy `Client/Mods/CosmeticsAPI.dll` into the client's **Mods** folder.
4. Copy `Server/Mods/CosmeticsAPI.dll` into the server's **Mods** folder.
5. Start the server and client. Every viewing player needs the client build.

**The DLLs create their UserData files automatically.** The client DLL contains the models and bundled manifests; the server DLL contains the matching manifests. No separate UserData upload is needed for the bundled cosmetics.

The client and server DLLs share a filename but are different builds. Use the correct one. Remove standalone `WayfarerWardrobe.dll` and `ClearShirt.dll`, if present, when switching to this API.

Requires PC ATT with **MelonLoader 0.7.3**. No SyncLib or UnlockCosmetics dependency is required.

## Included cosmetics

| Cosmetic | Category |
|---|---|
| Veilweave Shirt | Shirts; transparent selection |
| Oathbound Harness | Belts |
| Trailwarden Kilt | Skirt |
| Ironweave Hauberk | Shirts; wrist-length chainmail |
| 32 natural skin tones | Skin Color |
| 16 fantasy skin colours | Skin Color |

The [Wayfarer sample](Samples/Wayfarer) preserves existing outfit IDs. Shop previews use the actual model fitted to the display space; the clear shirt uses a visible selection marker.

On startup, files are installed under `UserData/CosmeticsAPI/Packs`. Existing matching files are reused. Files created and managed by the installer can be updated automatically; edited or independently installed packs are preserved. Startup logs report the scan path, manifest count, and `COSMETICS_PACK_READY` for registered packs.

To stop automatic installation of the bundled packs, set `installBundledPacks` to `false` in the generated `UserData/CosmeticsAPI/settings.json`. Remove any unwanted bundled pack folders while the game/server is stopped. Other exported packs can still be installed normally.

## Creator kit

The release provides:

- **Runtime ZIP:** the two self-contained DLLs and installation instructions.
- **Creator SDK:** a Unity package with authoring inspectors, reference skeleton, material shader, and examples.
- **Complete kit:** runtime, SDK, exported sample packs, and creator documentation.

To create a pack:

1. Import `CosmeticsAPI-CreatorSDK-0.2.0.unitypackage` into a **Unity 2020.3.23f1 Built-in 3D** project.
2. Use **Cosmetics API → Create Starter Accessory Pack** for an authoring example, or **Assets → Create → Cosmetics API → Cosmetic Pack** for a new pack.
3. Set a unique Pack ID, version, and permanent SKU for each cosmetic. Assign your prefab and a native template ID for the wardrobe category.
4. Use **Skinned** for clothing bound to the reference skeleton, or **Rigid** for an accessory attached to one bone.
5. Select the pack and click **Export Client + Server Pack** in the Inspector.

Install exported packs under `UserData/CosmeticsAPI/Packs` on client/server. Model bundles are client-only. The sample data in this repository is already bundled in the runtime DLLs.

For custom skin palettes, choose **Cosmetics API → Create Skin Tone Palettes**, select either palette under `Assets/CosmeticsAPI/Samples`, and edit **Tones**. Each tone has shadow, midtone, and highlight colours. Export both client/server manifests from the Inspector.

See the [creator guide](docs/CREATOR-GUIDE.md) for rigging, templates, materials, stable IDs, and troubleshooting. Mods can register folders or carry their own embedded packs through the [C# API](docs/API-REFERENCE.md).

## Compatibility

Supports schema-1 clothing and schema-2 skin palettes. The runtime also preserves the previous complete avatar appearance while a replacement loads, and gives native detail levels independent clothing renderers.

Additional custom packs do not automatically download to other players. The API does not automatically fit arbitrary models, add clothing dye brushes, or support Quest/Android. This is a preview release; live multiplayer and save/rejoin verification remain ongoing.

## Build from source

Creators using the exporter do not need to rebuild the API. To build the runtime, clone this repository and provide your installed game and Unity editor paths:

```powershell
./Build.ps1 -ClientGame 'C:\ATTClient' -ServerGame 'C:\ATTServer' -UnityEditor 'C:\Program Files\Unity\Hub\Editor\2020.3.23f1\Editor'
```

Separate DLLs are written under `Build/Client/Mods` and `Build/Server/Mods`. The build embeds the bundled content from `Samples`. Game and MelonLoader assemblies are read from your installations and are not distributed here.
