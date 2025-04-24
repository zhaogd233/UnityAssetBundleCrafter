- [README 中文](./README_zh.md)
- [README English](./README.md)

# UnityAssetBundleCrafter

**UnityAssetBundleCrafter** is a toolset for **serializing and deserializing Unity AssetBundle files**.  
It allows you to modify the contents of AssetBundles and reserialize them back to local files.

---

## Features

- Deserialize AssetBundles into editable data
- Modify AssetBundle content programmatically
- Reserialize edited data back to AssetBundle format

---

## 🔧 Tool: RemoveTypeTree

**RemoveTypeTree** is a tool built on top of UnityAssetBundleCrafter, designed to **strip TypeTree information** from AssetBundles.

### Compatibility

- ✅ Tested with Unity **2022.3.x** and **2019.3.x**
- ✅ Only supports **LZ4-compressed** AssetBundles
- ❌ Other compression types are **not supported**

> ⚠ Compatibility with other Unity versions or compression formats has **not been tested**.

---

## 🧩 Open Source Libraries Used

- [AssetStudio by Perfare](https://github.com/Perfare/AssetStudio)

---
