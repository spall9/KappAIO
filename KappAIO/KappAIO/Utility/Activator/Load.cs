using System;
using EloBuddy.SDK.Menu;
using KappAIO.Common;
using KappAIO.Utility.Activator.Spells;

namespace KappAIO.Utility.Activator
{
    internal class Load
    {
        internal static Menu MenuIni;

        public static void Init()
        {
            try
            {
                MenuIni = MainMenu.AddMenu("KappActivator", "KappActivator");
                MenuIni.CreateCheckBox("Champ", "Load Only Activator", false);

                Items.Potions.Init();
                Cleanse.Qss.Init();
                Summoners.Init();
                Items.Offence.Init();
                Items.Defence.Init();
            }
            catch (Exception ex)
            {
                Logger.Send("Activator Load Error While Init", ex, Logger.LogLevel.Error);
            }
        }
    }
}
