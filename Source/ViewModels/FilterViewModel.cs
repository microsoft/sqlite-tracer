namespace SQLiteLogViewer.ViewModels
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Toolkit;

    public class FilterViewModel : ObservableObject
    {
        private bool enabled = false;
        private Filter filter = new Filter();

        public LogViewModel Parent { get; set; }

        internal Filter Filter
        {
            get { return this.filter; }
        }

        private static Dictionary<string, EntryType?> entryTypes = new Dictionary<string, EntryType?>
        {
            { "Any", null },
            { "Log Message", EntryType.Message },
            { "Query", EntryType.Query },
        };

        public static Dictionary<string, EntryType?> EntryTypes
        {
            get { return entryTypes; }
        }

        public string Description
        {
            get
            {
                var description = new StringBuilder();

                if (this.Complete != null)
                {
                    if (this.InvertComplete)
                    {
                        description.Append("not ");
                    }

                    description.AppendFormat("{0} ", this.Complete.Value ? "complete" : "incomplete");
                }

                if (this.Type != null)
                {
                    if (this.InvertType)
                    {
                        description.Append("not ");
                    }

                    description.AppendFormat("{0} ", Enum.GetName(typeof(EntryType), this.Type.Value));
                }

                if (!string.IsNullOrEmpty(this.Database))
                {
                    if (this.InvertDatabase)
                    {
                        description.Append("not ");
                    }

                    description.AppendFormat("from '{0}' ", this.Database);
                }

                if (!string.IsNullOrEmpty(this.Text))
                {
                    if (this.InvertText)
                    {
                        description.Append("not ");
                    }

                    description.AppendFormat("text '{0}' ", this.Text);
                }

                if (!string.IsNullOrEmpty(this.Plan))
                {
                    if (this.InvertPlan)
                    {
                        description.Append("not ");
                    }

                    description.AppendFormat("plan '{0}' ", this.Plan);
                }

                return description.ToString();
            }
        }

        public bool Enabled
        {
            get { return this.enabled; }
            set { this.SetField(ref this.enabled, value); }
        }

        public EntryType? Type
        {
            get { return this.filter.Type; }
            set { this.SetFilterField(ref this.filter.Type, value); }
        }

        public bool InvertType
        {
            get { return this.filter.Invert.HasFlag(FilterField.Type); }
            set { this.SetFilterFlag(ref this.filter.Invert, FilterField.Text, value); }
        }

        public string Database
        {
            get { return this.filter.Database; }
            set { this.SetFilterField(ref this.filter.Database, value); }
        }

        public bool InvertDatabase
        {
            get { return this.filter.Invert.HasFlag(FilterField.Database); }
            set { this.SetFilterFlag(ref this.filter.Invert, FilterField.Database, value); }
        }

        public bool? Complete
        {
            get { return this.filter.Complete; }
            set { this.SetFilterField(ref this.filter.Complete, value); }
        }

        public bool InvertComplete
        {
            get { return this.filter.Invert.HasFlag(FilterField.Complete); }
            set { this.SetFilterFlag(ref this.filter.Invert, FilterField.Complete, value); }
        }

        public string Text
        {
            get { return this.filter.Text; }
            set { this.SetFilterField(ref this.filter.Text, value); }
        }

        public bool InvertText
        {
            get { return this.filter.Invert.HasFlag(FilterField.Text); }
            set { this.SetFilterFlag(ref this.filter.Invert, FilterField.Text, value); }
        }

        public string Plan
        {
            get { return this.filter.Plan; }
            set { this.SetFilterField(ref this.filter.Plan, value); }
        }

        public bool InvertPlan
        {
            get { return this.filter.Invert.HasFlag(FilterField.Plan); }
            set { this.SetFilterFlag(ref this.filter.Invert, FilterField.Plan, value); }
        }

        private void SetFilterField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            this.SetField(ref field, value, propertyName);
            this.NotifyPropertyChanged("Description");
        }

        private void SetFilterFlag(ref FilterField flags, FilterField field, bool value, [CallerMemberName] string propertyName = "")
        {
            if (value && !flags.HasFlag(field))
            {
                flags |= field;
            }
            else if (!value && flags.HasFlag(field))
            {
                flags &= ~field;
            }
            else
            {
                return;
            }

            this.NotifyPropertyChanged(propertyName);
            this.NotifyPropertyChanged("Description");
        }
    }
}
