using Microsoft.EntityFrameworkCore;

namespace Mnema.Database;

internal class SqliteMnemaDataContext(DbContextOptions options) : MnemaDataContext(options);
