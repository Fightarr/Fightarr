using System.Runtime.InteropServices;
using Fightarr.Api.Data;
using Fightarr.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Fightarr.Api.Services;

/// <summary>
/// Handles importing downloaded media files into the library
/// </summary>
public class FileImportService
{
    private readonly FightarrDbContext _db;
    private readonly MediaFileParser _parser;
    private readonly FileNamingService _namingService;
    private readonly DownloadClientService _downloadClientService;
    private readonly ILogger<FileImportService> _logger;

    // Supported video file extensions
    private static readonly string[] VideoExtensions = { ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts" };

    public FileImportService(
        FightarrDbContext db,
        MediaFileParser parser,
        FileNamingService namingService,
        DownloadClientService downloadClientService,
        ILogger<FileImportService> logger)
    {
        _db = db;
        _parser = parser;
        _namingService = namingService;
        _downloadClientService = downloadClientService;
        _logger = logger;
    }

    /// <summary>
    /// Import a completed download
    /// </summary>
    public async Task<ImportHistory> ImportDownloadAsync(DownloadQueueItem download)
    {
        _logger.LogInformation("Starting import for download: {Title} (ID: {DownloadId})",
            download.Title, download.DownloadId);

        // Update status to importing
        download.Status = DownloadStatus.Importing;
        await _db.SaveChangesAsync();

        try
        {
            // Get event
            var eventInfo = await _db.Events
                .Include(e => e.QualityProfile)
                .FirstOrDefaultAsync(e => e.Id == download.EventId);

            if (eventInfo == null)
            {
                throw new Exception($"Event {download.EventId} not found");
            }

            // Get media management settings
            var settings = await GetMediaManagementSettingsAsync();

            // Get download path from download client
            var downloadPath = await GetDownloadPathAsync(download);

            if (string.IsNullOrEmpty(downloadPath) || !Directory.Exists(downloadPath) && !File.Exists(downloadPath))
            {
                throw new Exception($"Download path not found: {downloadPath}");
            }

            // Find video files
            var videoFiles = FindVideoFiles(downloadPath);

            if (videoFiles.Count == 0)
            {
                throw new Exception($"No video files found in: {downloadPath}");
            }

            // For now, take the largest file (most likely the main video)
            var sourceFile = videoFiles.OrderByDescending(f => new FileInfo(f).Length).First();
            var fileInfo = new FileInfo(sourceFile);

            _logger.LogInformation("Found video file: {File} ({Size:N0} bytes)",
                sourceFile, fileInfo.Length);

            // Parse filename
            var parsed = _parser.Parse(Path.GetFileName(sourceFile));

            // Build destination path
            var rootFolder = await GetBestRootFolderAsync(settings, fileInfo.Length);
            var destinationPath = BuildDestinationPath(settings, eventInfo, parsed, fileInfo.Extension, rootFolder);

            _logger.LogInformation("Destination path: {Path}", destinationPath);

            // Check free space
            if (!settings.SkipFreeSpaceCheck)
            {
                CheckFreeSpace(destinationPath, fileInfo.Length, settings.MinimumFreeSpace);
            }

            // Create destination directory
            var destDir = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir!);
                _logger.LogDebug("Created directory: {Directory}", destDir);
            }

            // Move or copy file
            await TransferFileAsync(sourceFile, destinationPath, settings);

            // Set permissions (Linux/macOS only)
            if (settings.SetPermissions && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetFilePermissions(destinationPath, settings);
            }

            // Create import history record
            var history = new ImportHistory
            {
                EventId = eventInfo.Id,
                Event = eventInfo,
                DownloadQueueItemId = download.Id,
                DownloadQueueItem = download,
                SourcePath = sourceFile,
                DestinationPath = destinationPath,
                Quality = _parser.BuildQualityString(parsed),
                Size = fileInfo.Length,
                Decision = ImportDecision.Approved,
                ImportedAt = DateTime.UtcNow
            };

            _db.ImportHistories.Add(history);

            // Update download status
            download.Status = DownloadStatus.Imported;
            download.ImportedAt = DateTime.UtcNow;

            // Update event status
            eventInfo.Status = EventStatus.Downloaded;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Successfully imported: {Title} -> {Path}",
                download.Title, destinationPath);

            // Clean up download folder if configured
            if (settings.RemoveCompletedDownloads)
            {
                await CleanupDownloadAsync(downloadPath, sourceFile);
            }

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import download: {Title}", download.Title);

            // Update status to failed
            download.Status = DownloadStatus.Failed;
            download.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync();

            throw;
        }
    }

    /// <summary>
    /// Find all video files in a directory (or return the file if it's a single file)
    /// </summary>
    private List<string> FindVideoFiles(string path)
    {
        var files = new List<string>();

        if (File.Exists(path))
        {
            // Single file
            if (IsVideoFile(path))
                files.Add(path);
        }
        else if (Directory.Exists(path))
        {
            // Directory - search recursively
            files.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(IsVideoFile));
        }

        return files;
    }

    /// <summary>
    /// Check if file is a video file
    /// </summary>
    private bool IsVideoFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return VideoExtensions.Contains(ext);
    }

    /// <summary>
    /// Build destination file path
    /// </summary>
    private string BuildDestinationPath(
        MediaManagementSettings settings,
        Event eventInfo,
        ParsedFileInfo parsed,
        string extension,
        string rootFolder)
    {
        var destinationPath = rootFolder;

        // Add event folder if configured
        if (settings.CreateEventFolder)
        {
            var folderName = _namingService.BuildFolderName(settings.EventFolderFormat, eventInfo);
            destinationPath = Path.Combine(destinationPath, folderName);
        }

        // Build filename
        string filename;
        if (settings.RenameFiles)
        {
            var tokens = new FileNamingTokens
            {
                EventTitle = eventInfo.Title,
                EventTitleThe = eventInfo.Title,
                AirDate = eventInfo.Date,
                Quality = parsed.Quality ?? "Unknown",
                QualityFull = _parser.BuildQualityString(parsed),
                ReleaseGroup = parsed.ReleaseGroup ?? string.Empty,
                OriginalTitle = parsed.EventTitle,
                OriginalFilename = Path.GetFileNameWithoutExtension(parsed.EventTitle)
            };

            filename = _namingService.BuildFileName(settings.StandardFileFormat, tokens, extension);
        }
        else
        {
            filename = parsed.EventTitle + extension;
        }

        destinationPath = Path.Combine(destinationPath, filename);

        // Handle duplicates
        destinationPath = GetUniqueFilePath(destinationPath);

        return destinationPath;
    }

    /// <summary>
    /// Get unique file path (add number if file exists)
    /// </summary>
    private string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path))
            return path;

        var directory = Path.GetDirectoryName(path)!;
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        var counter = 1;
        string newPath;

        do
        {
            newPath = Path.Combine(directory, $"{filenameWithoutExt} ({counter}){extension}");
            counter++;
        }
        while (File.Exists(newPath));

        return newPath;
    }

    /// <summary>
    /// Transfer file (move, copy, or hardlink)
    /// </summary>
    private async Task TransferFileAsync(string source, string destination, MediaManagementSettings settings)
    {
        _logger.LogDebug("Transferring: {Source} -> {Destination}", source, destination);

        if (settings.UseHardlinks && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Create hardlink (Linux/macOS)
            CreateHardLink(source, destination);
        }
        else if (settings.CopyFiles)
        {
            // Copy file
            await CopyFileAsync(source, destination);
        }
        else
        {
            // Move file
            File.Move(source, destination, overwrite: false);
        }
    }

    /// <summary>
    /// Copy file asynchronously
    /// </summary>
    private async Task CopyFileAsync(string source, string destination)
    {
        const int bufferSize = 81920; // 80KB buffer

        using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
        using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

        await sourceStream.CopyToAsync(destStream);
    }

    /// <summary>
    /// Create hardlink (Linux/macOS only)
    /// </summary>
    private void CreateHardLink(string source, string destination)
    {
        // On Unix systems, use ln command
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ln",
                Arguments = $"\"{source}\" \"{destination}\"",
                UseShellExecute = false,
                RedirectStandardError = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new Exception($"Failed to create hardlink: {error}");
        }
    }

    /// <summary>
    /// Set file permissions (Linux/macOS only)
    /// </summary>
    private void SetFilePermissions(string path, MediaManagementSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.FileChmod))
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"{settings.FileChmod} \"{path}\"",
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
        }

        if (!string.IsNullOrEmpty(settings.ChownUser))
        {
            var chown = settings.ChownUser;
            if (!string.IsNullOrEmpty(settings.ChownGroup))
                chown += ":" + settings.ChownGroup;

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chown",
                    Arguments = $"{chown} \"{path}\"",
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }

    /// <summary>
    /// Check if there's enough free space
    /// </summary>
    private void CheckFreeSpace(string path, long fileSize, long minimumFreeSpaceMB)
    {
        var drive = new DriveInfo(Path.GetPathRoot(path)!);
        var availableSpaceMB = drive.AvailableFreeSpace / 1024 / 1024;
        var fileSizeMB = fileSize / 1024 / 1024;

        if (availableSpaceMB - fileSizeMB < minimumFreeSpaceMB)
        {
            throw new Exception($"Not enough free space. Available: {availableSpaceMB} MB, Required: {fileSizeMB + minimumFreeSpaceMB} MB");
        }
    }

    /// <summary>
    /// Get best root folder based on free space
    /// </summary>
    private async Task<string> GetBestRootFolderAsync(MediaManagementSettings settings, long fileSize)
    {
        var rootFolders = settings.RootFolders
            .Where(rf => rf.Accessible)
            .OrderByDescending(rf => rf.FreeSpace)
            .ToList();

        if (rootFolders.Count == 0)
        {
            throw new Exception("No accessible root folders configured");
        }

        // Return first folder with enough space
        var fileSizeMB = fileSize / 1024 / 1024;
        var folder = rootFolders.FirstOrDefault(rf => rf.FreeSpace > fileSizeMB + settings.MinimumFreeSpace);

        if (folder == null)
        {
            // Fall back to folder with most space
            folder = rootFolders.First();
            _logger.LogWarning("No root folder has enough free space, using folder with most space: {Path}", folder.Path);
        }

        return folder.Path;
    }

    /// <summary>
    /// Get download path from download client
    /// </summary>
    private async Task<string> GetDownloadPathAsync(DownloadQueueItem download)
    {
        if (download.DownloadClient == null)
        {
            throw new Exception("Download client not found");
        }

        // Query download client for status which includes save path
        var status = await _downloadClientService.GetDownloadStatusAsync(download.DownloadClient, download.DownloadId);

        if (status?.SavePath != null)
        {
            return status.SavePath;
        }

        // Fallback to default path if status doesn't include it
        _logger.LogWarning("Could not get save path from download client, using fallback");
        return Path.Combine(Path.GetTempPath(), "downloads", download.DownloadId);
    }

    /// <summary>
    /// Clean up download folder after successful import
    /// </summary>
    private async Task CleanupDownloadAsync(string downloadPath, string importedFile)
    {
        try
        {
            if (File.Exists(importedFile))
            {
                File.Delete(importedFile);
                _logger.LogDebug("Deleted source file: {File}", importedFile);
            }

            // If the download was in a folder, try to delete empty folder
            if (Directory.Exists(downloadPath))
            {
                var remainingFiles = Directory.GetFiles(downloadPath, "*.*", SearchOption.AllDirectories);
                if (remainingFiles.Length == 0)
                {
                    Directory.Delete(downloadPath, recursive: true);
                    _logger.LogDebug("Deleted empty download folder: {Folder}", downloadPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup download folder: {Path}", downloadPath);
        }
    }

    /// <summary>
    /// Get media management settings
    /// </summary>
    private async Task<MediaManagementSettings> GetMediaManagementSettingsAsync()
    {
        var settings = await _db.MediaManagementSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            // Create default settings
            settings = new MediaManagementSettings
            {
                RootFolders = new List<RootFolder>(),
                RenameFiles = true,
                StandardFileFormat = "{Event Title} - {Air Date} - {Quality Full}",
                CreateEventFolder = true,
                EventFolderFormat = "{Event Title}",
                CopyFiles = false,
                MinimumFreeSpace = 100,
                RemoveCompletedDownloads = true
            };

            _db.MediaManagementSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return settings;
    }
}
