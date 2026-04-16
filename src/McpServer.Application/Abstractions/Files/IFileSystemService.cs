using LanguageExt;
using McpServer.Application.Files.Commands;
using McpServer.Application.Files.Results;

namespace McpServer.Application.Abstractions.Files;

public interface IFileSystemService
{
    ValueTask<Fin<FileTextResult>> ReadTextAsync(ReadFileTextCommand command, CancellationToken ct);
    ValueTask<Fin<DirectoryListingResult>> ListDirectoryAsync(ListDirectoryCommand command, CancellationToken ct);
    ValueTask<Fin<FileMetadataResult>> GetMetadataAsync(GetMetadataCommand command, CancellationToken ct);

    ValueTask<Fin<Unit>> WriteTextAsync(WriteFileTextCommand command, CancellationToken ct);
    ValueTask<Fin<Unit>> AppendTextAsync(AppendFileTextCommand command, CancellationToken ct);
    ValueTask<Fin<Unit>> CreateDirectoryAsync(CreateDirectoryCommand command, CancellationToken ct);
    ValueTask<Fin<Unit>> MovePathAsync(MovePathCommand command, CancellationToken ct);
    ValueTask<Fin<Unit>> CopyPathAsync(CopyPathCommand command, CancellationToken ct);
    ValueTask<Fin<Unit>> DeletePathAsync(DeletePathCommand command, CancellationToken ct);
}
