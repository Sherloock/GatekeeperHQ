using GatekeeperHQ.Domain.Entities;

namespace GatekeeperHQ.Infrastructure.Webhooks;

public interface IWebhookDispatcher
{
    Task DispatchAsync(Webhook webhook, string eventName, object payload);
}
