namespace RoadMate.Dtos.Media;

/// <param name="Kind">photo | video.</param>
/// <param name="ContentType">image/jpeg|png|webp hoặc video/mp4|quicktime; phải khớp Content-Type khi PUT.</param>
/// <param name="SizeBytes">Dung lượng dự kiến (byte), server kiểm tra không vượt giới hạn.</param>
public record UploadUrlRequest(string Kind, string ContentType, long SizeBytes);

/// <param name="MediaPath">Đường dẫn object: gửi lại ở POST trips/{id}/memories (field mediaPath).</param>
/// <param name="Method">Luôn là PUT.</param>
/// <param name="Headers">Gửi y nguyên các header này khi PUT, nếu không GCS trả 403 SignatureDoesNotMatch.</param>
/// <param name="MaxBytes">Dung lượng tối đa GCS sẽ nhận cho lần upload này.</param>
public record UploadUrlResponse(string MediaPath, string UploadUrl, string Method, IReadOnlyDictionary<string, string> Headers, DateTime ExpiresAt, long MaxBytes);

public record MediaUrlResponse(string Url, DateTime ExpiresAt);
