using KappAIO.Common;
using KappAIO.Common.KappaEvade;

namespace KappAIO.Utility
{
    class Load
    {
        public static void Init()
        {
            KappaEvade.Init();
            Events.Init();
            Activator.Load.Init();
            //Tracker.Ganks.Init();
        }
    }
}
