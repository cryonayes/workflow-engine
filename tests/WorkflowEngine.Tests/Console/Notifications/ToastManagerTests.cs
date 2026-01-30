using FluentAssertions;
using WorkflowEngine.Console.Notifications;

namespace WorkflowEngine.Tests.Console.Notifications;

public class ToastManagerTests
{
    [Fact]
    public void Show_AddsToast()
    {
        // Arrange
        var manager = new ToastManager();

        // Act
        manager.Show("Test message", ToastType.Info);
        var active = manager.GetActive();

        // Assert
        active.Should().HaveCount(1);
        active[0].Message.Should().Be("Test message");
        active[0].Type.Should().Be(ToastType.Info);
    }

    [Fact]
    public void Show_MultipleToasts_AllActive()
    {
        // Arrange
        var manager = new ToastManager();

        // Act
        manager.Show("Message 1", ToastType.Info);
        manager.Show("Message 2", ToastType.Success);
        manager.Show("Message 3", ToastType.Error);
        var active = manager.GetActive();

        // Assert
        active.Should().HaveCount(3);
    }

    [Fact]
    public void GetActive_ReturnsEmptyList_WhenNoToasts()
    {
        // Arrange
        var manager = new ToastManager();

        // Act
        var active = manager.GetActive();

        // Assert
        active.Should().BeEmpty();
    }

    [Fact]
    public void GetActive_RemovesExpiredToasts()
    {
        // Arrange
        var manager = new ToastManager();
        manager.Show("Short lived", ToastType.Info, durationSeconds: 0.001);

        // Wait for expiry
        Thread.Sleep(10);

        // Act
        var active = manager.GetActive();

        // Assert
        active.Should().BeEmpty();
    }

    [Fact]
    public void GetActive_KeepsNonExpiredToasts()
    {
        // Arrange
        var manager = new ToastManager();
        manager.Show("Long lived", ToastType.Info, durationSeconds: 60);

        // Act
        var active = manager.GetActive();

        // Assert
        active.Should().HaveCount(1);
    }

    [Fact]
    public void Show_ErrorType_HasLongerDefaultDuration()
    {
        // Arrange
        var manager = new ToastManager();

        // Act
        manager.Show("Error message", ToastType.Error);
        var active = manager.GetActive();

        // Assert
        // Error toasts have 5 second duration by default vs 3 seconds for others
        active.Should().HaveCount(1);
        active[0].Expiry.Should().BeAfter(DateTimeOffset.UtcNow.AddSeconds(4));
    }

    [Fact]
    public void Show_CustomDuration_OverridesDefault()
    {
        // Arrange
        var manager = new ToastManager();
        var expectedExpiry = DateTimeOffset.UtcNow.AddSeconds(10);

        // Act
        manager.Show("Custom duration", ToastType.Info, durationSeconds: 10);
        var active = manager.GetActive();

        // Assert
        active.Should().HaveCount(1);
        active[0].Expiry.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void GetActive_ReturnsNewListEachTime()
    {
        // Arrange
        var manager = new ToastManager();
        manager.Show("Test", ToastType.Info);

        // Act
        var active1 = manager.GetActive();
        var active2 = manager.GetActive();

        // Assert
        active1.Should().NotBeSameAs(active2);
    }
}
