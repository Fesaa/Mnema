using System;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Database;
using Mnema.Models;
using NSubstitute;
using Xunit.Abstractions;

namespace Mnema.Tests;

public abstract class DatabaseTests(ITestOutputHelper testOutputHelper): IAsyncDisposable
{
    private SqliteConnection? _connection;
    private SqliteMnemaDataContext? _context;

    internal async Task<(IUnitOfWork, SqliteMnemaDataContext, IMapper)> CreateDatabase()
    {

        GlobalConfiguration.Configuration.UseInMemoryStorage();

        // Dispose any previous connection if CreateDatabase is called multiple times
        if (_connection != null)
        {
            await _context!.DisposeAsync();
            await _connection.DisposeAsync();
        }

        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var contextOptions = ((DbContextOptionsBuilder)new DbContextOptionsBuilder<SqliteMnemaDataContext>()
                .UseSqlite(_connection)).EnableSensitiveDataLogging()
            .Options;

        _context = new SqliteMnemaDataContext(contextOptions);

        await _context.Database.EnsureCreatedAsync();


        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(AutoMapperProfiles).Assembly);
        }, Substitute.For<ILoggerFactory>());
        var mapper = config.CreateMapper();

        var unitOfWork = new UnitOfWork(Substitute.For<ILogger<UnitOfWork>>(), _context, mapper);

        _context.ChangeTracker.Clear();

        return (unitOfWork, _context, mapper);
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
