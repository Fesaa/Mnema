using Mnema.Common;
using Mnema.Models.Entities.External;

namespace Mnema.Models.DTOs;

public class ExternalConnectionDto
{
    public Guid Id { get; set; }
    
    public ExternalConnectionType Type { get; set; }
    public string Name { get; set; }
    public List<ExternalConnectionEvent> FollowedEvents { get; set; }
    public MetadataBag Metadata { get; set; }
}