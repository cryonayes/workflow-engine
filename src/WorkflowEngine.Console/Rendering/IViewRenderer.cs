using WorkflowEngine.Console.State;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Interface for view renderers.
/// </summary>
internal interface IViewRenderer
{
    /// <summary>
    /// Builds the view as a list of markup lines.
    /// </summary>
    /// <param name="state">The current renderer state.</param>
    /// <param name="width">Terminal width.</param>
    /// <param name="height">Terminal height.</param>
    /// <returns>List of markup lines.</returns>
    List<string> Build(RendererState state, int width, int height);
}
