using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class DeliveryRepository : BaseRepository<Delivery, DeliveryEntity, DeliveryEntityMapper>, IDeliveryRepository
{
    private readonly AppDbContext _context;
    private readonly DeliveryEntityMapper _mapper;

    public DeliveryRepository(AppDbContext context, DeliveryEntityMapper mapper) : base(context, mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<Delivery>> GetAllForAdminAsync()
    {
        var entities = await _context.Deliveries.AsNoTracking()
            .OrderByDescending(x => x.SentAt).ToListAsync();
        return _mapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<Delivery?> GetForAdminAsync(Guid id)
    {
        return _mapper.Map(await _context.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id));
    }

    public async Task<Delivery?> GetByMessageIdAsync(string messageId, EChannel channel, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Deliveries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MessageId == messageId && x.Channel == channel, cancellationToken);
        return _mapper.Map(entity);
    }
}
