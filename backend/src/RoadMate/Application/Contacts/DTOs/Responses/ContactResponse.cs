namespace RoadMate.Application.Contacts.DTOs.Responses;

public record ContactResponse(Guid Id, string Name, string Phone, string? Relation, int Priority, bool HasApp);
