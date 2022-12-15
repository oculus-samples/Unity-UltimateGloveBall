using System;
using System.Runtime.InteropServices;

namespace Oculus.Avatar2
{
    public partial class CAPI
    {

        //-----------------------------------------------------------------
        //
        // State
        //
        //

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2StreamingPlaybackState
        {
            public UInt32 numSamples; // Number of samples in the playback buffer
            public float interpolationBlendWeight; // Interpolation blend between the oldest 2 samples
            public UInt64 oldestSampleTime; // Time in microseconds of the oldest sample
            public UInt64 latestSampleTime; // Time in microseconds of the newest sample
            public UInt64 remoteTime; ///< Time in microseconds of the remote time (for snapshot playback)
            public UInt64 localTime; ///< Time in microseconds of local time (for snapshot playback)
            public UInt64 recordingPlaybackTime; ///< Time in microseconds of recordingPlayback time (for recording playback)
            [MarshalAs(UnmanagedType.U1)]
            bool poseValid; ///< Whether the playback pose is valid
        }

        //-----------------------------------------------------------------
        //
        // Record
        //
        //

        public enum ovrAvatar2StreamLOD : Int32
        {
            Full, // Full avatar state with lossless compression
            High, // Full avatar state with lossy compression
            Medium, // Partial avatar state with lossy compression
            Low, // Minimal avatar state with lossy compression
        }
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_RecordStart(ovrAvatar2EntityId entityId);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_RecordStop(ovrAvatar2EntityId entityId);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_RecordSnapshot(ovrAvatar2EntityId entityId);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_SerializeRecording(
            ovrAvatar2EntityId entityId, ovrAvatar2StreamLOD lod, IntPtr destinationPtr, out UInt64 bytes);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_GetRecordingSize(
            ovrAvatar2EntityId entityId, ovrAvatar2StreamLOD lod, out UInt64 bytes);

        //-----------------------------------------------------------------
        //
        // Playback
        //
        //

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_PlaybackStart(ovrAvatar2EntityId entityId);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_PlaybackStop(ovrAvatar2EntityId entityId);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_DeserializeRecording(
            ovrAvatar2EntityId entityId, IntPtr source, UInt64 bytes);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_SetPlaybackTimeDelay(
            ovrAvatar2EntityId entityId, float delaySeconds);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_GetPlaybackState(
            ovrAvatar2EntityId entityId, out ovrAvatar2StreamingPlaybackState playbackState);
    }
}
