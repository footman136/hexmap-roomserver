using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameUtils;
using Main;
using UnityEngine;

public class UrbanManager
{
    public Dictionary<long, UrbanCity> AllCities = new Dictionary<long, UrbanCity>();

    public void AddCity(UrbanCity city)
    {
        AllCities[city.CityId] = city;
    }

    public UrbanCity GetCity(long cityId)
    {
        return AllCities.ContainsKey(cityId) ? AllCities[cityId] : null;
    }

    public bool RemoveCity(long cityId)
    {
        if (AllCities.ContainsKey(cityId))
        {
            var city = AllCities[cityId];
            AllCities.Remove(cityId);
            return true;
        }

        return false;
    }

    public byte[] SaveBuffer()
    {
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        int version = 1;
        bw.Write(version);
        bw.Write(AllCities.Count);
        int index = 0;
        foreach (var keyValue in AllCities)
        {
            bw.Write(index++); // 这里保存一个序号,纯粹是为了校验
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

        AllCities.Clear();
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
        ServerRoomManager.Instance.Log($"UrbanManager LoadBuffer OK - Count of Cities ：{AllCities.Count}"); // 城市个数

        return true;
    }
    public int CountOfThePlayer(long ownerId)
    {
        int count = 0;
        foreach (var keyValue in AllCities)
        {
            if (keyValue.Value.OwnerId == ownerId)
            {
                count++;
            }
        }

        return count;
    }
}
