// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// Handles the update of the players name plate and state if they are a master client/Host.
    /// The different parts can be visible separately and we can turn on/off the visibility of the whole visual.
    /// </summary>
    public class PlayerNameVisual : MonoBehaviour
    {
        [SerializeField] private Transform m_canvas;
        [SerializeField] private TMP_Text m_usernameText;
        [SerializeField] private Image m_masterIcon;
        [SerializeField] private Image m_userIcon;

        private bool m_isEnabled = true;
        private bool m_visible = true;

        public void SetEnableState(bool enable)
        {
            m_isEnabled = enable;
            SetVisibility(m_visible);
        }

        public void SetVisibility(bool show)
        {
            m_visible = show;
            m_canvas?.gameObject.SetActive(show && m_isEnabled);
        }

        public void SetUsername(string username)
        {
            if (m_usernameText != null)
            {
                m_usernameText.text = username;
            }
        }

        public void SetUserIcon(Sprite userIcon)
        {
            if (m_userIcon != null)
            {
                m_userIcon.sprite = userIcon;
            }

            m_userIcon.enabled = userIcon != null;
        }

        public void ShowUsername(bool show)
        {
            m_usernameText?.gameObject.SetActive(show);
        }

        public void ShowMasterIcon(bool show)
        {
            m_masterIcon?.gameObject.SetActive(show);
        }

        public void ShowUserIcon(bool show)
        {
            m_userIcon?.gameObject.SetActive(show);
        }
    }
}