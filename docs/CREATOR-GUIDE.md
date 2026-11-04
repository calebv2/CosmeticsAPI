# Creator guide

## First export

API 0.2.2 installs the bundled Wayfarer and skin palettes from its DLLs on startup. Additional exported creations still use the pack-folder workflow below. Use your own Pack ID and SKUs to avoid duplicating the bundled samples.

Import the Creator SDK into a Unity 2020.3.23f1 **Built-in 3D** project, with Unity's standard built-in modules enabled (including AssetBundle, Animation, and JSONSerialize). URP/HDRP projects are not supported by this ATT exporter. The **Cosmetics API** menu contains the reference rig, starter accessory, and export commands. Create a pack through **Assets → Create → Cosmetics API → Cosmetic Pack** and edit it in the Inspector.

The starter example is an original copper brooch. Duplicate its pack and prefab, then change the pack ID and SKU before making your own release. Its native template places it in the Belts category, where it replaces another belt/harness selection.

### Pack and item settings

| Setting | Meaning |
|---|---|
| Pack ID | Unique stable lowercase name, e.g. `yourname.forestgear`. Used for discovery and provider isolation. |
| Version | Your content release version. Keep client/server manifests identical. |
| SKU | Permanent item identity, e.g. `yourname.forestgear.shirt`. Display names may change; SKUs should not. |
| Template ID | A native cosmetic to supply wardrobe category and slots. It does not supply your model. |
| Prefab | Authoring model. Use an identity root: position/rotation zero, scale one. Place and scale geometry under it. |
| Geometry | Skinned garment or rigid accessory. |
| Rigid bone | Native bone name for a rigid accessory, such as `chest_jnt`, `wrist_L_jnt`, or a head bone from the reference rig. |
| Empty visual | Deliberately invisible clothing; leave the prefab empty. |
| Hide in first person | Hides hats/face pieces from their wearer while keeping them visible on other avatars/mannequins. |
| Skin tone | Uses a native full-body colour ramp. Requires a skin template and no prefab; empty visual and first-person hiding must be off. |

Verified native templates in this ATT installation:

| Template ID | Native template | Wardrobe category |
|---|---|---|
| 28346 | Starter Tee Shirt1 | Shirts |
| 37751 | Buckled Wayfarer Shirt | Shirts |
| 56783 | Buckled Wayfarer Harness | Belts |
| 15368 | Buckled Wayfarer Skirt | Skirt |
| 38247 | Skin 01 | Skin Color |

For another category, supply an actual native cosmetic ID from that category. This first release does not create arbitrary new wardrobe categories. Custom and native choices in the same category replace one another. For a full outfit with independent selections, export one item per category.

## Skin-colour palettes

Choose **Cosmetics API → Create Skin Tone Palettes** for the 32-tone Natural and 16-tone Fantasy examples in `Assets/CosmeticsAPI/Samples`. Selecting either asset shows editable **Tones** and a ramp preview. Expand a tone and click the Shadow, Midtone, or Highlight colour to choose any RGB value. Alpha is not transparency: ATT uses fixed ramp positions 0, 0.5, and 1. The exporter supplies those positions through the API.

Use **Export Client + Server Skin Palette** in the Inspector. A palette exports two identical manifests, with no mesh, shader, or asset bundle. Install API 0.2.0 and the same palette on the server and every viewing client. The game uses the normal **Skin Color** category and native coloured swatch pickups. These colour the body/head/hands through ATT's existing skin system; they do not change eyes, hair, or clothing dyes.

For a new palette, use **Assets → Create → Cosmetics API → Skin Colour Palette**. Set a unique Pack ID, name, version, and template **38247**. Increase the Tones array size and give each entry a unique permanent SKU, name, and three colours. Each pack supports 1–128 tones. Duplicate the examples for a starting palette, but change the pack ID and every SKU before distributing it as a separate creation. The sample creation menu preserves already-existing palette assets.

Changing a released tone's colours keeps the same saved selection if its SKU stays unchanged. Increase its pack version and update everyone together. A SKU collision is rejected like any other cosmetic. Names such as Silver or Gold describe RGB colours, not metallic materials.

The general Cosmetic Pack inspector can also combine skin tones with models in one pack. Enable Skin Tone on the colour entries and leave their Prefab blank. Skin entries use schema 2; older schema-1 clothing packs are still supported.

## Skinned clothing

The reference prefab contains ATT's 141 named bones and their compatible authoring rest pose. It contains no game body mesh. Bind your clothing to that skeleton in your modelling workflow, preserving names and rest matrices. Merely adding a SkinnedMeshRenderer or matching names does not create correct weights.

The SDK authoring convention is Y-up, Z-forward. It converts meshes, normals, winding, tangents, and weights into ATT's native convention at export. Bone subsets/reordered bones are accepted when their names and rest bind matrices match the reference; they are remapped to the full native order.

The exporter rejects a different rest pose, missing weights, invalid bone references, and blend shapes. Bake blend shapes first. Keep the prefab root at identity. A transform placed on the mesh after binding does not automatically rebind it; the exporter reports this discrepancy instead of producing an offset outfit.

## Rigid accessories

Choose Rigid for a hat, brooch, buckle, or similar item that should move with a single bone. The source uses ordinary MeshRenderers. Position its child geometry at the desired location relative to the reference skeleton and enter the target bone name. The exporter generates the skin weights and native bind poses for you.

Moving a rigid object to the origin and expecting the API to infer a head or wrist offset will not work. Its authored position is the intended position on the reference body. Enable Hide in first person for accessories that would obstruct the wearer's view.

## Materials and VR

Use the included **CosmeticsAPI/Creator Lit** shader for tint, colour texture, normal map, metallic, and smoothness controls. Unity Standard and the existing Wayfarer shaders can also be exported. Arbitrary custom shaders are allowed with a warning because their stereo behavior cannot be inferred from the material alone.

The exporter enables single-pass instancing and VR shader retention and disables unused mesh-component stripping during bundle builds. It then reloads the bundle and checks that all vertices still have weights. These project settings are intentionally retained for subsequent exports.

Custom handwritten shaders need Unity stereo/instance input and output handling. Always check both eyes in game. A shader being supported in a flat Unity preview does not establish stereo support.

## Files produced

`Client/UserData/CosmeticsAPI/Packs/<packId>/` contains `pack.json` and, for visible items, `assets.bundle`. The server folder contains the exact same manifest. The manifest records the bundle SHA-256 so accidentally mixing a new manifest with an old asset file is rejected.

The API checks this hash locally; it does not exchange pack lists or download missing assets over the network. Server owners must distribute matching pack versions to players. Keep the Client/Server split when sharing a pack. Install the API runtime once, then add as many compatible pack folders as needed.

Empty-only and skin-only packs need no bundle. Do not install a pack through both folder discovery and a C# embedded mod; that is duplicate registration.

## Troubleshooting

| Message/symptom | Action |
|---|---|
| Pack already registered | Remove the duplicate folder or embedded registration. |
| Cosmetic ID collision | Identify the conflicting pack/native ID. Change only a new, unreleased SKU; keep existing saved IDs stable. |
| Bundle SHA-256 mismatch | Reinstall the manifest and bundle from the same export. |
| Native template could not load | Verify that the template ID is a native cosmetic in this ATT build. |
| Missing skin weights | Re-export using the SDK; do not manually optimize away skin channels. |
| Bind pose differs from ATT | Rebind to the reference rest pose and apply model transforms correctly. |
| Left eye only | Check custom shader stereo support and rebuild using the exporter. |
| Hat blocks view | Enable Hide in first person and export again. |
| Nothing appears | Confirm `COSMETICS_PACK_READY`, the correct wardrobe category, and matching API/pack files on client/server. |
| Custom clothes disappear after an avatar detail change | Update clients and server to API 0.2.1, then restart. Each native detail level now has independent renderer instances. |
| Avatar body missing while joining or changing outfits | Use API 0.2.1 on every client/server, install matching packs, and check for `COSMETICS_PACK_FAILED`. Queued custom definitions now finish loading before the new appearance is applied. |

Before sharing your pack, check dressing/removal, both eyes, arm/body motion, the mannequin, another modded player's view, and save/rejoin.
