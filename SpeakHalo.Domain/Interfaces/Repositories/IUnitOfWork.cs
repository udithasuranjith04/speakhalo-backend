namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ICallRepository Calls { get; }
    IAppointmentRepository Appointments { get; }
    ICustomerRepository Customers { get; }
    ISMSConversationRepository SMSConversations { get; }
    ISMSMessageRepository SMSMessages { get; }
    IWhatsAppConversationRepository WhatsAppConversations { get; }
    IWhatsAppMessageRepository WhatsAppMessages { get; }
    IQuoteRepository Quotes { get; }
    INotificationRepository Notifications { get; }
    IPhoneNumberRepository PhoneNumbers { get; }
    IMediaFileRepository MediaFiles { get; }
    IKnowledgeBaseArticleRepository KnowledgeBaseArticles { get; }
    IProductKnowledgeRepository ProductKnowledge { get; }
    IAIConversationContextRepository AIConversationContexts { get; }
    IRepository<T> Repository<T>() where T : Entities.BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
