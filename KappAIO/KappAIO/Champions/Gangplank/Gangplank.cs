using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
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

            MenuIni = MainMenu.AddMenu(MenuName, MenuName);
            AutoMenu = MenuIni.AddSubMenu("Auto");
            ComboMenu = MenuIni.AddSubMenu("Combo");
            HarassMenu = MenuIni.AddSubMenu("Harass");
            JungleClearMenu = MenuIni.AddSubMenu("JungleClear");
            LaneClearMenu = MenuIni.AddSubMenu("LaneClear");
            KillStealMenu = MenuIni.AddSubMenu("KillSteal");
            DrawMenu = MenuIni.AddSubMenu("Drawings");
            SpellList.Add(Q);
            SpellList.Add(E);
            SpellList.Add(R);
            SpellList.ForEach(
                i =>
                {
                    ComboMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                    if (i != R)
                    {
                        HarassMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                        HarassMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                        HarassMenu.AddSeparator(0);
                        LaneClearMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                        LaneClearMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                        LaneClearMenu.AddSeparator(0);
                        JungleClearMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                        JungleClearMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                        JungleClearMenu.AddSeparator(0);
                        DrawMenu.CreateCheckBox(i.Slot, "Draw " + i.Slot);
                    }
                    KillStealMenu.CreateCheckBox(i.Slot, i.Slot + " KillSteal");
                });

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && args.Slot == SpellSlot.Q && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
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
                            Chat.Print("fire9");
                            if (BarrelsList.Count(b => b.Barrel.Distance(position) <= E.Width) < 0)
                            {
                                Chat.Print("fire10");
                                E.Cast(position);
                            }
                        }
                    }
                }
            }
        }

        public override void Active()
        {
            if (user.IsCC())
            {
                W.Cast();
            }
        }

        public override void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range + 100, DamageType.Physical);
            if(target == null || !target.IsKillable(E.Range)) return;
            
            var castpos = E.GetPrediction(target).CastPosition;

            Orbwalker.ForcedTarget = AABarrel(target);

            if (AABarrel(target) != null)
            {
                if (E.IsReady())
                {
                    if (BarrelsList.Count(b => b.Barrel.Distance(user) <= Q.Range) > 0 && BarrelsList.Count(b => b.Barrel.Distance(castpos) <= E.Width) < 0)
                    {
                        E.Cast(castpos);
                    }
                }
                //Player.IssueOrder(GameObjectOrder.AttackUnit, AABarrel(target));
                return;
            }

            if (Q.IsReady())
            {
                if (((BarrelsList.Count(b => b.Barrel.Distance(user) <= E.Range) < 1 && !E.IsReady()) || Q.WillKill(target)) && target.IsKillable(Q.Range))
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
                                castpos = Secondbarrel?.Barrel.Position.Extend(castpos, ConnectionRange).To3D() ?? KillableBarrel(targetbarrel).Position.Extend(castpos, ConnectionRange).To3D();
                                E.Cast(castpos);
                            }
                        }
                        else
                        {
                            E.Cast(castpos);
                        }
                    }
                }
            }
            if (R.IsReady())
            {
                //R.CastIfItWillHit(3);
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
            if (E.IsReady())
            {
                foreach (var minion in EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(m => m.Health).Where(m => m.IsKillable(E.Range)))
                {
                    var pred = E.GetPrediction(minion);
                    if (pred.CastPosition.CountEnemyMinionsInRange(E.Width) > 1)
                    {
                        if (BarrelsList.Count(b => b.Barrel.IsInRange(pred.CastPosition, E.Width)) < 1
                            || (BarrelsList.Count(b => b.Barrel.IsInRange(pred.CastPosition, ConnectionRange)) > 0 && BarrelsList.Count(b => b.Barrel.IsInRange(pred.CastPosition, E.Width)) < 1))
                        {
                            E.Cast(pred.CastPosition);
                        }
                    }
                }
            }
            if (Q.IsReady())
            {
                var barrel = BarrelsList.OrderByDescending(b => b.Barrel.CountEnemyMinionsInRange(E.Width)).FirstOrDefault(m => KillableBarrel(m) != null && m.Barrel.CountEnemyMinionsInRange(E.Width) > 0 && (KillableBarrel(m).IsValidTarget(Q.Range) || KillableBarrel(m).IsInRange(user, user.GetAutoAttackRange())));
                if (barrel != null)
                {
                    if (KillableBarrel(barrel).IsValidTarget(Q.Range) && !KillableBarrel(barrel).IsValidTarget(user.GetAutoAttackRange()))
                    {
                        Q.Cast(barrel.Barrel);
                    }
                }
                else
                {
                    foreach (var minion in EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(m => m.Health).Where(m => m.IsKillable(Q.Range) && Q.WillKill(m) && !BarrelsList.Any(b => b.Barrel.Distance(m) <= E.Width)))
                    {
                        Q.Cast(minion);
                    }
                }
            }
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
                        castpos = Secondbarrel?.Barrel.Position.Extend(castpos, ConnectionRange).To3D() ?? KillableBarrel(targetbarrel).Position.Extend(castpos, ConnectionRange).To3D();
                        Circle.Draw(Color.PaleVioletRed, 325, castpos);
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
