using AutoMapper;
using Mnema.API;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;

namespace Mnema.Database.Repositories;

public class ConnectionRepository(MnemaDataContext ctx, IMapper mapper)
    : AbstractEntityEntityRepository<Connection, ConnectionDto>(ctx, mapper), IConnectionRepository;
