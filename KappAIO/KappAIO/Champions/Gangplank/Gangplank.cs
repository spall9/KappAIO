using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Rendering;
using KappAIO.Common;
using SharpDX;
using static KappAIO.Champions.Gangplank.BarrelsManager;

namespace KappAIO.Champions.Gangplank
{
    internal class Gangplank : Base
    {
        internal static int ConnectionRange = 680;

        public static Spell.Targeted Q { get; }
        public static Spell.Active W { get; }
        public static Spell.Skillshot E { get; }
        public static Spell.Skillshot R { get; }

        static Gangplank()
        {
            Init();

            Q = new Spell.Targeted(SpellSlot.Q, 625);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 1000, SkillShotType.Circular, 250, int.MaxValue, 325);
            R = new Spell.Skillshot(SpellSlot.R, int.MaxValue, SkillShotType.Circular, 250, int.MaxValue, 600);

            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && args.Slot == SpellSlot.Q)
            {
                Chat.Print("fire1");
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                var position = Vector3.Zero;
                var startposition = Vector3.Zero;
                if (args.Slot == SpellSlot.Q)
                {
                    Chat.Print("fire2");
                    var barrel = BarrelsList.FirstOrDefault(b => b.Barrel.NetworkId == args.Target.NetworkId);
                    var Secondbarrel = BarrelsList.FirstOrDefault(b => b.Barrel.NetworkId != args.Target.NetworkId && b.Barrel.Distance(args.Target) <= ConnectionRange);
                    if (barrel != null)
                    {
                        Chat.Print("fire3");
                        startposition = Secondbarrel?.Barrel.Position ?? barrel.Barrel.Position;
                    }
                    if (startposition != Vector3.Zero)
                    {
                        Chat.Print("fire4");
                        if (target != null && target.IsKillable(E.Range + E.Width))
                        {
                            Chat.Print("fire5");
                            if (target.Distance(startposition) <= ConnectionRange + E.Width && target.Distance(startposition) > E.Width - 75)
                            {
                                Chat.Print("fire6");
                                position = E.GetPrediction(target).CastPosition;
                            }
                        }
                        else
                        {
                            Chat.Print("fire7");
                            target = EntityManager.Heroes.Enemies.FirstOrDefault(e => e.Distance(startposition) <= ConnectionRange + E.Width && e.IsKillable(E.Range + E.Width));
                            if (target != null)
                            {
                                Chat.Print("fire8");
                                position = E.GetPrediction(target).CastPosition;
                            }
                        }
                        if (position != Vector3.Zero)
                        {
                            if (BarrelsList.Count(b => b.Barrel.Distance(position) <= E.Width) < 0)
                            {
                                Chat.Print("fire9");
                                E.Cast(position);
                            }
                        }
                    }
                }
            }
        }

        public override void Active()
        {
        }

        public override void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range + 100, DamageType.Physical);
            if(target == null || !target.IsKillable(E.Range)) return;
            
            Orbwalker.ForcedTarget = AABarrel(target);

            if(AABarrel(target) != null) return;

            var castpos = E.GetPrediction(target).CastPosition;

            if (Q.IsReady())
            {
                if (BarrelsList.Count(b => b.Barrel.Distance(user) <= E.Range) < 1 && !E.IsReady() && target.IsKillable(Q.Range))
                {
                    Q.Cast(target);
                }

                foreach (var A in BarrelsList)
                {
                    if (KillableBarrel(A) != null && KillableBarrel(A).IsValidTarget(Q.Range))
                    {
                        if (target.IsInRange(KillableBarrel(A), E.Width))
                        {
                            Q.Cast(KillableBarrel(A));
                        }

                        var Secondbarrel = BarrelsList.FirstOrDefault(b => b.Barrel.NetworkId != KillableBarrel(A).NetworkId && b.Barrel.Distance(KillableBarrel(A)) <= ConnectionRange);
                        if (Secondbarrel != null)
                        {
                            if (target.IsInRange(Secondbarrel.Barrel, E.Width))
                            {
                                Q.Cast(KillableBarrel(A));
                            }
                        }
                    }
                }
                if (E.IsReady())
                {
                    if (BarrelsList.Count(b => b.Barrel.IsInRange(target, E.Width)) < 1)
                    {
                        if (BarrelsList.Count(b => b.Barrel.IsInRange(target, E.Radius + ConnectionRange)) > 0)
                        {
                            var targetbarrel = BarrelsList.FirstOrDefault(b => KillableBarrel(b) != null && (b.Barrel.IsValidTarget(Q.Range) || b.Barrel.IsValidTarget(user.GetAutoAttackRange())) && b.Barrel.IsInRange(target, E.Radius + ConnectionRange));
                            if (targetbarrel != null && KillableBarrel(targetbarrel) != null)
                            {
                                var Secondbarrel = BarrelsList.FirstOrDefault(b => b.Barrel.NetworkId != KillableBarrel(targetbarrel).NetworkId && b.Barrel.Distance(KillableBarrel(targetbarrel)) <= ConnectionRange);
                                E.Cast(Secondbarrel?.Barrel.Position.Extend(castpos, ConnectionRange).To3D() ?? KillableBarrel(targetbarrel).Position.Extend(castpos, ConnectionRange).To3D());
                            }
                        }
                        else
                        {
                            E.Cast(castpos);
                        }
                    }
                }
            }
        }

        public override void Flee()
        {
        }

        public override void Harass()
        {
        }

        public override void LaneClear()
        {
        }

        public override void JungleClear()
        {
        }

        public override void KillSteal()
        {
        }

        public override void Draw()
        {
            /*
            foreach (var A in BarrelsList)
            {
                Circle.Draw(Color.AliceBlue, 325, A.Barrel);
                foreach (var B in BarrelsList.Where(b => b.Barrel.NetworkId != A.Barrel.NetworkId))
                {
                    if (B.Barrel.Distance(A.Barrel) <= ConnectionRange)
                    {
                        Drawing.DrawLine(new Vector2(A.Barrel.ServerPosition.WorldToScreen().X, A.Barrel.ServerPosition.WorldToScreen().Y), new Vector2(B.Barrel.ServerPosition.WorldToScreen().X, B.Barrel.ServerPosition.WorldToScreen().Y), 2, System.Drawing.Color.Red);
                    }
                    Circle.Draw(Color.AliceBlue, 325, B.Barrel);
                }
            }*/

            foreach (var b in BarrelsList)
            {
                if (KillableBarrel(b) != null)
                {
                    Circle.Draw(Color.AliceBlue, 325, KillableBarrel(b));
                }
            }
            var target = TargetSelector.GetTarget(E.Range + 100, DamageType.Physical);
            if (target == null || !target.IsKillable(E.Range))
                return;

            var castpos = E.GetPrediction(target).CastPosition;

            if (BarrelsList.Count(b => b.Barrel.IsInRange(target, E.Width)) < 1)
            {
                if (BarrelsList.Count(b => b.Barrel.IsInRange(target, E.Radius + ConnectionRange)) > 0)
                {
                    var targetbarrel = BarrelsList.FirstOrDefault(b => KillableBarrel(b) != null && (b.Barrel.IsValidTarget(Q.Range) || b.Barrel.IsValidTarget(user.GetAutoAttackRange())) && b.Barrel.IsInRange(target, E.Radius + ConnectionRange));
                    if (targetbarrel != null && KillableBarrel(targetbarrel) != null)
                    {
                        var Secondbarrel = BarrelsList.FirstOrDefault(b => b.Barrel.NetworkId != KillableBarrel(targetbarrel).NetworkId && b.Barrel.Distance(KillableBarrel(targetbarrel)) <= ConnectionRange);
                        Circle.Draw(Color.PaleVioletRed, 325, Secondbarrel?.Barrel.Position.Extend(castpos, ConnectionRange).To3D() ?? KillableBarrel(targetbarrel).Position.Extend(castpos, ConnectionRange).To3D());
                    }
                }
                else
                {
                    Circle.Draw(Color.PaleVioletRed, 325, castpos);
                }
            }
        }
    }
}
