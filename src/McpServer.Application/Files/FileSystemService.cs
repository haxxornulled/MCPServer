using System.Runtime.CompilerServices;
using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files.Commands;
using McpServer.Application.Files.Results;
using McpServer.Application.Files.Utils;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Files
{
    public class FileSystemService : IFileSystemService
    {
        private readonly ILogger<FileSystemService> _logger;

        public FileSystemService(ILogger<FileSystemService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<FileTextResult>> ReadTextAsync(ReadFileTextCommand command, CancellationToken ct)
        {
            // Hot path optimization - check for null/empty immediately
            if (string.IsNullOrWhiteSpace(command.Path))
            {
                _logger.LogWarning("ReadTextAsync called with invalid path: {Path}", command.Path);
                return Fin<FileTextResult>.Fail(Error.New($"Invalid path: {command.Path}"));
            }

            try
            {
                _logger.LogInformation("Reading text from file: {Path}", command.Path);

                if (!File.Exists(command.Path))
                {
                    return Fin<FileTextResult>.Fail(Error.New($"File not found: {command.Path}"));
                }

                var encoding = EncodingHelper.GetEncoding(command.Encoding);
                var content = await File.ReadAllTextAsync(command.Path, encoding, ct);

                _logger.LogInformation("Successfully read text from file: {Path}", command.Path);
                
                return Fin<FileTextResult>.Succ(new FileTextResult(command.Path, content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read text from file: {Path}", command.Path);
                return Fin<FileTextResult>.Fail(Error.New($"Failed to read file: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<FileTextResult>> WriteTextAsync(WriteFileTextCommand command, CancellationToken ct)
        {
            // Hot path optimization - check for null/empty immediately
            if (string.IsNullOrWhiteSpace(command.Path))
            {
                _logger.LogWarning("WriteTextAsync called with invalid parameters");
                return Fin<FileTextResult>.Fail(Error.New("Path cannot be null or empty"));
            }

            try
            {
                _logger.LogInformation("Writing text to file: {Path}", command.Path);

                var directory = Path.GetDirectoryName(command.Path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var encoding = EncodingHelper.GetEncoding(command.Encoding);
                await File.WriteAllTextAsync(command.Path, command.Content, encoding, ct);

                _logger.LogInformation("Successfully wrote text to file: {Path}", command.Path);
                
                return Fin<FileTextResult>.Succ(new FileTextResult(command.Path, command.Content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write text to file: {Path}", command.Path);
                return Fin<FileTextResult>.Fail(Error.New($"Failed to write file: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<FileTextResult>> AppendTextAsync(AppendFileTextCommand command, CancellationToken ct)
        {
            // Hot path optimization - check for null/empty immediately
            if (string.IsNullOrWhiteSpace(command.Path))
            {
                _logger.LogWarning("AppendTextAsync called with invalid parameters");
                return Fin<FileTextResult>.Fail(Error.New("Path cannot be null or empty"));
            }

            try
            {
                _logger.LogInformation("Appending text to file: {Path}", command.Path);

                var directory = Path.GetDirectoryName(command.Path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var encoding = EncodingHelper.GetEncoding(command.Encoding);
                await File.AppendAllTextAsync(command.Path, command.Content, encoding, ct);

                _logger.LogInformation("Successfully appended text to file: {Path}", command.Path);
                
                return Fin<FileTextResult>.Succ(new FileTextResult(command.Path, command.Content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to append text to file: {Path}", command.Path);
                return Fin<FileTextResult>.Fail(Error.New($"Failed to append to file: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<DirectoryListingResult>> ListDirectoryAsync(ListDirectoryCommand command, CancellationToken ct)
        {
            // Hot path optimization - check for null/empty immediately
            if (string.IsNullOrWhiteSpace(command.Path))
            {
                _logger.LogWarning("ListDirectoryAsync called with invalid path: {Path}", command.Path);
                return Fin<DirectoryListingResult>.Fail(Error.New($"Invalid path: {command.Path}"));
            }

            try
            {
                _logger.LogInformation("Listing directory contents: {Path}", command.Path);

                if (!Directory.Exists(command.Path))
                {
                    return Fin<DirectoryListingResult>.Fail(Error.New($"Directory not found: {command.Path}"));
                }

                var entries = new List<DirectoryEntry>();
                var searchPattern = command.SearchPattern ?? "*";

                // Hot path optimization - use optimized directory enumeration
                var files = Directory.EnumerateFiles(command.Path, searchPattern);
                foreach (var file in files)
                {
                    entries.Add(new DirectoryEntry(Path.GetFileName(file), false));
                }

                var directories = Directory.EnumerateDirectories(command.Path, searchPattern);
                foreach (var dir in directories)
                {
                    entries.Add(new DirectoryEntry(Path.GetFileName(dir), true));
                }

                _logger.LogInformation("Successfully listed directory contents: {Path}", command.Path);
                
                return Fin<DirectoryListingResult>.Succ(new DirectoryListingResult(command.Path, entries));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list directory contents: {Path}", command.Path);
                return Fin<DirectoryListingResult>.Fail(Error.New($"Failed to list directory: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<FileMetadataResult>> GetMetadataAsync(GetMetadataCommand command, CancellationToken ct)
        {
            // Hot path optimization - check for null/empty immediately
            if (string.IsNullOrWhiteSpace(command.Path))
            {
                _logger.LogWarning("GetMetadataAsync called with invalid path: {Path}", command.Path);
                return Fin<FileMetadataResult>.Fail(Error.New($"Invalid path: {command.Path}"));
            }

            try
            {
                _logger.LogInformation("Getting metadata for path: {Path}", command.Path);

                if (!File.Exists(command.Path) && !Directory.Exists(command.Path))
                {
                    return Fin<FileMetadataResult>.Fail(Error.New($"Path not found: {command.Path}"));
                }

                // Hot path optimization - use fileInfo directly instead of multiple checks
                var fileInfo = new FileInfo(command.Path);
                var directoryInfo = new DirectoryInfo(command.Path);

                bool exists = File.Exists(command.Path) || Directory.Exists(command.Path);
                bool isDirectory = Directory.Exists(command.Path);
                long? size = null;
                
                if (File.Exists(command.Path))
                {
                    size = fileInfo.Length;
                }

                var metadata = new FileMetadataResult(
                    command.Path,
                    exists,
                    isDirectory,
                    size,
                    fileInfo.CreationTime,
                    fileInfo.LastWriteTime,
                    fileInfo.Attributes.ToString());

                _logger.LogInformation("Successfully got metadata for path: {Path}", command.Path);
                
                return Fin<FileMetadataResult>.Succ(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metadata for path: {Path}", command.Path);
                return Fin<FileMetadataResult>.Fail(Error.New($"Failed to get file metadata: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<Unit>> CreateDirectoryAsync(CreateDirectoryCommand command, CancellationToken ct)
        {
            // Hot path optimization - check for null/empty immediately
            if (string.IsNullOrWhiteSpace(command.Path))
            {
                _logger.LogWarning("CreateDirectoryAsync called with invalid path: {Path}", command.Path);
                return Fin<Unit>.Fail(Error.New($"Invalid path: {command.Path}"));
            }

            try
            {
                _logger.LogInformation("Creating directory: {Path}", command.Path);

                if (Directory.Exists(command.Path))
                {
                    _logger.LogDebug("Directory already exists: {Path}", command.Path);
                    return Fin<Unit>.Succ(Unit.Default);
                }

                Directory.CreateDirectory(command.Path);

                _logger.LogInformation("Successfully created directory: {Path}", command.Path);
                
                return Fin<Unit>.Succ(Unit.Default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {Path}", command.Path);
                return Fin<Unit>.Fail(Error.New($"Failed to create directory: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<Unit>> MovePathAsync(MovePathCommand command, CancellationToken ct)
        {
            // Hot path optimization - check for null/empty immediately
            if (string.IsNullOrWhiteSpace(command.SourcePath) || string.IsNullOrWhiteSpace(command.DestinationPath))
            {
                _logger.LogWarning("MovePathAsync called with invalid parameters");
                return Fin<Unit>.Fail(Error.New("Source and destination paths cannot be null or empty"));
            }

            try
            {
                _logger.LogInformation("Moving path from {SourcePath} to {DestinationPath}", command.SourcePath, command.DestinationPath);

                if (!File.Exists(command.SourcePath) && !Directory.Exists(command.SourcePath))
                {
                    return Fin<Unit>.Fail(Error.New($"Source path not found: {command.SourcePath}"));
                }

                var destinationDirectory = Path.GetDirectoryName(command.DestinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                if (File.Exists(command.SourcePath))
                {
                    if (command.Overwrite && File.Exists(command.DestinationPath))
                    {
                        File.Delete(command.DestinationPath);
                    }
                    File.Move(command.SourcePath, command.DestinationPath);
                }
                else
                {
                    if (command.Overwrite && Directory.Exists(command.DestinationPath))
                    {
                        Directory.Delete(command.DestinationPath, true);
                    }
                    Directory.Move(command.SourcePath, command.DestinationPath);
                }

                _logger.LogInformation("Successfully moved path from {SourcePath} to {DestinationPath}", command.SourcePath, command.DestinationPath);
                
                return Fin<Unit>.Succ(Unit.Default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move path from {SourcePath} to {DestinationPath}", command.SourcePath, command.DestinationPath);
                return Fin<Unit>.Fail(Error.New($"Failed to move path: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<Unit>> CopyPathAsync(CopyPathCommand command, CancellationToken ct)
        {
            // Hot path optimization - check for null/empty immediately
            if (string.IsNullOrWhiteSpace(command.SourcePath) || string.IsNullOrWhiteSpace(command.DestinationPath))
            {
                _logger.LogWarning("CopyPathAsync called with invalid parameters");
                return Fin<Unit>.Fail(Error.New("Source and destination paths cannot be null or empty"));
            }

            try
            {
                _logger.LogInformation("Copying path from {SourcePath} to {DestinationPath}", command.SourcePath, command.DestinationPath);

                if (!File.Exists(command.SourcePath) && !Directory.Exists(command.SourcePath))
                {
                    return Fin<Unit>.Fail(Error.New($"Source path not found: {command.SourcePath}"));
                }

                var destinationDirectory = Path.GetDirectoryName(command.DestinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                if (File.Exists(command.SourcePath))
                {
                    if (command.Overwrite && File.Exists(command.DestinationPath))
                    {
                        File.Delete(command.DestinationPath);
                    }
                    File.Copy(command.SourcePath, command.DestinationPath, command.Overwrite);
                }
                else
                {
                    CopyDirectory(command.SourcePath, command.DestinationPath, command.Overwrite);
                }

                _logger.LogInformation("Successfully copied path from {SourcePath} to {DestinationPath}", command.SourcePath, command.DestinationPath);
                
                return Fin<Unit>.Succ(Unit.Default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy path from {SourcePath} to {DestinationPath}", command.SourcePath, command.DestinationPath);
                return Fin<Unit>.Fail(Error.New($"Failed to copy path: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<Unit>> DeletePathAsync(DeletePathCommand command, CancellationToken ct)
        {
            // Hot path optimization - check for null/empty immediately
            if (string.IsNullOrWhiteSpace(command.Path))
            {
                _logger.LogWarning("DeletePathAsync called with invalid path: {Path}", command.Path);
                return Fin<Unit>.Fail(Error.New($"Invalid path: {command.Path}"));
            }

            try
            {
                _logger.LogInformation("Deleting path: {Path}", command.Path);

                if (!File.Exists(command.Path) && !Directory.Exists(command.Path))
                {
                    _logger.LogDebug("Path does not exist, nothing to delete: {Path}", command.Path);
                    return Fin<Unit>.Succ(Unit.Default); // Already deleted, so success
                }

                if (File.Exists(command.Path))
                {
                    File.Delete(command.Path);
                }
                else
                {
                    Directory.Delete(command.Path, command.Recursive);
                }

                _logger.LogInformation("Successfully deleted path: {Path}", command.Path);
                
                return Fin<Unit>.Succ(Unit.Default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete path: {Path}", command.Path);
                return Fin<Unit>.Fail(Error.New($"Failed to delete path: {ex.Message}"));
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir, bool overwrite)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");

            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                var destFile = Path.Combine(destinationDir, file.Name);
                if (overwrite && File.Exists(destFile))
                {
                    File.Delete(destFile);
                }
                file.CopyTo(destFile);
            }

            foreach (var subDir in dir.GetDirectories())
            {
                var destSubDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, destSubDir, overwrite);
            }
        }
    }
}

