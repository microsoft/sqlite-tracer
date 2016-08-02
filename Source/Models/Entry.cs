// -----------------------------------------------------------------------
// <copyright file="Entry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer.Models
{
    using SQLiteDebugger;
    using System;
    using System.Data;
    using System.Globalization;
    using System.Linq;

    public class Entry
    {
        public int Id { get; set; }

        public Log Parent { get; set; }

        public EntryType Type { get; set; }

        public int Connection { get; set; }

        public string Database { get; set; }

        public DateTime Start { get; set; }

        private DateTime end;

        public DateTime End
        {
            get
            {
                return this.end;
            }

            set
            {
                this.end = value;
                if (this.Parent != null)
                {
                    this.Parent.UpdateEnd(this.Id, value);
                }
            }
        }

        public string Text { get; set; }

        public string Plan { get; set; }

        private DataTable results;

        public DataTable Results
        {
            get
            {
                return this.results;
            }

            set
            {
                this.results = value;
                if (this.Parent != null)
                {
                    this.Parent.UpdateResults(this.Id, value);
                }
            }
        }
    }

    public enum EntryType
    {
        Message,
        Query,
    }
}
