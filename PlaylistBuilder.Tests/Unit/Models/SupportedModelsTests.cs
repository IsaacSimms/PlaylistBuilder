using FluentAssertions;
using PlaylistBuilder.Core;
using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Tests.Unit.Models;

// == SupportedModels Tests == //
public class SupportedModelsTests
{
    // == Catalog Tests == //

    [Fact]
    public void All_ContainsAtLeastOneModel()
    {
        SupportedModels.All.Should().NotBeEmpty();
    }

    [Fact]
    public void All_HasExactlyOneDefault()
    {
        SupportedModels.All.Count(m => m.IsDefault).Should().Be(1);
    }

    [Fact]
    public void All_HasUniqueIds()
    {
        var ids = SupportedModels.All.Select(m => m.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_NoEmptyIdsOrDisplayNames()
    {
        foreach (var model in SupportedModels.All)
        {
            model.Id.Should().NotBeNullOrWhiteSpace();
            model.DisplayName.Should().NotBeNullOrWhiteSpace();
        }
    }

    // == DefaultModelId Tests == //

    [Fact]
    public void DefaultModelId_ReturnsIdOfDefaultModel()
    {
        var expected = SupportedModels.All.First(m => m.IsDefault).Id;
        SupportedModels.DefaultModelId.Should().Be(expected);
    }

    [Fact]
    public void DefaultModelId_IsClaude46Sonnet()
    {
        SupportedModels.DefaultModelId.Should().Be("claude-sonnet-4-6");
    }

    // == IsValid Tests == //

    [Theory]
    [InlineData("claude-sonnet-4-6", true)]
    [InlineData("claude-3-5-sonnet-20241022", true)]
    [InlineData("claude-3-5-haiku-20241022", true)]
    [InlineData("claude-3-opus-20240229", true)]
    [InlineData("claude-3-haiku-20240307", true)]
    [InlineData("nonexistent-model", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ReturnsExpected(string? modelId, bool expected)
    {
        SupportedModels.IsValid(modelId).Should().Be(expected);
    }

    // == Resolve Tests == //

    [Fact]
    public void Resolve_WithValidModelId_ReturnsSameId()
    {
        SupportedModels.Resolve("claude-3-opus-20240229").Should().Be("claude-3-opus-20240229");
    }

    [Fact]
    public void Resolve_WithNull_ReturnsDefault()
    {
        SupportedModels.Resolve(null).Should().Be(SupportedModels.DefaultModelId);
    }

    [Fact]
    public void Resolve_WithInvalidId_ReturnsDefault()
    {
        SupportedModels.Resolve("invalid-model").Should().Be(SupportedModels.DefaultModelId);
    }

    [Fact]
    public void Resolve_WithEmptyString_ReturnsDefault()
    {
        SupportedModels.Resolve("").Should().Be(SupportedModels.DefaultModelId);
    }
}
