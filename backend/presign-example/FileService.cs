using FileService.DTOs;
using FileService.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Messaging.Contracts.Common;
using System.Security.Claims;

namespace FileService.Services.Implements
{
    public class FileService : IFileService
    {
        private const int SignedUrlExpiresInSeconds = 900;
        private static readonly TimeSpan SignedUrlDuration = TimeSpan.FromSeconds(SignedUrlExpiresInSeconds);

        private readonly IConfiguration _configuration;

        public FileService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ApiResponse<PresignUrlResponse> GetUserUploadUrl(string? fileName, string? contentType, ClaimsPrincipal user)
        {
            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType))
                return new ApiResponse<PresignUrlResponse>(StatusCodes.Status400BadRequest, "Invalid upload url request");

            var userId = GetCurrentUserId(user);
            if (!userId.HasValue)
                return new ApiResponse<PresignUrlResponse>(StatusCodes.Status401Unauthorized, "Invalid user context");

            var filePath = $"users/{userId}/{CreateSafeFileName(fileName)}";

            return new ApiResponse<PresignUrlResponse>(
                StatusCodes.Status200OK,
                CreateUploadUrl(filePath, contentType.Trim()));
        }

        public ApiResponse<PresignReadUrlResponse> GetUserReadUrl(string? fileKey, ClaimsPrincipal user)
        {
            var normalizedFileKey = NormalizeFileKey(fileKey);

            if (string.IsNullOrWhiteSpace(normalizedFileKey))
                return new ApiResponse<PresignReadUrlResponse>(StatusCodes.Status400BadRequest, "File key is required");

            if (!CanReadUserFile(normalizedFileKey, user))
                return new ApiResponse<PresignReadUrlResponse>(StatusCodes.Status403Forbidden, "You can only read your own files");

            return new ApiResponse<PresignReadUrlResponse>(
                StatusCodes.Status200OK,
                CreateReadUrl(normalizedFileKey));
        }

        public ApiResponse<PresignUrlResponse> GetDeliveryUploadUrl(Guid orderId, Guid shipperId, string? stage, string? fileName, string? contentType, ClaimsPrincipal user)
        {
            if (orderId == Guid.Empty || shipperId == Guid.Empty || string.IsNullOrWhiteSpace(stage) || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType))
                return new ApiResponse<PresignUrlResponse>(StatusCodes.Status400BadRequest, "Invalid upload url request");

            if (!CanAccessShipper(user, shipperId))
                return new ApiResponse<PresignUrlResponse>(StatusCodes.Status403Forbidden, "You can only create upload URLs for your own delivery");

            var filePath = $"deliveries/{orderId}/{shipperId}/{stage.Trim().ToLowerInvariant()}/{CreateSafeFileName(fileName)}";

            return new ApiResponse<PresignUrlResponse>(
                StatusCodes.Status200OK,
                CreateUploadUrl(filePath, contentType.Trim()));
        }

        public ApiResponse<PresignReadUrlResponse> GetDeliveryReadUrl(string? fileKey, ClaimsPrincipal user)
        {
            var normalizedFileKey = NormalizeFileKey(fileKey);

            if (string.IsNullOrWhiteSpace(normalizedFileKey))
                return new ApiResponse<PresignReadUrlResponse>(StatusCodes.Status400BadRequest, "File key is required");

            if (!TryGetShipperIdFromDeliveryFileKey(normalizedFileKey, out var shipperId))
                return new ApiResponse<PresignReadUrlResponse>(StatusCodes.Status400BadRequest, "Invalid delivery file key");

            if (!CanAccessShipper(user, shipperId))
                return new ApiResponse<PresignReadUrlResponse>(StatusCodes.Status403Forbidden, "You can only read files for your own delivery");

            return new ApiResponse<PresignReadUrlResponse>(
                StatusCodes.Status200OK,
                CreateReadUrl(normalizedFileKey));
        }

        private PresignUrlResponse CreateUploadUrl(string objectName, string contentType)
        {
            var requestTemplate = CreateRequestTemplate(objectName, HttpMethod.Put)
                .WithContentHeaders(new Dictionary<string, IEnumerable<string>>
                {
                    { "Content-Type", new[] { contentType } }
                });

            return new PresignUrlResponse
            {
                FileKey = objectName,
                UploadUrl = Sign(requestTemplate),
                ContentType = contentType
            };
        }

        private PresignReadUrlResponse CreateReadUrl(string objectName)
        {
            var requestTemplate = CreateRequestTemplate(objectName, HttpMethod.Get);

            return new PresignReadUrlResponse
            {
                FileKey = objectName,
                ReadUrl = Sign(requestTemplate),
                ExpiresInSeconds = SignedUrlExpiresInSeconds
            };
        }

        private UrlSigner.RequestTemplate CreateRequestTemplate(string objectName, HttpMethod httpMethod)
        {
            var bucketName = _configuration["BUCKET_NAME"];

            if (string.IsNullOrWhiteSpace(bucketName))
                throw new InvalidOperationException("BUCKET_NAME configuration is missing.");

            return UrlSigner.RequestTemplate
                .FromBucket(bucketName)
                .WithObjectName(objectName)
                .WithHttpMethod(httpMethod);
        }

        private static string Sign(UrlSigner.RequestTemplate requestTemplate)
        {
            var credential = GoogleCredential.GetApplicationDefault();
            var urlSigner = UrlSigner.FromCredential(credential);
            var options = UrlSigner.Options.FromDuration(SignedUrlDuration);

            return urlSigner.Sign(requestTemplate, options);
        }

        private static string CreateSafeFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return $"{Guid.NewGuid()}{extension}";
        }

        private static bool CanReadUserFile(string fileKey, ClaimsPrincipal user)
        {
            if (IsCurrentUserAdmin(user))
                return true;

            var userId = GetCurrentUserId(user);
            return userId.HasValue && fileKey.StartsWith($"users/{userId}/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanAccessShipper(ClaimsPrincipal user, Guid shipperId)
        {
            if (IsCurrentUserAdmin(user))
                return true;

            var currentShipperId = GetCurrentShipperId(user);
            if (currentShipperId.HasValue)
                return currentShipperId.Value == shipperId;

            return IsCurrentUserInRole(user, "Shipper", "SHIPPER");
        }

        private static Guid? GetCurrentUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? user.FindFirstValue("userId");

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private static Guid? GetCurrentShipperId(ClaimsPrincipal user)
        {
            var shipperIdClaim = user.FindFirstValue("shipperId")
                ?? user.FindFirstValue("ShipperId")
                ?? user.FindFirstValue("shipper_id");

            return Guid.TryParse(shipperIdClaim, out var shipperId) ? shipperId : null;
        }

        private static bool IsCurrentUserAdmin(ClaimsPrincipal user)
        {
            return IsCurrentUserInRole(user, "Admin", "ADMIN");
        }

        private static bool IsCurrentUserInRole(ClaimsPrincipal user, params string[] allowedRoles)
        {
            var roles = user.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Concat(user.FindAll("role").Select(c => c.Value));

            return roles.Any(role => allowedRoles.Any(allowed => string.Equals(role, allowed, StringComparison.OrdinalIgnoreCase)));
        }

        private static string NormalizeFileKey(string? fileKey)
        {
            return string.IsNullOrWhiteSpace(fileKey)
                ? string.Empty
                : fileKey.Trim().TrimStart('/').Replace('\\', '/');
        }

        private static bool TryGetShipperIdFromDeliveryFileKey(string fileKey, out Guid shipperId)
        {
            shipperId = Guid.Empty;

            var segments = fileKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 5 || !string.Equals(segments[0], "deliveries", StringComparison.OrdinalIgnoreCase))
                return false;

            return Guid.TryParse(segments[2], out shipperId);
        }
    }
}
