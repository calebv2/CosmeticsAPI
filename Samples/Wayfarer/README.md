# Wayfarer sample pack

Four cosmetic choices packaged for CosmeticsAPI. Install API 0.2.1 on client and server, then merge this pack's **Client** and **Server** folders into their respective game folders. Every viewing player needs the same pack. Restart after installation.

| Cosmetic | Category | Saved ID |
|---|---|---|
| Veilweave Shirt | Shirts; transparent | 63925 |
| Oathbound Harness | Belts | 63704 |
| Trailwarden Kilt | Skirt | 52837 |
| Ironweave Hauberk | Shirts; wrist-length chainmail | 15096 |

Remove the standalone `WayfarerWardrobe.dll` and `ClearShirt.dll`, if present, before installing this API pack. The saved IDs remain unchanged.

The client folder includes `pack.json` and its matching `assets.bundle`. The server folder contains the same manifest without graphics. Keep each manifest paired with its original bundle; the API checks the bundle checksum.

This sample shows model entries and an intentionally empty shirt entry in one pack. Native templates supply the wardrobe categories and dressing slots. Use the Unity Creator SDK's reference rig and accessory example to author your own models; this folder is an exported runtime pack.

For a new creation, use your own Pack ID and SKUs. Do not rename these SKUs in an installed Wayfarer pack, because saved selections use their IDs.
