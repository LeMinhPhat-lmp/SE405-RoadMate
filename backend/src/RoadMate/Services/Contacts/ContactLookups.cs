namespace RoadMate.Services.Contacts;

public static class ContactLookups
{
  /// <summary>Xác nhận mọi id đều là liên hệ của <paramref name="userId"/>; trả về danh sách id đã loại trùng.</summary>
  public static async Task<Result<List<Guid>>> ResolveOwnedIdsAsync(this IEmergencyContactRepository repo, Guid userId, IEnumerable<Guid>? ids, CancellationToken ct)
  {
    var distinct = (ids ?? new List<Guid>()).Distinct().ToList();
    if (distinct.Count == 0) return distinct;
    var found = await repo.GetByIdsAsync(distinct, userId, ct);
    return found.Count == distinct.Count ? distinct : Error.Validation("INVALID_CONTACT", "Có liên hệ không thuộc về bạn.");
  }

  /// <summary>Id tài khoản RoadMate dùng số điện thoại này (để nhận push); null nếu không có hoặc chính là chủ liên hệ.</summary>
  public static async Task<Guid?> ResolveLinkedUserIdAsync(this IUserRepository users, string phone, Guid ownerId, CancellationToken ct)
  {
    var linked = await users.GetByPhoneAsync(phone, ct);
    return linked != null && linked.Id != ownerId ? linked.Id : null;
  }
}
