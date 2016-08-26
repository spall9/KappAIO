using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Rendering;

namespace KappAIO.Utility.Tracker
{
    internal class Ganks
    {
        private static Enemies EnemyJungler;

        private static Text JunglerTime;

        public static void Init()
        {
            EnemyJungler = new Enemies(EntityManager.Heroes.Enemies.FirstOrDefault(e => e.Spellbook.Spells.Any(s =>s.Name.ToLower().Contains("smite") && (s.Slot == SpellSlot.Summoner1 || s.Slot == SpellSlot.Summoner2))), Core.GameTickCount);
            if(EnemyJungler?.Hero == null) return;

            JunglerTime = new Text(string.Empty, new Font("Tahoma", 10, FontStyle.Bold)) { Color = Color.White };
            Drawing.OnEndScene += Drawing_OnEndScene;
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (EnemyJungler.Hero.IsValidTarget())
            {
                EnemyJungler.StartTick = Core.GameTickCount;
            }

            var traveltime = EnemyJungler.Hero.Distance(Player.Instance) / EnemyJungler.Hero.MoveSpeed * 1000;
            var countdown = (int)(EnemyJungler.StartTick + traveltime - Core.GameTickCount);
            
            if (countdown >= 20000)
            {
                JunglerTime.Color = Color.GreenYellow;
            }
            if (countdown >= 10000 && countdown < 20000)
            {
                JunglerTime.Color = Color.Orange;
            }
            if (countdown < 10000)
            {
                JunglerTime.Color = Color.OrangeRed;
            }
            JunglerTime.TextValue = (EnemyJungler.Hero.IsDead ? "Dead" : countdown.ToString(CultureInfo.CurrentCulture)) + " - " + EnemyJungler.Hero.ChampionName;
            JunglerTime.Position = Player.Instance.ServerPosition.WorldToScreen();
            JunglerTime.Draw();
        }

        private class Enemies
        {
            internal AIHeroClient Hero;
            internal float StartTick;
            internal Enemies(AIHeroClient Enemy, float Tick)
            {
                this.Hero = Enemy;
                this.StartTick = Tick;
            }
        }
    }
}
