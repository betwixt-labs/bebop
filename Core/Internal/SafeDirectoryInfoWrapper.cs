
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Core.Internal;

/// <summary>
/// A wrapper for <see cref="DirectoryInfoBase" /> that doesn't throw for inaccessible directories or files.
/// </summary>
/// <remarks>
/// This is a workaround for WASI where some weird behavior is observed when preopening directories.
/// </remarks>
internal class SafeDirectoryInfoWrapper : DirectoryInfoBase
{
    private readonly DirectoryInfo _directoryInfo;
    private readonly bool _isParentPath;

    private static readonly EnumerationOptions _enumerationOptions = new()
    {
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
        IgnoreInaccessible = true
    };

    /// <summary>
    /// Initializes an instance of <see cref="SafeDirectoryInfoWrapper" />.
    /// </summary>
    /// <param name="directoryInfo">The <see cref="DirectoryInfo" />.</param>
    public SafeDirectoryInfoWrapper(DirectoryInfo directoryInfo)
        : this(directoryInfo, isParentPath: false)
    { }

    private SafeDirectoryInfoWrapper(DirectoryInfo directoryInfo, bool isParentPath)
    {
        _directoryInfo = directoryInfo;
        _isParentPath = isParentPath;
    }

    /// <inheritdoc />
    public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
    {
        if (_directoryInfo.Exists)
        {
            IEnumerable<FileSystemInfo> fileSystemInfos;
            try
            {
                fileSystemInfos = _directoryInfo.EnumerateFileSystemInfos("*", _enumerationOptions);
            }
            catch (DirectoryNotFoundException)
            {
                yield break;
            }
            foreach (FileSystemInfo fileSystemInfo in fileSystemInfos)
            {
                if (fileSystemInfo is DirectoryInfo directoryInfo)
                {
                    yield return new SafeDirectoryInfoWrapper(directoryInfo);
                }
                else
                {
                    yield return new FileInfoWrapper((FileInfo)fileSystemInfo);
                }
            }
        }
    }

    /// <summary>
    /// Returns an instance of <see cref="DirectoryInfoBase" /> that represents a subdirectory.
    /// </summary>
    /// <remarks>
    /// If <paramref name="name" /> equals '..', this returns the parent directory.
    /// </remarks>
    /// <param name="name">The directory name</param>
    /// <returns>The directory</returns>
    public override DirectoryInfoBase? GetDirectory(string name)
    {
        bool isParentPath = string.Equals(name, "..", StringComparison.Ordinal);

        if (isParentPath)
        {
            return new SafeDirectoryInfoWrapper(
                new DirectoryInfo(Path.Combine(_directoryInfo.FullName, name)),
                isParentPath);
        }
        else
        {
            DirectoryInfo[] dirs = _directoryInfo.GetDirectories(name);

            if (dirs.Length == 1)
            {
                return new SafeDirectoryInfoWrapper(dirs[0], isParentPath);
            }
            else if (dirs.Length == 0)
            {
                return null;
            }
            else
            {
                // This shouldn't happen. The parameter name isn't supposed to contain wild card.
                throw new InvalidOperationException(
                    $"More than one sub directories are found under {_directoryInfo.FullName} with name {name}.");
            }
        }
    }

    /// <inheritdoc />
    public override FileInfoBase GetFile(string name)
        => new FileInfoWrapper(new FileInfo(Path.Combine(_directoryInfo.FullName, name)));

    /// <inheritdoc />
    public override string Name => _isParentPath ? ".." : _directoryInfo.Name;

    /// <summary>
    /// Returns the full path to the directory.
    /// </summary>
    /// <remarks>
    /// Equals the value of <seealso cref="System.IO.FileSystemInfo.FullName" />.
    /// </remarks>
    public override string FullName => _directoryInfo.FullName;

    /// <summary>
    /// Returns the parent directory.
    /// </summary>
    /// <remarks>
    /// Equals the value of <seealso cref="System.IO.DirectoryInfo.Parent" />.
    /// </remarks>
    public override DirectoryInfoBase? ParentDirectory
        => new DirectoryInfoWrapper(_directoryInfo.Parent!);
}