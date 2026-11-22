namespace RoadMate.Application.Contacts.DTOs.Requests;

public record ContactRequest(string Name, string Phone, string? Relation, int? Priority);
