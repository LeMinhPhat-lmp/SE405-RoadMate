namespace RoadMate.Application.Journal.DTOs.Responses;

/// <param name="MediaPath">Đường dẫn object: gửi lại ở POST trips/{id}/memories (field mediaPath).</param>
/// <param name="Method">Luôn là PUT.</param>
/// <param name="Headers">Gửi y nguyên các header này khi PUT, nếu không GCS trả 403 SignatureDoesNotMatch.</param>
/// <param name="MaxBytes">Dung lượng tối đa GCS sẽ nhận cho lần upload này.</param>
public record UploadUrlResponse(string MediaPath, string UploadUrl, string Method, IReadOnlyDictionary<string, string> Headers, DateTime ExpiresAt, long MaxBytes);
