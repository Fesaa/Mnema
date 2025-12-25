using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace Mnema.Services.Store;

public class CustomTicketStore(IDistributedCache cache, TicketSerializer ticketSerializer): ITicketStore
{
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        // Note: It might not be needed to make this cryptographic random, but better safe than sorry
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var key = Convert.ToBase64String(bytes);

        await RenewAsync(key, ticket);

        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new DistributedCacheEntryOptions();
        var expiresUtc = ticket.Properties.ExpiresUtc;
        if (expiresUtc.HasValue)
        {
            options.AbsoluteExpiration = expiresUtc.Value;
        }
        else
        {
            options.SlidingExpiration = TimeSpan.FromDays(7);
        }
        
        var data = ticketSerializer.Serialize(ticket);
        await cache.SetAsync(key, data, options);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var data = await cache.GetAsync(key);
        return data == null ? null : ticketSerializer.Deserialize(data);
    }

    public async Task RemoveAsync(string key)
    {
        await cache.RemoveAsync(key);
    }
}