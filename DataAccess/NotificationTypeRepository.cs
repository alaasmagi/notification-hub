using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class NotificationTypeRepository : BaseRepository<NotificationType, NotificationTypeEntity, NotificationTypeEntityMapper>, INotificationTypeRepository
{
    private readonly AppDbContext _context;
    private readonly NotificationTypeEntityMapper _mapper;

    public NotificationTypeRepository(AppDbContext context, NotificationTypeEntityMapper mapper) : base(context, mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<NotificationType>> GetAllForAdminAsync()
    {
        var entities = await _context.NotificationTypes.AsNoTracking()
            .OrderBy(x => x.Key).ToListAsync();
        return _mapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<NotificationType?> GetForAdminAsync(Guid id)
    {
        return _mapper.Map(await _context.NotificationTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id));
    }

    public async Task<NotificationType?> GetActiveAsync(Guid clientId, string key)
    {
        var entity = await _context.NotificationTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId && x.Key == key && x.IsActive);
        return _mapper.Map(entity);
    }
}
