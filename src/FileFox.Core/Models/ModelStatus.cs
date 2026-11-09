namespace FileFox.Core.Models;

/// <summary>Whether the local language model can be used.</summary>
/// <param name="IsAvailable"><c>true</c> if the model is installed and the server answers.</param>
/// <param name="ModelName">Configured model name.</param>
/// <param name="Message">Human-readable explanation, e.g. how to fix a problem.</param>
public sealed record ModelStatus(bool IsAvailable, string ModelName, string Message);
