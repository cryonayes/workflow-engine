using FluentAssertions;
using WorkflowEngine.Console.Rendering;

namespace WorkflowEngine.Tests.Console.Rendering;

public class ToastOverlayTests
{
    [Fact]
    public void StripMarkup_PlainText_ReturnsUnchanged()
    {
        // Act
        var result = ToastOverlay.StripMarkup("Hello World");

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    public void StripMarkup_EmptyString_ReturnsEmpty()
    {
        // Act
        var result = ToastOverlay.StripMarkup("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void StripMarkup_SimpleMarkup_RemovesTags()
    {
        // Act
        var result = ToastOverlay.StripMarkup("[red]Hello[/]");

        // Assert
        result.Should().Be("Hello");
    }

    [Fact]
    public void StripMarkup_NestedMarkup_RemovesAllTags()
    {
        // Act
        var result = ToastOverlay.StripMarkup("[bold][red]Hello[/] World[/]");

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    public void StripMarkup_MultipleColors_RemovesAllTags()
    {
        // Act
        var result = ToastOverlay.StripMarkup("[green]Pass[/] [red]Fail[/]");

        // Assert
        result.Should().Be("Pass Fail");
    }

    [Fact]
    public void StripMarkup_ComplexMarkup_RemovesAllTags()
    {
        // Act
        var result = ToastOverlay.StripMarkup("[bold cyan on grey15]Status[/]: [green]OK[/]");

        // Assert
        result.Should().Be("Status: OK");
    }
}
