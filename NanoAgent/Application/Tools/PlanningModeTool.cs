using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using NanoAgent.Application.Tools.Models;
using NanoAgent.Application.Tools.Serialization;

namespace NanoAgent.Application.Tools;

internal sealed class PlanningModeTool : ITool
{
    private static readonly string[] Instructions =
    [
        "Inspect the relevant codebase and facts before editing.",
        "Check installed build tools, compilers, SDKs, package managers, or runtimes with safe shell probes before choosing scaffold, build, or test commands.",
        "Use shell_command for toolchain work during execution when it materially advances the task: project scaffolding, dependency restore/install, code generation, build, test, lint, or format checks.",
        "Separate verified findings from assumptions or open questions.",
        "Produce a high-quality ordered task list in Codex style that names likely files, commands, validation steps, and risks.",
        "Keep one active step at a time and revise the plan when new evidence changes the safest path; if the user asked only for a plan, stop after planning, otherwise continue execution when practical."
    ];

    private static readonly string[] SuggestedResponseSections =
    [
        "Objective",
        "Current understanding",
        "Environment / toolchain",
        "Toolchain commands",
        "Relevant files / areas",
        "Plan",
        "Validation",
        "Risks / unknowns",
        "Recommended approach"
    ];

    public string Description =>
        "Switch into a Codex-style plan-first workflow for the current task. Use this when you want to inspect the repo, check the local toolchain when relevant, separate facts from assumptions, think through risks, and produce a high-quality task list before making changes. This tool does not modify files. After planning, continue execution in the same turn and work one step at a time unless the user asked only for a plan.";

    public string Name => AgentToolNames.PlanningMode;

    public string PermissionRequirements => """
        {
          "approvalMode": "Automatic"
        }
        """;

    public string Schema => """
        {
          "type": "object",
          "properties": {
            "objective": {
              "type": "string",
              "description": "The task or goal that should be planned before execution."
            }
          },
          "required": ["objective"],
          "additionalProperties": false
        }
        """;

    public Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (!ToolArguments.TryGetNonEmptyString(context.Arguments, "objective", out string? objective))
        {
            return Task.FromResult(ToolResultFactory.InvalidArguments(
                "missing_objective",
                "Tool 'planning_mode' requires a non-empty 'objective' string.",
                new ToolRenderPayload(
                    "Invalid planning_mode arguments",
                    "Provide a non-empty 'objective' string.")));
        }

        PlanningModeResult result = new(
            objective!,
            Instructions,
            SuggestedResponseSections);

        return Task.FromResult(ToolResultFactory.Success(
            $"Planning mode activated for '{objective}'.",
            result,
            ToolJsonContext.Default.PlanningModeResult,
            new ToolRenderPayload(
                $"Planning mode: {objective}",
                string.Join(Environment.NewLine, Instructions.Select(static (item, index) => $"{index + 1}. {item}")))));
    }
}
