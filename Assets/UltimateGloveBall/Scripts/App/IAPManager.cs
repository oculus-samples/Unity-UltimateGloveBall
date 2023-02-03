// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections.Generic;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// Manages in app purchases. It's a wrapper on the Oculus.Platform.IAP functionalities.
    /// This makes it easy to fetch all products and purchases as well as make a purchase.
    /// Referenced from: https://developer.oculus.com/documentation/unity/ps-iap/
    /// </summary>
    public class IAPManager
    {
        #region Singleton
        private static IAPManager s_instance;

        public static IAPManager Instance
        {
            get
            {
                s_instance ??= new IAPManager();
                return s_instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void DestroyInstance()
        {
            s_instance = null;
        }
        #endregion // Singleton

        /// <summary>
        /// The data we get from the error message json string on purchase
        /// </summary>
        private class PurchaseErrorMessage
        {
            // Since we convert from json the naming must stay the same as the json format, which is all lowercase
#pragma warning disable IDE1006
            // ReSharper disable once InconsistentNaming
            public string category;
            // ReSharper disable once InconsistentNaming
            public int code;
            // ReSharper disable once InconsistentNaming
            public string message;
#pragma warning restore IDE1006
        }

        private Dictionary<string, Product> m_products = new();
        private Dictionary<string, Purchase> m_purchases = new();

        private Dictionary<string, List<string>> m_productsByCategory = new();
        private List<string> m_availableSkus = new();
        public IList<string> AvailableSkus => m_availableSkus;

        /// <summary>
        /// Asynchronously fetch all products based on the sku
        /// </summary>
        public void FetchProducts(string[] skus, string category = null)
        {
            _ = IAP.GetProductsBySKU(skus).OnComplete(message =>
            {
                GetProductsBySKUCallback(message, category);
            });
        }

        /// <summary>
        /// Asynchronously fetch all purchases that were made by the user
        /// </summary>
        public void FetchPurchases()
        {
            _ = IAP.GetViewerPurchases().OnComplete(GetViewerPurchasesCallback);
        }

        public List<string> GetProductSkusForCategory(string category)
        {
            return m_productsByCategory.TryGetValue(category, out var categorySkus) ? categorySkus : null;
        }

        public Product GetProduct(string sku)
        {
            if (m_products.TryGetValue(sku, out var product))
            {
                return product;
            }

            Debug.LogError($"[IAPManager] Product {sku} doesn't exist!");
            return null;
        }

        public bool IsPurchased(string sku)
        {
            return m_purchases.TryGetValue(sku, out _);
        }

        public Purchase GetPurchase(string sku)
        {
            return m_purchases.TryGetValue(sku, out var purchase) ? purchase : null;
        }

        public void Purchase(string sku, Action<string, bool, string> onPurchaseFlowCompleted)
        {
#if UNITY_EDITOR
            m_purchases[sku] = null; // we can't create a purchase in Editor, but we need to keep track of the purchase
            onPurchaseFlowCompleted?.Invoke(sku, true, null);
#else
            IAP.LaunchCheckoutFlow(sku).OnComplete((Message<Purchase> msg) =>
            {
                if (msg.IsError)
                {
                    var errorMsgString = msg.GetError().Message;
                    Debug.LogError($"[IAPManager] Error while purchasing: {errorMsgString}");
                    var errorData = JsonUtility.FromJson<PurchaseErrorMessage>(errorMsgString);
                    onPurchaseFlowCompleted?.Invoke(sku, false, errorData.message);
                    return;
                }

                var p = msg.GetPurchase();
                Debug.Log("[IAPManager] Purchased " + p.Sku);
                m_purchases[sku] = p;
                onPurchaseFlowCompleted?.Invoke(sku, true, null);
            });
#endif
        }

        public void ConsumePurchase(string sku, Action<string, bool> onConsumptionCompleted)
        {
#if UNITY_EDITOR
            m_purchases.Remove(sku);
            onConsumptionCompleted?.Invoke(sku, true);
#else
            _ = IAP.ConsumePurchase(sku).OnComplete(msg =>
            {
                if (msg.IsError)
                {
                    Debug.LogError($"[IAPManager] Error while consuming: {msg.GetError().Message}");
                    onConsumptionCompleted?.Invoke(sku, false);
                    return;
                }

                Debug.Log("[IAPManager] Consumed " + sku);
                m_purchases.Remove(sku);
                onConsumptionCompleted?.Invoke(sku, true);
            });
#endif
        }

        private void GetProductsBySKUCallback(Message<ProductList> msg, string category)
        {
            if (msg.IsError)
            {
                Debug.LogError($"[IAPManager] Failed to fetch products, {msg.GetError().Message}");
                return;
            }

            foreach (var p in msg.GetProductList())
            {
                Debug.LogFormat("[IAPManager] Product: sku:{0} name:{1} price:{2}", p.Sku, p.Name, p.FormattedPrice);
                m_products[p.Sku] = p;
                m_availableSkus.Add(p.Sku);
                if (!string.IsNullOrWhiteSpace(category))
                {
                    if (!m_productsByCategory.TryGetValue(category, out var categorySkus))
                    {
                        categorySkus = new List<string>();
                        m_productsByCategory[category] = categorySkus;
                    }

                    categorySkus.Add(p.Sku);
                }
            }
        }

        private void GetViewerPurchasesCallback(Message<PurchaseList> msg)
        {
            if (msg.IsError)
            {
                Debug.LogError($"[IAPManager] Failed to fetch purchased products, {msg.GetError().Message}");
                return;
            }

            foreach (var p in msg.GetPurchaseList())
            {
                Debug.Log($"[IAPManager] Purchased: sku:{p.Sku} granttime:{p.GrantTime} id:{p.ID}");
                m_purchases[p.Sku] = p;
            }
        }
    }
}