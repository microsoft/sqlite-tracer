namespace SQLiteLogViewer.ViewModels
{
    using Models;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Toolkit;

    using Page = System.Tuple<int, EntryViewModel[]>;

    /// <summary>
    /// This class virtualizes the SQLite table stored in Log by holding a cache of fixed-size
    /// pages. Cache replacement policy is least-recently-used, implemented by moving pages to the
    /// head of a linked list on access.
    /// 
    /// PageSize and PageCount determine the cache size and can be tuned to control memory usage.
    /// </summary>
    public class VirtualLog : ObservableObject, IList<EntryViewModel>, INotifyCollectionChanged, IDisposable
    {
        private const int PageSize = 128;
        private const int PageCount = 4;

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
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

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

        public void Add(EntryViewModel entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            this.log.AddEntry(entry.Entry);
        }

        public void Insert(int index, EntryViewModel entry)
        {
            throw new InvalidOperationException("Log can only be appended to.");
        }

        public bool Remove(EntryViewModel entry)
        {
            throw new InvalidOperationException("Log can only be appended to.");
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Log can only be appended to.");
        }

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

            return entry.Entry.Id - 1;
        }

        public bool Contains(EntryViewModel entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            return entry.Entry.Id - 1 < this.Count;
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

        public EntryViewModel this[int index]
        {
            get
            {
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
                    foreach (var entry in this.log.GetEntries(pageIndex * PageSize, PageSize))
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

        private int count = -1;

        public int Count
        {
            get
            {
                if (this.count == -1)
                {
                    this.count = this.log.Count;
                }

                return this.count;
            }

            private set
            {
                this.count = value;
                this.NotifyPropertyChanged("Count");
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

        private void EntryAdded(Entry entry)
        {
            if (this.disposed)
            {
                return;
            }

            this.Count += 1;

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
    }
}
