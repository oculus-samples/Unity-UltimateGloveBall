// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Utilities;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Player.Menu;
using UltimateGloveBall.Arena.Services;
using UltimateGloveBall.Arena.Spectator;
using UltimateGloveBall.Arena.VFX;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// Handles player inputs.
    /// Based on the state of the player it will process to the proper inputs and call the appropriate methods.
    /// </summary>
    public class PlayerInputController : Singleton<PlayerInputController>
    {
        [SerializeField] private PlayerInGameMenu m_playerMenu;

        private SpectatorNetwork m_spectatorNet = null;
        private bool m_freeLocomotionEnabled = true;
        public bool InputEnabled { get; set; } = true;

        public bool MovementEnabled { get; set; } = true;

        private bool m_wasMoving = false;

        public void SetSpectatorMode(SpectatorNetwork spectator)
        {
            m_spectatorNet = spectator;
        }

        public void OnSettingsUpdated()
        {
            m_freeLocomotionEnabled = !GameSettings.Instance.IsFreeLocomotionDisabled;
            PlayerMovement.Instance.RotationEitherThumbstick = !m_freeLocomotionEnabled;
        }

        private void Start()
        {
            m_freeLocomotionEnabled = !GameSettings.Instance.IsFreeLocomotionDisabled;
            PlayerMovement.Instance.RotationEitherThumbstick = !m_freeLocomotionEnabled;
        }

        private void OnDestroy()
        {
            PlayerMovement.Instance.RotationEitherThumbstick = true;
        }

        private void Update()
        {
            // Player menu can be triggered at all times
            if (OVRInput.GetUp(OVRInput.Button.Start) || Keyboard.current.escapeKey.wasReleasedThisFrame)
            {
                m_playerMenu.Toggle();
            }

            if (m_spectatorNet != null)
            {
                ProcessSpectatorInput();
            }
            else
            {
                ProcessPlayerInput();
            }
        }

        private void ProcessSpectatorInput()
        {
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger) || Mouse.current.leftButton.wasReleasedThisFrame)
            {
                m_spectatorNet.TriggerLeftAction();
            }
            if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger) || Mouse.current.rightButton.wasReleasedThisFrame)
            {
                m_spectatorNet.TriggerRightAction();
            }
        }

        private void ProcessPlayerInput()
        {
            if (!InputEnabled)
            {
                if (m_wasMoving)
                {
                    ScreenFXManager.Instance.ShowLocomotionFX(false);
                    m_wasMoving = false;
                }
                return;
            }

            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) || Mouse.current.leftButton.wasPressedThisFrame)
            {
                var glove = LocalPlayerEntities.Instance.LeftGloveHand;
                if (glove)
                {
                    glove.TriggerAction(false);
                }

                var gloveArmature = LocalPlayerEntities.Instance.LeftGloveArmature;
                if (gloveArmature)
                {
                    gloveArmature.Activated = true;
                }
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger) || Mouse.current.leftButton.wasReleasedThisFrame)
            {
                var glove = LocalPlayerEntities.Instance.LeftGloveHand;
                if (glove)
                {
                    glove.TriggerAction(true, LocalPlayerEntities.Instance.LeftGloveArmature.SpringCompression);
                }
                var gloveArmature = LocalPlayerEntities.Instance.LeftGloveArmature;
                if (gloveArmature)
                {
                    gloveArmature.Activated = false;
                }
            }

            if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) || Mouse.current.rightButton.wasPressedThisFrame)
            {
                var glove = LocalPlayerEntities.Instance.RightGloveHand;
                if (glove)
                {
                    glove.TriggerAction(false);
                }
                var gloveArmature = LocalPlayerEntities.Instance.RightGloveArmature;
                if (gloveArmature)
                {
                    gloveArmature.Activated = true;
                }
            }
            if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger) || Mouse.current.rightButton.wasReleasedThisFrame)
            {
                var glove = LocalPlayerEntities.Instance.RightGloveHand;
                if (glove)
                {
                    glove.TriggerAction(true, LocalPlayerEntities.Instance.RightGloveArmature.SpringCompression);
                }
                var gloveArmature = LocalPlayerEntities.Instance.RightGloveArmature;
                if (gloveArmature)
                {
                    gloveArmature.Activated = false;
                }
            }

            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger) || Keyboard.current.oKey.wasPressedThisFrame)
            {
                var playerController = LocalPlayerEntities.Instance.LocalPlayerController;
                playerController.TriggerShield(Glove.GloveSide.Left);
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger) || Keyboard.current.oKey.wasReleasedThisFrame)
            {
                var playerController = LocalPlayerEntities.Instance.LocalPlayerController;
                playerController.StopShieldServerRPC(Glove.GloveSide.Left);
            }
            if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger) || Keyboard.current.pKey.wasPressedThisFrame)
            {
                var playerController = LocalPlayerEntities.Instance.LocalPlayerController;
                playerController.TriggerShield(Glove.GloveSide.Right);
            }
            if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger) || Keyboard.current.pKey.wasReleasedThisFrame)
            {
                var playerController = LocalPlayerEntities.Instance.LocalPlayerController;
                playerController.StopShieldServerRPC(Glove.GloveSide.Right);
            }

            if (MovementEnabled && m_freeLocomotionEnabled)
            {
                var direction = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                if (direction == Vector2.zero)
                {
                    direction.x += Keyboard.current.aKey.isPressed ? -1 : 0;
                    direction.x += Keyboard.current.dKey.isPressed ? 1 : 0;
                    direction.y += Keyboard.current.sKey.isPressed ? -1 : 0;
                    direction.y += Keyboard.current.wKey.isPressed ? 1 : 0;
                }
                if (direction != Vector2.zero)
                {
                    var dir = new Vector3(direction.x, 0, direction.y);
                    PlayerMovement.Instance.WalkInDirectionRelToForward(dir);
                    if (!m_wasMoving)
                    {
                        ScreenFXManager.Instance.ShowLocomotionFX(true);
                    }

                    m_wasMoving = true;
                }
                else if (m_wasMoving)
                {
                    ScreenFXManager.Instance.ShowLocomotionFX(false);
                    m_wasMoving = false;
                }
            }
        }
    }
}