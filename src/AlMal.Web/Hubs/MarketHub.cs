using Microsoft.AspNetCore.SignalR;

namespace AlMal.Web.Hubs;

public class MarketHub : Hub
{
    public async Task JoinStockGroup(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"stock:{symbol}");
    }

    public async Task LeaveStockGroup(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"stock:{symbol}");
    }

    public async Task JoinSectorGroup(int sectorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"sector:{sectorId}");
    }

    public async Task LeaveSectorGroup(int sectorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sector:{sectorId}");
    }

    public async Task JoinMarketGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "market:all");
    }

    public async Task LeaveMarketGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "market:all");
    }
}
