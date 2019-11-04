using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UrbanCity
{
    public long RoomId;
    public long OwnerId;
    public long CityId;

    public int PosX;
    public int PosZ;
    public int CellIndex;

    public string CityName;
    public int CitySize;
    public bool IsCapital;

    public void Init(long roomId, long ownerId, long cityId, int posX, int posZ, int cellIndex, string cityName, int citySize, bool isCapital)
    {
        RoomId = roomId;
        OwnerId = ownerId;
        CityId = cityId;
        PosX = posX;
        PosZ = posZ;
        CellIndex = cellIndex;
        CityName = cityName;
        CitySize = citySize;
        IsCapital = isCapital;
    }

    public void SaveBuffer(BinaryWriter bw)
    {
        bw.Write(RoomId);
        bw.Write(OwnerId);
        bw.Write(CityId);
        bw.Write(PosX);
        bw.Write(PosZ);
        bw.Write(CellIndex);
        bw.Write(CityName);
        bw.Write((byte)CitySize);
        bw.Write(IsCapital);
    }

    public void LoadBuffer(BinaryReader br)
    {
        RoomId = br.ReadInt64();
        OwnerId = br.ReadInt64();
        CityId = br.ReadInt64();
        PosX = br.ReadInt32();
        PosZ = br.ReadInt32();
        CellIndex = br.ReadInt32();
        CityName = br.ReadString();
        CitySize = br.ReadByte();
        IsCapital = br.ReadBoolean();
    }
}
