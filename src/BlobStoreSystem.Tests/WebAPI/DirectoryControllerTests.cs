using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlobStoreSystem.Tests.WebAPI;

public class DirectoryControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public DirectoryControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnauthorizedRequests_ShouldReturn401()
    {
        var response = await _client.PostAsync("/api/directory/create?path=/TestDir", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateDirectory_ShouldSucceed_WhenAuthenticated()
    {
        // 1. Register a new user
        var registerResp = await _client.PostAsJsonAsync("/api/auth/register",
            new {Username = "TestUser", Password = "Tu123"});
        registerResp.EnsureSuccessStatusCode();

        // 2. Login to get JWT
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Username = "TestUser", Password = "Tu123" });
        loginResp.EnsureSuccessStatusCode();

        var loginJson = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginJson?.Token;
        Assert.False(string.IsNullOrEmpty(token));

        // 3. Set Authorization header
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 4. Create directory
        var createResp = await _client.PostAsync("/api/directory/create?path=/TestDir", null);
        createResp.EnsureSuccessStatusCode();

        // 5. Verify response message
        var createResult = JsonSerializer.Deserialize<CreateDeleteResponse>(
            await createResp.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        Assert.NotNull(createResult);
        Assert.Equal("/TestDir", createResult.Path);

        // 6. List directory to confirm presence
        var listResp = await _client.GetAsync("/api/directory/list?path=/");
        listResp.EnsureSuccessStatusCode();
        
        var nodes = await listResp.Content.ReadFromJsonAsync<List<FsNodeDto>>();
        Assert.NotNull(nodes);

        // We expect "TestDir" as a child of "/"
        Assert.Contains(nodes, n => n.Path == "/TestDir");
    }

    [Fact]
    public async Task MoveDirectory_ShouldUpdateItsPath()
    {
        var token = await RegisterAndLoginAsync("TestUser", "Tu123");
        _client.DefaultRequestHeaders.Authorization
            = new AuthenticationHeaderValue("Bearer", token);

        // Create "/OldDir"
        var createResp = await _client.PostAsync("/api/directory/create?path=/OldDir", null);
        createResp.EnsureSuccessStatusCode();

        // Move "/OldDir" to "/NewDir"
        var moveResp = await _client.PostAsync("/api/directory/move?oldPath=/OldDir&newPath=/NewDir", null);
        moveResp.EnsureSuccessStatusCode();

        // List root to confirm
        var listResp = await _client.GetAsync("/api/directory/list?path=/");
        listResp.EnsureSuccessStatusCode();

        var nodes = await listResp.Content.ReadFromJsonAsync<List<FsNodeDto>>();
        Assert.NotNull(nodes);

        // "/OldDir" should not exist
        Assert.DoesNotContain(nodes, x => x.Path == "/OldDir");
        // "/NewDir" should exist
        Assert.Contains(nodes, x => x.Path == "/NewDir");
    }

    private async Task<string> RegisterAndLoginAsync(string username, string password)
    {
        // Register
        var regResp = await _client.PostAsJsonAsync("/api/auth/register",
            new { Username = username, Password = password });
        regResp.EnsureSuccessStatusCode();

        // Login
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Username = username, Password = password });
        loginResp.EnsureSuccessStatusCode();

        var loginObj = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        return loginObj?.Token ?? string.Empty;
    }

    private record LoginResponse(string Token);
    private record CreateDeleteResponse(string Message, string Path);
    private record FsNodeDto
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
    }
}