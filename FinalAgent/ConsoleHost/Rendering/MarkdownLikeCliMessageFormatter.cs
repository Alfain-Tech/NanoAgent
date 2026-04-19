namespace FinalAgent.ConsoleHost.Rendering;

internal sealed class MarkdownLikeCliMessageFormatter : ICliMessageFormatter
{
    public CliRenderDocument Format(
        CliRenderMessageKind kind,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return kind == CliRenderMessageKind.Assistant
            ? new CliRenderDocument(kind, ParseAssistantMessage(message))
            : new CliRenderDocument(kind, [CreateAlertBlock(message)]);
    }

    private static IReadOnlyList<CliRenderBlock> ParseAssistantMessage(string message)
    {
        string[] lines = NormalizeLines(message);
        List<CliRenderBlock> blocks = [];
        List<string> paragraphLines = [];

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];

            if (IsFenceStart(line, out string? language))
            {
                FlushParagraph(blocks, paragraphLines);
                blocks.Add(ParseFencedBlock(lines, ref index, language));
                continue;
            }

            if (TryParseHeading(line, out int headingLevel, out string? headingText))
            {
                FlushParagraph(blocks, paragraphLines);
                blocks.Add(new CliRenderBlock(
                    CliRenderBlockKind.Heading,
                    [new CliRenderLine(ParseInlineSegments(headingText!))],
                    headingLevel: headingLevel));
                continue;
            }

            if (IsStandaloneDiffLine(line))
            {
                FlushParagraph(blocks, paragraphLines);
                blocks.Add(ParseStandaloneDiff(lines, ref index));
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(blocks, paragraphLines);
                continue;
            }

            paragraphLines.Add(line);
        }

        FlushParagraph(blocks, paragraphLines);

        if (blocks.Count == 0)
        {
            blocks.Add(CreateParagraphBlock([message.Trim()]));
        }

        return blocks;
    }

    private static CliRenderBlock CreateAlertBlock(string message)
    {
        string[] lines = NormalizeLines(message)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (lines.Length == 0)
        {
            lines = [message.Trim()];
        }

        return new CliRenderBlock(
            CliRenderBlockKind.Alert,
            lines.Select(static line => new CliRenderLine(ParseInlineSegments(line.Trim())))
                .ToArray());
    }

    private static void FlushParagraph(
        List<CliRenderBlock> blocks,
        List<string> paragraphLines)
    {
        if (paragraphLines.Count == 0)
        {
            return;
        }

        blocks.Add(CreateParagraphBlock(paragraphLines));
        paragraphLines.Clear();
    }

    private static CliRenderBlock CreateParagraphBlock(IReadOnlyList<string> paragraphLines)
    {
        return new CliRenderBlock(
            CliRenderBlockKind.Paragraph,
            paragraphLines
                .Select(static line => new CliRenderLine(ParseInlineSegments(line.Trim())))
                .ToArray());
    }

    private static CliRenderBlock ParseFencedBlock(
        string[] lines,
        ref int index,
        string? language)
    {
        List<string> contentLines = [];

        index++;
        for (; index < lines.Length; index++)
        {
            if (lines[index].Trim() == "```")
            {
                break;
            }

            contentLines.Add(lines[index]);
        }

        bool isDiff = string.Equals(language, "diff", StringComparison.OrdinalIgnoreCase) ||
                      contentLines.Any(static line => IsStandaloneDiffLine(line));

        return isDiff
            ? CreateDiffBlock(contentLines)
            : CreateCodeBlock(contentLines, language);
    }

    private static CliRenderBlock ParseStandaloneDiff(
        string[] lines,
        ref int index)
    {
        List<string> diffLines = [];

        for (; index < lines.Length; index++)
        {
            string line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                index--;
                break;
            }

            if (!IsStandaloneDiffLine(line))
            {
                index--;
                break;
            }

            diffLines.Add(line);
        }

        return CreateDiffBlock(diffLines);
    }

    private static CliRenderBlock CreateCodeBlock(
        IReadOnlyList<string> lines,
        string? language)
    {
        IReadOnlyList<CliRenderLine> renderLines = lines.Count == 0
            ? [new CliRenderLine([new CliInlineSegment(string.Empty)])]
            : lines.Select(static line => new CliRenderLine([new CliInlineSegment(line)]))
                .ToArray();

        return new CliRenderBlock(
            CliRenderBlockKind.CodeBlock,
            renderLines,
            language);
    }

    private static CliRenderBlock CreateDiffBlock(IReadOnlyList<string> lines)
    {
        IReadOnlyList<CliRenderLine> renderLines = lines.Count == 0
            ? [new CliRenderLine([new CliInlineSegment(string.Empty)], CliRenderLineKind.DiffContext)]
            : lines.Select(static line => new CliRenderLine(
                    [new CliInlineSegment(line)],
                    GetDiffLineKind(line)))
                .ToArray();

        return new CliRenderBlock(
            CliRenderBlockKind.Diff,
            renderLines,
            "diff");
    }

    private static bool IsFenceStart(
        string line,
        out string? language)
    {
        string trimmed = line.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            language = null;
            return false;
        }

        language = trimmed.Length == 3
            ? null
            : trimmed[3..].Trim();

        return true;
    }

    private static bool TryParseHeading(
        string line,
        out int headingLevel,
        out string? headingText)
    {
        string trimmed = line.Trim();
        headingLevel = 0;
        headingText = null;

        if (trimmed.Length < 2 || trimmed[0] != '#')
        {
            return false;
        }

        while (headingLevel < trimmed.Length &&
               trimmed[headingLevel] == '#')
        {
            headingLevel++;
        }

        if (headingLevel == 0 ||
            headingLevel > 6 ||
            headingLevel >= trimmed.Length ||
            trimmed[headingLevel] != ' ')
        {
            return false;
        }

        headingText = trimmed[(headingLevel + 1)..].Trim();
        return !string.IsNullOrWhiteSpace(headingText);
    }

    private static bool IsStandaloneDiffLine(string line)
    {
        return line.StartsWith("+", StringComparison.Ordinal) ||
               line.StartsWith("-", StringComparison.Ordinal) ||
               line.StartsWith("@@", StringComparison.Ordinal) ||
               line.StartsWith("diff ", StringComparison.Ordinal) ||
               line.StartsWith("index ", StringComparison.Ordinal) ||
               line.StartsWith("---", StringComparison.Ordinal) ||
               line.StartsWith("+++", StringComparison.Ordinal);
    }

    private static CliRenderLineKind GetDiffLineKind(string line)
    {
        if (line.StartsWith("+", StringComparison.Ordinal) &&
            !line.StartsWith("+++", StringComparison.Ordinal))
        {
            return CliRenderLineKind.DiffAddition;
        }

        if (line.StartsWith("-", StringComparison.Ordinal) &&
            !line.StartsWith("---", StringComparison.Ordinal))
        {
            return CliRenderLineKind.DiffRemoval;
        }

        if (line.StartsWith("@@", StringComparison.Ordinal) ||
            line.StartsWith("diff ", StringComparison.Ordinal) ||
            line.StartsWith("index ", StringComparison.Ordinal) ||
            line.StartsWith("---", StringComparison.Ordinal) ||
            line.StartsWith("+++", StringComparison.Ordinal))
        {
            return CliRenderLineKind.DiffHeader;
        }

        return CliRenderLineKind.DiffContext;
    }

    private static string[] NormalizeLines(string message)
    {
        return message
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');
    }

    private static IReadOnlyList<CliInlineSegment> ParseInlineSegments(string text)
    {
        List<CliInlineSegment> segments = [];
        int index = 0;

        while (index < text.Length)
        {
            if (TryConsumeDelimited(text, index, "**", CliInlineStyle.Strong, out CliInlineSegment? strongSegment, out int strongLength))
            {
                segments.Add(strongSegment!);
                index += strongLength;
                continue;
            }

            if (TryConsumeDelimited(text, index, "`", CliInlineStyle.Code, out CliInlineSegment? codeSegment, out int codeLength))
            {
                segments.Add(codeSegment!);
                index += codeLength;
                continue;
            }

            if (TryConsumeDelimited(text, index, "*", CliInlineStyle.Emphasis, out CliInlineSegment? emphasisSegment, out int emphasisLength))
            {
                segments.Add(emphasisSegment!);
                index += emphasisLength;
                continue;
            }

            int nextMarker = FindNextMarker(text, index);
            if (nextMarker == index)
            {
                segments.Add(new CliInlineSegment(text[index].ToString()));
                index++;
                continue;
            }

            string chunk = nextMarker < 0
                ? text[index..]
                : text[index..nextMarker];

            if (!string.IsNullOrEmpty(chunk))
            {
                segments.Add(new CliInlineSegment(chunk));
            }

            index = nextMarker < 0
                ? text.Length
                : nextMarker;
        }

        if (segments.Count == 0)
        {
            segments.Add(new CliInlineSegment(text));
        }

        return MergePlainSegments(segments);
    }

    private static bool TryConsumeDelimited(
        string text,
        int startIndex,
        string delimiter,
        CliInlineStyle style,
        out CliInlineSegment? segment,
        out int consumedLength)
    {
        if (!text.AsSpan(startIndex).StartsWith(delimiter, StringComparison.Ordinal))
        {
            segment = null;
            consumedLength = 0;
            return false;
        }

        int innerStart = startIndex + delimiter.Length;
        int closingIndex = text.IndexOf(delimiter, innerStart, StringComparison.Ordinal);
        if (closingIndex <= innerStart)
        {
            segment = null;
            consumedLength = 0;
            return false;
        }

        string innerText = text[innerStart..closingIndex];
        if (string.IsNullOrEmpty(innerText))
        {
            segment = null;
            consumedLength = 0;
            return false;
        }

        segment = new CliInlineSegment(innerText, style);
        consumedLength = (closingIndex + delimiter.Length) - startIndex;
        return true;
    }

    private static int FindNextMarker(string text, int startIndex)
    {
        int nextStrong = text.IndexOf("**", startIndex, StringComparison.Ordinal);
        int nextCode = text.IndexOf('`', startIndex);
        int nextEmphasis = text.IndexOf('*', startIndex);

        int[] candidates =
        [
            nextStrong,
            nextCode,
            nextEmphasis
        ];

        return candidates
            .Where(static index => index >= 0)
            .DefaultIfEmpty(-1)
            .Min();
    }

    private static IReadOnlyList<CliInlineSegment> MergePlainSegments(IReadOnlyList<CliInlineSegment> segments)
    {
        List<CliInlineSegment> mergedSegments = [];

        foreach (CliInlineSegment segment in segments)
        {
            if (mergedSegments.Count > 0 &&
                mergedSegments[^1].Style == CliInlineStyle.Plain &&
                segment.Style == CliInlineStyle.Plain)
            {
                CliInlineSegment previousSegment = mergedSegments[^1];
                mergedSegments[^1] = new CliInlineSegment(previousSegment.Text + segment.Text);
            }
            else
            {
                mergedSegments.Add(segment);
            }
        }

        return mergedSegments;
    }
}
