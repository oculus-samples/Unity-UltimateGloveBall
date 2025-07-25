// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.XR.Samples;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// This is to deserialize the deeplink message we receive from the Arena Destinations.
    /// </summary>
    [MetaCodeSample("UltimateGloveBall")]
    public class ArenaDeepLinkMessage
    {
        public string Region;
    }
}