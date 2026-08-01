using System.Threading;
using System.Threading.Tasks;

namespace Mnema.API;

public interface ICalendarService
{
    Task<string> CreateCalendar(CancellationToken cancellationToken);
}
