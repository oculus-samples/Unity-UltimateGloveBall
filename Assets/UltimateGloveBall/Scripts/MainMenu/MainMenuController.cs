// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using Meta.XR.Samples;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Services;
using UltimateGloveBall.Utils;
using UnityEngine;

namespace UltimateGloveBall.MainMenu
{
    /// <summary>
    /// Controls the state of the MainMenu scene. Navigate trhough the different menus and handles the state change.
    /// Implement event functions for the different button clicked.
    /// </summary>
    [MetaCodeSample("UltimateGloveBall")]
    public class MainMenuController : MonoBehaviour
    {
        private enum MenuState
        {
            Main,
            Options,
            Controls,
            Friends,
            Settings,
            BallTypes,
            Store,
        }

        [Header("Main Canvas")]
        [SerializeField] private BoxCollider m_canvasCollider;

        [Header("Menu Controllers")]
        [SerializeField] private BaseMenuController m_mainMenuController;
        [SerializeField] private BaseMenuController m_optionMenuController;
        [SerializeField] private BaseMenuController m_controlsMenuController;
        [SerializeField] private BaseMenuController m_friendsMenuController;
        [SerializeField] private BaseMenuController m_settingsMenuController;
        [SerializeField] private BaseMenuController m_ballTypesMenuController;
        [SerializeField] private BaseMenuController m_storeMenuController;

        [Header("Error Panel")]
        [SerializeField] private MenuErrorPanel m_errorPanel;

        [Header("Anchors")]
        [SerializeField] private Transform m_startPosition;
        [SerializeField] private Transform m_avatarTransform;

        [SerializeField] private AudioFadeInOut m_menuMusicFader;

        private BaseMenuController m_currentMenu;
        private float m_baseMenuMusicVolume;

        private MenuState m_currentState;

        private void Awake()
        {
            PlayerMovement.Instance.SnapPositionToTransform(m_startPosition);
            // we snap the avatar transform right away so there is no lag
            m_avatarTransform.SetPositionAndRotation(m_startPosition.position, m_startPosition.rotation);
            // we start in mainMenu
            m_currentMenu = m_mainMenuController;
        }

        private void Start()
        {
            OVRScreenFade.instance.FadeIn();
            // once we are back in main menu we have used the cat
            LocalPlayerState.Instance.SpawnCatInNextGame = false;
        }

        public void OnQuickMatchClicked()
        {
            Debug.Log("QUICK MATCH");
            DisableButtons();
            UGBApplication.Instance.NavigationController.NavigateToMatch(false);
            m_menuMusicFader.FadeOut();
        }

        public void OnHostMatchClicked()
        {
            Debug.Log("HOST MATCH");
            DisableButtons();
            UGBApplication.Instance.NavigationController.NavigateToMatch(true);
            m_menuMusicFader.FadeOut();
        }

        public void OnWatchMatchClicked()
        {
            Debug.Log("WATCH MATCH");
            DisableButtons();
            UGBApplication.Instance.NavigationController.WatchRandomMatch();
            m_menuMusicFader.FadeOut();
        }

        public void OnFriendsClicked()
        {
            ChangeMenuState(MenuState.Friends);
        }

        public void OnFriendsBackClicked()
        {
            ChangeMenuState(MenuState.Main);
        }

        public void OnOptionsClicked()
        {
            ChangeMenuState(MenuState.Options);
        }

        public void OnSettingsClicked()
        {
            ChangeMenuState(MenuState.Settings);
        }

        public void OnSettingsBackClicked()
        {
            ChangeMenuState(MenuState.Options);
        }

        public void OnStoreClicked()
        {
            ChangeMenuState(MenuState.Store);
        }

        public void OnStoreBackClicked()
        {
            ChangeMenuState(MenuState.Main);
        }

        public void OnBallTypesClicked()
        {
            ChangeMenuState(MenuState.BallTypes);
        }

        public void OnBallTypesBackClicked()
        {
            ChangeMenuState(MenuState.Options);
        }

        public void OnExitClicked()
        {
            DisableButtons();
            Application.Quit();
        }

        public void OnOptionsBackClicked()
        {
            ChangeMenuState(MenuState.Main);
        }

        public void OnControlsClicked()
        {
            ChangeMenuState(MenuState.Controls);
        }

        public void OnControlsBackClicked()
        {
            ChangeMenuState(MenuState.Options);
        }

        public void EnableButtons()
        {
            m_canvasCollider.enabled = true;
            m_currentMenu.EnableButtons();
        }

        public void DisableButtons()
        {
            m_canvasCollider.enabled = false;
            m_currentMenu.DisableButtons();
        }

        public void OnErrorPanelCloseClicked()
        {
            m_errorPanel.Close();
            ChangeMenuState(m_currentState);
        }

        public void OnReturnToMenu(ArenaApprovalController.ConnectionStatus connectionStatus)
        {
            EnableButtons();
            m_menuMusicFader.FadeIn();
            if (connectionStatus != ArenaApprovalController.ConnectionStatus.Success)
            {
                var errorMsg = connectionStatus switch
                {
                    ArenaApprovalController.ConnectionStatus.Undefined => LocalPlayerState.Instance.IsSpectator
                                                ? "No match found to spectate, please try later."
                                                : "An error occured when trying to join.",
                    ArenaApprovalController.ConnectionStatus.PlayerFull =>
                        "No more space for players, you can try to join a different arena.",
                    ArenaApprovalController.ConnectionStatus.SpectatorFull =>
                        "This arena has reached it's spectator limit.",
                    ArenaApprovalController.ConnectionStatus.Success => throw new NotImplementedException(),
                    _ => throw new ArgumentOutOfRangeException(nameof(connectionStatus), connectionStatus, null),
                };
                ShowErrorMessage(errorMsg);
            }
        }

        public void OnShowErrorMsgEvent(string errorMsg)
        {
            ShowErrorMessage(errorMsg);
        }

        private void ShowErrorMessage(string errorMsg)
        {
            m_currentMenu.Hide();
            m_errorPanel.ShowMessage(errorMsg);
        }

        private void ChangeMenuState(MenuState newState)
        {
            m_currentState = newState;
            m_currentMenu.Hide();

            m_currentMenu = newState switch
            {
                MenuState.Main => m_mainMenuController,
                MenuState.Options => m_optionMenuController,
                MenuState.Controls => m_controlsMenuController,
                MenuState.Friends => m_friendsMenuController,
                MenuState.Settings => m_settingsMenuController,
                MenuState.BallTypes => m_ballTypesMenuController,
                MenuState.Store => m_storeMenuController,
                _ => throw new ArgumentOutOfRangeException(nameof(newState), newState, null)
            };
            m_currentMenu.Show();
            EnableButtons();
        }
    }
}