using System;
using Oculus.Avatar2;
using Unity.Collections;
using UnityEngine;
using StreamLOD = Oculus.Avatar2.OvrAvatarEntity.StreamLOD;

/// <summary>
/// This class is an example of how to use the Streaming functions of the avatar to send and receive data over the network
/// </summary>
public class SampleRemoteLoopbackManager : RemoteLoopbackManagerBase
{
    class SamplePacketData : PacketData, IDisposable
    {
        public NativeArray<byte> data;
        public UInt32 dataByteCount;

        ~SamplePacketData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (data.IsCreated)
            {
                data.Dispose();
            }
            data = default;
        }
    };

    protected override PacketData GeneratePacketData(OvrAvatarEntity entity, StreamLOD lod)
    {
        SamplePacketData packet = FetchPacketFromPool() as SamplePacketData ?? new SamplePacketData();
        packet.Retain();

        packet.dataByteCount = entity.RecordStreamData_AutoBuffer(lod, ref packet.data);
        Debug.Assert(packet.dataByteCount > 0);

        return packet;
    }

    protected override void ProcessPacketData(OvrAvatarEntity entity, PacketData packet)
    {
        var samplePacket = packet as SamplePacketData;
        Debug.Assert(samplePacket != null, "Invalid packet format");

        var dataSlice = samplePacket.data.Slice(0, (int)samplePacket.dataByteCount);
        entity.ApplyStreamData(in dataSlice);
    }
}
