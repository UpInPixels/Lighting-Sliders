# Lighting Sliders (Poiyomi)

An automated setup tool and VRCFury prefab for VRChat avatars that adds full lighting, brightness, grayscale, and post-processing controls for Poiyomi materials.

---

## Requirements

* **Unity:** 2022.3.22f1 or newer
* **VRCFury:** [Download VRCFury](https://vrcfury.com/download)
* **Shader:** Poiyomi Shaders (Poiyomi Toon / Pro / Master)

---

## Quick Setup (Automated Tool)

1. Import **VRCFury** into your Unity project.
2. Import `Lighting Sliders.unitypackage`.
3. Open the setup tool from the top menu:
**`Tools > UpInPixels > Lighting Sliders Setup`**
4. Drag your **Avatar Root** into the **Avatar Root** target field.
5. *(Optional)* Drag any extra Poiyomi materials into the **Extra Materials** list.
6. Click **Setup Materials & Add Sliders**.

The tool automatically detects Poiyomi materials, unlocks/locks them safely, marks required lighting properties as animated, and attaches the VRCFury prefab directly to your avatar.

---

## Manual Setup (Without Tool)

1. Drag the **`Lighting Sliders`** prefab into your avatar hierarchy.
2. Select all Poiyomi materials on your avatar in the Project window.
3. Right-click the selected materials $\rightarrow$ **`Thry > Materials > Unlock All`**.
4. In the same menu, click **`Add to Cross Shader Editor`**.
5. In the Cross Shader Editor window, locate and right-click the following properties, then set them to **Animated when locked**:
* **Shading / Light Data:**
* Max Brightness (`_LightingCap`)
* Min Brightness (`_LightingMinLightBrightness`)
* Grayscale Lighting (`_LightingMonochromatic`)


* **Global Modifiers & Data / Post Processing / PP Animations:**
* Lighting Multiplier (`_PPLightingMultiplier`)
* Lighting Add (`_PPLightingAddition`)
* Emission Multiplier (`_PPEmissionMultiplier`)
* Final Color Multiplier (`_PPFinalColorMultiplier`)




6. Right-click the materials $\rightarrow$ **`Thry > Materials > Lock All`**.
7. Upload your avatar!

---

## Terms of Service

* Do **not** resell, share, or redistribute this asset.
* Do **not** claim this asset as your own.
* Allowed for **personal** and **commercial** use (with credit).
* Credit **UpInPixels** when using this asset commercially.

---

## Author & Support

Developed by **UpInPixels**

* **Original Concept Credit:** Sacred
* **Store:** [Payhip Store](https://payhip.com/upinpixels)
* **Discord Support:** `TheUploader`