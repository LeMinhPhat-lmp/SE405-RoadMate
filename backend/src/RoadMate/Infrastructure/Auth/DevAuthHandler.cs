using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;

namespace RoadMate.Infrastructure.Auth;

/// <summary>Chỉ dùng khi dev: gửi header X-Dev-Uid (tuỳ chọn X-Dev-Email, X-Dev-Phone) thay cho Firebase token.</summary>
public class DevAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
  : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
  public const string Scheme = "Dev";

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    var uid = Request.Headers["X-Dev-Uid"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(uid)) return Task.FromResult(AuthenticateResult.NoResult());
    var claims = new List<Claim>
    {
      new(UserClaims.FirebaseUid, uid), new("sub", uid),
      new("email", Request.Headers["X-Dev-Email"].FirstOrDefault() ?? $"{uid}@dev.local"),
      new("name", uid)
    };
    var phone = Request.Headers["X-Dev-Phone"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(phone)) claims.Add(new Claim("phone_number", phone));
    var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme)), Scheme);
    return Task.FromResult(AuthenticateResult.Success(ticket));
  }
}
