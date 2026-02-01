using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GatekeeperHQ.Tests.Common.Extensions;

public static class HttpClientExtensions
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};

	/// <summary>
	/// Adds a Bearer token to the Authorization header.
	/// </summary>
	public static HttpClient WithBearerToken(this HttpClient client, string token)
	{
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		return client;
	}

	/// <summary>
	/// Adds an API key to the X-API-Key header.
	/// </summary>
	public static HttpClient WithApiKey(this HttpClient client, string apiKey)
	{
		client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
		return client;
	}

	/// <summary>
	/// Adds a tenant ID to the X-Tenant-ID header.
	/// </summary>
	public static HttpClient WithTenantId(this HttpClient client, int tenantId)
	{
		client.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId.ToString());
		return client;
	}

	/// <summary>
	/// Removes the Authorization header.
	/// </summary>
	public static HttpClient ClearAuth(this HttpClient client)
	{
		client.DefaultRequestHeaders.Authorization = null;
		return client;
	}

	/// <summary>
	/// Deserializes the response content as the specified type.
	/// </summary>
	public static async Task<T?> ReadAsJsonAsync<T>(this HttpResponseMessage response)
	{
		var content = await response.Content.ReadAsStringAsync();
		if (string.IsNullOrWhiteSpace(content))
			return default;

		return JsonSerializer.Deserialize<T>(content, JsonOptions);
	}

	/// <summary>
	/// Posts JSON content to the specified URL.
	/// </summary>
	public static async Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T content)
	{
		return await client.PostAsJsonAsync(url, content, JsonOptions);
	}

	/// <summary>
	/// Puts JSON content to the specified URL.
	/// </summary>
	public static async Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T content)
	{
		return await client.PutAsJsonAsync(url, content, JsonOptions);
	}
}
