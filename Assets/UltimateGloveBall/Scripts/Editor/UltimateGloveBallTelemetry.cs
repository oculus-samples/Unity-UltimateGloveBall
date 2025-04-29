// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UnityEditor;

namespace UltimateGloveBall.Editor
{
    /// <summary>
    /// This class helps us track the usage of this showcase
    /// </summary>
    [InitializeOnLoad]
    public static class UltimateGloveBallTelemetry
    {
        // This is the name of this showcase
        private const string PROJECT_NAME = "Unity-UltimateGloveBall";

        private const string SESSION_KEY = "OculusTelemetry-module_loaded-" + PROJECT_NAME;
        static UltimateGloveBallTelemetry() => Collect();

        private static void Collect(bool force = false)
        {
            if (SessionState.GetBool(SESSION_KEY, false) == false)
            {
                _ = OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
                _ = OVRPlugin.SendEvent("module_loaded", PROJECT_NAME, "integration");
                SessionState.SetBool(SESSION_KEY, true);
            }
        }
    }
}