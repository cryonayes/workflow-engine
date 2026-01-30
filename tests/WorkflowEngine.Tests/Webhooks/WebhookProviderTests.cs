using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Webhooks.Providers;

namespace WorkflowEngine.Tests.Webhooks;

public class WebhookProviderTests
{
    [Fact]
    public void DiscordWebhookProvider_ProviderType_ReturnsDiscord()
    {
        // Arrange
        var provider = new DiscordWebhookProvider(
            new HttpClient(),
            NullLogger<DiscordWebhookProvider>.Instance);

        // Assert
        provider.ProviderType.Should().Be("discord");
    }

    [Fact]
    public void SlackWebhookProvider_ProviderType_ReturnsSlack()
    {
        // Arrange
        var provider = new SlackWebhookProvider(
            new HttpClient(),
            NullLogger<SlackWebhookProvider>.Instance);

        // Assert
        provider.ProviderType.Should().Be("slack");
    }

    [Fact]
    public void TelegramWebhookProvider_ProviderType_ReturnsTelegram()
    {
        // Arrange
        var provider = new TelegramWebhookProvider(
            new HttpClient(),
            NullLogger<TelegramWebhookProvider>.Instance);

        // Assert
        provider.ProviderType.Should().Be("telegram");
    }

    [Fact]
    public void GenericHttpWebhookProvider_ProviderType_ReturnsHttp()
    {
        // Arrange
        var provider = new GenericHttpWebhookProvider(
            new HttpClient(),
            NullLogger<GenericHttpWebhookProvider>.Instance);

        // Assert
        provider.ProviderType.Should().Be("http");
    }

    [Fact]
    public async Task TelegramWebhookProvider_SendAsync_ThrowsWithoutChatId()
    {
        // Arrange
        var provider = new TelegramWebhookProvider(
            new HttpClient(),
            NullLogger<TelegramWebhookProvider>.Instance);

        var config = new WebhookConfig
        {
            Provider = "telegram",
            Url = "https://api.telegram.org/bot123/sendMessage"
            // Note: No chat_id in options
        };

        var notification = new WebhookNotification
        {
            EventType = WebhookEventType.WorkflowCompleted,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Test"
        };

        // Act & Assert
        var result = await provider.SendAsync(config, notification);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("chat_id");
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    public MockHttpMessageHandler(HttpStatusCode statusCode, string content = "")
    {
        _responseFactory = _ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_responseFactory(request));
    }
}

public class GenericHttpProviderIntegrationTests
{
    [Fact]
    public async Task GenericHttpProvider_SendAsync_IncludesCorrectPayload()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new MockHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().Result;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var provider = new GenericHttpWebhookProvider(
            httpClient,
            NullLogger<GenericHttpWebhookProvider>.Instance);

        var config = new WebhookConfig
        {
            Provider = "http",
            Url = "https://example.com/webhook",
            Headers = new Dictionary<string, string>
            {
                ["X-Custom-Header"] = "custom-value"
            }
        };

        var notification = new WebhookNotification
        {
            EventType = WebhookEventType.WorkflowCompleted,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-123",
            RunId = "run-456",
            WorkflowName = "Build Pipeline",
            Duration = TimeSpan.FromSeconds(45)
        };

        // Act
        var result = await provider.SendAsync(config, notification);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.Headers.Should().Contain(h => h.Key == "X-Custom-Header");

        var payload = JsonSerializer.Deserialize<JsonElement>(capturedBody!);
        payload.GetProperty("eventType").GetString().Should().Be("WorkflowCompleted");
        payload.GetProperty("workflowId").GetString().Should().Be("wf-123");
        payload.GetProperty("runId").GetString().Should().Be("run-456");
        payload.GetProperty("workflowName").GetString().Should().Be("Build Pipeline");
    }

    [Fact]
    public async Task GenericHttpProvider_SendAsync_RetriesOnTransientFailure()
    {
        // Arrange
        var attempts = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            attempts++;
            if (attempts < 3)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var provider = new GenericHttpWebhookProvider(
            httpClient,
            NullLogger<GenericHttpWebhookProvider>.Instance);

        var config = new WebhookConfig
        {
            Provider = "http",
            Url = "https://example.com/webhook",
            RetryCount = 3
        };

        var notification = new WebhookNotification
        {
            EventType = WebhookEventType.WorkflowCompleted,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Test"
        };

        // Act
        var result = await provider.SendAsync(config, notification);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Attempts.Should().Be(3);
    }

    [Fact]
    public async Task GenericHttpProvider_SendAsync_DoesNotRetryOnClientError()
    {
        // Arrange
        var attempts = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Invalid request")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var provider = new GenericHttpWebhookProvider(
            httpClient,
            NullLogger<GenericHttpWebhookProvider>.Instance);

        var config = new WebhookConfig
        {
            Provider = "http",
            Url = "https://example.com/webhook",
            RetryCount = 3
        };

        var notification = new WebhookNotification
        {
            EventType = WebhookEventType.WorkflowCompleted,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Test"
        };

        // Act
        var result = await provider.SendAsync(config, notification);

        // Assert
        result.IsSuccess.Should().BeFalse();
        attempts.Should().Be(1); // No retries for client errors
    }
}
