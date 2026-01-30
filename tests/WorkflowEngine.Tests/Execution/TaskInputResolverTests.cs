using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Execution;
using WorkflowEngine.Execution.InputResolvers;

namespace WorkflowEngine.Tests.Execution;

public class TaskInputResolverTests
{
    private readonly IExpressionInterpolator _interpolator;
    private readonly TaskInputResolver _resolver;
    private readonly WorkflowContext _context;

    public TaskInputResolverTests()
    {
        _interpolator = Substitute.For<IExpressionInterpolator>();

        // Create resolvers explicitly (proper DI pattern)
        var resolvers = new IInputTypeResolver[]
        {
            new TextInputResolver(_interpolator),
            new BytesInputResolver(),
            new FileInputResolver(NullLogger<FileInputResolver>.Instance),
            new PipeInputResolver(_interpolator, NullLogger<PipeInputResolver>.Instance)
        };

        _resolver = new TaskInputResolver(resolvers, NullLogger<TaskInputResolver>.Instance);

        var workflow = new Workflow { Name = "Test", Tasks = [] };
        _context = new WorkflowContext { Workflow = workflow };
    }

    [Fact]
    public async Task ResolveInputAsync_WithNoInput_ReturnsNull()
    {
        // Arrange
        var task = new WorkflowTask { Id = "test", Run = "echo test", Input = null };

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveInputAsync_WithNoneInputType_ReturnsNull()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo test",
            Input = new TaskInput { Type = InputType.None }
        };

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveInputAsync_WithTextInput_ReturnsInterpolatedBytes()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "cat",
            Input = new TaskInput { Type = InputType.Text, Value = "Hello ${{ env.NAME }}" }
        };

        _interpolator.Interpolate("Hello ${{ env.NAME }}", _context)
            .Returns("Hello World");

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().NotBeNull();
        Encoding.UTF8.GetString(result!).Should().Be("Hello World");
    }

    [Fact]
    public async Task ResolveInputAsync_WithEmptyTextInput_ReturnsNull()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "cat",
            Input = new TaskInput { Type = InputType.Text, Value = "" }
        };

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveInputAsync_WithValidBase64_ReturnsDecodedBytes()
    {
        // Arrange
        var originalData = "Hello, World!";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalData));

        var task = new WorkflowTask
        {
            Id = "test",
            Run = "cat",
            Input = new TaskInput { Type = InputType.Bytes, Value = base64 }
        };

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().NotBeNull();
        Encoding.UTF8.GetString(result!).Should().Be(originalData);
    }

    [Fact]
    public async Task ResolveInputAsync_WithInvalidBase64_FallsBackToUtf8()
    {
        // Arrange
        var invalidBase64 = "This is not valid base64!!!";

        var task = new WorkflowTask
        {
            Id = "test",
            Run = "cat",
            Input = new TaskInput { Type = InputType.Bytes, Value = invalidBase64 }
        };

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().NotBeNull();
        Encoding.UTF8.GetString(result!).Should().Be(invalidBase64);
    }

    [Fact]
    public async Task ResolveInputAsync_WithEmptyBytesInput_ReturnsNull()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "cat",
            Input = new TaskInput { Type = InputType.Bytes, Value = "" }
        };

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveInputAsync_WithFileInput_ReturnsFileContents()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var fileContent = "File content here";
        await File.WriteAllTextAsync(tempFile, fileContent);

        try
        {
            var task = new WorkflowTask
            {
                Id = "test",
                Run = "cat",
                Input = new TaskInput { Type = InputType.File, FilePath = tempFile }
            };

            // Act
            var result = await _resolver.ResolveInputAsync(task, _context);

            // Assert
            result.Should().NotBeNull();
            Encoding.UTF8.GetString(result!).Should().Be(fileContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ResolveInputAsync_WithMissingFile_ReturnsNull()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "cat",
            Input = new TaskInput { Type = InputType.File, FilePath = "/nonexistent/file.txt" }
        };

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveInputAsync_WithFileInputNoPath_ReturnsNull()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "cat",
            Input = new TaskInput { Type = InputType.File, FilePath = null }
        };

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveInputAsync_WithPipeInput_ReturnsInterpolatedBytes()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "consumer",
            Run = "cat",
            Input = new TaskInput
            {
                Type = InputType.Pipe,
                Value = "${{ tasks.producer.output }}"
            }
        };

        _interpolator.Interpolate("${{ tasks.producer.output }}", _context)
            .Returns("piped data from producer");

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().NotBeNull();
        Encoding.UTF8.GetString(result!).Should().Be("piped data from producer");
    }

    [Fact]
    public async Task ResolveInputAsync_WithEmptyPipeResult_ReturnsNull()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "consumer",
            Run = "cat",
            Input = new TaskInput
            {
                Type = InputType.Pipe,
                Value = "${{ tasks.producer.output }}"
            }
        };

        _interpolator.Interpolate("${{ tasks.producer.output }}", _context)
            .Returns("");

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveInputAsync_WithNullPipeValue_ReturnsNull()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "consumer",
            Run = "cat",
            Input = new TaskInput { Type = InputType.Pipe, Value = null }
        };

        // Act
        var result = await _resolver.ResolveInputAsync(task, _context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveInputAsync_WithCancellation_ThrowsOperationCancelledException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "content");

        try
        {
            var task = new WorkflowTask
            {
                Id = "test",
                Run = "cat",
                Input = new TaskInput { Type = InputType.File, FilePath = tempFile }
            };

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert (TaskCanceledException inherits from OperationCanceledException)
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await _resolver.ResolveInputAsync(task, _context, cts.Token));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
