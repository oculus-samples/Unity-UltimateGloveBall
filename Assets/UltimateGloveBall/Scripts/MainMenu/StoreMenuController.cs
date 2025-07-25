// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections.Generic;
using Meta.XR.Samples;
using TMPro;
using UltimateGloveBall.App;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateGloveBall.MainMenu
{
    /// <summary>
    /// This menu controller presents the Store were we can buy items. It handles showing the icons to be purchased and
    /// selected. It also handles the purchase flow.
    /// </summary>
    [MetaCodeSample("UltimateGloveBall")]
    public class StoreMenuController : BaseMenuController
    {
        [SerializeField] private StoreIconButton m_storeIconButtonPrefab;
        [SerializeField] private GameObject m_iconSelectionView;
        [SerializeField] private Transform m_grid;
        [SerializeField] private GameObject m_backButton;
        [SerializeField] private MainMenuController m_menuController;
        [Header("Purchase Flow")]
        [SerializeField] private GameObject m_purchaseFlowRoot;
        [SerializeField] private TMP_Text m_purchaseName;
        [SerializeField] private Image m_purchaseImage;
        [SerializeField] private TMP_Text m_purchasePrice;
        [SerializeField] private GameObject m_processingMessage;

        [Header("Cats")]
        [SerializeField] private TMP_Text m_catCountText;
        [SerializeField] private Button m_catBuyButton;

        private StoreIconButton m_selectedButton;

        private Dictionary<string, StoreIconButton> m_skuToButton = new();

        private string m_skuToPurchase;
        private void Start()
        {
            m_iconSelectionView.gameObject.SetActive(true);
            m_purchaseFlowRoot.SetActive(false);
            m_processingMessage.SetActive(false);
            SetupIcons();
            UpdateCatPurchaseState();
        }

        private void OnEnable()
        {
            UpdateCatPurchaseState();
        }

        private void UpdateCatPurchaseState()
        {
            var catCount = GameSettings.Instance.OwnedCatsCount;
            m_catCountText.text = catCount.ToString();
            m_catBuyButton.gameObject.SetActive(catCount < 3);
        }

        private void SetupIcons()
        {
            var noneIconButton = Instantiate(m_storeIconButtonPrefab, m_grid);
            noneIconButton.Setup(null, "None", null, null, true, OnIconClicked);
            var iap = IAPManager.Instance;
            foreach (var sku in iap.GetProductSkusForCategory(ProductCategories.ICONS))
            {
                var product = iap.GetProduct(sku);
                if (product != null)
                {
                    var iconButton = Instantiate(m_storeIconButtonPrefab, m_grid);
                    iconButton.Setup(sku, product.Name, product.FormattedPrice, UserIconManager.Instance.GetIconForSku(sku),
                        iap.IsPurchased(sku), OnIconClicked);
                    m_skuToButton[sku] = iconButton;
                    if (sku == GameSettings.Instance.SelectedUserIconSku)
                    {
                        SelectButton(iconButton);
                    }
                }
            }

            if (m_selectedButton == null)
            {
                SelectButton(noneIconButton);
            }
        }

        private void OnIconClicked(StoreIconButton button)
        {
            if (button.Owned)
            {
                SelectButton(button);
            }
            else
            {
                ShowPurchaseFlow(button.SKU);
            }
        }

        private void ShowPurchaseFlow(string sku)
        {
            m_iconSelectionView.gameObject.SetActive(false);
            m_purchaseFlowRoot.SetActive(true);
            m_backButton.SetActive(false);

            var iap = IAPManager.Instance;
            m_skuToPurchase = sku;
            var product = iap.GetProduct(sku);
            m_purchaseName.text = product.Name;
            m_purchaseImage.sprite = UserIconManager.Instance.GetIconForSku(sku);
            var price = product.FormattedPrice;
            m_purchasePrice.text = price.Contains("0.00") ? "Free" : price;
        }

        public void OnCancelPurchaseFlowClicked()
        {
            m_iconSelectionView.gameObject.SetActive(true);
            m_purchaseFlowRoot.SetActive(false);
            m_backButton.SetActive(true);
            m_skuToPurchase = null;
        }

        public void OnBuyClicked()
        {
            m_purchaseFlowRoot.SetActive(false);
            m_processingMessage.SetActive(true);
            IAPManager.Instance.Purchase(m_skuToPurchase, OnPurchaseFlowCompleted);
        }

        public void OnBuyCatClicked()
        {
            m_iconSelectionView.SetActive(false);
            m_processingMessage.SetActive(true);
            m_backButton.SetActive(false);
            if (IAPManager.Instance.IsPurchased(ProductCategories.CAT))
            {
                // if something happened and we already had purchased it, but not used it
                // we consume the purchase
                IAPManager.Instance.ConsumePurchase(ProductCategories.CAT, OnCatPurchaseConsumed);
            }
            else
            {
                IAPManager.Instance.Purchase(ProductCategories.CAT, OnCatPurchaseCompleted);
            }
        }

        private void OnCatPurchaseCompleted(string sku, bool success, string errorMsg)
        {
            if (success)
            {
                // After successful purchase we consume the purchase and save it in our inventory
                IAPManager.Instance.ConsumePurchase(ProductCategories.CAT, OnCatPurchaseConsumed);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(errorMsg) && m_menuController)
                {
                    m_menuController.OnShowErrorMsgEvent(errorMsg);
                }
                m_purchaseFlowRoot.SetActive(false);
                m_processingMessage.SetActive(false);
                m_iconSelectionView.SetActive(true);
                m_backButton.SetActive(true);
            }
        }

        private void OnCatPurchaseConsumed(string sku, bool success)
        {
            if (success)
            {
                GameSettings.Instance.OwnedCatsCount++;
                UpdateCatPurchaseState();
            }

            OnPurchaseComplete();
        }

        private void OnPurchaseFlowCompleted(string sku, bool success, string errorMsg)
        {
            if (success)
            {
                var button = m_skuToButton[sku];
                button.OnPurchased();
                SelectButton(button);
            }
            else if (!string.IsNullOrWhiteSpace(errorMsg) && m_menuController)
            {
                m_menuController.OnShowErrorMsgEvent(errorMsg);
            }

            OnPurchaseComplete();
        }

        private void OnPurchaseComplete()
        {
            m_purchaseFlowRoot.SetActive(false);
            m_processingMessage.SetActive(false);
            m_iconSelectionView.SetActive(true);
            m_backButton.SetActive(true);
        }

        private void SelectButton(StoreIconButton button)
        {
            if (m_selectedButton)
            {
                m_selectedButton.Deselect();
            }

            m_selectedButton = button;
            m_selectedButton.Select();
            GameSettings.Instance.SelectedUserIconSku = button.SKU;
        }
    }
}