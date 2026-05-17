using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Persistence;

namespace QuartzKnowledgeMcp.Api.Bronze;

public sealed class BronzeIngestionService(
    McpKnowledgeDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task<BronzeImportResult> ImportAsync(
        CreateBronzeSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateForImport(request);

        var sourceType = BronzeSourceTypes.Normalize(request.SourceType!);
        var sourceUri = NormalizeOptional(request.SourceUri);
        var contentHash = ComputeSha256(request.RawContent!);

        var duplicate = await dbContext.BronzeSources
            .AsNoTracking()
            .FirstOrDefaultAsync(
                source => source.SourceUri == sourceUri
                    && source.ContentHash == contentHash,
                cancellationToken);

        if (duplicate is not null)
        {
            return new BronzeImportResult(duplicate, Created: false);
        }

        var source = new BronzeSource
        {
            Id = Guid.NewGuid(),
            SourceType = sourceType,
            SourceUri = sourceUri,
            RawContent = request.RawContent!,
            ContentHash = contentHash,
            Status = BronzeSourceStatuses.Imported,
            ImportedBy = NormalizeOptional(request.ImportedBy),
            ImportedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.BronzeSources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BronzeImportResult(source, Created: true);
    }

    public async Task<BronzeSourceListResponse> ListAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        status = NormalizeOptional(status);

        var query = dbContext.BronzeSources.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(source => source.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(source => source.ImportedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(source => ToResponse(source))
            .ToListAsync(cancellationToken);

        return new BronzeSourceListResponse(items, page, pageSize, totalCount);
    }

    public async Task<BronzeSourceDetailResponse?> GetDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.BronzeSources
            .AsNoTracking()
            .Where(source => source.Id == id)
            .Select(source => ToDetailResponse(source))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static BronzeSourceResponse ToResponse(BronzeSource source)
    {
        return new BronzeSourceResponse(
            source.Id,
            source.SourceType,
            source.SourceUri,
            source.Status,
            source.ImportedAtUtc);
    }

    private static BronzeSourceDetailResponse ToDetailResponse(BronzeSource source)
    {
        return new BronzeSourceDetailResponse(
            source.Id,
            source.SourceType,
            source.SourceUri,
            source.RawContent,
            source.Status,
            source.ImportedBy,
            source.ImportedAtUtc);
    }

    private static void ValidateForImport(CreateBronzeSourceRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!BronzeSourceTypes.IsAllowed(request.SourceType))
        {
            errors["sourceType"] =
            [
                "sourceType must be one of: manual, github-readme, docs-url, json-import."
            ];
        }

        if (string.IsNullOrWhiteSpace(request.RawContent))
        {
            errors["rawContent"] =
            [
                "rawContent is required."
            ];
        }

        if (errors.Count > 0)
        {
            throw new BronzeValidationException(errors);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
