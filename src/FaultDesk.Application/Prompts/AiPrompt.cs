namespace FaultDesk.Application.Prompts;

/// <summary>A versioned prompt pair. The version is persisted with anything the prompt produces so outputs stay reproducible.</summary>
public sealed record AiPrompt(string Version, string System, string User);
