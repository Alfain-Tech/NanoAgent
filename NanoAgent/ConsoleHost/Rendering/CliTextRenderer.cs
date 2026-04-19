namespace NanoAgent.ConsoleHost.Rendering;

internal sealed class CliTextRenderer : ICliTextRenderer
{
    private readonly ICliOutputTarget _outputTarget;

    public CliTextRenderer(ICliOutputTarget outputTarget)
    {
        _outputTarget = outputTarget;
    }

    public Task RenderAsync(
        CliRenderDocument document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        if (document.Kind == CliRenderMessageKind.Assistant)
        {
            RenderAssistant(document);
        }
        else
        {
            RenderStatus(document);
        }

        return Task.CompletedTask;
    }

    private void RenderAssistant(CliRenderDocument document)
    {
        _outputTarget.WriteLine([
            new CliOutputSegment("assistant", CliOutputStyle.AssistantLabel)
        ]);

        bool firstBlock = true;

        foreach (CliRenderBlock block in document.Blocks)
        {
            if (!firstBlock)
            {
                _outputTarget.WriteLine();
            }

            RenderAssistantBlock(block);
            firstBlock = false;
        }
    }

    private void RenderAssistantBlock(CliRenderBlock block)
    {
        switch (block.Kind)
        {
            case CliRenderBlockKind.Heading:
                RenderHeading(block);
                break;
            case CliRenderBlockKind.CodeBlock:
                RenderCodeBlock(block);
                break;
            case CliRenderBlockKind.Diff:
                RenderDiff(block);
                break;
            case CliRenderBlockKind.Alert:
                RenderAlertBlock(block, CliRenderMessageKind.Warning);
                break;
            default:
                RenderParagraph(block);
                break;
        }
    }

    private void RenderStatus(CliRenderDocument document)
    {
        foreach (CliRenderBlock block in document.Blocks)
        {
            RenderAlertBlock(block, document.Kind);
        }
    }

    private void RenderParagraph(CliRenderBlock block)
    {
        foreach (CliRenderLine line in block.Lines)
        {
            List<CliOutputSegment> outputSegments =
            [
                new CliOutputSegment("  ", CliOutputStyle.Muted)
            ];

            outputSegments.AddRange(MapLineSegments(line, CliOutputStyle.AssistantText));
            _outputTarget.WriteLine(outputSegments);
        }
    }

    private void RenderHeading(CliRenderBlock block)
    {
        foreach (CliRenderLine line in block.Lines)
        {
            List<CliOutputSegment> outputSegments =
            [
                new CliOutputSegment("  ", CliOutputStyle.Muted)
            ];

            outputSegments.AddRange(MapLineSegments(line, CliOutputStyle.Heading));
            _outputTarget.WriteLine(outputSegments);

            string underline = new string('-', Math.Max(8, GetContentLength(line.Segments)));
            _outputTarget.WriteLine([
                new CliOutputSegment("  ", CliOutputStyle.Muted),
                new CliOutputSegment(underline, CliOutputStyle.Muted)
            ]);
        }
    }

    private void RenderCodeBlock(CliRenderBlock block)
    {
        string label = string.IsNullOrWhiteSpace(block.Language)
            ? "[code]"
            : $"[{block.Language}]";

        _outputTarget.WriteLine([
            new CliOutputSegment("  ", CliOutputStyle.Muted),
            new CliOutputSegment(label, CliOutputStyle.CodeFence)
        ]);

        foreach (CliRenderLine line in block.Lines)
        {
            List<CliOutputSegment> outputSegments =
            [
                new CliOutputSegment("  | ", CliOutputStyle.CodeFence)
            ];

            outputSegments.AddRange(MapLineSegments(line, CliOutputStyle.CodeText));
            _outputTarget.WriteLine(outputSegments);
        }
    }

    private void RenderDiff(CliRenderBlock block)
    {
        _outputTarget.WriteLine([
            new CliOutputSegment("  ", CliOutputStyle.Muted),
            new CliOutputSegment("[diff]", CliOutputStyle.CodeFence)
        ]);

        foreach (CliRenderLine line in block.Lines)
        {
            CliOutputStyle baseStyle = line.Kind switch
            {
                CliRenderLineKind.DiffAddition => CliOutputStyle.DiffAddition,
                CliRenderLineKind.DiffRemoval => CliOutputStyle.DiffRemoval,
                CliRenderLineKind.DiffHeader => CliOutputStyle.DiffHeader,
                _ => CliOutputStyle.DiffContext
            };

            List<CliOutputSegment> outputSegments =
            [
                new CliOutputSegment("  ", CliOutputStyle.Muted)
            ];

            outputSegments.AddRange(MapLineSegments(line, baseStyle));
            _outputTarget.WriteLine(outputSegments);
        }
    }

    private void RenderAlertBlock(
        CliRenderBlock block,
        CliRenderMessageKind kind)
    {
        CliOutputStyle style = kind switch
        {
            CliRenderMessageKind.Error => CliOutputStyle.Error,
            CliRenderMessageKind.Warning => CliOutputStyle.Warning,
            _ => CliOutputStyle.Info
        };

        string prefix = kind switch
        {
            CliRenderMessageKind.Error => "[error] ",
            CliRenderMessageKind.Warning => "[warning] ",
            _ => "[info] "
        };

        for (int index = 0; index < block.Lines.Count; index++)
        {
            List<CliOutputSegment> outputSegments = [];
            if (index == 0)
            {
                outputSegments.Add(new CliOutputSegment(prefix, style));
            }
            else
            {
                outputSegments.Add(new CliOutputSegment(new string(' ', prefix.Length), style));
            }

            outputSegments.AddRange(MapLineSegments(block.Lines[index], style));
            _outputTarget.WriteLine(outputSegments);
        }
    }

    private static IReadOnlyList<CliOutputSegment> MapLineSegments(
        CliRenderLine line,
        CliOutputStyle baseStyle)
    {
        return line.Segments
            .Select(segment => new CliOutputSegment(
                segment.Text,
                MapInlineStyle(segment.Style, baseStyle)))
            .ToArray();
    }

    private static CliOutputStyle MapInlineStyle(
        CliInlineStyle style,
        CliOutputStyle baseStyle)
    {
        return style switch
        {
            CliInlineStyle.Code => CliOutputStyle.InlineCode,
            CliInlineStyle.Strong => CliOutputStyle.Strong,
            CliInlineStyle.Emphasis => CliOutputStyle.Emphasis,
            _ => baseStyle
        };
    }

    private static int GetContentLength(IReadOnlyList<CliInlineSegment> segments)
    {
        return segments.Sum(static segment => segment.Text.Length);
    }
}
