namespace SQLiteLogViewer.ViewModels
{
    using Models;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using Toolkit;

    using Page = System.Tuple<int, EntryViewModel[]>;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// This class virtualizes the SQLite table stored in Log by holding a cache of fixed-size
    /// pages. Cache replacement policy is least-recently-used, implemented by moving pages to the
    /// head of a linked list on access.
    /// 
    /// PageSize and PageCount determine the cache size and can be tuned to control memory usage.
    /// </summary>
    public class VirtualLog : ObservableObject, IList<EntryViewModel>, ICollectionView, IDisposable
    {
        private const int PageSize = 512;
        private const int PageCount = 64;

        private int count = -1;
        private EntryViewModel currentItem;
        private int currentPosition;

        private bool refreshDeferred = false;

        private bool disposed = false;
        private Log log;
        private Dictionary<int, LinkedListNode<Page>> pages = new Dictionary<int, LinkedListNode<Page>>();
        private LinkedList<Page> lru = new LinkedList<Page>();

        public VirtualLog(EventAggregator events, Log log)
        {
            if (events == null)
            {
                throw new ArgumentNullException("events");
            }

            this.log = log;
            events.Subscribe<Entry>(this.EntryAdded, ThreadAffinity.PublisherThread);

            this.Filters = new ObservableCollection<FilterViewModel>();
            this.Filters.CollectionChanged += Filters_CollectionChanged;

            this.SortDescriptions = new SortDescriptionCollection();
            (this.SortDescriptions as INotifyCollectionChanged).CollectionChanged += this.Sort_CollectionChanged;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event CurrentChangingEventHandler CurrentChanging;

        public event EventHandler CurrentChanged;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // EventAggregator's Publish can sometimes race with our Unsubscribe calls here
                // thus, event handlers that use unmanaged resources must check this flag
                this.disposed = true;
            }
        }

        [Obsolete("Only for interface", true)]
        public void Add(EntryViewModel entry)
        {
            throw new InvalidOperationException("Log cannot be modified through VirtualLog.");
        }

        [Obsolete("Only for interface", true)]
        public void Insert(int index, EntryViewModel entry)
        {
            throw new InvalidOperationException("Log can only be appended to.");
        }

        [Obsolete("Only for interface", true)]
        public bool Remove(EntryViewModel entry)
        {
            throw new InvalidOperationException("Log can only be appended to.");
        }

        [Obsolete("Only for interface", true)]
        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Log can only be appended to.");
        }

        [Obsolete("Only for interface", true)]
        public void Clear()
        {
            throw new InvalidOperationException("Log can only be appended to.");
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public int IndexOf(EntryViewModel entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            var i = 0;
            foreach (var e in this)
            {
                if (e.Entry.Id == entry.Entry.Id)
                {
                    return i;
                }

                i += 1;
            }

            return -1;
        }

        public bool Contains(EntryViewModel entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            return this.IndexOf(entry) > -1;
        }

        public bool Contains(object item)
        {
            var entry = item as EntryViewModel;
            if (entry != null)
            {
                return this.Contains(entry);
            }
            else
            {
                return false;
            }
        }

        public void CopyTo(EntryViewModel[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            foreach (var entry in this)
            {
                array[index++] = entry;
            }
        }

        public Predicate<object> Filter
        {
            get { return null; }
            set { }
        }

        public bool CanFilter
        {
            get { return false; }
        }

        public ReadOnlyObservableCollection<object> Groups
        {
            get { return null; }
        }

        public ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { return null; }
        }

        public bool CanGroup
        {
            get { return false; }
        }

        public SortDescriptionCollection SortDescriptions { get; private set; }

        public bool CanSort
        {
            get { return true; }
        }

        public CultureInfo Culture { get; set; }

        public IEnumerable SourceCollection
        {
            get { return this; }
        }

        public bool IsEmpty
        {
            get { return this.Count > 0; }
        }

        public object CurrentItem
        {
            get { return this.currentItem; }
        }

        public int CurrentPosition
        {
            get { return this.currentPosition; }
        }

        public bool IsCurrentAfterLast
        {
            get { return this.CurrentPosition >= this.Count; }
        }

        public bool IsCurrentBeforeFirst
        {
            get { return this.CurrentPosition < 0; }
        }

        public bool MoveCurrentToPosition(int position)
        {
            var changing = this.CurrentChanging;
            if (changing != null)
            {
                changing(this, new CurrentChangingEventArgs(false));
            }

            if (0 <= position && position < this.Count)
            {
                this.currentPosition = position;
                this.currentItem = this[position];

                var changed = this.CurrentChanged;
                if (changed != null)
                {
                    changed(this, new EventArgs());
                }

                return true;
            }

            return false;
        }

        public bool MoveCurrentTo(object item)
        {
            var entry = item as EntryViewModel;
            if (entry == null)
            {
                throw new ArgumentException("Current item must be an EntryViewModel", "item");
            }

            var index = this.IndexOf(entry);
            if (index > -1)
            {
                return this.MoveCurrentToPosition(index);
            }

            return false;
        }

        public bool MoveCurrentToFirst()
        {
            return this.MoveCurrentToPosition(0);
        }

        public bool MoveCurrentToLast()
        {
            return this.MoveCurrentToPosition(this.Count - 1);
        }

        public bool MoveCurrentToNext()
        {
            return this.MoveCurrentToPosition(this.CurrentPosition + 1);
        }

        public bool MoveCurrentToPrevious()
        {
            return this.MoveCurrentToPosition(this.CurrentPosition - 1);
        }

        public void Refresh()
        {
            this.ResetCache();
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Required by API")]
        public IDisposable DeferRefresh()
        {
            this.refreshDeferred = true;
            return new RefreshDeferral { Parent = this };
        }

        private class RefreshDeferral : IDisposable
        {
            public VirtualLog Parent { get; set; }

            public void Dispose()
            {
                this.Parent.refreshDeferred = false;
                this.Parent.Refresh();
            }
        }

        public IEnumerator<EntryViewModel> GetEnumerator()
        {
            for (var i = 0; i < this.Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public EntryViewModel this[int index]
        {
            get
            {
                if (this.Count == 0 && index == 0)
                {
                    return null;
                }

                if (index < 0 || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                var pageIndex = index / PageSize;
                var pageOffset = index % PageSize;

                if (!this.pages.ContainsKey(pageIndex))
                {
                    if (this.lru.Count >= PageCount)
                    {
                        var evicted = this.lru.Last;
                        this.lru.RemoveLast();
                        this.pages.Remove(evicted.Value.Item1);
                    }

                    var newPage = new EntryViewModel[PageSize];
                    var i = 0;
                    foreach (var entry in this.log.GetEntries(pageIndex * PageSize, PageSize, this.FilterSet, this.SortSet))
                    {
                        newPage[i++] = new EntryViewModel(entry);
                    }

                    var node = this.lru.AddFirst(Tuple.Create(pageIndex, newPage));
                    this.pages.Add(pageIndex, node);
                }

                var page = this.pages[pageIndex];
                this.lru.Remove(page);
                this.lru.AddFirst(page);

                return page.Value.Item2[pageOffset];
            }

            set
            {
                throw new InvalidOperationException("Log can only be appended to.");
            }
        }

        public int Count
        {
            get
            {
                if (this.count == -1)
                {
                    this.count = this.log.Count(this.FilterSet, this.SortSet);
                }

                return this.count;
            }

            private set
            {
                this.count = value;
                this.NotifyPropertyChanged("Count");
            }
        }

        private ISet<Filter> FilterSet
        {
            get
            {
                return new HashSet<Filter>(this.Filters
                    .Where((filter) => filter.Enabled)
                    .Select((filter) => filter.Filter));
            }
        }

        private IDictionary<string, bool> SortSet
        {
            get
            {
                return this.SortDescriptions.ToDictionary(
                    (sort) => sort.PropertyName,
                    (sort) => sort.Direction == ListSortDirection.Ascending);
            }
        }

        private void EntryAdded(Entry entry)
        {
            if (this.disposed)
            {
                return;
            }

            this.Count += 1;

            if (this.Filters.Any((filter) => filter.Enabled))
            {
                this.ResetCache();
                return;
            }

            var index = entry.Id - 1;

            var entryViewModel = this[index];
            if (entryViewModel == null)
            {
                var pageIndex = index / PageSize;
                var pageOffset = index % PageSize;

                entryViewModel = this.pages[pageIndex].Value.Item2[pageOffset] = new EntryViewModel(entry);
            }

            var handler = this.CollectionChanged;
            if (handler != null)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, entryViewModel);
                handler(this, args);
            }
        }

        public ObservableCollection<FilterViewModel> Filters { get; private set; }

        private void ResetCache()
        {
            if (this.refreshDeferred)
            {
                return;
            }

            this.count = -1;
            this.pages.Clear();
            this.lru.Clear();

            var handler = this.CollectionChanged;
            if (handler != null)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                handler(this, args);
            }
        }

        private void Filters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var filter in e.NewItems.Cast<FilterViewModel>())
                {
                    filter.PropertyChanged += this.Filter_PropertyChanged;
                }
            }

            this.ResetCache();
        }

        private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Enabled" || (sender as FilterViewModel).Enabled)
            {
                this.ResetCache();
            }
        }

        private void Sort_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.ResetCache();
        }
    }
}
