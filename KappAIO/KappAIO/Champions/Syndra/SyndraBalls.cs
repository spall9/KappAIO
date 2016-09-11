using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using KappAIO.Common;

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

        private static Obj_AI_Minion theball;
        internal static Obj_AI_Minion SelectBall(Obj_AI_Base target)
        {
            if (target == null || !Syndra.E.IsReady() || Core.GameTickCount - Lastupdate < 40 || !BallsList.Any(b => b.IsInRange(target, Syndra.Eball.Range) && Syndra.E.IsInRange(b)))
                return theball;
            
            var CastPosition = Syndra.Q.GetPrediction(target).CastPosition;
            foreach (var ball in BallsList.Where(b => b != null && Syndra.E.IsInRange(b)))
            {
                var source = Player.Instance.PrediectPosition(Game.Ping + Syndra.Eball.CastDelay);
                var start = ball.ServerPosition.Extend(source, 100).To3D();
                var end = source.Extend(ball.ServerPosition, Syndra.Eball.Range).To3D();
                var rect = new Geometry.Polygon.Rectangle(start, end, Syndra.Eball.Width);
                if (rect.IsInside(CastPosition))
                {
                    theball = ball;
                }
            }
            return theball;
        }

        private static float Lastdmgcalc;
        internal static float ComboDamage(Obj_AI_Base target, bool R = false)
        {
            if (target == null || Core.GameTickCount - Lastupdate < 100)
                return Lastdmgcalc;
            
            var Qdmg = target.IsKillable(Syndra.Q.Range) ? Syndra.Q.IsReady() ? Player.Instance.GetSpellDamage(target, SpellSlot.Q) : 0 : 0;
            var Wdmg = target.IsKillable(Syndra.W.Range) ? Syndra.W.IsReady() ? Player.Instance.GetSpellDamage(target, SpellSlot.W) : 0 : 0;
            var Edmg = target.IsKillable(Syndra.E.Range) || SelectBall(target) != null ? Syndra.E.IsReady() ? Player.Instance.GetSpellDamage(target, SpellSlot.E) : 0 : 0;
            var Rdmg = target.IsKillable(Syndra.R.Range) ? Syndra.R.IsReady() ? R ? RDamage(target) : 0 : 0 : 0;

            Lastdmgcalc = (Qdmg + Wdmg + Edmg + Rdmg) * 0.9f - target.HPRegenRate;
            return Lastdmgcalc;
        }

        internal static float RDamage(Obj_AI_Base target)
        {
            if (target == null || !Syndra.R.IsLearned)
                return 0;

            var ap = Player.Instance.FlatMagicDamageMod;
            var index = Player.GetSpell(SpellSlot.R).Level - 1;
            var mindmg = new float[] { 270, 405, 540 }[index] + 0.6f * ap;
            var maxdmg = new float[] { 630, 975, 1260 }[index] + 1.4f * ap;
            var perballdmg = (new float[] { 90, 135, 180 }[index] + 0.2f * ap) * BallsList.Count();

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, Math.Max(mindmg, maxdmg) + perballdmg) * 0.9f;
        }
    }
}
