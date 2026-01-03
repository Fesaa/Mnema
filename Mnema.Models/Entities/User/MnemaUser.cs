using System;
using System.Collections.Generic;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.UI;

namespace Mnema.Models.Entities.User;

public class MnemaUser
{
    public Guid Id { get; set; }
    
    public UserPreferences Preferences { get; set; }
    public IList<Subscription> Subscriptions { get; set; }
    public IList<Page> Pages { get; set; }
    public IList<Notification> Notifications { get; set; }
}