using Microsoft.EntityFrameworkCore.Storage;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private readonly Dictionary<Type, object> _repositories = new();

    private IUserRepository? _userRepository;
    private ICallRepository? _callRepository;
    private IAppointmentRepository? _appointmentRepository;
    private ICustomerRepository? _customerRepository;
    private ISMSConversationRepository? _smsConversationRepository;
    private ISMSMessageRepository? _smsMessageRepository;
    private IWhatsAppConversationRepository? _whatsAppConversationRepository;
    private IWhatsAppMessageRepository? _whatsAppMessageRepository;
    private IQuoteRepository? _quoteRepository;
    private INotificationRepository? _notificationRepository;
    private IPhoneNumberRepository? _phoneNumberRepository;
    private IMediaFileRepository? _mediaFileRepository;
    private IKnowledgeBaseArticleRepository? _knowledgeBaseArticleRepository;
    private IProductKnowledgeRepository? _productKnowledgeRepository;
    private IAIConversationContextRepository? _aiConversationContextRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public ICallRepository Calls => _callRepository ??= new CallRepository(_context);
    public IAppointmentRepository Appointments => _appointmentRepository ??= new AppointmentRepository(_context);
    public ICustomerRepository Customers => _customerRepository ??= new CustomerRepository(_context);
    public ISMSConversationRepository SMSConversations => _smsConversationRepository ??= new SMSConversationRepository(_context);
    public ISMSMessageRepository SMSMessages => _smsMessageRepository ??= new SMSMessageRepository(_context);
    public IWhatsAppConversationRepository WhatsAppConversations => _whatsAppConversationRepository ??= new WhatsAppConversationRepository(_context);
    public IWhatsAppMessageRepository WhatsAppMessages => _whatsAppMessageRepository ??= new WhatsAppMessageRepository(_context);
    public IQuoteRepository Quotes => _quoteRepository ??= new QuoteRepository(_context);
    public INotificationRepository Notifications => _notificationRepository ??= new NotificationRepository(_context);
    public IPhoneNumberRepository PhoneNumbers => _phoneNumberRepository ??= new PhoneNumberRepository(_context);
    public IMediaFileRepository MediaFiles => _mediaFileRepository ??= new MediaFileRepository(_context);
    public IKnowledgeBaseArticleRepository KnowledgeBaseArticles => _knowledgeBaseArticleRepository ??= new KnowledgeBaseArticleRepository(_context);
    public IProductKnowledgeRepository ProductKnowledge => _productKnowledgeRepository ??= new ProductKnowledgeRepository(_context);
    public IAIConversationContextRepository AIConversationContexts => _aiConversationContextRepository ??= new AIConversationContextRepository(_context);

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
        {
            _repositories[type] = new Repository<T>(_context);
        }
        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
