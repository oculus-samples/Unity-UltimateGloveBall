// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;

public class XRTrackedPoseDriver : TrackedPoseDriver
{
    public int CurrentDataVersion { get; private set; }

    public event Action InputDataAvailable;

    public UnityEvent m_onPerformUpdate = new();

    protected override void PerformUpdate()
    {
        base.PerformUpdate();
        CurrentDataVersion += 1;
        InputDataAvailable?.Invoke();
        m_onPerformUpdate.Invoke();
    }

    protected override void OnUpdate()
    {
        PerformUpdate();
    }
}