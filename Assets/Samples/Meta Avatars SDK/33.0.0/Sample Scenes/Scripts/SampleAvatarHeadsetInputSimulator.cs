#nullable enable

using UnityEngine;

namespace Oculus.Avatar2
{
    public class SampleAvatarHeadsetInputSimulator : OvrAvatarInputTrackingDelegate
    {
        private const string LOG_SCOPE = "SampleAvatarHeadsetInputSimulator";
        private const float MOVEMENT_SPEED = 1.4f;
        private const float ROTATION_SPEED = 60.0f;

        private struct HeadsetState
        {
            public Vector3 HeadsetPosition;
            public Quaternion HeadsetRotation;
        }

        private HeadsetState _currentState;
        private bool _isActive = true;

        private readonly Vector3 _positionOffset = new(0f, 1.5f, 0f);
        private readonly Quaternion _rotationOffset = Quaternion.Euler(0f, 180f, 0f);
        private const float RESET_DELAY = 1f;

        #region Keyboard Assignments

        private const KeyCode FORWARD_KEY = KeyCode.Y;
        private const KeyCode BACKWARD_KEY = KeyCode.H;
        private const KeyCode LEFT_KEY = KeyCode.G;
        private const KeyCode RIGHT_KEY = KeyCode.J;
        private const KeyCode UP_KEY = KeyCode.T;
        private const KeyCode DOWN_KEY = KeyCode.U;
        private const KeyCode TURN_LEFT_KEY = KeyCode.K;
        private const KeyCode TURN_RIGHT_KEY = KeyCode.Semicolon;
        private const KeyCode LOOK_UP_KEY = KeyCode.O;
        private const KeyCode LOOK_DOWN_KEY = KeyCode.L;
        private const KeyCode RESET_KEY = KeyCode.R;

        #endregion

        private float _startResetTime;

        public SampleAvatarHeadsetInputSimulator()
        {
            ResetCurrentState();
        }

        private void ResetCurrentState()
        {
            _currentState.HeadsetPosition = Vector3.zero;
            _currentState.HeadsetRotation = Quaternion.identity;
        }

        private void EmulateHeadPositionWithKeyboardInput()
        {
            Vector3 position = _currentState.HeadsetPosition;
            Quaternion rotation = _currentState.HeadsetRotation;

            if (Input.GetKey(FORWARD_KEY)) // Forward
            {
                position.z += MOVEMENT_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(BACKWARD_KEY)) // Backward
            {
                position.z -= MOVEMENT_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(LEFT_KEY)) // Left
            {
                position.x -= MOVEMENT_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(RIGHT_KEY)) // Right
            {
                position.x += MOVEMENT_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(UP_KEY)) // Up
            {
                position.y += MOVEMENT_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(DOWN_KEY)) // Down
            {
                position.y -= MOVEMENT_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(TURN_LEFT_KEY)) // Turn left
            {
                rotation *= Quaternion.Euler(0, -ROTATION_SPEED * Time.deltaTime, 0);
            }

            if (Input.GetKey(TURN_RIGHT_KEY)) // Turn right
            {
                rotation *= Quaternion.Euler(0, ROTATION_SPEED * Time.deltaTime, 0);
            }

            if (Input.GetKey(LOOK_UP_KEY)) // Look up
            {
                rotation *= Quaternion.Euler(-ROTATION_SPEED * Time.deltaTime, 0, 0);
            }

            if (Input.GetKey(LOOK_DOWN_KEY)) // Look down
            {
                rotation *= Quaternion.Euler(ROTATION_SPEED * Time.deltaTime, 0, 0);
            }

            if (Input.GetKey(RESET_KEY))
            {
                ResetPosition();
            }

            _currentState.HeadsetPosition = position;
            _currentState.HeadsetRotation = rotation;
        }

        private void ResetPosition()
        {
            _isActive = false;
            _startResetTime = Time.time;
        }


        public override bool GetRawInputTrackingState(out OvrAvatarInputTrackingState inputTrackingState)
        {
            inputTrackingState = default;

            inputTrackingState.leftControllerActive = false;
            inputTrackingState.rightControllerActive = false;
            inputTrackingState.leftControllerVisible = false;
            inputTrackingState.rightControllerVisible = false;

            if (_isActive)
            {
                EmulateHeadPositionWithKeyboardInput();
            }
            else
            {
                if (Time.time - _startResetTime > RESET_DELAY)
                {
                    ResetCurrentState();
                    _isActive = true;
                }
                else
                {
                    inputTrackingState.headsetActive = false;
                    return false;
                }
            }


            inputTrackingState.headsetActive = true;
            inputTrackingState.headset.position = _currentState.HeadsetPosition + _positionOffset;
            inputTrackingState.headset.orientation = _currentState.HeadsetRotation * _rotationOffset;
            inputTrackingState.headset.scale = Vector3.one;

            return true;
        }
    }
}
