using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PlaylistBuilder.Api.Controllers;
using PlaylistBuilder.Core;
using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Tests.Unit.Controllers;

// == ModelsController Tests == //
public class ModelsControllerTests
{
    private readonly ModelsController _controller;

    public ModelsControllerTests()
    {
        _controller = new ModelsController();
    }

    [Fact]
    public void GetModels_ReturnsOkWithAllSupportedModels()
    {
        // Act
        var result = _controller.GetModels();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var models = okResult.Value.Should().BeAssignableTo<IReadOnlyList<SupportedModel>>().Subject;
        models.Should().HaveCount(SupportedModels.All.Count);
    }

    [Fact]
    public void GetModels_ContainsDefaultModel()
    {
        // Act
        var result = _controller.GetModels();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var models = okResult.Value.Should().BeAssignableTo<IReadOnlyList<SupportedModel>>().Subject;
        models.Should().Contain(m => m.IsDefault);
    }
}
