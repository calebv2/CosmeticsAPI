# CosmeticsAPI

Create and share custom cosmetics for **A Township Tale on PC**. Export a pack from Unity, install it alongside CosmeticsAPI, and use it through the native cosmetic shop and wardrobe.

Supports skinned clothing, rigid accessories, transparent clothing selections, and additional skin-colour palettes. Install the API once, then add matching packs on the client and server. No SyncLib or UnlockCosmetics dependency is required.

**[Download v0.2.1](https://github.com/calebv2/CosmeticsAPI/releases/tag/v0.2.1)** · [Creator guide](docs/CREATOR-GUIDE.md) · [C# API](docs/API-REFERENCE.md)

## Downloads

| File | Contents |
|---|---|
| `CosmeticsAPI-Kit-0.2.1.zip` | Client/server runtime, Unity creator package, Wayfarer sample outfits, optional skin palettes, and instructions. |
| `CosmeticsAPI-Wayfarer-Update-0.2.1.zip` | Runtime and updated Wayfarer pack merged into ready-to-copy Client/Server folders. |
| `CosmeticsAPI-Runtime-0.2.1.zip` | The two runtime DLLs for installing or updating the API. |
| `CosmeticsAPI-CreatorSDK-0.2.0.unitypackage` | Unity authoring tools, reference skeleton, material shader, and examples. |
| `Wayfarer-CosmeticPack-1.0.1.zip` | Four ready-to-install sample cosmetics. Requires the API. |
| `NaturalSkin-CosmeticPack-1.0.0.zip` | 32 additional natural skin tones. Requires API 0.2.0 or newer. |
| `FantasySkin-CosmeticPack-1.0.0.zip` | 16 optional fantasy skin colours. Requires API 0.2.0 or newer. |

## Install

Requires **PC A Township Tale**, **MelonLoader 0.7.3**, and a server where you can install mods. The creator tools use **Unity 2020.3.23f1**, Built-in 3D.

1. Close the game and server.
2. Copy the **Client** `CosmeticsAPI.dll` into the client's `Mods` folder.
3. Copy the **Server** `CosmeticsAPI.dll` into the server's `Mods` folder. The two builds have the same filename; use the correct one.
4. Choose a sample or exported pack. Merge its **Client** folder into the client game folder and its **Server** folder into the server game folder.
5. Restart both. Every player who should see the cosmetics needs the same API and pack versions.

In the complete kit, the DLLs are under `Runtime/Client/Mods` and `Runtime/Server/Mods`. The sample packs are under `Samples`.

The client installs a pack like this:

```text
Mods/CosmeticsAPI.dll
UserData/CosmeticsAPI/Packs/caleb.wayfarer/pack.json
UserData/CosmeticsAPI/Packs/caleb.wayfarer/assets.bundle
```

The server needs its API DLL and the identical `pack.json`, but no graphics bundle. Skin-only packs need no bundle on either side. Packs load at startup; restart after changes.

## Wayfarer sample

[Wayfarer](Samples/Wayfarer) demonstrates a pack containing independently selectable cosmetic pieces:

| Cosmetic | Category |
|---|---|
| Veilweave Shirt | Shirts; transparent selection |
| Oathbound Harness | Belts |
| Trailwarden Kilt | Skirt |
| Ironweave Hauberk | Shirts; chainmail with wrist-length sleeves |

When migrating from the standalone outfit mods, remove `WayfarerWardrobe.dll` and `ClearShirt.dll`, if present, from client and server. They must not register the same items alongside this pack. Existing outfit IDs are preserved.

Custom item previews use the actual cosmetic model fitted to the shop display. The transparent shirt has a visible shirt marker so its preview can still be selected. Update both the runtime and Wayfarer pack for the sleeve and preview changes.

Version 0.2.1 also fixes custom clothing disappearing during avatar detail-level changes. Appearance updates wait for queued custom definitions and keep the previous valid appearance until the replacement is ready.

The optional [Natural Skin](Samples/NaturalSkin) and [Fantasy Skin](Samples/FantasySkin) packs add choices to the native **Skin Color** category.

## Create a pack

1. Import the Creator SDK into a Unity **2020.3.23f1 Built-in 3D** project.
2. Use **Cosmetics API → Create Starter Accessory Pack** for a working authoring example, or **Assets → Create → Cosmetics API → Cosmetic Pack** for a new pack.
3. Set a unique Pack ID, a version, and a permanent SKU for each cosmetic. Assign your prefab and a native template ID for its wardrobe category.
4. Use **Skinned** for clothing bound to the reference skeleton, or **Rigid** for an accessory attached to one bone.
5. Select the pack and click **Export Client + Server Pack** in the Inspector.
6. Share the exported Client and Server folders with the API installation instructions.

For skin colours, use **Cosmetics API → Create Skin Tone Palettes**, select either palette under `Assets/CosmeticsAPI/Samples`, and edit its **Tones**. Each tone has shadow, midtone, and highlight colours. Click **Export Client + Server Skin Palette** to export it.

See the [creator guide](docs/CREATOR-GUIDE.md) for templates, rigging, materials, stable IDs, and troubleshooting. Mods can also register packs through the [C# API](docs/API-REFERENCE.md).

## Compatibility

Version 0.2.1 supports existing schema-1 clothing packs and schema-2 skin palettes. Packs do not automatically download to other players. The API does not automatically fit arbitrary clothing, add clothing dye brushes, or support Quest/Android.

This is a preview release. Live multiplayer and save/rejoin verification is still pending. Check new cosmetics in both eyes, on the mannequin, and on another player's avatar before distributing your own pack.

## Build from source

Creators using the Unity exporter do not need to rebuild the API. To build the runtime itself, clone this repository and provide your installed client, server, and Unity editor paths:

```powershell
./Build.ps1 -ClientGame 'C:\ATTClient' -ServerGame 'C:\ATTServer' -UnityEditor 'C:\Program Files\Unity\Hub\Editor\2020.3.23f1\Editor'
```

The script writes separate client and server DLLs to `Build`. Game and MelonLoader assemblies are read from your installations and are not included in this repository.
