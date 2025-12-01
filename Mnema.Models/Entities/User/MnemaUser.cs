using Mnema.Models.Entities.Content;

namespace Mnema.Models.Entities.User;

public class MnemaUser
{
    public Guid Id { get; set; }
    
    public string ExternalId { get; set; }
    
    public UserPreferences Preferences { get; set; }
    public IList<Subscription> Subscriptions { get; set; }
}