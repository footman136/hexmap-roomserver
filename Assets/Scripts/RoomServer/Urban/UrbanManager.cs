using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameUtils;
using Main;
using UnityEngine;

public class UrbanManager
{
    public Dictionary<long, UrbanCity> Cities = new Dictionary<long, UrbanCity>();

    public void AddCity(UrbanCity city)
    {
        if (Cities.ContainsKey(city.CityId))
        {
            ServerRoomManager.Instance.Log("MSG: Duplicated city!");
        }
        else
        {
            Cities.Add(city.CityId, city);
        }
    }

    public bool RemoveCity(long cityId)
    {
        if (Cities.ContainsKey(cityId))
        {
            var city = Cities[cityId];
            Cities.Remove(cityId);
            return true;
        }

        return false;
    }

    public int GetMyCityCount(long ownerId)
    {
        int count = 0;
        foreach (var keyValue in Cities)
        {
            var city = keyValue.Value;
            if (city.OwnerId == ownerId)
                count++;
        }

        return count;
    }

    public byte[] SaveBuffer()
    {
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        int version = 1;
        bw.Write(version);
        bw.Write(Cities.Count);
        int index = 0;
        foreach (var keyValue in Cities)
        {
            bw.Write(index++);
            keyValue.Value.SaveBuffer(bw);
        }

        return ms.GetBuffer();
    }

    public bool LoadBuffer(byte[] bytes, int size)
    {
        MemoryStream ms = new MemoryStream(bytes);
        BinaryReader br = new BinaryReader(ms);
        int version = br.ReadInt32();
        int cityCount = br.ReadInt32();
        if (cityCount < 0 || cityCount > 9999)
        {
            ServerRoomManager.Instance.Log($"UrbanManager - LoadBuffer Error - count of cities is invalid: {cityCount} - should between:{0}~{9999}");
            return false;
        }

        Cities.Clear();
        for (int i = 0; i < cityCount; ++i)
        {
            int index = br.ReadInt32();
            if (index != i)
            {
                ServerRoomManager.Instance.Log($"UrbanManager - LoadBuffer Error - city index is not valid:{index}- should between:{0}~{cityCount}");
                return false;
            }

            UrbanCity city = new UrbanCity();
            city.LoadBuffer(br);
            AddCity(city);
        }
        ServerRoomManager.Instance.Log($"UrbanManager LoadBuffer - 城市个数：{Cities.Count}");

        return true;
    }
}
