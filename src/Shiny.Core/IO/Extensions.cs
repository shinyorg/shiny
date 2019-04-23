using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.IO
{
    public static class Extensions
    {
        /// <summary>
        /// Returns the full content of a file when it changes
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static IObservable<string> WhenTextContentChanged(this FileInfo file)
            => file
                .WhenChanged()
                .Where(_ => file.Exists)
                .Select(_ => File.ReadAllText(file.FullName))
                .StartWith(File.ReadAllText(file.FullName));


        /// <summary>
        /// Returns a FileSystemEvent for the file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static IObservable<FileSystemEvent> WhenChanged(this FileInfo file)
            => RxFileSystemWatcher.Create(file.FullName);


        /// <summary>
        /// Returns a FileSystemEvent for the directory
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static IObservable<FileSystemEvent> WhenChanged(this DirectoryInfo directory)
            => RxFileSystemWatcher.Create(directory.FullName);


        public static Stream ToStream(this string s, Encoding encoding = null)
        {
            var bytes = (encoding ?? Encoding.UTF8).GetBytes(s);
            return new MemoryStream(bytes);
        }


        public static string ToTempFile(this Stream stream, int bufferSize = 8192)
        {
            var path = Path.GetTempFileName();
            using (var fs = File.Create(path, bufferSize))
                stream.CopyTo(fs, bufferSize);

            return path;
        }


        public static async Task<string> ToTempFileAsync(this Stream stream, int bufferSize = 8192, CancellationToken? cancelToken = null)
        {
            var path = Path.GetTempFileName();
            using (var fs = File.Create(path, bufferSize))
                await stream.CopyToAsync(fs, bufferSize, cancelToken ?? CancellationToken.None);

            return path;
        }


        public static IObservable<FileProgress> CopyProgress(this FileInfo from, FileInfo target, bool overwriteIfExists) => Observable.Create<FileProgress>(async ob =>
        {
            var completed = false;
            var cts = new CancellationTokenSource();
            if (overwriteIfExists)
                target.DeleteIfExists();

            var buffer = new byte[65535];
            var totalCopy = 0;
            var start = DateTime.UtcNow;

            using (var readStream = from.OpenRead())
            {
                using (var writeStream = target.Create())
                {
                    var read = await readStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    while (read > 0 && !cts.IsCancellationRequested)
                    {
                        await writeStream.WriteAsync(buffer, 0, read, cts.Token).ConfigureAwait(false);
                        read = await readStream.ReadAsync(buffer, 0, buffer.Length, cts.Token).ConfigureAwait(false);
                        totalCopy += read;
                        ob.OnNext(new FileProgress(totalCopy, from.Length, start));
                    }
                }
            }
            completed = true;
            ob.OnCompleted();

            return () =>
            {
                cts.Cancel();
                if (!completed)
                    target.DeleteIfExists();
            };
        });


        public static FileInfo GetExistingFile(this DirectoryInfo directory, string fileName)
            => directory
                .GetFiles()
                .FirstOrDefault(x => x.Name.Equals(fileName, StringComparison.Ordinal));



        public static FileDeleteResult TryDeleteIfExists(this FileInfo file) => TryDeleteIfExists(file.FullName);

        public static FileDeleteResult TryDeleteIfExists(string filePath)
        {
            if (!File.Exists(filePath))
                return FileDeleteResult.DoesNotExist;

            try
            {
                File.Delete(filePath);
                return FileDeleteResult.Success;
            }
            catch
            {
                return FileDeleteResult.Fail;
            }
        }


        public static bool IsSuccess(this FileDeleteResult result) => result != FileDeleteResult.Fail;
        public static void DeleteIfExists(this FileInfo file) => DeleteIfExists(file.FullName);
        public static void DeleteIfExists(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}