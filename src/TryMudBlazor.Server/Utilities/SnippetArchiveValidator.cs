using System.IO.Compression;
using Try.Core;
using TryMudBlazor.Client.Services;

namespace TryMudBlazor.Server.Utilities;

/// <summary>
/// Checks an uploaded snippet archive before it is stored: it must be a zip of a few small
/// .razor/.cs files that pass the same rules the client applies when creating a snippet.
/// </summary>
public static class SnippetArchiveValidator
{
    public const int MaxArchiveBytes = 512 * 1024;
    public const int MaxFileBytes = 256 * 1024;
    public const int MaxFiles = 20;

    /// <summary>
    /// Returns null when the archive is acceptable, otherwise a message describing the first problem.
    /// </summary>
    public static string? Validate(Stream archiveStream)
    {
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            return "The snippet must be a zip archive.";
        }

        using (archive)
        {
            if (archive.Entries.Count == 0)
            {
                return "The snippet contains no files.";
            }

            if (archive.Entries.Count > MaxFiles)
            {
                return $"A snippet can contain at most {MaxFiles} files.";
            }

            var codeFiles = new List<CodeFile>(archive.Entries.Count);
            foreach (var entry in archive.Entries)
            {
                if (entry.Length > MaxFileBytes)
                {
                    return $"File '{entry.FullName}' is larger than {MaxFileBytes / 1024} KB.";
                }

                using var reader = new StreamReader(entry.Open());
                codeFiles.Add(new CodeFile { Path = entry.FullName, Content = reader.ReadToEnd() });
            }

            return CodeFilesHelper.ValidateCodeFilesForSnippetCreation(codeFiles);
        }
    }
}
