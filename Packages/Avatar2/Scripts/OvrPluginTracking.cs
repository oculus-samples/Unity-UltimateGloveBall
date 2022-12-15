using System;
using System.Runtime.InteropServices;

namespace Oculus.Avatar2
{
    internal static class OvrPluginTracking
    {
        private const string LibFile = OvrAvatarManager.IsAndroidStandalone ? "ovrplugintracking" : "libovrplugintracking";

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ovrpTracking_Initialize(CAPI.LoggingDelegate loggingDelegate, IntPtr loggingContext);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ovrpTracking_Shutdown();


        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ovrpTracking_CreateHandTrackingContext(
            out CAPI.ovrAvatar2HandTrackingDataContext outContext);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrpTracking_CreateHandTrackingContext")]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ovrpTracking_CreateHandTrackingContextNative(
            out CAPI.ovrAvatar2HandTrackingDataContextNative outContext);


        public static bool Initialize(CAPI.LoggingDelegate cb, IntPtr logContext)
        {
            try
            {
                return ovrpTracking_Initialize(cb, logContext);
            }
            catch (DllNotFoundException)
            {
                OvrAvatarLog.LogWarning($"Lib {LibFile} not found");
                return false;
            }
        }

        public static void Shutdown()
        {
            try
            {
                ovrpTracking_Shutdown();
            }
            catch (DllNotFoundException)
            {

            }
        }



        private static CAPI.ovrAvatar2HandTrackingDataContext? CreateHandTrackingContext()
        {
            if (ovrpTracking_CreateHandTrackingContext(out var context))
            {
                return context;
            }

            return null;
        }

        private static CAPI.ovrAvatar2HandTrackingDataContextNative? CreateHandTrackingContextNative()
        {
            if (ovrpTracking_CreateHandTrackingContextNative(out var context))
            {
                return context;
            }

            return null;
        }

        public static IOvrAvatarHandTrackingDelegate CreateHandTrackingDelegate()
        {
            var context = CreateHandTrackingContext();
            var native = CreateHandTrackingContextNative();
            return context.HasValue && native.HasValue ? new HandTrackingDelegate(context.Value, native.Value) : null;
        }



        private class HandTrackingDelegate : IOvrAvatarHandTrackingDelegate, IOvrAvatarNativeHandDelegate
        {
            private CAPI.ovrAvatar2HandTrackingDataContext _context;
            public CAPI.ovrAvatar2HandTrackingDataContextNative NativeContext { get; }

            public HandTrackingDelegate(CAPI.ovrAvatar2HandTrackingDataContext context, CAPI.ovrAvatar2HandTrackingDataContextNative native)
            {
                _context = context;
                NativeContext = native;
            }

            public bool GetHandData(OvrAvatarTrackingHandsState handData)
            {
                if (_context.handTrackingCallback(out var native, _context.context))
                {
                    handData.FromNative(ref native);
                    return true;
                }

                return false;
            }
        }


    }
}
