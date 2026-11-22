namespace RoadMate.Application.Weather.DTOs.Responses;

/// <param name="FailedCount">Số điểm không lấy được dự báo (các điểm còn lại vẫn trả về).</param>
public record TripWeatherResponse(Guid TripId, List<TripWeatherPointResponse> Points, int FailedCount);
