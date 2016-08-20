using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Spells;
using KappAIO.Common;

namespace KappAIO.Utility.Activator.Spells
{
    internal class Summoners
    {
        private static Menu SummMenu;

        public static void Init()
        {
            SummMenu = Load.MenuIni.AddSubMenu("SummonerSpells");
            SummMenu.AddGroupLabel("Allies");
            SummMenu.CreateCheckBox("ally", "Use Heal For Allies");
            SummMenu.CreateSlider("allyhp", "Ally HealthPercent {0}% To Use Heal", 30);
            SummMenu.AddSeparator(0);
            SummMenu.AddGroupLabel("Self");
            SummMenu.CreateCheckBox("me", "Use Heal For Self");
            SummMenu.CreateSlider("hp", "HealthPercent {0}% To Use Heal For ME", 30);

            Events.OnIncomingDamage += Events_OnIncomingDamage;
        }

        private static void Events_OnIncomingDamage(Events.InComingDamageEventArgs args)
        {
            if(args.Target == null || !args.Target.IsKillable() || args.Target.Distance(Player.Instance) > 800 || !SummonerSpells.Heal.IsReady()) return;

            var damagepercent = args.InComingDamage / args.Target.TotalShieldHealth() * 100;
            var death = args.InComingDamage >= args.Target.Health && args.Target.HealthPercent < 99;

            if (SummMenu.CheckBoxValue("ally") && args.Target.IsAlly && !args.Target.IsMe && (SummMenu.SliderValue("allyhp") >= args.Target.HealthPercent || death))
            {
                SummonerSpells.Heal.Cast();
            }

            if (SummMenu.CheckBoxValue("me") && args.Target.IsMe && (SummMenu.SliderValue("hp") >= args.Target.HealthPercent || death))
            {
                SummonerSpells.Heal.Cast();
            }
        }
    }
}
