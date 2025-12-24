using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Mnema.Services.Hubs;

[Authorize]
internal class MessageHub: Hub {}