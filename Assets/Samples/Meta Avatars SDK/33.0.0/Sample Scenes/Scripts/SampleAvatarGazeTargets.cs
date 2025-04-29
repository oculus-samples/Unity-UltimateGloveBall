#nullable enable

using Oculus.Avatar2;
using System.Collections;
using UnityEngine;

// When added to a SampleAvatarEntity, this creates gaze targets for this avatar's head and hands
[RequireComponent(typeof(OvrAvatarEntity))]
public class SampleAvatarGazeTargets : MonoBehaviour
{
    private const string logScope = "SampleAvatarGazeTargets";

    private const CAPI.ovrAvatar2JointType HEAD_GAZE_TARGET_JNT = CAPI.ovrAvatar2JointType.Head;
    private const CAPI.ovrAvatar2JointType LEFT_HAND_GAZE_TARGET_JNT = CAPI.ovrAvatar2JointType.LeftHandIndexProximal;
    private const CAPI.ovrAvatar2JointType RIGHT_HAND_GAZE_TARGET_JNT = CAPI.ovrAvatar2JointType.RightHandIndexProximal;

    [SerializeField]
    [HideInInspector]
    private OvrAvatarEntity? _avatarEnt;

    protected IEnumerator Start()
    {
        if (_avatarEnt == null)
        {
            FindAvatarEntity();
        }

        if (_avatarEnt == null)
        {
            OvrAvatarLog.LogError("No entity found for SampleAvatarGazeTargets!", logScope, this);
            enabled = false;
            yield break;
        }

        if (!ValidateCriticalJoint(HEAD_GAZE_TARGET_JNT)
            || !ValidateCriticalJoint(LEFT_HAND_GAZE_TARGET_JNT)
            || !ValidateCriticalJoint(RIGHT_HAND_GAZE_TARGET_JNT))
        {
            enabled = false;
            yield break;
        }

        yield return new WaitUntil(
            () => _avatarEnt.HasJoints && !_avatarEnt.IsPendingAvatar && !_avatarEnt.IsApplyingModels);

        CreateGazeTarget("HeadGazeTarget", HEAD_GAZE_TARGET_JNT, CAPI.ovrAvatar2GazeTargetType.AvatarHead);
        CreateGazeTarget("LeftHandGazeTarget", LEFT_HAND_GAZE_TARGET_JNT, CAPI.ovrAvatar2GazeTargetType.AvatarHand);
        CreateGazeTarget("RightHandGazeTarget", RIGHT_HAND_GAZE_TARGET_JNT, CAPI.ovrAvatar2GazeTargetType.AvatarHand);
    }

    private void CreateGazeTarget(string gameObjectName, CAPI.ovrAvatar2JointType jointType, CAPI.ovrAvatar2GazeTargetType targetType)
    {
        System.Diagnostics.Debug.Assert(_avatarEnt != null);

        Transform jointTransform = _avatarEnt.GetSkeletonTransform(jointType);
        if (jointTransform)
        {
            var gazeTargetObj = new GameObject(gameObjectName);
            var gazeTarget = gazeTargetObj.AddComponent<OvrAvatarGazeTarget>();
            gazeTarget.TargetType = targetType;
            gazeTargetObj.transform.SetParent(jointTransform, false);
        }
        else
        {
            OvrAvatarLog.LogError($"SampleAvatarGazeTargets: In game object {gameObject.name}, no joint transform \"{jointType}\" found for {gameObjectName}");
        }
    }

    private bool ValidateCriticalJoint(CAPI.ovrAvatar2JointType jointType)
    {
        System.Diagnostics.Debug.Assert(_avatarEnt != null);

        bool hasJoint = _avatarEnt.HasCriticalJoint(jointType);
        if (!hasJoint)
        {
            OvrAvatarLog.LogError($"Entity ({_avatarEnt}) does not have a critical joint ({jointType}) configured!", logScope, this);
        }
        return hasJoint;
    }

    private void FindAvatarEntity()
    {
        _avatarEnt = GetComponent<SampleAvatarEntity>();
    }

#if UNITY_EDITOR
    protected void OnValidate()
    {
        FindAvatarEntity();
    }
#endif // UNITY_EDITOR
}
