using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Storage;

/// <param name="Headers">Header client PHẢI gửi y nguyên khi PUT (nằm trong chữ ký), nếu không GCS trả 403.</param>
public record SignedUpload(string Url, IReadOnlyDictionary<string, string> Headers, DateTime ExpiresAt);
