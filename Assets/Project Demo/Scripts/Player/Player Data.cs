using Unity.Netcode;

public struct PlayerData : INetworkSerializable
{
    public ulong _id;
    public ushort _length;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _id);
        serializer.SerializeValue(ref _length);
    }
}
