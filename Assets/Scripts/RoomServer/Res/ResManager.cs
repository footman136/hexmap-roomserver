using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

public class ResManager
{
    public Dictionary<int, ResInfo> AllRes = new Dictionary<int, ResInfo>();

    public void AddRes(int cellIndex, ResInfo ri)
    {
//        if (!AllRes.ContainsKey(cellIndex))
//        {
//            ri = new ResInfo();
//        }
        AllRes[cellIndex] = ri;
    }

    public ResInfo GetRes(int cellIndex)
    {
        if (!AllRes.ContainsKey(cellIndex))
            return null;            
        return AllRes[cellIndex];
    }

    public byte[] SaveBuffer()
    {
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        int version = 1;
        bw.Write(version);
        bw.Write(AllRes.Count);
        foreach (var keyValue in AllRes)
        {
            bw.Write(keyValue.Key);
            keyValue.Value.SaveBuffer(bw);
        }

        return ms.GetBuffer();
    }

    public bool LoadBuffer(byte[] bytes, int size)
    {
        MemoryStream ms = new MemoryStream(bytes);
        BinaryReader br = new BinaryReader(ms);
        int version = br.ReadInt32();
        int resCount = br.ReadInt32();
        if (resCount < 0 || resCount > 9999)
        {
            ServerRoomManager.Instance.Log($"ResManager - LoadBuffer Error - count of resources is invalid: {resCount} - should between:{0}~{9999}");
            return false;
        }

        AllRes.Clear();
        for (int i = 0; i < resCount; ++i)
        {
            int cellIndex = br.ReadInt32();

            ResInfo res = new ResInfo();
            res.LoadBuffer(br, version);
            AddRes(cellIndex, res);
        }
        ServerRoomManager.Instance.Log($"ResManager LoadBuffer OK - 资源个数：{AllRes.Count}");

        return true;
    }
}
