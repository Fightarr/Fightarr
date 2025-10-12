using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Qualities;
using Fightarr.Http;

namespace Fightarr.Api.V3.Profiles.Quality
{
    [FightarrApiController("qualityprofile/schema")]
    public class QualityProfileSchemaController : Controller
    {
        private readonly IQualityProfileService _profileService;

        public QualityProfileSchemaController(IQualityProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public QualityProfileResource GetSchema()
        {
            var qualityProfile = _profileService.GetDefaultProfile(string.Empty);

            return qualityProfile.ToResource();
        }
    }
}
