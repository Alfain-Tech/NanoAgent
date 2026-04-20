using NanoAgent.Domain.Models;

namespace NanoAgent.Application.Models;

public sealed class ReplSessionContext
{
    private const string DefaultApplicationName = "NanoAgent";
    private readonly HashSet<string> _availableModelIds;
    private readonly List<ConversationRequestMessage> _conversationHistory = [];
    private readonly Stack<WorkspaceFileEditTransaction> _redoFileEditTransactions = new();
    private readonly Stack<WorkspaceFileEditTransaction> _undoFileEditTransactions = new();
    private readonly List<PermissionRule> _permissionOverrides = [];

    public ReplSessionContext(
        AgentProviderProfile providerProfile,
        string activeModelId,
        IReadOnlyList<string> availableModelIds)
        : this(DefaultApplicationName, providerProfile, activeModelId, availableModelIds)
    {
    }

    public ReplSessionContext(
        string applicationName,
        AgentProviderProfile providerProfile,
        string activeModelId,
        IReadOnlyList<string> availableModelIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentNullException.ThrowIfNull(providerProfile);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeModelId);
        ArgumentNullException.ThrowIfNull(availableModelIds);

        ApplicationName = applicationName.Trim();
        ProviderProfile = providerProfile;
        AvailableModelIds = availableModelIds
            .Where(static modelId => !string.IsNullOrWhiteSpace(modelId))
            .Select(static modelId => modelId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (AvailableModelIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one available model must be provided.",
                nameof(availableModelIds));
        }

        _availableModelIds = new HashSet<string>(AvailableModelIds, StringComparer.Ordinal);

        string normalizedActiveModelId = activeModelId.Trim();
        if (!_availableModelIds.Contains(normalizedActiveModelId))
        {
            throw new ArgumentException(
                "The active model must exist in the available model set.",
                nameof(activeModelId));
        }

        ActiveModelId = normalizedActiveModelId;
    }

    public string ApplicationName { get; }

    public string ActiveModelId { get; private set; }

    public IReadOnlyList<string> AvailableModelIds { get; }

    public AgentProviderProfile ProviderProfile { get; }

    public string ProviderName => ProviderProfile.ProviderKind.ToDisplayName();

    public IReadOnlyList<ConversationRequestMessage> ConversationHistory => _conversationHistory;

    public IReadOnlyList<PermissionRule> PermissionOverrides => _permissionOverrides;

    public int TotalEstimatedOutputTokens { get; private set; }

    public void AddPermissionOverride(PermissionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _permissionOverrides.Add(rule);
    }

    public bool ContainsModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        return _availableModelIds.Contains(modelId.Trim());
    }

    public void SetActiveModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        string normalizedModelId = modelId.Trim();
        if (!_availableModelIds.Contains(normalizedModelId))
        {
            throw new InvalidOperationException(
                $"Model '{normalizedModelId}' is not available in the current session.");
        }

        ActiveModelId = normalizedModelId;
    }

    public int AddEstimatedOutputTokens(int estimatedOutputTokens)
    {
        if (estimatedOutputTokens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedOutputTokens));
        }

        TotalEstimatedOutputTokens += estimatedOutputTokens;
        return TotalEstimatedOutputTokens;
    }

    public void AddConversationTurn(
        string userInput,
        string assistantResponse)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);
        ArgumentException.ThrowIfNullOrWhiteSpace(assistantResponse);

        _conversationHistory.Add(ConversationRequestMessage.User(userInput.Trim()));
        _conversationHistory.Add(ConversationRequestMessage.AssistantMessage(assistantResponse.Trim()));
    }

    public IReadOnlyList<ConversationRequestMessage> GetConversationHistory(int maxHistoryTurns)
    {
        if (maxHistoryTurns <= 0 || _conversationHistory.Count == 0)
        {
            return [];
        }

        int maxMessageCount = checked(maxHistoryTurns * 2);
        if (_conversationHistory.Count <= maxMessageCount)
        {
            return _conversationHistory.ToArray();
        }

        return _conversationHistory
            .Skip(_conversationHistory.Count - maxMessageCount)
            .ToArray();
    }

    public void RecordFileEditTransaction(WorkspaceFileEditTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        _undoFileEditTransactions.Push(transaction);
        _redoFileEditTransactions.Clear();
    }

    public bool TryGetPendingUndoFileEdit(out WorkspaceFileEditTransaction? transaction)
    {
        if (_undoFileEditTransactions.Count == 0)
        {
            transaction = null;
            return false;
        }

        transaction = _undoFileEditTransactions.Peek();
        return true;
    }

    public void CompleteUndoFileEdit()
    {
        if (_undoFileEditTransactions.Count == 0)
        {
            throw new InvalidOperationException("There is no file edit transaction to undo.");
        }

        WorkspaceFileEditTransaction transaction = _undoFileEditTransactions.Pop();
        _redoFileEditTransactions.Push(transaction);
    }

    public bool TryGetPendingRedoFileEdit(out WorkspaceFileEditTransaction? transaction)
    {
        if (_redoFileEditTransactions.Count == 0)
        {
            transaction = null;
            return false;
        }

        transaction = _redoFileEditTransactions.Peek();
        return true;
    }

    public void CompleteRedoFileEdit()
    {
        if (_redoFileEditTransactions.Count == 0)
        {
            throw new InvalidOperationException("There is no file edit transaction to redo.");
        }

        WorkspaceFileEditTransaction transaction = _redoFileEditTransactions.Pop();
        _undoFileEditTransactions.Push(transaction);
    }
}
