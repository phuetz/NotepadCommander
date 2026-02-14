using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.Search;

public interface IMultiFileSearchService
{
    IAsyncEnumerable<FileSearchResult> SearchInDirectory(
        string directory,
        string pattern,
        MultiFileSearchOptions options,
        CancellationToken cancellationToken = default);
}
