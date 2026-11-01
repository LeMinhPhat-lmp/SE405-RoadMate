namespace RoadMate.Services.Contacts;

public static class ContactRequestValidator
{
  /// <summary>Kiểm tra tên + số điện thoại; trả về số điện thoại đã chuẩn hoá (+84...).</summary>
  public static Result<string> Validate(ContactRequest req)
  {
    if (string.IsNullOrWhiteSpace(req.Name)) return Error.Validation("INVALID_NAME", "Tên không được để trống.");
    var phone = PhoneNumber.NormalizeVietnam(req.Phone);
    return phone is null ? Error.Validation("INVALID_PHONE", "Số điện thoại Việt Nam không hợp lệ.") : phone;
  }
}
