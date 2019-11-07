// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: MsgDefineRoom.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Protobuf.Room {

  /// <summary>Holder for reflection information generated from MsgDefineRoom.proto</summary>
  public static partial class MsgDefineRoomReflection {

    #region Descriptor
    /// <summary>File descriptor for MsgDefineRoom.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static MsgDefineRoomReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChNNc2dEZWZpbmVSb29tLnByb3RvEg1Qcm90b2J1Zi5Sb29tKoMDCgRST09N",
            "EgwKCE1zZ1N0YXJ0EAASDwoJSGVhcnRCZWF0EKCcARIRCgtQbGF5ZXJFbnRl",
            "chChnAESDwoJRW50ZXJSb29tEKOcARIPCglMZWF2ZVJvb20QpZwBEg8KCVVw",
            "bG9hZE1hcBCnnAESEQoLRG93bmxvYWRNYXAQqZwBEhEKC0Rlc3Ryb3lSb29t",
            "EKucARIUCg5Eb3dubG9hZENpdGllcxC1nAESDQoHQ2l0eUFkZBC2nAESEAoK",
            "Q2l0eVJlbW92ZRC3nAESFAoORG93bmxvYWRBY3RvcnMQv5wBEg4KCEFjdG9y",
            "QWRkEMGcARIRCgtBY3RvclJlbW92ZRDCnAESDwoJVHJvb3BNb3ZlEMWcARIS",
            "CgxUcm9vcEFpU3RhdGUQx5wBEg8KCVVwZGF0ZVBvcxDJnAESEgoMSGFydmVz",
            "dFN0YXJ0ENOcARIRCgtIYXJ2ZXN0U3RvcBDVnAESEQoLRG93bmxvYWRSZXMQ",
            "15wBEhAKCkZpZ2h0U3RhcnQQ3ZwBKtcDCgpST09NX1JFUExZEhEKDU1zZ1N0",
            "YXJ0UmVwbHkQABIWChBQbGF5ZXJFbnRlclJlcGx5EKKcARIUCg5FbnRlclJv",
            "b21SZXBseRCknAESFAoOTGVhdmVSb29tUmVwbHkQxpoMEhQKDlVwbG9hZE1h",
            "cFJlcGx5EKicARIWChBEb3dubG9hZE1hcFJlcGx5EKqcARIWChBEZXN0cm95",
            "Um9vbVJlcGx5EKycARIZChNEb3dubG9hZENpdGllc1JlcGx5ELacARISCgxD",
            "aXR5QWRkUmVwbHkQuJwBEhUKD0NpdHlSZW1vdmVSZXBseRC6nAESGQoTRG93",
            "bmxvYWRBY3RvcnNSZXBseRDAnAESEwoNQWN0b3JBZGRSZXBseRDCnAESFgoQ",
            "QWN0b3JSZW1vdmVSZXBseRDEnAESFAoOVHJvb3BNb3ZlUmVwbHkQxpwBEhcK",
            "EVRyb29wQWlTdGF0ZVJlcGx5EMicARIUCg5VcGRhdGVQb3NSZXBseRDKnAES",
            "FwoRSGFydmVzdFN0YXJ0UmVwbHkQ1JwBEhYKEEhhcnZlc3RTdG9wUmVwbHkQ",
            "1pwBEhYKEERvd25sb2FkUmVzUmVwbHkQ2JwBEhAKCkZpZ2h0UmVwbHkQ3pwB",
            "YgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::Protobuf.Room.ROOM), typeof(global::Protobuf.Room.ROOM_REPLY), }, null, null));
    }
    #endregion

  }
  #region Enums
  /// <summary>
  /// 客户端发送到房间
  /// </summary>
  public enum ROOM {
    /// <summary>
    /// proto3枚举的第一个成员必须是0
    /// </summary>
    [pbr::OriginalName("MsgStart")] MsgStart = 0,
    /// <summary>
    /// 心跳
    /// </summary>
    [pbr::OriginalName("HeartBeat")] HeartBeat = 20000,
    [pbr::OriginalName("PlayerEnter")] PlayerEnter = 20001,
    [pbr::OriginalName("EnterRoom")] EnterRoom = 20003,
    [pbr::OriginalName("LeaveRoom")] LeaveRoom = 20005,
    [pbr::OriginalName("UploadMap")] UploadMap = 20007,
    [pbr::OriginalName("DownloadMap")] DownloadMap = 20009,
    [pbr::OriginalName("DestroyRoom")] DestroyRoom = 20011,
    [pbr::OriginalName("DownloadCities")] DownloadCities = 20021,
    [pbr::OriginalName("CityAdd")] CityAdd = 20022,
    [pbr::OriginalName("CityRemove")] CityRemove = 20023,
    [pbr::OriginalName("DownloadActors")] DownloadActors = 20031,
    [pbr::OriginalName("ActorAdd")] ActorAdd = 20033,
    [pbr::OriginalName("ActorRemove")] ActorRemove = 20034,
    /// <summary>
    /// 部队移动,此指令已废弃,被TroopAiState取代
    /// </summary>
    [pbr::OriginalName("TroopMove")] TroopMove = 20037,
    [pbr::OriginalName("TroopAiState")] TroopAiState = 20039,
    /// <summary>
    /// 因为AI在客户端进行,所以要从客户端同步坐标到服务器
    /// </summary>
    [pbr::OriginalName("UpdatePos")] UpdatePos = 20041,
    /// <summary>
    /// 采集
    /// </summary>
    [pbr::OriginalName("HarvestStart")] HarvestStart = 20051,
    /// <summary>
    /// 停止采集
    /// </summary>
    [pbr::OriginalName("HarvestStop")] HarvestStop = 20053,
    /// <summary>
    /// 更新资源变更
    /// </summary>
    [pbr::OriginalName("DownloadRes")] DownloadRes = 20055,
    /// <summary>
    /// 战斗
    /// </summary>
    [pbr::OriginalName("FightStart")] FightStart = 20061,
  }

  /// <summary>
  /// 房间发送到客户端
  /// </summary>
  public enum ROOM_REPLY {
    /// <summary>
    /// proto3枚举的第一个成员必须是0
    /// </summary>
    [pbr::OriginalName("MsgStartReply")] MsgStartReply = 0,
    [pbr::OriginalName("PlayerEnterReply")] PlayerEnterReply = 20002,
    [pbr::OriginalName("EnterRoomReply")] EnterRoomReply = 20004,
    [pbr::OriginalName("LeaveRoomReply")] LeaveRoomReply = 200006,
    [pbr::OriginalName("UploadMapReply")] UploadMapReply = 20008,
    [pbr::OriginalName("DownloadMapReply")] DownloadMapReply = 20010,
    [pbr::OriginalName("DestroyRoomReply")] DestroyRoomReply = 20012,
    [pbr::OriginalName("DownloadCitiesReply")] DownloadCitiesReply = 20022,
    [pbr::OriginalName("CityAddReply")] CityAddReply = 20024,
    [pbr::OriginalName("CityRemoveReply")] CityRemoveReply = 20026,
    [pbr::OriginalName("DownloadActorsReply")] DownloadActorsReply = 20032,
    [pbr::OriginalName("ActorAddReply")] ActorAddReply = 20034,
    [pbr::OriginalName("ActorRemoveReply")] ActorRemoveReply = 20036,
    [pbr::OriginalName("TroopMoveReply")] TroopMoveReply = 20038,
    [pbr::OriginalName("TroopAiStateReply")] TroopAiStateReply = 20040,
    [pbr::OriginalName("UpdatePosReply")] UpdatePosReply = 20042,
    [pbr::OriginalName("HarvestStartReply")] HarvestStartReply = 20052,
    [pbr::OriginalName("HarvestStopReply")] HarvestStopReply = 20054,
    [pbr::OriginalName("DownloadResReply")] DownloadResReply = 20056,
    [pbr::OriginalName("FightReply")] FightReply = 20062,
  }

  #endregion

}

#endregion Designer generated code
