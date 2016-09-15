using System;
using EloBuddy;
using EloBuddy.SDK.Events;
using KappAIO.Champions;
using KappAIO.Common;

namespace KappAIO
{
    internal class Program
    {
        private static void Main()
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            //Utility.Load.Init();
            //if(Utility.Activator.Load.MenuIni.CheckBoxValue("Champ")) return;
            try
            {
                var Instance = (Base)Activator.CreateInstance(null, "KappAIO.Champions." + Player.Instance.Hero + "." + Player.Instance.Hero).Unwrap();
                CheckVersion.Init();
            }
            catch (Exception)
            {
                Logger.Send(Player.Instance.ChampionName + " Not Supported By KappAIO", Logger.LogLevel.Warn);
            }
        }
    }
}
