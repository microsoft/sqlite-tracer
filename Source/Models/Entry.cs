// -----------------------------------------------------------------------
// <copyright file="Entry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer.Models
{
    using System;
    using System.Data;

    public class Entry
    {
        public EntryType Type { get; set; }

        public int ID { get; set; }

        public int Database { get; set; }

        public string Filename { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public string Text { get; set; }

        public string Plan { get; set; }

        public DataTable Results { get; set; }
    }

    public enum EntryType
    {
        Message,
        Query,
    }
}
