using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Play.Trading.Services.StateMachines;

namespace Play.Trading.Services.SignalR;

public class MessageHub : Hub
{
    [Authorize]
    public async Task SendStatusAsync(PurchaseState status)
    {
        if (Clients is not null)
            await Clients.User(Context.UserIdentifier).SendAsync("ReceivePurchaseStatus", status);
    }
}
