using RoadMate.Application.Contacts.DTOs.Responses;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Contacts.Mappers;

public static class ContactMappers
{
  public static ContactResponse ToDto(this EmergencyContact c) => new(c.Id, c.Name, c.Phone, c.Relation, c.Priority, c.LinkedUserId != null);
}
