// -----------------------------------------------------------------------
// <copyright file="IDebugTraceSender.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteDebugger
{
    using System.Threading.Tasks;

    public interface IDebugTraceSender
    {
        void SendMessage(string message);
    }
}