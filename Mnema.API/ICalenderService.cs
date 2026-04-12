using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mnema.API;

public interface ICalendarService
{
    Task<string> CreateCalendar(Guid userId, CancellationToken cancellationToken);
}
