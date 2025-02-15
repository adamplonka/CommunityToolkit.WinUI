// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;

#nullable enable

namespace CommunityToolkit.WinUI.UI.Animations
{
    /// <summary>
    /// A <see cref="KeyFrame{TValue,TKeyFrame}"/> type for <see cref="Vector3"/> animations.
    /// </summary>
    public sealed class Vector3KeyFrame : KeyFrame<string, Vector3>
    {
        /// <inheritdoc/>
        protected override Vector3 GetParsedValue()
        {
            return Value!.ToVector3();
        }
    }
}