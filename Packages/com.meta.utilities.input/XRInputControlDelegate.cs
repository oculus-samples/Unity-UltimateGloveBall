// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Avatar2;

public class XRInputControlDelegate : SampleInputControlDelegate
{
    private XRInputControlActions m_controlActions;

    public XRInputControlDelegate(XRInputControlActions controlActions) => m_controlActions = controlActions;

    public override bool GetInputControlState(out OvrAvatarInputControlState inputControlState)
    {
        if (OVRInput.GetConnectedControllers() != OVRInput.Controller.None)
            return base.GetInputControlState(out inputControlState);

        inputControlState = default;
        UpdateControllerInput(ref inputControlState.leftControllerState, ref m_controlActions.m_left);
        UpdateControllerInput(ref inputControlState.rightControllerState, ref m_controlActions.m_right);
        return true;
    }

    private void UpdateControllerInput(ref OvrAvatarControllerState controllerState, ref XRInputControlActions.Controller controller)
    {
        controllerState.buttonMask = 0;
        controllerState.touchMask = 0;

        // Button Press
        if (controller.m_buttonOne.action.ReadValue<float>() > 0.5f)
        {
            controllerState.buttonMask |= CAPI.ovrAvatar2Button.One;
        }
        if (controller.m_buttonTwo.action.ReadValue<float>() > 0.5f)
        {
            controllerState.buttonMask |= CAPI.ovrAvatar2Button.Two;
        }
        if (controller.m_buttonThree.action.ReadValue<float>() > 0.5f)
        {
            controllerState.buttonMask |= CAPI.ovrAvatar2Button.Three;
        }

        // Button Touch
        if (controller.m_touchOne.action.ReadValue<float>() > 0.5f)
        {
            controllerState.touchMask |= CAPI.ovrAvatar2Touch.One;
        }
        if (controller.m_touchTwo.action.ReadValue<float>() > 0.5f)
        {
            controllerState.touchMask |= CAPI.ovrAvatar2Touch.Two;
        }
        if (controller.m_touchPrimaryThumbstick.action.ReadValue<float>() > 0.5f)
        {
            controllerState.touchMask |= CAPI.ovrAvatar2Touch.Joystick;
        }

        // Trigger
        controllerState.indexTrigger = controller.m_axisIndexTrigger.action.ReadValue<float>();

        // Grip
        controllerState.handTrigger = controller.m_axisHandTrigger.action.ReadValue<float>();
    }
}