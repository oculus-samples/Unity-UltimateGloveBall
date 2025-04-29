#nullable enable

// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Oculus.Avatar2
{
    /// <summary>
    /// A class that listens to state change event for the default animation state.
    ///
    /// A DEFAULT state is a state inside a custom animation graph that represents all
    /// the default animations. An avatar is said to be in default state when it's playing
    /// default animation.
    ///
    /// </summary>
    public class OvrAvatarDefaultStateListener : StateMachineBehaviour
    {
        public delegate void AnimationStateChangeDelegate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
        public event AnimationStateChangeDelegate? OnEnterState;
        public event AnimationStateChangeDelegate? OnUpdateState;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            OnEnterState?.Invoke(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            OnUpdateState?.Invoke(animator, stateInfo, layerIndex);
        }
    }
}
