using System;
using System.Runtime.InteropServices;

namespace Oculus.Avatar2
{
    public partial class CAPI
    {
        // Native libovravatar2 version this integration was built against
        private static FBVersionNumber TargetLibVersion;

        /* Debug string indicating the native libovravatar2 version this manager is targeting */
        public static string TargetAvatarLibVersionString
            => $"{TargetLibProductVersion}.{TargetLibMajorVersion}.{TargetLibMinorVersion}.{TargetLibPatchVersion}";

        // Native libovravatar2 version this integration was built against
        private static int TargetLibProductVersion = -1;
        private static int TargetLibMajorVersion = -1;
        private static int TargetLibMinorVersion = -1;
        private static int TargetLibPatchVersion = -1;

        internal const string LibFile =
        OvrAvatarManager.IsAndroidStandalone ? "ovravatar2" : "libovravatar2";

        // TODO: Add "INITIALIZED" frame count and assert in update when update called w/out init
        private const uint AVATAR_UPDATE_UNINITIALIZED_FRAME_COUNT = 0;

        //-----------------------------------------------------------------
        //
        // Forwards
        //
        //

        public const float DefaultAvatarColorRed = (30 / 255.0f);
        public const float DefaultAvatarColorGreen = (157 / 255.0f);
        public const float DefaultAvatarColorBlue = (255 / 255.0f);

        // Network thread update frequency.
        // 256 hz is updating every 0.004ms which we think is optimal in most use cases
        public const uint DefaultNetworkWorkerUpdateFrequency = 256;

        //-----------------------------------------------------------------
        //
        // Callback functions
        //
        //

        // Avatar Logging Level
        // Matches the Android Log Levels
        public enum ovrAvatar2LogLevel : Int32
        {
            Unknown = 0,
            Default,
            Verbose,
            Debug,
            Info,
            Warn,
            Error,
            Fatal,
            Silent,
        };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void LoggingDelegate(ovrAvatar2LogLevel prio, string msg, IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MemAllocDelegate(UInt64 byteCount, IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MemFreeDelegate(IntPtr buffer, IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ResourceDelegate(in ovrAvatar2Asset_Resource resource, IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RequestDelegate(ovrAvatar2RequestId requestId, ovrAvatar2Result status, IntPtr userContext);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate IntPtr FileOpenDelegate(IntPtr context, string filename);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        // TODO: Change return type, bool is an unreliable return type for an unmanaged function pointer
        public delegate bool FileReadDelegate(IntPtr context, IntPtr fileHandle, out IntPtr fileDataPtr, out UInt64 fileSize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        // TODO: Change return type, bool is an unreliable return type for an unmanaged function pointer
        public delegate bool FileCloseDelegate(IntPtr context, IntPtr fileHandle);

        //-----------------------------------------------------------------
        //
        // Initialization
        //
        //

        public enum ovrAvatar2Platform : Int32
        {
            PC = 0,
            Quest = 1,
            Quest2 = 2
        }

        [Flags]
        public enum ovrAvatar2InitializeFlags : Int32
        {
            ///< When set, ovrAvatar2_Shutdown() may return ovrAvatar2Result_MemoryLeak to indicate a
            ///< detected memory leak
            CheckMemoryLeaks = 1 << 0,
            UseDefaultImage = 1 << 1,
            // When set, skinningOrigin in ovrAvatar2PrimitiveRenderState is set with the skinning origin
            // and the skinning matrices root will be the skinning Origin
            EnableSkinningOrigin = 1 << 3,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        // This needs to be the csharp equivalent of ovrAvatar2InitializeInfo in Avatar.h
        public struct ovrAvatar2InitializeInfo
        {
            public FBVersionNumber versionNumber;
            public string clientVersion; // client version string for the user app (ex. "Unity v2019.2")
            public ovrAvatar2Platform platform;

            public ovrAvatar2InitializeFlags flags;

            public ovrAvatar2LogLevel loggingLevel; // logging threshold to control what is logged
            public LoggingDelegate loggingCallback; ///< override logging with user defined function. This
                                                    ///< function may be called from multiple threads
            public IntPtr loggingContext;
            public MemAllocDelegate memAllocCallback; // override memory management
            public MemFreeDelegate memFreeCallback; // overrride memory management
            public IntPtr memoryContext; // user context for memory callbacks

            public RequestDelegate requestCallback;

            public FileOpenDelegate fileOpenCallback; // used to open a file
            public FileReadDelegate fileReadCallback; // used to load file contents
            public FileCloseDelegate fileCloseCallback; // used to close a file
            public IntPtr fileReaderContext; // context for above file callbacks

            public ResourceDelegate resourceLoadCallback; // resource load callback
            public IntPtr resourceLoadContext;

            public string fallbackPathToOvrAvatar2AssetsZip;

            public UInt32 numWorkerThreads;

            public Int64 maxNetworkRequests;
            public Int64 maxNetworkSendBytesPerSecond;
            public Int64 maxNetworkReceiveBytesPerSecond;

            public ovrAvatar2Vector3f defaultModelColor;
            ///
            /// Defines the right axis in the game engine coordinate system.
            /// For the Avatar SDK and Unity, this is (1, 0, 0)
            ///
            public ovrAvatar2Vector3f clientSpaceRightAxis;

            ///
            /// Defines the up axis in the game engine coordinate system.
            /// For the Avatar SDK and Unity, this is (0, 1, 0)
            ///
            public ovrAvatar2Vector3f clientSpaceUpAxis;

            ///
            /// Defines the forward axis in the game engine coordinate system.
            /// For the Avatar SDK this is (0, 0, -1). For Unity, it is (0, 0, 1)
            ///
            public ovrAvatar2Vector3f clientSpaceForwardAxis;

            public string clientName;

            // 0 for running network update on the main thread
            public UInt32 networkWorkerUpdateFrequency;

            private IntPtr reserved1;       // Reserved  / internal use only
            private UInt32 reserved2;       // Reserved  / internal use only
        }

        internal static ovrAvatar2InitializeInfo OvrAvatar_DefaultInitInfo(string clientVersion, ovrAvatar2Platform platform)
        {
            // TODO: T86822707, This should be a method in the loaderShim/ovravatar2 lib
            // return ovrAvatar2_DefaultInitInfo(clientVersion, platform);

            // Copied from //arvr/libraries/avatar/Libraries/api/include/OvrAvatar/Avatar.h
            ovrAvatar2InitializeInfo info = default;
            info.versionNumber = SDKVersionInfo.CurrentVersion();
            info.flags = ovrAvatar2InitializeFlags.UseDefaultImage;
            info.clientVersion = clientVersion;
            info.platform = platform;
            info.loggingLevel = ovrAvatar2LogLevel.Warn;
            info.numWorkerThreads = 1;
            info.maxNetworkRequests = -1;
            info.maxNetworkSendBytesPerSecond = -1;
            info.maxNetworkReceiveBytesPerSecond = -1;

            // Default color of the default/blank avatar
            info.defaultModelColor.x = DefaultAvatarColorRed;
            info.defaultModelColor.y = DefaultAvatarColorGreen;
            info.defaultModelColor.z = DefaultAvatarColorBlue;
#if OVR_AVATAR_ENABLE_CLIENT_XFORM
            info.clientSpaceRightAxis = UnityEngine.Vector3.right;
            info.clientSpaceUpAxis = UnityEngine.Vector3.up;
            info.clientSpaceForwardAxis = UnityEngine.Vector3.forward;
#else
            info.clientSpaceRightAxis = UnityEngine.Vector3.right;
            info.clientSpaceUpAxis = UnityEngine.Vector3.up;
            info.clientSpaceForwardAxis = -UnityEngine.Vector3.forward;
#endif

            info.clientName = "unknown_unity";

            info.networkWorkerUpdateFrequency = DefaultNetworkWorkerUpdateFrequency;

            return info;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern ovrAvatar2Result ovrAvatar2_Initialize(in ovrAvatar2InitializeInfo infoPtr);

        public static bool OvrAvatar_Initialize(in ovrAvatar2InitializeInfo infoPtr)
        {
            if (ovrAvatar2_Initialize(in infoPtr)
                .EnsureSuccess("ovrAvatar2_Initialize"))
            {
                TargetLibVersion = infoPtr.versionNumber;
                return true;
            }
            return false;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern ovrAvatar2Result ovrAvatar2_Shutdown();

        internal static bool OvrAvatar_Shutdown()
        {
            OvrAvatar_Shutdown(out var result);
            return result.EnsureSuccess("ovrAvatar2_Shutdown");
        }
        internal static void OvrAvatar_Shutdown(out ovrAvatar2Result result)
        {
            avatarUpdateCount = AVATAR_UPDATE_UNINITIALIZED_FRAME_COUNT;
            result = ovrAvatar2_Shutdown();
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ovrAvatar2Result ovrAvatar2_UpdateAccessToken(string token);

        /// Update the network settings
        /// \param maxNetworkSendBytesPerSecond -1 for no limit
        /// \param maxNetworkReceiveBytesPerSecond -1 for no limit
        ///
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ovrAvatar2Result ovrAvatar2_UpdateNetworkSettings(
            Int64 maxNetworkSendBytesPerSecond, Int64 maxNetworkReceiveBytesPerSecond, Int64 maxNetworkRequests);

        //-----------------------------------------------------------------
        //
        // Work
        //
        //

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern ovrAvatar2Result ovrAvatar2_Update(float deltaSeconds);

        // TODO: Could this just be 0.0f?
        private const float AVATAR_UPDATE_SMALL_STEP = 0.1f;
        internal static uint avatarUpdateCount { get; private set; } = AVATAR_UPDATE_UNINITIALIZED_FRAME_COUNT;
        internal static bool OvrAvatar_Update(float deltaSeconds = AVATAR_UPDATE_SMALL_STEP)
        {
            var result = ovrAvatar2_Update(deltaSeconds);
            if (result.EnsureSuccess("ovrAvatar2_Update"))
            {
                ++avatarUpdateCount;
                return true;
            }
            return false;
        }

        /// Run a single task from the avatar task system.
        /// Return ovrAvatar2Result_Success upon completion after running a task.
        /// Return ovrAvatar2Result_NotFound when no tasks are currently in the queue.
        /// Return ovrAvatar2Result_Unsupported when library is configured to use worker threads.
        ///
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ovrAvatar2Result ovrAvatar2_RunTask();

        //-----------------------------------------------------------------
        //
        // Query
        //
        //

        /// Query to see if a user has an avatar
        /// ovrAvatar2_RequestCallback is called when the request is fulfilled
        /// Request status:
        ///   ovrAvatar2Result_Success - request succeeded
        ///   ovrAvatar2Result_Unknown - error while querying user avatar status
        /// ovrAvatar2_GetRequestBool() result
        ///   true - user has an avatar
        ///   false - user does not have an avatar
        ///
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern ovrAvatar2Result ovrAvatar2_HasAvatar(
            UInt64 userId, out ovrAvatar2RequestId requestId, IntPtr userContext);

        internal static bool OvrAvatar_HasAvatar(
            UInt64 userId, out ovrAvatar2RequestId requestId, IntPtr userContext)
        {
            var result = ovrAvatar2_HasAvatar(userId, out requestId, userContext);
            if (result.EnsureSuccess("ovrAvatar2_HasAvatar"))
            {
                return true;
            }
            requestId = ovrAvatar2RequestId.Invalid;
            return false;
        }

        /// Query to see if an entity's avatar has changed
        /// ovrAvatar2_RequestCallback is called when the request is fulfilled
        /// Request status:
        ///   ovrAvatar2Result_Success - request succeeded
        ///   ovrAvatar2Result_Unknown - error while querying user avatar status
        ///   ovrAvatar2Result_InvalidEntity - entity is no longer valid
        /// ovrAvatar2_GetRequestBool() result
        ///   true - user avatar has changed
        ///   false - user avatar has not changed
        ///
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern ovrAvatar2Result ovrAvatar2_HasAvatarChanged(
            ovrAvatar2EntityId entityId, out ovrAvatar2RequestId requestId, IntPtr userContext);

        internal static bool OvrAvatar2_HasAvatarChanged(
            ovrAvatar2EntityId entityId, out ovrAvatar2RequestId requestId, IntPtr userContext)
        {
            var result = ovrAvatar2_HasAvatarChanged(entityId, out requestId, userContext);
            if (result.EnsureSuccess("ovrAvatar2_HasAvatarChanged"))
            {
                return true;
            }
            requestId = ovrAvatar2RequestId.Invalid;
            return false;
        }


        /// Get the result of a reqeust
        /// Should be called in ovrAvatar2_RequestCallback
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern ovrAvatar2Result ovrAvatar2_GetRequestBool(
            ovrAvatar2RequestId requestId, [MarshalAs(UnmanagedType.U1)] out bool result);

        /* Query result of ovrAvatar2RequestId from `RequestCallback` */
        internal static bool OvrAvatar_GetRequestBool(
            ovrAvatar2RequestId requestId, out bool result)
        {
            if (ovrAvatar2_GetRequestBool(requestId, out result)
                .EnsureSuccess("ovrAvatar2_GetRequestBool"))
            {
                return true;
            }
            result = false;
            return false;
        }


        //-----------------------------------------------------------------
        //
        // Asset Sources
        //
        //

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ovrAvatar2Result ovrAvatar2_AddZipSourceFile(string filename);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ovrAvatar2Result ovrAvatar2_RemoveZipSource(string filename);


        //-----------------------------------------------------------------
        //
        // Stats
        //
        //

        // Avatar memory statistics

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2MemoryStats
        {
            public UInt64 currBytesUsed;
            public UInt64 currAllocationCount;
            public UInt64 maxBytesUsed;
            public UInt64 maxAllocationCount;
            public UInt64 totalBytesUsed;
            public UInt64 totalAllocationCount;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatar2_QueryMemoryStats(out ovrAvatar2MemoryStats stats,
            UInt32 statsStructSize);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ovrAvatar2NetworkStats
        {
            public UInt64 downloadTotalBytes;
            public UInt64 downloadSpeed;
            public UInt64 totalRequests;
            public UInt64 activeRequests;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern UInt32 ovrAvatar2_QueryNetworkStats(out ovrAvatar2NetworkStats stats, UInt32 statsStructSize);

        /// Avatar task statistics
        ///

        public const int TaskHistogramSize = 32;

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ovrAvatar2TaskStats
        {
            // UInt32 fixed size array of 32 items
            public fixed UInt32 histogram[TaskHistogramSize];
            public UInt32 pending;
        }

        /// Update the given stats struct with the current task statistics
        /// \param pointer to a stats structure to update
        /// \param size of the stats structure to update
        /// \returns number of bytes in the stats structure which were updated
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatar2_QueryTaskStats(out ovrAvatar2TaskStats stats, UInt32 statsStructSize);


        //-----------------------------------------------------------------
        //
        // Misc
        //
        //

        /// <summary>
        /// Get the string representation of an ovrAvatar2Result code
        /// </summary>
        /// <param name="result">The return code you want the string for</param>
        /// <param name="buffer">The buffer to return the string in</param>
        /// <param name="size">The size of the buffer</param>
        /// <returns>Success unless the result povided is out of range (BadParameter), or the buffer is null
        /// or too small (BufferTooSmall)</returns>
        ///
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe ovrAvatar2Result ovrAvatar2_GetResultString(ovrAvatar2Result result, char* buffer, UInt32* size);

        /// Get the avatar API version string
        /// \param versionBuffer string to populate with the version string
        /// \param bufferSize length of the version buffer
        ///
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern unsafe ovrAvatar2Result ovrAvatar2_GetVersionString(byte* versionBuffer, UInt32 bufferSize);

        internal static string OvrAvatar_GetVersionString()
        {
            unsafe
            {
                const int bufferSize = 1024;
                var versionBuffer = stackalloc byte[bufferSize];
                var result = ovrAvatar2_GetVersionString(versionBuffer, bufferSize);
                if (result.EnsureSuccess("ovrAvatar2_GetVersionString"))
                {
                    return Marshal.PtrToStringAnsi((IntPtr)versionBuffer);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Enables the link from the native runtime to Avatar developer tools.
        /// </summary>
        /// <returns>Returns success</returns>
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_EnableDevToolsLink();

        /// <summary>
        /// Disables the link from the native runtime to Avatar developer tools.
        /// </summary>
        /// <returns>Returns success</returns>
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_DisableDevToolsLink();
    }
}
