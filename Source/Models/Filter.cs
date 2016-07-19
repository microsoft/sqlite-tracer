namespace SQLiteLogViewer.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Flags]
    public enum FilterField
    {
        None = 0,
        Type = 1,
        Database = 2,
        Complete = 4,
        Text = 8,
        Plan = 16,
    }

    public struct Filter
    {
        public FilterField Invert;
        public EntryType? Type;
        public string Database;
        public bool? Complete;
        public string Text;
        public string Plan;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Filter a, Filter b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Filter a, Filter b)
        {
            return !a.Equals(b);
        }
    }
}
