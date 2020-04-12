using System;
using System.IO;
using System.Runtime.Caching;
using System.Timers;

namespace PartialityModReloader.IO
{
    public class DelayedFileSystemChangeWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher = new FileSystemWatcher {NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite, EnableRaisingEvents = false};
        private readonly MemoryCache _events = new MemoryCache(nameof(DelayedFileSystemChangeWatcher));
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
            _timer = new Timer(_delay.TotalMilliseconds) { Enabled = true };
            _timer.Elapsed += (sender, args) => _events.Trim(1); // force cache expiration check every "delay"
            _watcher.Changed += OnChanged;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (Changed != null)
            {
                _events.Set(e.FullPath, e, new CacheItemPolicy {SlidingExpiration = _delay, RemovedCallback = RemovedCallback});
            }
        }

        private void RemovedCallback(CacheEntryRemovedArguments arguments)
        {
            if (arguments.RemovedReason == CacheEntryRemovedReason.Expired)
                Changed?.Invoke(this, arguments.CacheItem.Value as FileSystemEventArgs);
        }

        public void Dispose()
        {
            _timer.Enabled = false;
            _timer.Close();
            _watcher.Dispose();
            _events.Dispose();
        }
    }
}
