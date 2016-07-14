// -----------------------------------------------------------------------
// <copyright file="Log.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer.Models
{
    using System;
    using System.Collections.ObjectModel;
    using System.Data;

    public class Log
    {
        private readonly ObservableCollection<Entry> log = new ObservableCollection<Entry>();

        public ObservableCollection<Entry> Entries
        {
            get { return this.log; }
        }
    }
}
