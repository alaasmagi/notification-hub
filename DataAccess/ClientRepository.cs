using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class ClientRepository : BaseRepository<Client, ClientEntity, ClientEntityMapper>, IClientRepository
{
    private readonly AppDbContext _context;
    private readonly ClientEntityMapper _mapper;

    public ClientRepository(AppDbContext context, ClientEntityMapper mapper) : base(context, mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<Client>> GetAllForAdminAsync()
    {
        var entities = await _context.Clients.AsNoTracking()
            .OrderBy(x => x.Source).ToListAsync();
        return _mapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<Client?> GetForAdminAsync(Guid id)
    {
        return _mapper.Map(await _context.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id));
    }

    public async Task<Client?> GetActiveBySourceAsync(string source)
    {
        var entity = await _context.Clients.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Source == source && x.IsActive);
        return _mapper.Map(entity);
    }
}
