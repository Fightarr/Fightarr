using Fightarr.Core.Models;

namespace Fightarr.Data.Repositories;

public interface IFightEventRepository
{
    Task<FightEvent?> GetByIdAsync(int id);
    Task<FightEvent?> GetByExternalIdAsync(string externalId);
    Task<List<FightEvent>> GetAllAsync();
    Task<List<FightEvent>> GetMonitoredAsync();
    Task<List<FightEvent>> GetUpcomingAsync(DateTime? fromDate = null);
    Task<List<FightEvent>> GetRecentAsync(DateTime? fromDate = null);
    Task<List<FightEvent>> SearchAsync(string query);
    Task<FightEvent> AddAsync(FightEvent fightEvent);
    Task<FightEvent> UpdateAsync(FightEvent fightEvent);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string externalId);
    Task<List<FightEvent>> GetByPromotionAsync(string promotion);
    Task<List<FightEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<FightEvent>> GetMissingAsync();
    Task<List<FightEvent>> GetDownloadedAsync();
}
