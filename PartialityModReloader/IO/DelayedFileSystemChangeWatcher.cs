using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace PartialityModReloader.IO
{
    public class DelayedFileSystemChangeWatcher : IDisposable
    {
        private class CacheItem<T>
        {
            public T Value { get; set; }
            public DateTime Expiration { get; set; }
        }

        private readonly FileSystemWatcher _watcher = new FileSystemWatcher {NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite, EnableRaisingEvents = false};
        private readonly Dictionary<string, CacheItem<FileSystemEventArgs>> _events = new Dictionary<string, CacheItem<FileSystemEventArgs>>();
        private readonly TimeSpan _delay;
        private readonly Timer _timer;

        #region Delegate to FileSystemWatcher

        public bool EnableRaisingEvents
        {
            get => _watcher.EnableRaisingEvents;
            set => _watcher.EnableRaisingEvents = value;
        }

        public string Filter
        {
            get => _watcher.Filter;
            set => _watcher.Filter = value;
        }

        public bool IncludeSubdirectories
        {
            get => _watcher.IncludeSubdirectories;
            set => _watcher.IncludeSubdirectories = value;
        }

        public int InternalBufferSize
        {
            get => _watcher.InternalBufferSize;
            set => _watcher.InternalBufferSize = value;
        }

        public string Path
        {
            get => _watcher.Path;
            set => _watcher.Path = value;
        }

        public event ErrorEventHandler Error
        {
            add => _watcher.Error += value;
            remove => _watcher.Error -= value;
        }

        #endregion

        public event FileSystemEventHandler Changed;

        public DelayedFileSystemChangeWatcher(TimeSpan delay)
        {
            _delay = delay;
            _timer = new Timer(_delay.TotalMilliseconds) {Enabled = true};
            _timer.Elapsed += (sender, args) => CacheEviction();
            _watcher.Changed += OnChanged;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (Changed == null) return;
            var item = new CacheItem<FileSystemEventArgs>
            {
                Value = e,
                Expiration = DateTime.Now.Add(_delay)
            };
            lock (_events)
            {
                _events.Add(e.FullPath, item);
            }
        }

        private void CacheEviction()
        {
            var expiredItems = new HashSet<string>();
            lock (_events)
            {
                foreach (KeyValuePair<string, CacheItem<FileSystemEventArgs>> pair in _events.Where(pair => pair.Value.Expiration > DateTime.Now))
                {
                    expiredItems.Add(pair.Key);
                    Changed?.Invoke(this, pair.Value.Value);
                }
                foreach (string filepath in expiredItems)
                    _events.Remove(filepath);
            }
        }

        public void Dispose()
        {
            _timer.Enabled = false;
            _timer.Close();
            _watcher.Dispose();
        }
    }
}
