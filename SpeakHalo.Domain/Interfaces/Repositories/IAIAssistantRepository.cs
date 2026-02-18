using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IProductKnowledgeRepository : IRepository<ProductKnowledge>
{
    Task<List<ProductKnowledge>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<List<ProductKnowledge>> GetByCategory(string category,
        CancellationToken cancellationToken = default);

    Task<List<ProductKnowledge>> GetCommonQuestionsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task IncrementUsageCountAsync(
        string knowledgeId,
        CancellationToken cancellationToken = default);
}

public interface IAIConversationContextRepository : IRepository<AIConversationContext>
{
    Task<AIConversationContext?> GetByConversationIdAsync(
        string conversationId,
        CancellationToken cancellationToken = default);

    Task<List<AIConversationContext>> GetActiveContextsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task CleanupExpiredContextsAsync(
        CancellationToken cancellationToken = default);
}

public interface IAITrainingExampleRepository : IRepository<AITrainingExample>
{
    Task<List<AITrainingExample>> GetByCategory(string category,
        CancellationToken cancellationToken = default);

    Task<List<AITrainingExample>> GetSuccessfulExamplesAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<List<AITrainingExample>> GetForTrainingAsync(
        CancellationToken cancellationToken = default);
}
