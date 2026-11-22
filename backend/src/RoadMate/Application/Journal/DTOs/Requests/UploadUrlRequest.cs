namespace RoadMate.Application.Journal.DTOs.Requests;

/// <param name="Kind">photo | video.</param>
/// <param name="ContentType">image/jpeg|png|webp hoặc video/mp4|quicktime; phải khớp Content-Type khi PUT.</param>
/// <param name="SizeBytes">Dung lượng dự kiến (byte), server kiểm tra không vượt giới hạn.</param>
public record UploadUrlRequest(string Kind, string ContentType, long SizeBytes);
