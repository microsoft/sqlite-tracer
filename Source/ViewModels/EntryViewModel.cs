// -----------------------------------------------------------------------
// <copyright file="EntryViewModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer.ViewModels
{
    using Models;
    using System;
    using System.Data;
    using System.IO;
    using Toolkit;

    public class EntryViewModel : ObservableObject
    {
        private int connection;
        private string preview;

        public EntryViewModel(Entry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            this.Entry = entry;
            this.connection = entry.Connection;
        }

        internal Entry Entry { get; private set; }

        public EntryType Type
        {
            get { return this.Entry.Type; }
        }

        public int Connection
        {
            get
            {
                return this.connection;
            }

            set
            {
                this.connection = value;
                this.NotifyPropertyChanged("Connection");
            }
        }

        public string Filename
        {
            get { return Path.GetFileNameWithoutExtension(this.Entry.Database); }
        }

        internal string Filepath
        {
            get { return this.Entry.Database; }
        }

        public DateTime Start
        {
            get { return this.Entry.Start; }
        }

        public DateTime End
        {
            get
            {
                return this.Entry.End;
            }

            set
            {
                this.Entry.End = value;
                this.NotifyPropertyChanged("End");
                this.NotifyPropertyChanged("Duration");
                this.NotifyPropertyChanged("Complete");
            }
        }

        public TimeSpan Duration
        {
            get { return this.Complete ? this.End - this.Start : TimeSpan.Zero; }
        }

        public bool Complete
        {
            get { return this.End != default(DateTime); }
        }

        public string Text
        {
            get { return this.Entry.Text; }
        }

        public string Preview
        {
            get
            {
                if (this.Text != null && this.preview == null)
                {
                    this.preview = this.Text.Replace(Environment.NewLine, " ").Replace("\n", " ");
                }

                return this.preview;
            }
        }

        public string Plan
        {
            get { return this.Entry.Plan; }
        }

        public DataView Results
        {
            get
            {
                return this.Entry.Results != null ? this.Entry.Results.AsDataView() : null;
            }

            set
            {
                var table = value != null ? value.Table : null;
                this.Entry.Results = table;
                this.NotifyPropertyChanged("Results");
            }
        }
    }
}
