// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: lobby.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Protobuf.Lobby {

  /// <summary>Holder for reflection information generated from lobby.proto</summary>
  public static partial class LobbyReflection {

    #region Descriptor
    /// <summary>File descriptor for lobby.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static LobbyReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cgtsb2JieS5wcm90bxIOUHJvdG9idWYubG9iYnkiLwoLUGxheWVyRW50ZXIS",
            "DwoHQWNjb3VudBgBIAEoCRIPCgdUb2tlbklkGAIgASgDIh8KEFBsYXllckVu",
            "dGVyUmVwbHkSCwoDUmV0GAEgASgIIh4KC0NoYXRNZXNzYWdlEg8KB01lc3Nh",
            "Z2UYASABKAkiDQoLQXNrUm9vbUxpc3QiaQoIUm9vbUluZm8SDAoETmFtZRgB",
            "IAEoCRIOCgZSb29tSWQYAiABKAMSEgoKQ3JlYXRlVGltZRgDIAEoAxITCgtQ",
            "bGF5ZXJDb3VudBgEIAEoBRIWCg5NYXhQbGF5ZXJDb3VudBgFIAEoBSI7ChBB",
            "c2tSb29tTGlzdFJlcGx5EicKBVJvb21zGAEgAygLMhguUHJvdG9idWYubG9i",
            "YnkuUm9vbUluZm9iBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Protobuf.Lobby.PlayerEnter), global::Protobuf.Lobby.PlayerEnter.Parser, new[]{ "Account", "TokenId" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Protobuf.Lobby.PlayerEnterReply), global::Protobuf.Lobby.PlayerEnterReply.Parser, new[]{ "Ret" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Protobuf.Lobby.ChatMessage), global::Protobuf.Lobby.ChatMessage.Parser, new[]{ "Message" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Protobuf.Lobby.AskRoomList), global::Protobuf.Lobby.AskRoomList.Parser, null, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Protobuf.Lobby.RoomInfo), global::Protobuf.Lobby.RoomInfo.Parser, new[]{ "Name", "RoomId", "CreateTime", "PlayerCount", "MaxPlayerCount" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Protobuf.Lobby.AskRoomListReply), global::Protobuf.Lobby.AskRoomListReply.Parser, new[]{ "Rooms" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class PlayerEnter : pb::IMessage<PlayerEnter> {
    private static readonly pb::MessageParser<PlayerEnter> _parser = new pb::MessageParser<PlayerEnter>(() => new PlayerEnter());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<PlayerEnter> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Protobuf.Lobby.LobbyReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public PlayerEnter() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public PlayerEnter(PlayerEnter other) : this() {
      account_ = other.account_;
      tokenId_ = other.tokenId_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public PlayerEnter Clone() {
      return new PlayerEnter(this);
    }

    /// <summary>Field number for the "Account" field.</summary>
    public const int AccountFieldNumber = 1;
    private string account_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Account {
      get { return account_; }
      set {
        account_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "TokenId" field.</summary>
    public const int TokenIdFieldNumber = 2;
    private long tokenId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long TokenId {
      get { return tokenId_; }
      set {
        tokenId_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as PlayerEnter);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(PlayerEnter other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Account != other.Account) return false;
      if (TokenId != other.TokenId) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Account.Length != 0) hash ^= Account.GetHashCode();
      if (TokenId != 0L) hash ^= TokenId.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Account.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Account);
      }
      if (TokenId != 0L) {
        output.WriteRawTag(16);
        output.WriteInt64(TokenId);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Account.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Account);
      }
      if (TokenId != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(TokenId);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(PlayerEnter other) {
      if (other == null) {
        return;
      }
      if (other.Account.Length != 0) {
        Account = other.Account;
      }
      if (other.TokenId != 0L) {
        TokenId = other.TokenId;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            Account = input.ReadString();
            break;
          }
          case 16: {
            TokenId = input.ReadInt64();
            break;
          }
        }
      }
    }

  }

  public sealed partial class PlayerEnterReply : pb::IMessage<PlayerEnterReply> {
    private static readonly pb::MessageParser<PlayerEnterReply> _parser = new pb::MessageParser<PlayerEnterReply>(() => new PlayerEnterReply());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<PlayerEnterReply> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Protobuf.Lobby.LobbyReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public PlayerEnterReply() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public PlayerEnterReply(PlayerEnterReply other) : this() {
      ret_ = other.ret_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public PlayerEnterReply Clone() {
      return new PlayerEnterReply(this);
    }

    /// <summary>Field number for the "Ret" field.</summary>
    public const int RetFieldNumber = 1;
    private bool ret_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Ret {
      get { return ret_; }
      set {
        ret_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as PlayerEnterReply);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(PlayerEnterReply other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Ret != other.Ret) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Ret != false) hash ^= Ret.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Ret != false) {
        output.WriteRawTag(8);
        output.WriteBool(Ret);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Ret != false) {
        size += 1 + 1;
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(PlayerEnterReply other) {
      if (other == null) {
        return;
      }
      if (other.Ret != false) {
        Ret = other.Ret;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            Ret = input.ReadBool();
            break;
          }
        }
      }
    }

  }

  public sealed partial class ChatMessage : pb::IMessage<ChatMessage> {
    private static readonly pb::MessageParser<ChatMessage> _parser = new pb::MessageParser<ChatMessage>(() => new ChatMessage());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ChatMessage> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Protobuf.Lobby.LobbyReflection.Descriptor.MessageTypes[2]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ChatMessage() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ChatMessage(ChatMessage other) : this() {
      message_ = other.message_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ChatMessage Clone() {
      return new ChatMessage(this);
    }

    /// <summary>Field number for the "Message" field.</summary>
    public const int MessageFieldNumber = 1;
    private string message_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Message {
      get { return message_; }
      set {
        message_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ChatMessage);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ChatMessage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Message != other.Message) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Message.Length != 0) hash ^= Message.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Message.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Message);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Message.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Message);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ChatMessage other) {
      if (other == null) {
        return;
      }
      if (other.Message.Length != 0) {
        Message = other.Message;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            Message = input.ReadString();
            break;
          }
        }
      }
    }

  }

  public sealed partial class AskRoomList : pb::IMessage<AskRoomList> {
    private static readonly pb::MessageParser<AskRoomList> _parser = new pb::MessageParser<AskRoomList>(() => new AskRoomList());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<AskRoomList> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Protobuf.Lobby.LobbyReflection.Descriptor.MessageTypes[3]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public AskRoomList() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public AskRoomList(AskRoomList other) : this() {
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public AskRoomList Clone() {
      return new AskRoomList(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as AskRoomList);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(AskRoomList other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(AskRoomList other) {
      if (other == null) {
        return;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
        }
      }
    }

  }

  public sealed partial class RoomInfo : pb::IMessage<RoomInfo> {
    private static readonly pb::MessageParser<RoomInfo> _parser = new pb::MessageParser<RoomInfo>(() => new RoomInfo());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<RoomInfo> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Protobuf.Lobby.LobbyReflection.Descriptor.MessageTypes[4]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RoomInfo() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RoomInfo(RoomInfo other) : this() {
      name_ = other.name_;
      roomId_ = other.roomId_;
      createTime_ = other.createTime_;
      playerCount_ = other.playerCount_;
      maxPlayerCount_ = other.maxPlayerCount_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RoomInfo Clone() {
      return new RoomInfo(this);
    }

    /// <summary>Field number for the "Name" field.</summary>
    public const int NameFieldNumber = 1;
    private string name_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Name {
      get { return name_; }
      set {
        name_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "RoomId" field.</summary>
    public const int RoomIdFieldNumber = 2;
    private long roomId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long RoomId {
      get { return roomId_; }
      set {
        roomId_ = value;
      }
    }

    /// <summary>Field number for the "CreateTime" field.</summary>
    public const int CreateTimeFieldNumber = 3;
    private long createTime_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long CreateTime {
      get { return createTime_; }
      set {
        createTime_ = value;
      }
    }

    /// <summary>Field number for the "PlayerCount" field.</summary>
    public const int PlayerCountFieldNumber = 4;
    private int playerCount_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int PlayerCount {
      get { return playerCount_; }
      set {
        playerCount_ = value;
      }
    }

    /// <summary>Field number for the "MaxPlayerCount" field.</summary>
    public const int MaxPlayerCountFieldNumber = 5;
    private int maxPlayerCount_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int MaxPlayerCount {
      get { return maxPlayerCount_; }
      set {
        maxPlayerCount_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as RoomInfo);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(RoomInfo other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Name != other.Name) return false;
      if (RoomId != other.RoomId) return false;
      if (CreateTime != other.CreateTime) return false;
      if (PlayerCount != other.PlayerCount) return false;
      if (MaxPlayerCount != other.MaxPlayerCount) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Name.Length != 0) hash ^= Name.GetHashCode();
      if (RoomId != 0L) hash ^= RoomId.GetHashCode();
      if (CreateTime != 0L) hash ^= CreateTime.GetHashCode();
      if (PlayerCount != 0) hash ^= PlayerCount.GetHashCode();
      if (MaxPlayerCount != 0) hash ^= MaxPlayerCount.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Name.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Name);
      }
      if (RoomId != 0L) {
        output.WriteRawTag(16);
        output.WriteInt64(RoomId);
      }
      if (CreateTime != 0L) {
        output.WriteRawTag(24);
        output.WriteInt64(CreateTime);
      }
      if (PlayerCount != 0) {
        output.WriteRawTag(32);
        output.WriteInt32(PlayerCount);
      }
      if (MaxPlayerCount != 0) {
        output.WriteRawTag(40);
        output.WriteInt32(MaxPlayerCount);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Name.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Name);
      }
      if (RoomId != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(RoomId);
      }
      if (CreateTime != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(CreateTime);
      }
      if (PlayerCount != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(PlayerCount);
      }
      if (MaxPlayerCount != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(MaxPlayerCount);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(RoomInfo other) {
      if (other == null) {
        return;
      }
      if (other.Name.Length != 0) {
        Name = other.Name;
      }
      if (other.RoomId != 0L) {
        RoomId = other.RoomId;
      }
      if (other.CreateTime != 0L) {
        CreateTime = other.CreateTime;
      }
      if (other.PlayerCount != 0) {
        PlayerCount = other.PlayerCount;
      }
      if (other.MaxPlayerCount != 0) {
        MaxPlayerCount = other.MaxPlayerCount;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            Name = input.ReadString();
            break;
          }
          case 16: {
            RoomId = input.ReadInt64();
            break;
          }
          case 24: {
            CreateTime = input.ReadInt64();
            break;
          }
          case 32: {
            PlayerCount = input.ReadInt32();
            break;
          }
          case 40: {
            MaxPlayerCount = input.ReadInt32();
            break;
          }
        }
      }
    }

  }

  public sealed partial class AskRoomListReply : pb::IMessage<AskRoomListReply> {
    private static readonly pb::MessageParser<AskRoomListReply> _parser = new pb::MessageParser<AskRoomListReply>(() => new AskRoomListReply());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<AskRoomListReply> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Protobuf.Lobby.LobbyReflection.Descriptor.MessageTypes[5]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public AskRoomListReply() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public AskRoomListReply(AskRoomListReply other) : this() {
      rooms_ = other.rooms_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public AskRoomListReply Clone() {
      return new AskRoomListReply(this);
    }

    /// <summary>Field number for the "Rooms" field.</summary>
    public const int RoomsFieldNumber = 1;
    private static readonly pb::FieldCodec<global::Protobuf.Lobby.RoomInfo> _repeated_rooms_codec
        = pb::FieldCodec.ForMessage(10, global::Protobuf.Lobby.RoomInfo.Parser);
    private readonly pbc::RepeatedField<global::Protobuf.Lobby.RoomInfo> rooms_ = new pbc::RepeatedField<global::Protobuf.Lobby.RoomInfo>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::Protobuf.Lobby.RoomInfo> Rooms {
      get { return rooms_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as AskRoomListReply);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(AskRoomListReply other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if(!rooms_.Equals(other.rooms_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= rooms_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      rooms_.WriteTo(output, _repeated_rooms_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      size += rooms_.CalculateSize(_repeated_rooms_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(AskRoomListReply other) {
      if (other == null) {
        return;
      }
      rooms_.Add(other.rooms_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            rooms_.AddEntriesFrom(input, _repeated_rooms_codec);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
