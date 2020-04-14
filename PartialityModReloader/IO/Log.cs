using System;

namespace PartialityModReloader.IO
{
    public static class Log
    {
        public static void WriteLine(string message)
        {
            Console.WriteLine($"[{nameof(PartialityModReloader)}] {DateTime.Now:s}: {message}");
        }
    }
}
