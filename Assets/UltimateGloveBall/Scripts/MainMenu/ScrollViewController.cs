// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateGloveBall.MainMenu
{
    public class ScrollViewController : MonoBehaviour
    {
        private enum Pointers
        {
            None,
            LeftPointer,
            RightPointer
        }

        [SerializeField] private GraphicRaycaster m_graphicRaycaster;
        [SerializeField] private ScrollRect m_scrollRect;
        [SerializeField] private float m_scrollSpeed = 0.05f;
        [SerializeField] private GameObject m_targetPointer;

        private OVRInput.Axis2D m_thumbstickL, m_thumbstickR;
        private OVRInput.Controller m_controllerL, m_controllerR;

        private Pointers m_activeController = Pointers.None;

        private void Start()
        {
            m_thumbstickL = OVRInput.Axis2D.PrimaryThumbstick;
            m_thumbstickR = OVRInput.Axis2D.SecondaryThumbstick;
            m_controllerL = OVRInput.Controller.LTouch;
            m_controllerR = OVRInput.Controller.RTouch;
        }

        private void Update()
        {
            var leftInputY = OVRInput.Get(m_thumbstickL).y;
            var rightInputY = OVRInput.Get(m_thumbstickR).y;
            var newScrollPos = m_scrollRect.verticalNormalizedPosition;
            var isScrollingThisFrame = false;

            if (rightInputY != 0 && m_activeController != Pointers.LeftPointer &&
                IsPointerOverGUI(Pointers.RightPointer))
            {
                m_activeController = Pointers.RightPointer;
                newScrollPos += rightInputY * m_scrollSpeed * Time.deltaTime;
                isScrollingThisFrame = true;
            }
            else if (leftInputY != 0 && m_activeController != Pointers.RightPointer &&
                     IsPointerOverGUI(Pointers.LeftPointer))
            {
                m_activeController = Pointers.LeftPointer;
                newScrollPos += leftInputY * m_scrollSpeed * Time.deltaTime;
                isScrollingThisFrame = true;
            }

            if (newScrollPos > 1) newScrollPos = 1;
            if (newScrollPos < -1) newScrollPos = -1;
            m_scrollRect.verticalNormalizedPosition = newScrollPos;

            if (!isScrollingThisFrame)
            {
                m_activeController = Pointers.None;
            }
        }

        private bool IsPointerOverGUI(Pointers pointer)
        {
            var controllerPosition = Vector3.zero;
            var controllerRotation = Quaternion.identity;

            if (pointer == Pointers.LeftPointer)
            {
                controllerPosition = OVRInput.GetLocalControllerPosition(m_controllerL);
                controllerRotation = OVRInput.GetLocalControllerRotation(m_controllerL);
            }

            if (pointer == Pointers.RightPointer)
            {
                controllerPosition = OVRInput.GetLocalControllerPosition(m_controllerR);
                controllerRotation = OVRInput.GetLocalControllerRotation(m_controllerR);
            }

            var controllerRay = new Ray(controllerPosition, controllerRotation * Vector3.forward);

            if (Physics.Raycast(controllerRay, out var hit))
            {
                var screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, hit.point);
                var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };

                var results = new List<RaycastResult>();
                m_graphicRaycaster.Raycast(eventData, results);

                foreach (var result in results)
                {
                    if (result.gameObject == m_targetPointer)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
