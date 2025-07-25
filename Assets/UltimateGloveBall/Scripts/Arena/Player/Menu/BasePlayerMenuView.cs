// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.XR.Samples;
using UnityEngine;

namespace UltimateGloveBall.Arena.Player.Menu
{
    /// <summary>
    /// Base Menu view for in game menu.
    /// </summary>
    [MetaCodeSample("UltimateGloveBall")]
    public abstract class BasePlayerMenuView : MonoBehaviour
    {
        public virtual void OnUpdate() { }
    }
}