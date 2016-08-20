using EloBuddy;
using EloBuddy.SDK;
using KappAIO.Common;

namespace KappAIO.Champions.Kalista
{
    static class Rend
    {
        public static bool EKill(this Obj_AI_Base target)
        {
            return target.EDamage(target.RendCount()) >= Prediction.Health.GetPrediction(target, Game.Ping / 2) && target.RendCount() > 0;
        }

        public static bool EKill(Obj_AI_Base From, Obj_AI_Base To)
        {
            return To.EDamage(From.RendCount() + To.RendCount()) + Player.Instance.GetSpellDamage(To, SpellSlot.Q) >= Prediction.Health.GetPrediction(To, Game.Ping / 2) && Kalista.Q.WillKill(From);
        }

        public static int RendCount(this Obj_AI_Base target)
        {
            return target.GetBuffCount("KalistaExpungeMarker");
        }

        public static float RendDamage(this Obj_AI_Base target, int stacks)
        {
            var flatAD = Player.Instance.FlatPhysicalDamageMod;
            var totalAD = Player.Instance.TotalAttackDamage;
            var index = Kalista.E.Level - 1;
            var Edmg = new float[] { 20, 30, 40, 50, 60 }[index];
            var EdmgPS = new float[] { 10, 14, 19, 25, 32 }[index];
            var EdmgPSM = new[] { 0.2f, 0.225f, 0.25f, 0.275f, 0.3f }[index];
            if (stacks == 0)
            {
                return 0;
            }
            return EdmgPS * stacks + (EdmgPSM * totalAD * stacks + Edmg + flatAD * 0.6f) + stacks;
        }

        public static float EDamage(this Obj_AI_Base target, int stacks)
        {
            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, RendDamage(target, stacks));
        }
    }
}
