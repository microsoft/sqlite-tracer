// -----------------------------------------------------------------------
// <copyright file="IDispatcher.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Toolkit
{
    using System;

    public interface IDispatcher
    {
        void Invoke(Action action);
    }
}
