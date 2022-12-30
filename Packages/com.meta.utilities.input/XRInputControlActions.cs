// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class XRInputControlActions : ScriptableObject
{
    [System.Serializable]
    public struct Controller
    {
        public InputActionProperty m_buttonOne;
        public InputActionProperty m_buttonTwo;
        public InputActionProperty m_buttonThree;
        public InputActionProperty m_buttonPrimaryThumbstick;

        public InputActionProperty m_touchOne;
        public InputActionProperty m_touchTwo;
        public InputActionProperty m_touchPrimaryThumbstick;
        public InputActionProperty m_touchPrimaryThumbRest;

        public InputActionProperty m_axisIndexTrigger;
        public InputActionProperty m_axisHandTrigger;

        public InputActionProperty[] AllActions => new[] {
            m_buttonOne,
            m_buttonTwo,
            m_buttonThree,
            m_buttonPrimaryThumbstick,
            m_touchOne,
            m_touchTwo,
            m_touchPrimaryThumbstick,
            m_touchPrimaryThumbRest,
            m_axisIndexTrigger,
            m_axisHandTrigger,
        };

        public InputActionProperty[] PrimaryThumbButtonTouches => new[] {
            m_touchOne,
            m_touchTwo,
            m_touchPrimaryThumbRest,
            m_touchPrimaryThumbstick
        };

        public float AnyPrimaryThumbButtonTouching => PrimaryThumbButtonTouches.Max(a => a.action.ReadValue<float>());
    }

    public Controller m_left;
    public Controller m_right;

    public IEnumerable<InputActionProperty> AllActions => new[] { m_left, m_right }.SelectMany(c => c.AllActions);

    public void EnableActions()
    {
        foreach (var action in AllActions)
            action.action.Enable();
    }
}