using Mnema.API.Repositories;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

public class DroppedContentRepository(MnemaDataContext ctx) : AbstractDbOnlyEntityRepository<DroppedContent>(ctx), IDroppedContentRepository;
