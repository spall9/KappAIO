using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using KappAIO.Common;
using SharpDX;

namespace KappAIO.Champions.Syndra
{
    internal static class SyndraBalls
    {
        internal static string SyndraEBallsBuff = "SyndraESphereMissile";

        internal static bool IsValidForW(this Obj_AI_Base target)
        {
            return !target.HasBuff(SyndraEBallsBuff) && target.IsValid && !target.IsDead && target.IsTargetable && !target.IsWard() && (target.IsMinion || target.IsMonster || target.IsSyndraBall());
        }

        private static bool IsSyndraBall(this Obj_AI_Base target)
        {
            return target.BaseSkinName.Equals("SyndraSphere");
        }

        private static float Lastupdate;
        internal static void Init()
        {
            Game.OnTick += delegate
                {
                    if (Core.GameTickCount - Lastupdate > 100)
                    {
                        BallsList.AddRange(ObjectManager.Get<Obj_AI_Minion>().Where(o => o != null && !o.IsDead && o.IsValid && o.Health > 0 && o.BaseSkinName.Equals("SyndraSphere")));
                        BallsList.RemoveAll(b => b == null || b.IsDead || !b.IsValid);
                        Lastupdate = Core.GameTickCount;
                    }
                };
        }
        
        internal static List<Obj_AI_Minion> BallsList = new List<Obj_AI_Minion>();

        internal static Obj_AI_Minion SelectBall(Vector3 pos)
        {
            if (pos == null || pos.IsZero || !Syndra.E.IsReady() || !BallsList.Any(b => b.IsInRange(pos, Syndra.Eball.Range) && Syndra.E.IsInRange(b)))
                return null;
            
            var source = Player.Instance.PrediectPosition(Game.Ping + Syndra.Eball.CastDelay);
            var theball =
                BallsList.FirstOrDefault(
                    b =>
                    Syndra.E.IsInRange(b)
                    && new Geometry.Polygon.Rectangle(b.ServerPosition.Extend(source, 100).To3D(), source.Extend(b.ServerPosition, Syndra.Eball.Range).To3D(), Syndra.Eball.Width).IsInside(pos));
            return theball;
        }

        internal static Obj_AI_Minion SelectBall(Obj_AI_Base target)
        {
            if (target == null)
                return null;

            var CastPosition = Syndra.Q.GetPrediction(target).CastPosition;
            return SelectBall(CastPosition);
        }
        
        internal static float ComboDamage(AIHeroClient target, bool R = false)
        {
            if (target == null || !target.IsKillable())
                return 0;
            
            var Qdmg = Syndra.Q.IsInRange(target) ? Syndra.Q.IsReady() ? Player.Instance.GetSpellDamage(target, SpellSlot.Q) : 0 : 0;
            var Wdmg = Syndra.W.IsInRange(target) ? Syndra.W.IsReady() ? Player.Instance.GetSpellDamage(target, SpellSlot.W) : 0 : 0;
            var Edmg = Syndra.E.IsInRange(target) || SelectBall(target) != null ? Syndra.E.IsReady() ? Player.Instance.GetSpellDamage(target, SpellSlot.E) : 0 : 0;
            var Rdmg = Syndra.R.IsInRange(target) ? Syndra.R.IsReady() ? R ? RDamage(target) : 0 : 0 : 0;
            
            return (Qdmg + Wdmg + Edmg + Rdmg) * 0.8f - target.HPRegenRate;
        }

        internal static float RDamage(AIHeroClient target)
        {
            if (target == null || !Syndra.R.IsLearned)
                return 0;

            var dmg = RMinDamage(target) * 0.8f;
            return dmg;
        }

        internal static float RMinDamage(AIHeroClient target)
        {
            if (target == null || !Syndra.R.IsLearned)
                return 0;

            var ap = Player.Instance.FlatMagicDamageMod;
            var index = Player.GetSpell(SpellSlot.R).Level - 1;
            var mindmg = new float[] { 270, 405, 540 }[index] + 0.6f * ap;
            var perballdmg = (new float[] { 90, 135, 180 }[index] + 0.2f * ap) * Syndra.R.AmmoQuantity * 0.8f;
            var ballsdamage = Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, perballdmg);

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, mindmg + ballsdamage);
        }

        internal static float RMaxDamage(AIHeroClient target)
        {
            if (target == null || !Syndra.R.IsLearned)
                return 0;

            var ap = Player.Instance.FlatMagicDamageMod;
            var index = Player.GetSpell(SpellSlot.R).Level - 1;
            var maxdmg = new float[] { 630, 975, 1260 }[index] + 1.4f * ap;
            var perballdmg = (new float[] { 90, 135, 180 }[index] + 0.2f * ap) * Syndra.R.AmmoQuantity;

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, maxdmg + perballdmg);
        }

        internal static bool RWillKill(AIHeroClient target)
        {
            return RDamage(target) >= target.TotalShieldHealth();
        }
    }
}
