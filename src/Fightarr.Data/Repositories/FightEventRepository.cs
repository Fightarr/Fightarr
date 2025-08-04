using Fightarr.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fightarr.Data.Repositories;

public class FightEventRepository : IFightEventRepository
{
    private readonly FightarrDbContext _context;
    private readonly ILogger<FightEventRepository> _logger;

    public FightEventRepository(FightarrDbContext context, ILogger<FightEventRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FightEvent?> GetByIdAsync(int id)
    {
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
                .ThenInclude(ef => ef.Quality)
            .Include(e => e.QualityProfile)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<FightEvent?> GetByExternalIdAsync(string externalId)
    {
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
                .ThenInclude(ef => ef.Quality)
            .Include(e => e.QualityProfile)
            .FirstOrDefaultAsync(e => e.ExternalId == externalId);
    }

    public async Task<List<FightEvent>> GetAllAsync()
    {
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
            .Include(e => e.QualityProfile)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<List<FightEvent>> GetMonitoredAsync()
    {
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
            .Include(e => e.QualityProfile)
            .Where(e => e.Monitored)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<List<FightEvent>> GetUpcomingAsync(DateTime? fromDate = null)
    {
        var startDate = fromDate ?? DateTime.UtcNow;
        
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
            .Include(e => e.QualityProfile)
            .Where(e => e.EventDate >= startDate)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<List<FightEvent>> GetRecentAsync(DateTime? fromDate = null)
    {
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
        
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
            .Include(e => e.QualityProfile)
            .Where(e => e.EventDate >= startDate && e.EventDate <= DateTime.UtcNow)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<List<FightEvent>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        var searchTerm = query.ToLower();
        
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
            .Include(e => e.QualityProfile)
            .Where(e => e.EventName.ToLower().Contains(searchTerm) ||
                       e.ShortName.ToLower().Contains(searchTerm) ||
                       e.Promotion.ToLower().Contains(searchTerm) ||
                       e.Venue!.ToLower().Contains(searchTerm))
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<FightEvent> AddAsync(FightEvent fightEvent)
    {
        _context.FightEvents.Add(fightEvent);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Added fight event: {EventName} ({ExternalId})", 
            fightEvent.EventName, fightEvent.ExternalId);
        
        return fightEvent;
    }

    public async Task<FightEvent> UpdateAsync(FightEvent fightEvent)
    {
        _context.FightEvents.Update(fightEvent);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated fight event: {EventName} ({Id})", 
            fightEvent.EventName, fightEvent.Id);
        
        return fightEvent;
    }

    public async Task DeleteAsync(int id)
    {
        var fightEvent = await _context.FightEvents.FindAsync(id);
        if (fightEvent != null)
        {
            _context.FightEvents.Remove(fightEvent);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted fight event: {EventName} ({Id})", 
                fightEvent.EventName, fightEvent.Id);
        }
    }

    public async Task<bool> ExistsAsync(string externalId)
    {
        return await _context.FightEvents
            .AnyAsync(e => e.ExternalId == externalId);
    }

    public async Task<List<FightEvent>> GetByPromotionAsync(string promotion)
    {
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
            .Include(e => e.QualityProfile)
            .Where(e => e.Promotion.ToLower() == promotion.ToLower())
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<List<FightEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
            .Include(e => e.QualityProfile)
            .Where(e => e.EventDate >= startDate && e.EventDate <= endDate)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<List<FightEvent>> GetMissingAsync()
    {
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
            .Include(e => e.QualityProfile)
            .Where(e => e.Monitored && 
                       e.Status == EventStatus.Missing && 
                       e.EventDate <= DateTime.UtcNow)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<List<FightEvent>> GetDownloadedAsync()
    {
        return await _context.FightEvents
            .Include(e => e.Fights)
            .Include(e => e.EventFiles)
            .Include(e => e.QualityProfile)
            .Where(e => e.Status == EventStatus.Downloaded)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }
}
