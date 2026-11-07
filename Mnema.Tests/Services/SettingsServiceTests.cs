using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.DTOs;
using Mnema.Services;
using NSubstitute;
using Xunit.Abstractions;

namespace Mnema.Tests.Services;

[TestSubject(typeof(SettingsService))]
public class SettingsServiceTests(ITestOutputHelper testOutputHelper) : DatabaseTests(testOutputHelper)
{
    private static SettingsService CreateService(IUnitOfWork unitOfWork)
    {
        return new SettingsService(unitOfWork, Substitute.For<INamingService>());
    }

    [Fact]
    public async Task GetSettingAsync_ReturnsKeyDefault_WhenRowDoesNotExist()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var service = CreateService(unitOfWork);

        var value = await service.GetSettingAsync(ServerSettingKeys.MaxConcurrentImages);

        Assert.Equal(5, value);
        Assert.Empty(await unitOfWork.SettingsRepository.GetSettingsAsync());
    }

    [Fact]
    public async Task SetSettingAsync_CreatesRow_WithNamedKey()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var service = CreateService(unitOfWork);

        await service.SetSettingAsync(ServerSettingKeys.MaxConcurrentImages, 3);

        var setting = await unitOfWork.SettingsRepository.GetSettingAsync("maxConcurrentImages");
        Assert.NotNull(setting);
        Assert.Equal("3", setting.Value);
    }

    [Fact]
    public async Task SetSettingAsync_UpdatesExistingRow()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var service = CreateService(unitOfWork);

        await service.SetSettingAsync(ServerSettingKeys.ImageConversionLossless, true);
        await service.SetSettingAsync(ServerSettingKeys.ImageConversionLossless, false);

        var setting = await unitOfWork.SettingsRepository.GetSettingAsync("imageConversionLossless");
        Assert.NotNull(setting);
        Assert.Equal("false", setting.Value);
    }

    [Fact]
    public async Task DateTimeKey_RoundTrips()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var service = CreateService(unitOfWork);
        var installDate = new DateTime(2026, 11, 7, 13, 31, 0, DateTimeKind.Utc);

        await service.SetSettingAsync(ServerSettingKeys.InstallDate, installDate);

        Assert.Equal(installDate, await service.GetSettingAsync(ServerSettingKeys.InstallDate));
    }

    [Fact]
    public async Task SaveSettingsAsync_WritesAllEditableSettings()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var service = CreateService(unitOfWork);

        await service.SaveSettingsAsync(new UpdateServerSettingsDto
        {
            MaxConcurrentImages = 2,
            AutoDisableProviderAfter = 7,
            ImageConversionLossless = true,
            ImageConversionQuality = 90,
        });

        var dto = await service.GetSettingsAsync();

        Assert.Equal(2, dto.MaxConcurrentImages);
        Assert.Equal(7, dto.AutoDisableProviderAfter);
        Assert.True(dto.ImageConversionLossless);
        Assert.Equal(90, dto.ImageConversionQuality);
    }

    [Fact]
    public async Task Password_RoundTripsAsString()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var service = CreateService(unitOfWork);

        await service.SetSettingAsync(ServerSettingKeys.Password, "hash");

        Assert.Equal("hash", await service.GetSettingAsync(ServerSettingKeys.Password));
    }

    [Fact]
    public void ServerSettingKeys_AreUnique()
    {
        var keys = typeof(ServerSettingKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (string)f.FieldType.GetProperty("Key")!.GetValue(f.GetValue(null))!)
            .ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }
}
