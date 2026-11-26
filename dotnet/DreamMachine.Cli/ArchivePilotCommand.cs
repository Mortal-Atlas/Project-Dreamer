using System;
using System.Diagnostics;
using DreamMachine.Libraries;

namespace DreamMachine.Cli
{
    static class ArchivePilotCommand
    {
        public static int Run(string[] args, int option)
        {
            int count = 1;
            if (option + 1 >= args.Length || args[option + 1].StartsWith("--", StringComparison.Ordinal) ||
                (args.Length != option + 2 &&
                    (args.Length != option + 4 || args[option + 2] != "--runs" ||
                        !int.TryParse(args[option + 3], out count) || count < 1)))
            {
                Console.Error.WriteLine("Usage: --archive-pilots <directory> [--runs positive-count]");
                return 2;
            }
            var archive = new PilotLibraryArchive(args[option + 1]);
            var watch = Stopwatch.StartNew();
            PilotVolume last = null;
            try
            {
                for (int seed = 0; seed < count; seed++)
                    last = archive.Capture(seed + 1, true, true);
                long captureMs = watch.ElapsedMilliseconds;
                LibraryStorage stats = archive.Storage();
                watch.Restart();
                var books = archive.Search(last.Id, LibraryShelf.LivedMemory, "teacher");
                var body = archive.Read(last.Id, books[0].Id);
                double retrievalMs = watch.Elapsed.TotalMilliseconds;
                watch.Restart();
                var restored = archive.Restore(last.Id);
                double restoreMs = watch.Elapsed.TotalMilliseconds;
                Console.WriteLine(FormattableString.Invariant($"volumes {stats.Volumes} | books {stats.Books} | unique bodies {stats.Blobs}"));
                Console.WriteLine(FormattableString.Invariant($"physical bytes {stats.PhysicalBytes} | logical body bytes {stats.LogicalContentBytes}"));
                Console.WriteLine(FormattableString.Invariant($"capture {captureMs} ms | teacher search/read {retrievalMs:F2} ms ({body.Length} chars) | checkpoint {restoreMs:F2} ms"));
                Console.WriteLine(FormattableString.Invariant($"restored tick {restored.TickCount} | peak working set {Process.GetCurrentProcess().PeakWorkingSet64} bytes"));
                Console.WriteLine("last volume " + last.Id);
                return 0;
            }
            catch (System.IO.IOException error)
            {
                Console.Error.WriteLine(error.Message);
                return 1;
            }
        }
    }
}
