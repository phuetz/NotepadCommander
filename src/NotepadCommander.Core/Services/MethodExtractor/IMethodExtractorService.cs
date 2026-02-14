using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.MethodExtractor;

public interface IMethodExtractorService
{
    IAsyncEnumerable<MethodInfo> ExtractMethods(
        string directory,
        string[] methodNames,
        CancellationToken cancellationToken = default);
}
