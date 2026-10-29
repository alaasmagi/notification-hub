using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class TemplateRepository : BaseRepository<Template, TemplateEntity, TemplateEntityMapper>, ITemplateRepository
{
    private readonly AppDbContext _context;
    private readonly TemplateEntityMapper _mapper;

    public TemplateRepository(AppDbContext context, TemplateEntityMapper mapper) : base(context, mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<Template>> GetAllForAdminAsync()
    {
        var entities = await _context.Templates.AsNoTracking()
            .OrderBy(x => x.LanguageCode).ToListAsync();
        return _mapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<Template?> GetForAdminAsync(Guid id)
    {
        return _mapper.Map(await _context.Templates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id));
    }

    public async Task<Template?> GetActiveAsync(Guid notificationTypeId, EChannel channel, string tenant, string languageCode)
    {
        var entity = await _context.Templates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NotificationTypeId == notificationTypeId && x.Channel == channel &&
                x.Tenant == tenant && x.LanguageCode == languageCode && x.IsActive);
        return _mapper.Map(entity);
    }
}
