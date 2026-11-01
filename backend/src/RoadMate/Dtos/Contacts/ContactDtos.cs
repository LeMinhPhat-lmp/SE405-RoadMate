namespace RoadMate.Dtos.Contacts;

public record ContactRequest(string Name, string Phone, string? Relation, int? Priority);
public record ContactDto(Guid Id, string Name, string Phone, string? Relation, int Priority, bool HasApp);
