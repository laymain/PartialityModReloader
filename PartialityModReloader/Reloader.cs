using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PartialityModReloader.IO;

namespace PartialityModReloader
{
    public class Reloader : IDisposable
    {
        private const BindingFlags AllBindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField |
                                                 BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;

        private readonly Dictionary<string, long> _reloadableMethods = new Dictionary<string, long>();
        private readonly DelayedFileSystemChangeWatcher _watcher;

        public Reloader(string folder)
        {
            BuildCache(folder);
            _watcher = new DelayedFileSystemChangeWatcher(TimeSpan.FromSeconds(3))
            {
                Path = folder,
                Filter = "*.dll"
            };
            _watcher.Changed += (sender, args) => ReloadMethods(args.FullPath);
            _watcher.EnableRaisingEvents = true;
        }

        private void BuildCache(string folder)
        {
            Log.WriteLine($"Looking for reloadable methods in {folder}");
            foreach (string filepath in Directory.GetFiles(folder, "*.dll"))
            {
                Log.WriteLine($"Analysing file: {filepath}");
                foreach (MethodInfo method in ReadReloadableMethods(filepath))
                {
                    _reloadableMethods.Add(method.GetKey(), method.GetAddress());
                    Log.WriteLine($"Reloadable method found: {method.GetKey()}");
                }
            }
        }

        private static IEnumerable<MethodInfo> ReadReloadableMethods(string filepath)
        {
            try
            {
                return
                    from type in Assembly.Load(File.ReadAllBytes(filepath)).GetTypes()
                    from method in type.GetMethods(AllBindings)
                    where method?.GetMethodImplementationFlags() == MethodImplAttributes.NoInlining
                    select method;
            }
            catch (Exception e)
            {
                Log.WriteLine($"Error while analysing file '{filepath}': {e.Message}");
                return Enumerable.Empty<MethodInfo>();
            }
        }

        private void ReloadMethods(string filepath)
        {
            Log.WriteLine($"File changed: {filepath}");
            try
            {
                foreach (MethodInfo method in ReadReloadableMethods(filepath))
                {
                    if (_reloadableMethods.TryGetValue(method.GetKey(), out long currentAddress))
                    {
                        Memory.WriteJump(currentAddress, method.GetAddress());
                        Log.WriteLine($"Reloadable method updated: {method.GetKey()}");
                    }
                    else
                    {
                        _reloadableMethods.Add(method.GetKey(), method.GetAddress());
                        Log.WriteLine($"New reloadable method found: {method.GetKey()}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine($"Error while reloading file '{filepath}': {e.Message}");
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }

    internal static class MethodInfoExtensions
    {
        public static string GetKey(this MethodInfo method)
        {
            return $"{method.DeclaringType?.FullName}::{method.Name}";
        }

        public static long GetAddress(this MethodInfo methodInfo)
        {
            long address = Memory.GetMethodStart(methodInfo, out Exception e);
            if (e != null) throw e;
            return address;
        }
    }
}
