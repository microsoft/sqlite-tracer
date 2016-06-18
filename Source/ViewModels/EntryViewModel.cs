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
        private readonly Entry entry;

        public EntryViewModel(Entry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            this.entry = entry;
        }

        public int ID
        {
            get { return this.entry.ID; }
        }

        public int Database
        {
            get { return this.entry.Database; }
        }

        public string Filename
        {
            get { return Path.GetFileNameWithoutExtension(this.entry.Filename); }
        }

        public DateTime Start
        {
            get { return this.entry.Start; }
        }

        public DateTime End
        {
            get
            {
                return this.entry.End;
            }

            set
            {
                this.entry.End = value;
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
            get { return this.entry.Text; }
        }

        private string preview;

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
            get { return this.entry.Plan; }
        }

        public DataView Results
        {
            get
            {
                return this.entry.Results != null ? this.entry.Results.AsDataView() : null;
            }

            set
            {
                var table = value != null ? value.Table : null;
                this.entry.Results = table;
                this.NotifyPropertyChanged("Results");
            }
        }
    }
}
