using System.Text;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Service;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class SettingsServiceTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13];

    private readonly FakeSettingsRepository _repository = new();
    private readonly FakeAgencyRepository _agencies = new(new AgencyModel { Id = Guid.NewGuid(), Name = "GPP" });
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    private SettingsService Service => new(_repository, _agencies, _time);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetAsync_NothingSaved_ReturnsDefaults()
    {
        var settings = await Service.GetAsync(Ct);

        Assert.Null(settings.DisplayName);
        Assert.False(settings.PublicGtfsEnabled);
        Assert.Null(settings.LogoUpdatedAt);
    }

    [Fact]
    public async Task SaveAsync_NormalizesColorsAndUrl()
    {
        var saved = await Service.SaveAsync(new SettingsModel
        {
            DisplayName = "  Vozni red GPP  ",
            PrimaryColor = "#1565c0",
            PrimaryTextColor = " ",
            PublicUrl = "https://voznired.gpp.hr/"
        }, Ct);

        Assert.Equal("Vozni red GPP", saved.DisplayName);
        Assert.Equal("1565C0", saved.PrimaryColor);
        Assert.Null(saved.PrimaryTextColor);
        Assert.Equal("https://voznired.gpp.hr", saved.PublicUrl);
    }

    [Fact]
    public async Task SaveAsync_InvalidValues_ReportsEachField()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Service.SaveAsync(new SettingsModel
        {
            DisplayName = new string('a', SettingsService.MaxDisplayNameLength + 1),
            PrimaryColor = "plava",
            PrimaryTextColor = "FFF",
            PublicUrl = "voznired.gpp.hr"
        }, Ct));

        Assert.Equal(
            ["DisplayName", "PrimaryColor", "PrimaryTextColor", "PublicUrl"],
            ex.Errors.Keys.Order());
    }

    [Fact]
    public async Task SaveLogoAsync_Png_IsStoredWithDetectedType()
    {
        var saved = await Service.SaveLogoAsync(Png, Ct);

        Assert.Equal("image/png", _repository.Logo!.ContentType);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, saved.LogoUpdatedAt);
    }

    [Fact]
    public async Task SaveLogoAsync_NotAnImage_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            Service.SaveLogoAsync(Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"), Ct));

        Assert.Contains("Logo", ex.Errors.Keys);
        Assert.Null(_repository.Logo);
    }

    [Fact]
    public async Task SaveLogoAsync_TooLarge_IsRejected()
    {
        var content = new byte[SettingsService.MaxLogoBytes + 1];
        Png.CopyTo(content, 0);

        await Assert.ThrowsAsync<ValidationException>(() => Service.SaveLogoAsync(content, Ct));
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 }, "image/webp")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38 }, null)]
    public void DetectImageType_RecognizesBySignature(byte[] content, string? expected)
    {
        Assert.Equal(expected, SettingsService.DetectImageType(content));
    }

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>", "image/svg+xml")]
    [InlineData("﻿<?xml version=\"1.0\"?>\n<!-- logo -->\n<svg></svg>", "image/svg+xml")]
    [InlineData("<html><body><svg></svg></body></html>", null)]
    [InlineData("hello <svg>", null)]
    [InlineData("<!-- nezatvoren komentar <svg>", null)]
    public void DetectImageType_Svg(string text, string? expected)
    {
        Assert.Equal(expected, SettingsService.DetectImageType(Encoding.UTF8.GetBytes(text)));
    }

    [Fact]
    public async Task DeleteLogoAsync_NoLogo_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service.DeleteLogoAsync(Ct));
    }

    [Fact]
    public async Task GetPublicAsync_WithoutDisplayName_UsesFirstAgency()
    {
        var settings = await Service.GetPublicAsync(Ct);

        Assert.Equal("GPP", settings.DisplayName);
    }

    [Fact]
    public async Task GetPublicAsync_DisplayNameWins()
    {
        await Service.SaveAsync(new SettingsModel { DisplayName = "Vozni red", PublicGtfsEnabled = true, PublicMapEnabled = true }, Ct);

        var settings = await Service.GetPublicAsync(Ct);

        Assert.Equal("Vozni red", settings.DisplayName);
        Assert.True(settings.PublicGtfsEnabled);
        Assert.True(settings.PublicMapEnabled);
    }
}
