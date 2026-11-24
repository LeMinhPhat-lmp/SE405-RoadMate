using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadMate.Api.Startup;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Journal.DTOs.Responses;
using RoadMate.Infrastructure.Storage;

namespace RoadMate.Tests.Api;

public class MediaUploadUrlTests
{
  [Fact]
  public void AddWebApi_serializes_signed_upload_url_without_escaping_ampersand()
  {
    var services = new ServiceCollection();
    var cfg = new ConfigurationBuilder().Build();
    services.AddWebApi(cfg, new StubHostEnvironment());

    using var sp = services.BuildServiceProvider();
    var jsonOptions = sp.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

    var url = "https://storage.googleapis.com/se405-roadmate.firebasestorage.app/users/alice/trips/1/a.jpg"
      + "?X-Goog-Algorithm=GOOG4-RSA-SHA256"
      + "&X-Goog-Credential=sa%40proj.iam.gserviceaccount.com%2F20261124%2Fauto%2Fstorage%2Fgoog4_request"
      + "&X-Goog-Date=20261124T140157Z"
      + "&X-Goog-Expires=900"
      + "&X-Goog-SignedHeaders=content-type%3Bhost"
      + "&X-Goog-Signature=deadbeef";

    var dto = new UploadUrlResponse(
      "users/alice/trips/1/a.jpg",
      url,
      "PUT",
      new Dictionary<string, string> { ["Content-Type"] = "image/jpeg" },
      new DateTime(2026, 11, 24, 14, 16, 57, DateTimeKind.Utc),
      5242880);

    var json = JsonSerializer.Serialize(dto, jsonOptions);

    Assert.DoesNotContain(@"\u0026", json, StringComparison.Ordinal);
    Assert.Contains("&X-Goog-Credential=", json, StringComparison.Ordinal);
    Assert.Contains("&X-Goog-Signature=deadbeef", json, StringComparison.Ordinal);
  }

  [Fact]
  public void GcsMediaStorage_CreateUploadUrl_defaults_to_signing_content_type_and_host_only()
  {
    using var rsa = RSA.Create(2048);
    var credential = new ServiceAccountCredential(
      new ServiceAccountCredential.Initializer("firebase-adminsdk-fbsvc@se405-roadmate.iam.gserviceaccount.com")
        .FromPrivateKey(rsa.ExportPkcs8PrivateKeyPem()));

    var opts = new StorageOptions { Bucket = "se405-roadmate.firebasestorage.app" };
    Assert.False(opts.EnforceSizeLimit);

    var storage = new GcsMediaStorage(
      credential,
      null!,
      Options.Create(opts),
      TimeProvider.System,
      NullLogger<GcsMediaStorage>.Instance);

    var objectPath = "users/alice/trips/60d10bc7-7e6c-4c83-8f47-658fcc171d3d/0e50e326f31c4477b1cf106188f5f98e.jpg";
    var result = storage.CreateUploadUrl(objectPath, "image/jpeg", 5242880);

    Assert.True(result.IsSuccess);
    var signed = result.Value;
    Assert.Single(signed.Headers);
    Assert.Equal("image/jpeg", signed.Headers["Content-Type"]);

    var uri = new Uri(signed.Url);
    var rawParams = ParseRawQuery(uri);
    Assert.Equal("content-type%3Bhost", rawParams["X-Goog-SignedHeaders"]);

    VerifyV4Signature(rsa, "PUT", uri, rawParams,
      [$"content-type:{signed.Headers["Content-Type"]}", "host:storage.googleapis.com"],
      "content-type;host");
  }

  [Fact]
  public void GcsMediaStorage_CreateUploadUrl_with_enforce_size_limit_signs_content_length_range()
  {
    using var rsa = RSA.Create(2048);
    var credential = new ServiceAccountCredential(
      new ServiceAccountCredential.Initializer("firebase-adminsdk-fbsvc@se405-roadmate.iam.gserviceaccount.com")
        .FromPrivateKey(rsa.ExportPkcs8PrivateKeyPem()));

    var storage = new GcsMediaStorage(
      credential,
      null!,
      Options.Create(new StorageOptions
      {
        Bucket = "se405-roadmate.firebasestorage.app",
        UploadUrlMinutes = 15,
        MaxImageBytes = 5242880,
        EnforceSizeLimit = true
      }),
      TimeProvider.System,
      NullLogger<GcsMediaStorage>.Instance);

    var objectPath = "users/alice/trips/60d10bc7-7e6c-4c83-8f47-658fcc171d3d/c20d13c8eb304d37aa4773ca383b4195.jpg";
    var result = storage.CreateUploadUrl(objectPath, "image/jpeg", 5242880);

    Assert.True(result.IsSuccess);
    var signed = result.Value;
    Assert.Equal("image/jpeg", signed.Headers["Content-Type"]);
    Assert.Equal("0,5242880", signed.Headers["x-goog-content-length-range"]);

    var uri = new Uri(signed.Url);
    var rawParams = ParseRawQuery(uri);
    Assert.Equal("content-type%3Bhost%3Bx-goog-content-length-range", rawParams["X-Goog-SignedHeaders"]);

    VerifyV4Signature(rsa, "PUT", uri, rawParams,
      [$"content-type:{signed.Headers["Content-Type"]}", "host:storage.googleapis.com", $"x-goog-content-length-range:{signed.Headers["x-goog-content-length-range"]}"],
      "content-type;host;x-goog-content-length-range");
  }

  [Fact]
  public void GcsMediaStorage_CreateDownloadUrl_produces_verifiable_v4_get_signature()
  {
    using var rsa = RSA.Create(2048);
    var credential = new ServiceAccountCredential(
      new ServiceAccountCredential.Initializer("firebase-adminsdk-fbsvc@se405-roadmate.iam.gserviceaccount.com")
        .FromPrivateKey(rsa.ExportPkcs8PrivateKeyPem()));

    var storage = new GcsMediaStorage(
      credential,
      null!,
      Options.Create(new StorageOptions { Bucket = "se405-roadmate.firebasestorage.app" }),
      TimeProvider.System,
      NullLogger<GcsMediaStorage>.Instance);

    var result = storage.CreateDownloadUrl("users/alice/trips/1/a.jpg");

    Assert.True(result.IsSuccess);
    var uri = new Uri(result.Value.Url);
    var rawParams = ParseRawQuery(uri);
    Assert.Equal("host", rawParams["X-Goog-SignedHeaders"]);

    VerifyV4Signature(rsa, "GET", uri, rawParams, ["host:storage.googleapis.com"], "host");
  }

  private static Dictionary<string, string> ParseRawQuery(Uri uri) => uri.Query.TrimStart('?')
    .Split('&', StringSplitOptions.RemoveEmptyEntries)
    .Select(p => p.Split('=', 2))
    .ToDictionary(p => p[0], p => p[1], StringComparer.Ordinal);

  private static void VerifyV4Signature(RSA rsa, string method, Uri uri, Dictionary<string, string> rawParams,
    IEnumerable<string> canonicalHeaders, string signedHeaders)
  {
    var signatureHex = rawParams["X-Goog-Signature"];
    var date = rawParams["X-Goog-Date"];
    var dateOnly = date[..8];

    var canonicalQuery = string.Join("&",
      rawParams.Where(kv => kv.Key != "X-Goog-Signature")
        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
        .Select(kv => $"{kv.Key}={kv.Value}"));

    var canonicalRequest = string.Join("\n",
      new[] { method, uri.AbsolutePath, canonicalQuery }
        .Concat(canonicalHeaders)
        .Concat(["", signedHeaders, "UNSIGNED-PAYLOAD"]));

    var canonicalHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
    var stringToSign = string.Join("\n",
      "GOOG4-RSA-SHA256",
      date,
      $"{dateOnly}/auto/storage/goog4_request",
      canonicalHash);

    Assert.True(
      rsa.VerifyData(Encoding.UTF8.GetBytes(stringToSign), Convert.FromHexString(signatureHex), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
      "V4 signature did not verify against the canonical request.");
  }

  private sealed class StubHostEnvironment : IHostEnvironment
  {
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "RoadMate.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
  }
}
