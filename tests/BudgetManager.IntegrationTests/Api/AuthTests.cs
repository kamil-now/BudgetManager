using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api;

public class AuthTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
  [Theory]
  [ClassData(typeof(EndpointsWithoutAuthentication))]
  public async Task Endpoint_WithoutAuthentication_ShouldReturn401(Task<HttpResponseMessage> sendAsync, string _)
  {
    var response = await sendAsync;
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  private sealed class EndpointsWithoutAuthentication : TheoryData<Task<HttpResponseMessage>, string>
  {
    public EndpointsWithoutAuthentication()
    {
      var httpClient = ApiFixture.SharedClient
        ?? throw new InvalidOperationException("ApiFixture.SharedClient has not been initialized.");

      var openApiResponse = httpClient.GetAsync("/openapi/v1.json").Result;
      if (!openApiResponse.IsSuccessStatusCode)
        throw new InvalidOperationException($"Failed to retrieve OpenAPI specification: {openApiResponse.StatusCode} {openApiResponse.RequestMessage?.RequestUri}");

      var json = openApiResponse.Content.ReadAsStringAsync().Result;
      var doc = JsonDocument.Parse(json);

      var paths = doc.RootElement.GetProperty("paths");
      foreach (var path in paths.EnumerateObject())
      {
        foreach (var operation in path.Value.EnumerateObject())
        {
          if (!operation.Value.TryGetProperty("responses", out var responses))
            continue;

          if (!responses.TryGetProperty("401", out _))
            continue;

          var summary = operation.Value.TryGetProperty("summary", out var s) ? s.GetString() : null;
          var label = summary ?? $"{operation.Name} {path.Name}";

          Add(
            operation.Name.ToLower() switch
            {
              "get" => httpClient.GetAsync(path.Name),
              "post" => httpClient.PostAsync(path.Name, JsonContent.Create(new { })),
              "put" => httpClient.PutAsync(path.Name, JsonContent.Create(new { })),
              "delete" => httpClient.DeleteAsync(path.Name),
              "patch" => httpClient.PatchAsync(path.Name, JsonContent.Create(new { })),
              _ => throw new NotSupportedException($"Operation {operation.Name} not supported")
            },
            label
          );
        }
      }
    }
  }
}