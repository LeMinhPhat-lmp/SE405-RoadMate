namespace RoadMate.Dtos.Contacts;

public static class ContactMappers
{
  public static ContactDto ToDto(this EmergencyContact c) => new(c.Id, c.Name, c.Phone, c.Relation, c.Priority, c.LinkedUserId != null);
}
