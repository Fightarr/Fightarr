using Fightarr.Core.Models;

namespace Fightarr.Core.Services;

public interface IMetadataService
{
    Task<List<FightEvent>> SearchEventsAsync(string query);
    Task<FightEvent?> GetEventByIdAsync(string externalId);
    Task<List<FightEvent>> GetUpcomingEventsAsync(int days = 30);
    Task<List<FightEvent>> GetRecentEventsAsync(int days = 7);
    Task<FightEvent?> UpdateEventMetadataAsync(int eventId);
}
