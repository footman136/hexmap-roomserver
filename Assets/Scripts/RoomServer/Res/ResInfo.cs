using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 对应于客户端的HexResource,专门存储地图上的资源数据
/// </summary>
public class ResInfo
{
    public const int RES_MAX_TYPE = 3;

    public enum RESOURCE_TYPE
    {
        WOOD = 0,
        FOOD = 1,
        IRON = 2,
    };

    // 储量等级,服务器端对这个没有意义,
    public int[] RESERVE_LEVEL = {
        0, 100, 400, 800
    };

    // 木材-0；粮食-1；铁矿-2
    public RESOURCE_TYPE ResType; 
    private int [] ResAmount = new int[RES_MAX_TYPE];
    
    public void SaveBuffer(BinaryWriter writer)
    {
        writer.Write((byte)ResType);
        writer.Write(ResAmount[(int)ResType]);
    }
    public void LoadBuffer(BinaryReader reader, int header)
    {
        ResType = (RESOURCE_TYPE)reader.ReadByte();
        ResAmount[(int)ResType] = reader.ReadInt32();
    }

    public static int GetSaveSize()
    {
        return 5;
    }

    public void SetAmount(RESOURCE_TYPE type, int value)
    {
        ResType = type;
        ResAmount[(int)type] = value;
    }

    public int GetAmount(RESOURCE_TYPE type)
    {
        return ResAmount[(int)type];
    }

    public int GetLevel(RESOURCE_TYPE type)
    {
        for (int i = 0; i < 4; ++i)
        {
            if (ResAmount[(int) type] <= RESERVE_LEVEL[i])
            {
                return i;
            }
        }

        return 3;
    }

    // 支持一个格子多种资源
//    public void SaveBuffer(BinaryWriter bw)
//    {
//        bw.Write((byte)ResType);
//        bw.Write(CellIndex);
//        bw.Write((byte)3); // 资源的个数
//        bw.Write(ResAmount[0]);
//        bw.Write(ResAmount[1]);
//        bw.Write(ResAmount[2]);
//    }
//
//    public void LoadBuffer(BinaryReader br, header)
//    {
//        ResType = (RESOURCE_TYPE)br.ReadByte();
//        CellIndex = br.ReadInt32();
//        byte count = br.ReadByte();
//        for (int i = 0; i < count; ++i)
//        {
//            ResAmount[i] = br.ReadInt32();
//        }
//    }
}
