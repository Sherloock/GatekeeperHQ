using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GatekeeperHQ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GatekeeperHQ.Infrastructure.Webhooks;

public class WebhookDispatcher : IWebhookDispatcher
{
	private readonly HttpClient _httpClient;
	private readonly ILogger<WebhookDispatcher> _logger;
	private const int MaxRetries = 3;
	private const int BaseDelayMs = 1000;

	public WebhookDispatcher(HttpClient httpClient, ILogger<WebhookDispatcher> logger)
	{
		_httpClient = httpClient;
		_logger = logger;
	}

	public async Task DispatchAsync(Webhook webhook, string eventName, object payload)
	{
		var payloadJson = JsonSerializer.Serialize(payload);
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
		var signature = ComputeSignature(webhook.Secret, timestamp, payloadJson);

		var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
		{
			Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
		};

		request.Headers.Add("X-Webhook-Event", eventName);
		request.Headers.Add("X-Webhook-Signature", signature);
		request.Headers.Add("X-Webhook-Timestamp", timestamp);

		for (int attempt = 1; attempt <= MaxRetries; attempt++)
		{
			try
			{
				var response = await _httpClient.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformation("Webhook dispatched successfully: {Url}, Event: {Event}", webhook.Url, eventName);
					return;
				}

				_logger.LogWarning("Webhook dispatch failed: {Url}, Status: {Status}, Attempt: {Attempt}",
					webhook.Url, response.StatusCode, attempt);

				if (attempt < MaxRetries)
				{
					var delay = BaseDelayMs * (int)Math.Pow(2, attempt - 1); // Exponential backoff
					await Task.Delay(delay);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error dispatching webhook: {Url}, Attempt: {Attempt}", webhook.Url, attempt);

				if (attempt < MaxRetries)
				{
					var delay = BaseDelayMs * (int)Math.Pow(2, attempt - 1);
					await Task.Delay(delay);
				}
			}
		}

		_logger.LogError("Webhook dispatch failed after {MaxRetries} attempts: {Url}", MaxRetries, webhook.Url);
	}

	private static string ComputeSignature(string secret, string timestamp, string payload)
	{
		var message = $"{timestamp}.{payload}";
		var keyBytes = Encoding.UTF8.GetBytes(secret);
		var messageBytes = Encoding.UTF8.GetBytes(message);

		using var hmac = new HMACSHA256(keyBytes);
		var hashBytes = hmac.ComputeHash(messageBytes);
		return Convert.ToHexString(hashBytes).ToLower();
	}
}
