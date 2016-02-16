﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wyam.Common.IO;

namespace Wyam.Core.IO
{
    // Initially based on code from Cake (http://cakebuild.net/)
    internal sealed class FileSystem : IConfigurableFileSystem
    {
        public bool IsCaseSensitive { get; set; }

        public PathComparer PathComparer { get; private set; }

        private DirectoryPath _rootPath = System.IO.Directory.GetCurrentDirectory();
        private DirectoryPath _outputPath = "output";
        
        public DirectoryPath RootPath
        {
            get { return _rootPath; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(RootPath));
                }
                if (value.IsRelative)
                {
                    throw new ArgumentException("The root path must not be relative");
                }
                _rootPath = value;
            }
        }

        public PathCollection<DirectoryPath> InputPaths { get; private set; }

        IReadOnlyList<DirectoryPath> IFileSystem.InputPaths => InputPaths;

        public DirectoryPath OutputPath
        {
            get { return _outputPath; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(OutputPath));
                }
                _outputPath = value;
            }
        }

        public IFile GetInputFile(FilePath path) =>
            path.IsRelative ? GetInput(inputPath => 
                new File(RootPath.Combine(inputPath).CombineFile(path).Collapse())) : new File(path);

        public IDirectory GetInputDirectory(DirectoryPath path) =>
            path.IsRelative ? GetInput(inputPath => 
                new Directory(RootPath.Combine(inputPath).Combine(path).Collapse())) : new Directory(path);

        public IReadOnlyList<IDirectory> GetInputDirectories() =>
            InputPaths.Select(GetRootDirectory).ToImmutableArray();

        private T GetInput<T>(Func<DirectoryPath, T> factory) where T : IFileSystemInfo
        {
            T notFound = default(T);
            foreach (DirectoryPath inputPath in InputPaths.Reverse())
            {
                T info = factory(inputPath);
                if (notFound == null)
                {
                    notFound = info;
                }
                if (info.Exists)
                {
                    return info;
                }
            }
            if (notFound == null)
            {
                throw new InvalidOperationException("The input paths collection must have at least one path");
            }
            return notFound;
        }

        public IFile GetOutputFile(FilePath path) =>
            new File(RootPath.Combine(OutputPath).CombineFile(path).Collapse());

        public IDirectory GetOutputDirectory(DirectoryPath path) =>
            new Directory(RootPath.Combine(OutputPath).Combine(path).Collapse());

        public IDirectory GetOutputDirectory() =>
            GetRootDirectory(OutputPath);

        public IFile GetRootFile(FilePath path) =>
            new File(RootPath.CombineFile(path).Collapse());

        public IDirectory GetRootDirectory(DirectoryPath path) =>
            new Directory(RootPath.Combine(path).Collapse());

        public IDirectory GetRootDirectory() =>
            new Directory(RootPath.Collapse());

        public IFile GetFile(FilePath path)
        {
            if (path.IsRelative)
            {
                throw new ArgumentException("The path must be absolute");
            }
            return new File(path.Collapse());
        }

        public IDirectory GetDirectory(DirectoryPath path)
        {
            if (path.IsRelative)
            {
                throw new ArgumentException("The path must be absolute");
            }
            return new Directory(path.Collapse());
        }

        public FileSystem()
        {
            PathComparer = new PathComparer(this);
            InputPaths = new PathCollection<DirectoryPath>(
                new[]
                {
                    DirectoryPath.FromString("input")
                }, PathComparer);
        }

        // *** Retry logic (used by File and Directory)

        private static readonly TimeSpan InitialInterval = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan IntervalDelta = TimeSpan.FromMilliseconds(100);
        
        private const int RetryCount = 3;

        public static T Retry<T>(Func<T> func)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    TimeSpan? interval = ShouldRetry(retryCount, ex);
                    if (!interval.HasValue)
                    {
                        throw;
                    }
                    Thread.Sleep(interval.Value);
                }
                retryCount++;
            }
        }

        public static void Retry(Action action)
        {
            Retry<object>(() =>
            {
                action();
                return null;
            });
        }

        private static TimeSpan? ShouldRetry(int retryCount, Exception exception) =>
            (exception is IOException || exception is UnauthorizedAccessException) && retryCount < RetryCount
                ? (TimeSpan?)InitialInterval.Add(TimeSpan.FromMilliseconds(IntervalDelta.TotalMilliseconds * retryCount)) : null;
    }
}
