// -----------------------------------------------------------------------
// <copyright file="DelegateCommand.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Toolkit
{
    using System;
    using System.Windows.Input;

    public class DelegateCommand : CommandBase
    {
        private readonly Action action;
        private readonly Func<bool> canExecute;

        public DelegateCommand(Action action, Func<bool> canExecute = null)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public override void Execute(object parameter)
        {
            this.action();
        }

        public override bool CanExecute(object parameter)
        {
            return this.canExecute == null ? true : this.canExecute();
        }
    }
}
