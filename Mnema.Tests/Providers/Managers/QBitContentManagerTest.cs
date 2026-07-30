using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Providers.Managers.QBit;
using NSubstitute;
using Xunit.Abstractions;

namespace Mnema.Tests.Providers.Managers;

internal sealed record Services(
    IQBitClient QBitClient,
    QBitContentManager QBitContentManager,
    IMessageService MessageService,
    ICleanupService CleanupService,
    IParserService ParserService,
    IScannerService ScannerService,
    IMetadataResolver MetadataResolver,
    IUnitOfWork UnitOfWork)
{

    internal QBitContentManager.ResolvedServices ToResolvedSeries() => new QBitContentManager.ResolvedServices(
        MetadataResolver,
        ParserService,
        ScannerService,
        default!,
        MessageService,
        UnitOfWork
    );

};

public partial class QBitContentManagerTest(ITestOutputHelper testOutputHelper): DatabaseTests(testOutputHelper)
{
    #region Helpers

    private DownloadRequestDto CreateDownloadRequestDto(MetadataBag? metadata = null) => new DownloadRequestDto
    {
        Provider = Provider.Nyaa,
        Id = SpiceAndWolfHash,
        BaseDir = string.Empty,
        TempTitle = "Spice and Wolf v01-24",
        DownloadUrl = "https://nyaa.si/download/1707045.torrent",
        Metadata = metadata ?? new MetadataBag()
    };

    #endregion

    private Services CreateServices(IUnitOfWork unitOfWork, IParserService? parser = null)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IUnitOfWork)).Returns(unitOfWork);
        serviceProvider.GetService(typeof(IConnectionService)).Returns(Substitute.For<IConnectionService>());

        var messageService = Substitute.For<IMessageService>();
        serviceProvider.GetService(typeof(IMessageService)).Returns(messageService);

        var cleanupService = Substitute.For<ICleanupService>();
        serviceProvider.GetService(typeof(ICleanupService)).Returns(cleanupService);

        var parserService = parser ?? Substitute.For<IParserService>();
        serviceProvider.GetService(typeof(IParserService)).Returns(parserService);

        var scannerService = Substitute.For<IScannerService>();
        serviceProvider.GetService(typeof(IScannerService)).Returns(scannerService);

        var metadataResolver = Substitute.For<IMetadataResolver>();
        serviceProvider.GetService(typeof(IMetadataResolver)).Returns(metadataResolver);

        var scope = Substitute.For<IServiceScope>();

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var qBitClient = Substitute.For<IQBitClient>();

        var qBitContentManager = new QBitContentManager(
            Substitute.For<ILogger<QBitContentManager>>(),
            new ApplicationConfiguration() {DownloadDir = "/"},
            scopeFactory,
            qBitClient);

        return new Services(qBitClient, qBitContentManager, messageService, cleanupService, parserService, scannerService, metadataResolver, unitOfWork);
    }
}
