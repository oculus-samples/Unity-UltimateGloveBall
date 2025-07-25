// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections.Generic;
using Meta.Utilities;
using Meta.XR.Samples;
using UnityEngine;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// Manages the icons that can be used in game. This singleton will be on a gameObject in the startup scene
    /// with the list of icons sku mapped to the icon sprite. This makes it easy to map the sku to the sprite
    /// through the game.
    /// </summary>
    [MetaCodeSample("UltimateGloveBall")]
    public class UserIconManager : Singleton<UserIconManager>
    {
        [Serializable]
        public struct IconData
        {
            public string SKU;
            public Sprite Icon;
        }

        [SerializeField] private IconData[] m_iconDataArray;

        private Dictionary<string, Sprite> m_skuToIcon = new();

        private List<string> m_allSkus = new();

        public string[] AllSkus => m_allSkus.ToArray();

        public Sprite GetIconForSku(string sku)
        {
            return m_skuToIcon.TryGetValue(sku, out var icon) ? icon : null;
        }

        protected override void InternalAwake()
        {
            base.InternalAwake();

            foreach (var iconData in m_iconDataArray)
            {
                m_skuToIcon[iconData.SKU] = iconData.Icon;
                m_allSkus.Add(iconData.SKU);
            }
        }
    }
}