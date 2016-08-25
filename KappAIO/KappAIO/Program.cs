using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK.Events;
using KappAIO.Champions;
using KappAIO.Common;
using KappAIO.Common.KappaEvade;

namespace KappAIO
{
    internal class Program
    {
        private static readonly List<Champion> SupportedHeros = new List<Champion> { Champion.Gangplank, Champion.Kalista, Champion.Viktor };

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            KappaEvade.Init();
            Events.Init();
            Utility.Activator.Load.Init();
            if (!SupportedHeros.Contains(Player.Instance.Hero)) return;
            var Instance = (Base)Activator.CreateInstance(null, "KappAIO.Champions." + Player.Instance.Hero + "." + Player.Instance.Hero).Unwrap();
            CheckVersion.Init();
        }
    }
}
