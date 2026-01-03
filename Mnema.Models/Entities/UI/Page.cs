using System;
using System.Collections.Generic;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Models.Entities.UI;

public class Page
{
   
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Icon { get; set; } = string.Empty;
    public required int SortValue { get; set; }
    public required Provider Provider { get; set; }
    public string CustomRootDir { get; set; } = string.Empty;
    
    public IList<MnemaUser> Users { get; set; } = [];

}