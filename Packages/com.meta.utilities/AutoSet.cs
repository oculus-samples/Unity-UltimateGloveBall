// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace Meta.Utilities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoSetAttribute : PropertyAttribute
    {
        public AutoSetAttribute(Type type = default) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class AutoSetFromParentAttribute : AutoSetAttribute
    {
        public bool IncludeInactive { get; set; } = false;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class AutoSetFromChildrenAttribute : AutoSetAttribute
    {
        public bool IncludeInactive { get; set; } = false;
    }
}