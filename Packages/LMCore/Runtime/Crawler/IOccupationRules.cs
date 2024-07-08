using System.Collections.Generic;

namespace LMCore.Crawler
{
    public interface IOccupationRules
    {
        public bool MayCoexist(GridEntity entity, IEnumerable<GridEntity> occupants);
        public void HandleMeeting(GridEntity entity, IEnumerable<GridEntity> occupants);

        public void HandleDeparture(GridEntity entity, IEnumerable<GridEntity> occupants);
    }
}
