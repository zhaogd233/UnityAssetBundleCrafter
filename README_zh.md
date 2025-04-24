- [README 中文](./README_zh.md)
- [README English](./README.md)

# UnityAssetBundleCrafter

**UnityAssetBundleCrafter** 是一套用于 **序列化和反序列化 Unity AssetBundle 资源文件** 的工具。  
支持在反序列化后对内容进行修改，并重新序列化回本地文件。

---

## 功能特点

- 反序列化 AssetBundle 文件为可编辑数据
- 支持对 AssetBundle 内容进行程序化修改
- 支持将修改后的数据重新序列化为 AssetBundle 文件

---

## 🔧 工具：RemoveTypeTree

**RemoveTypeTree** 是基于 UnityAssetBundleCrafter 实现的一个工具，功能是 **移除 AssetBundle 中的 TypeTree 信息**。

### 兼容性说明

- ✅ 已在 Unity **2022.3.x** 和 **2019.3.x** 版本中验证可用
- ✅ 当前仅支持 **LZ4 压缩格式** 的 AssetBundle
- ❌ 不支持其他压缩类型

> ⚠ 兼容性尚未在更多版本和压缩方式下进行测试，谨慎用于生产环境。

---

## 🧩 使用的开源库

- [AssetStudio by Perfare](https://github.com/Perfare/AssetStudio)

---
