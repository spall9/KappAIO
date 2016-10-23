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

        internal static Obj_AI_Minion SelectBall(Obj_AI_Base target)
        {
            if (target == null || !Syndra.E.IsReady() || !BallsList.Any(b => b.IsInRange(target, Syndra.Eball.Range) && Syndra.E.IsInRange(b)))
                return null;

            var CastPosition = Syndra.Q.GetPrediction(target).CastPosition;
            var source = Player.Instance.PrediectPosition(Game.Ping + Syndra.Eball.CastDelay);
            var theball =
                BallsList.FirstOrDefault(
                    b =>
                    Syndra.E.IsInRange(b)
                    && new Geometry.Polygon.Rectangle(b.ServerPosition.Extend(source, 100).To3D(), source.Extend(b.ServerPosition, Syndra.Eball.Range).To3D(), Syndra.Eball.Width).IsInside(CastPosition));
            return theball;
        }
        
        internal static float ComboDamage(Obj_AI_Base target, bool R = false)
        {
            if (target == null)
                return 0;
            
            var Qdmg = target.IsKillable(Syndra.Q.Range) ? Syndra.Q.IsReady() ? Player.Instance.GetSpellDamage(target, SpellSlot.Q) : 0 : 0;
            var Wdmg = target.IsKillable(Syndra.W.Range) ? Syndra.W.IsReady() ? Player.Instance.GetSpellDamage(target, SpellSlot.W) : 0 : 0;
            var Edmg = target.IsKillable(Syndra.E.Range) || SelectBall(target) != null ? Syndra.E.IsReady() ? Player.Instance.GetSpellDamage(target, SpellSlot.E) : 0 : 0;
            var Rdmg = target.IsKillable(Syndra.R.Range) ? Syndra.R.IsReady() ? R ? RDamage(target) : 0 : 0 : 0;
            
            return (Qdmg + Wdmg + Edmg + Rdmg) * 0.8f - target.HPRegenRate;
        }

        internal static float RDamage(Obj_AI_Base target)
        {
            if (target == null || !Syndra.R.IsLearned)
                return 0;

            var ap = Player.Instance.FlatMagicDamageMod;
            var index = Player.GetSpell(SpellSlot.R).Level - 1;
            var mindmg = new float[] { 270, 405, 540 }[index] + 0.6f * ap;
            var maxdmg = new float[] { 630, 975, 1260 }[index] + 1.4f * ap;
            var perballdmg = (new float[] { 90, 135, 180 }[index] + 0.2f * ap) * Syndra.R.AmmoQuantity;

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, Math.Max(mindmg, maxdmg) + perballdmg) * 0.8f;
        }
    }
}
