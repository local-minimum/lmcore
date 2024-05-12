using LMCore.IO;

namespace LMCore.Crawler
{
    public interface GridEntityController
    {
        public bool CanMoveTo(Movement movement, int length);
    }
}
