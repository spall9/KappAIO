using System;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace KappAIO.Champions.Jhin
{
    internal static class JhinStuff
    {
        internal static string FirstR = "JhinR";
        internal static string JhinEBuffName = "JhinESpottedDebuff";

        internal static bool HasJhinEBuff(this AIHeroClient target)
        {
            var traveltime = Player.Instance.Distance(target) / Jhin.W.Speed * 1000 + Jhin.W.CastDelay + Game.Ping;
            var buff = target.GetBuff(JhinEBuffName);
            return buff != null && !target.HasBuffOfType(BuffType.SpellShield) && buff.IsActive && (buff.EndTime - Game.Time) * 1000 >= traveltime;
        }

        private static float JhinRDamage(Obj_AI_Base target)
        {
            var index = Jhin.R.Level - 1;
            var MinRDmg = new float[] { 40, 100, 160 }[index];
            var MaxRDmg = new float[] { 140, 350, 560 }[index];

            var MHADP = 1 + (100 - target.HealthPercent) * 0.025f;

            var mindmg = (MinRDmg + 0.2f * Player.Instance.TotalAttackDamage) * MHADP;
            var maxdmg = MaxRDmg + 0.7f * Player.Instance.TotalAttackDamage;
            
            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, Math.Min(mindmg, maxdmg));
        }

        private static float FinalJhinRDamage(Obj_AI_Base target)
        {
            return JhinRDamage(target) * 2f * Player.Instance.FlatCritChanceMod;
        }

        internal static float TotalRDamage(Obj_AI_Base target)
        {
            return FinalJhinRDamage(target) + JhinRDamage(target) * (3f - Jhin.CurrentRShot);
        }

        internal static float CurrentRDamage(Obj_AI_Base target)
        {
            return Jhin.CurrentRShot >= 3 ? FinalJhinRDamage(target) : JhinRDamage(target);
        }
        
        internal static Geometry.Polygon.Sector JhinRSector(Vector3 RCastedPos)
        {
            return new Geometry.Polygon.Sector(Player.Instance.ServerPosition, Player.Instance.ServerPosition.Extend(RCastedPos, Jhin.R.Range).To3D(), (float)(60f * Math.PI / 180), Jhin.R.Range - 175);
        }
    }
}
