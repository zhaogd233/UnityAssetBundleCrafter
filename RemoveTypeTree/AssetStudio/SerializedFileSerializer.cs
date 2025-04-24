using System.Text;
using UnityFS;

namespace AssetStudio
{

    public class SerializedFileSerializer
    {
        public void Serialize(SerializedFile file, MemoryStream outputStream)
        {
            Serialize(outputStream, file);
        }

        private void Serialize(Stream targetStream, SerializedFile file)
        {
            Stream writeStream = targetStream;
            var writer = new EndianBinaryWriter(writeStream, file.header.m_Endianess == 0 ? EndianType.LittleEndian : EndianType.BigEndian);
            {
                // header
                WriteHeader(writer, file);
                // writer.Write(file.headerMeataDataNoTypeTree);

                //metadata
                WriteMetadataTypeTree(writer, file);
                // types
                WriteSerializedTypeList(writer, file, file.header);
                // bigID
                if (file.header.m_Version >= SerializedFileFormatVersion.Unknown_7 && file.header.m_Version < SerializedFileFormatVersion.Unknown_14)
                    writer.Write((int)file.bigIDEnabled);
                // objects
                WriteObjects(writer, file);
                // script types
                WriteScriptTypes(writer, file);
                // externals
                WriteExternals(writer, file);
                // ref types
                if (file.header.m_Version >= SerializedFileFormatVersion.SupportsRefObject)
                    WriteSerializedTypeList(writer, file, file.header, isRefType: true);
                // user info
                if (file.header.m_Version >= SerializedFileFormatVersion.Unknown_5)
                    writer.Write(Encoding.UTF8.GetBytes(file.userInformation + "\0"));
                //writer.Flush();

                long datasize = file.header.m_FileSize - file.header.m_DataOffset;
                //  file.header.m_MetadataSize = (uint)writer.BaseStream.Position;
                file.header.m_MetadataSize = (uint)writer.BaseStream.Position - 48;
                //write leftData
                writer.Write(file.leftFileData);
                file.header.m_FileSize = (uint)writer.BaseStream.Position;
                file.header.m_DataOffset = file.header.m_FileSize - datasize;
                //rewrite header size
                writer.Position = 0;
                writer.Endian = EndianType.BigEndian;
                WriteHeader(writer, file);
                writer.Position = file.header.m_FileSize;
            }
        }

        private void WriteHeader(EndianBinaryWriter w, SerializedFile f)
        {
            w.Write((uint)f.header.m_MetadataSize);
            /*  if (f.header.m_Version >= SerializedFileFormatVersion.LargeFilesSupport)
                  w.Write((long)f.header.m_FileSize);
              else*/
            w.Write((uint)f.header.m_FileSize);
            w.Write((uint)f.header.m_Version);
            /* if (f.header.m_Version >= SerializedFileFormatVersion.LargeFilesSupport)
                 w.Write((long)f.header.m_DataOffset);
             else*/
            w.Write((uint)f.header.m_DataOffset);

            if (f.header.m_Version >= SerializedFileFormatVersion.Unknown_9)
            {
                w.Write(f.header.m_Endianess);
                w.Write(f.header.m_Reserved);
            }
            else
            {
                // legacy: write padding so reader.Seek works
                w.Seek((int)(f.header.m_FileSize - f.header.m_MetadataSize), SeekOrigin.Begin);
                w.Write(f.header.m_Endianess);
            }

            if (f.header.m_Version >= SerializedFileFormatVersion.LargeFilesSupport)
            {
                w.Position = 0;
                w.Write(f.header.headerData);
                w.Write((uint)f.header.m_MetadataSize);
                w.Write((long)f.header.m_FileSize);
                w.Write((long)f.header.m_DataOffset);
                w.Write((long)0); // unknown
            }
        }

        private void WriteMetadataTypeTree(EndianBinaryWriter w, SerializedFile f)
        {
            if (f.header.m_Version >= SerializedFileFormatVersion.Unknown_7)
                w.Write(Encoding.UTF8.GetBytes(f.unityVersion + "\0"));
            if (f.header.m_Version >= SerializedFileFormatVersion.Unknown_8)
                w.Write((int)f.m_TargetPlatform);
            if (f.header.m_Version >= SerializedFileFormatVersion.HasTypeTreeHashes)
                w.Write(f.m_EnableTypeTree);
        }

        private void WriteSerializedTypeList(EndianBinaryWriter w, SerializedFile f, SerializedFileHeader header, bool isRefType = false)
        {
            var types = isRefType ? f.m_RefTypes : f.m_Types;
            w.Write(types.Count);
            foreach (var type in types)
            {
                w.Write(type.classID);
                if (header.m_Version >= SerializedFileFormatVersion.RefactoredClassId)
                    w.Write(type.m_IsStrippedType);
                if (header.m_Version >= SerializedFileFormatVersion.RefactorTypeData)
                    w.Write((short)type.m_ScriptTypeIndex);
                if (header.m_Version >= SerializedFileFormatVersion.HasTypeTreeHashes)
                {
                    if (isRefType && type.m_ScriptTypeIndex >= 0)
                        w.Write(type.m_ScriptID);
                    else if ((header.m_Version < SerializedFileFormatVersion.RefactoredClassId && type.classID < 0) ||
                             (header.m_Version >= SerializedFileFormatVersion.RefactoredClassId && type.classID == 114))
                        w.Write(type.m_ScriptID);
                    w.Write(type.m_OldTypeHash);
                }
                if (f.m_EnableTypeTree)
                {
                    if (header.m_Version >= SerializedFileFormatVersion.Unknown_12 || header.m_Version == SerializedFileFormatVersion.Unknown_10)
                    {
                        var nodes = type.m_Type.m_Nodes;
                        w.Write(nodes.Count);
                        int totalStrLen = type.m_Type.m_StringBuffer?.Length ?? 0;
                        w.Write(totalStrLen);
                        foreach (var node in nodes)
                        {
                            w.Write((ushort)node.m_Version);
                            w.Write((byte)node.m_Level);
                            w.Write((byte)node.m_TypeFlags);
                            w.Write(node.m_TypeStrOffset);
                            w.Write(node.m_NameStrOffset);
                            w.Write(node.m_ByteSize);
                            w.Write(node.m_Index);
                            w.Write(node.m_MetaFlag);
                            if (header.m_Version >= SerializedFileFormatVersion.TypeTreeNodeWithTypeFlags)
                                w.Write(node.m_RefTypeHash);
                        }
                        if (type.m_Type.m_StringBuffer != null && type.m_Type.m_StringBuffer.Length > 0)
                            w.Write(type.m_Type.m_StringBuffer);
                    }
                    else
                    {
                        WriteTypeTree(w, f.header, type.m_Type);
                    }

                    if (header.m_Version >= SerializedFileFormatVersion.StoresTypeDependencies)
                    {
                        if (isRefType)
                        {
                            w.Write(Encoding.UTF8.GetBytes(type.m_KlassName + "\0"));
                            w.Write(Encoding.UTF8.GetBytes(type.m_NameSpace + "\0"));
                            w.Write(Encoding.UTF8.GetBytes(type.m_AsmName + "\0"));
                        }
                        else
                        {
                            w.Write(type.m_TypeDependencies?.Length ?? 0);
                            foreach (var dep in type.m_TypeDependencies ?? Array.Empty<int>())
                                w.Write(dep);
                        }
                    }
                }
            }
        }
        private void WriteTypeTree(EndianBinaryWriter w, SerializedFileHeader header, TypeTree typeTree)
        {
            void WriteNode(TypeTreeNode node, IEnumerator<TypeTreeNode> enumerator, int level)
            {
                w.Write(Encoding.UTF8.GetBytes(node.m_Type + "\0"));
                w.Write(Encoding.UTF8.GetBytes(node.m_Name + "\0"));
                w.Write(node.m_ByteSize);

                if (header.m_Version == SerializedFileFormatVersion.Unknown_2)
                    w.Write(0); // variableCount 占位

                if (header.m_Version != SerializedFileFormatVersion.Unknown_3)
                    w.Write(node.m_Index);

                w.Write(node.m_TypeFlags);
                w.Write(node.m_Version);

                if (header.m_Version != SerializedFileFormatVersion.Unknown_3)
                    w.Write(node.m_MetaFlag);

                // 获取 children
                var children = new List<TypeTreeNode>();
                long currentLevel = level;
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.m_Level == level + 1)
                    {
                        children.Add(enumerator.Current);
                    }
                    else if (enumerator.Current.m_Level <= level)
                    {
                        break;
                    }
                }

                w.Write(children.Count);
                foreach (var child in children)
                {
                    WriteNode(child, enumerator, level + 1);
                }
            }

            var enumerator = typeTree.m_Nodes.GetEnumerator();
            if (enumerator.MoveNext())
            {
                WriteNode(enumerator.Current, enumerator, 0);
            }
        }

        private void WriteObjects(EndianBinaryWriter w, SerializedFile f)
        {
            w.Write(f.m_Objects.Count);
            foreach (var info in f.m_Objects)
            {
                if (f.bigIDEnabled != 0)
                    w.Write(info.m_PathID);
                else if (f.header.m_Version < SerializedFileFormatVersion.Unknown_14)
                    w.Write((int)info.m_PathID);
                else
                {
                    Align(w, 4);
                    w.Write(info.m_PathID);
                }
                if (f.header.m_Version >= SerializedFileFormatVersion.LargeFilesSupport)
                    w.Write((long)(info.byteStart - f.header.m_DataOffset));
                else
                    w.Write((uint)(info.byteStart - f.header.m_DataOffset));
                w.Write(info.byteSize);
                w.Write(info.typeID);
                if (f.header.m_Version < SerializedFileFormatVersion.RefactoredClassId)
                    w.Write((ushort)info.classID);
                if (f.header.m_Version < SerializedFileFormatVersion.HasScriptTypeIndex)
                    w.Write(info.isDestroyed);
                if (f.header.m_Version >= SerializedFileFormatVersion.HasScriptTypeIndex && f.header.m_Version < SerializedFileFormatVersion.RefactorTypeData)
                    w.Write((short)info.serializedType.m_ScriptTypeIndex);
                if (f.header.m_Version == SerializedFileFormatVersion.SupportsStrippedObject || f.header.m_Version == SerializedFileFormatVersion.RefactoredClassId)
                    w.Write(info.stripped);
            }
        }

        private void WriteScriptTypes(EndianBinaryWriter w, SerializedFile f)
        {
            if (f.header.m_Version >= SerializedFileFormatVersion.HasScriptTypeIndex)
            {
                w.Write(f.m_ScriptTypes.Count);
                foreach (var s in f.m_ScriptTypes)
                {
                    w.Write(s.localSerializedFileIndex);
                    if (f.header.m_Version < SerializedFileFormatVersion.Unknown_14)
                        w.Write((int)s.localIdentifierInFile);
                    else
                    {
                        Align(w, 4);
                        w.Write(s.localIdentifierInFile);
                    }
                }
            }
        }

        private void WriteExternals(EndianBinaryWriter w, SerializedFile f)
        {
            w.Write(f.m_Externals.Count);
            foreach (var ext in f.m_Externals)
            {
                if (f.header.m_Version >= SerializedFileFormatVersion.Unknown_6)
                    w.Write(Encoding.UTF8.GetBytes("\0"));
                if (f.header.m_Version >= SerializedFileFormatVersion.Unknown_5)
                {
                    w.Write(ext.guid.ToByteArray());
                    w.Write(ext.type);
                }
                w.Write(Encoding.UTF8.GetBytes(ext.pathName + "\0"));
            }
        }

        private static void Align(EndianBinaryWriter w, int alignment)
        {
            var pad = (int)(alignment - (w.Position % alignment)) % alignment;
            if (pad > 0)
                w.Write(new byte[pad]);
        }

        private void WriteUserInformation(EndianBinaryWriter w, SerializedFile f)
        {
            if (f.header.m_Version >= SerializedFileFormatVersion.Unknown_5)
                w.Write(Encoding.UTF8.GetBytes(f.userInformation + "\0"));
        }
    }
}
