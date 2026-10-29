using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class EmailSenderRepository : BaseRepository<EmailSender, EmailSenderEntity, EmailSenderEntityMapper>, IEmailSenderRepository
{
    private readonly AppDbContext _context;
    private readonly EmailSenderEntityMapper _mapper;

    public EmailSenderRepository(AppDbContext context, EmailSenderEntityMapper mapper) : base(context, mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<EmailSender>> GetAllForAdminAsync()
    {
        var entities = await _context.EmailSenders.AsNoTracking()
            .OrderBy(x => x.Tenant).ToListAsync();
        return _mapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<EmailSender?> GetForAdminAsync(Guid id)
    {
        return _mapper.Map(await _context.EmailSenders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id));
    }

    public async Task<EmailSender?> GetActiveAsync(Guid clientId, string tenant)
    {
        var entity = await _context.EmailSenders.AsNoTracking()
            .Where(x => x.ClientId == clientId && x.IsActive && (x.Tenant == tenant || x.Tenant == ""))
            .OrderByDescending(x => x.Tenant == tenant)
            .FirstOrDefaultAsync();
        return _mapper.Map(entity);
    }
}
