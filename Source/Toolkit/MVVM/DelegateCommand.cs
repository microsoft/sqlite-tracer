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

        public DelegateCommand(Action action)
        {
            this.action = action;
        }

        public override event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public override void Execute(object parameter)
        {
            this.action();
        }

        public override bool CanExecute(object parameter)
        {
            return true;
        }
    }
}
