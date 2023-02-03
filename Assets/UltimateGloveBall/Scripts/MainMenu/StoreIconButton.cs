// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateGloveBall.MainMenu
{
    /// <summary>
    /// This button shows the icons that can be purchased or selected. It handles the internal state of the button
    /// and expose key data to be used in the <see cref="StoreMenuController"/>.
    /// </summary>
    public class StoreIconButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_priceText;
        [SerializeField] private TMP_Text m_nameText;
        [SerializeField] private Image m_iconImg;
        [SerializeField] private Image m_bgImg;

        private Color m_baseColor;
        private Action<StoreIconButton> m_onClickedCallback;
        public string SKU { get; private set; }

        public bool Owned { get; private set; }

        private void Awake()
        {
            m_baseColor = m_bgImg.color;
        }

        public void Setup(string sku, string name, string price, Sprite icon, bool purchased, Action<StoreIconButton> onClicked)
        {
            SKU = sku;
            m_nameText.text = name;
            Owned = purchased;
            m_priceText.text = purchased ? "Owned" : price.Contains("0.00") ? "Free" : price;
            m_iconImg.sprite = icon;
            m_iconImg.gameObject.SetActive(icon != null);

            m_onClickedCallback = onClicked;
        }

        public void OnPurchased()
        {
            Owned = true;
            m_priceText.text = "Owned";
        }

        public void Select()
        {
            m_bgImg.color = Color.cyan;
        }

        public void Deselect()
        {
            m_bgImg.color = m_baseColor;
        }

        public void OnClick()
        {
            m_onClickedCallback?.Invoke(this);
        }
    }
}