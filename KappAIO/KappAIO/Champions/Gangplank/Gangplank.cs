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
        internal static int ConnectionRange = 685;

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

            ComboMenu.CreateSlider("RAOE", "R AoE Hit {0}", 3, 1, 6);
            KillStealMenu.CreateSlider("Rdmg", "Multipy R Damage By X{0}", 3, 1, 10);
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                //Chat.Print("fire1");
                var target =
                    EntityManager.Heroes.Enemies.FirstOrDefault(e => e.IsKillable() &&
                    BarrelsList.Any(b => b.Barrel.IsValidTarget(Q.Range) && (KillableBarrel(b)?.Distance(e) <= E.Width || BarrelsList.Any(a => KillableBarrel(b)?.Distance(a.Barrel) <= ConnectionRange && e.Distance(b.Barrel) <= E.Width))))
                    ?? TargetSelector.GetTarget(E.Range, DamageType.Physical);
                var position = Vector3.Zero;
                var startposition = Vector3.Zero;
                if (args.Slot == SpellSlot.Q && E.IsReady())
                {
                    //Chat.Print("fire2");
                    var barrel = BarrelsList.FirstOrDefault(b => b.Barrel.NetworkId == args.Target.NetworkId);
                    var Secondbarrel = BarrelsList.FirstOrDefault(b => b.Barrel.NetworkId != args.Target.NetworkId && b.Barrel.Distance(args.Target) <= ConnectionRange);
                    if (barrel != null)
                    {
                        //Chat.Print("fire3");
                        startposition = Secondbarrel?.Barrel.Position ?? barrel.Barrel.Position;
                    }
                    if (startposition != Vector3.Zero)
                    {
                        //Chat.Print("fire4");
                        if (target != null && target.IsKillable(E.Range + E.Width))
                        {
                            //Chat.Print("fire5");
                            if (target.Distance(startposition) <= ConnectionRange + E.Width && target.Distance(startposition) > E.Width - 75)
                            {
                                //Chat.Print("fire6");
                                position = target.Distance(startposition) < E.Width + ConnectionRange ? E.GetPrediction(target).CastPosition : startposition.Extend(E.GetPrediction(target).CastPosition, ConnectionRange).To3D();
                            }
                        }
                        else
                        {
                            //Chat.Print("fire7");
                            target = EntityManager.Heroes.Enemies.FirstOrDefault(e => e.Distance(startposition) <= ConnectionRange + E.Width && e.IsKillable(E.Range + E.Width));
                            if (target != null)
                            {
                                //Chat.Print("fire8");
                                position = target.Distance(startposition) < E.Width + ConnectionRange ? E.GetPrediction(target).CastPosition : startposition.Extend(E.GetPrediction(target).CastPosition, ConnectionRange).To3D();
                            }
                        }
                        if (position != Vector3.Zero)
                        {
                            //Chat.Print("fire9");
                            if (BarrelsList.Count(b => b.Barrel.Distance(position) <= E.Width) < 0)
                            {
                                //Chat.Print("fire10");
                                //Chat.Print("Casted event");
                                E.Cast(position);
                                Q.Cast(barrel?.Barrel);
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

            if (R.IsReady() && ComboMenu.CheckBoxValue(SpellSlot.R))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsKillable(3000)))
                {
                    if (enemy.CountEnemiesInRange(R.Width) >= ComboMenu.SliderValue("RAOE"))
                    {
                        R.Cast(enemy);
                    }
                }
            }

            var target = 
                EntityManager.Heroes.Enemies.OrderByDescending(TargetSelector.GetPriority).FirstOrDefault(e => e.IsKillable() &&
                BarrelsList.Any(b => b.Barrel.IsValidTarget(Q.Range) && (KillableBarrel(b)?.Distance(e) <= E.Width || BarrelsList.Any(a => KillableBarrel(b)?.Distance(a.Barrel) <= ConnectionRange && e.Distance(b.Barrel) <= E.Width))))
                ?? TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if(target == null || !target.IsKillable()) return;

            var pred = target.PrediectPosition((int)QTravelTime(target));
            var castpos = E.GetPrediction(target).CastPosition;

            Orbwalker.ForcedTarget = AABarrel(target);

            if (AABarrel(target) != null)
            {
                if (E.IsReady() && ComboMenu.CheckBoxValue(SpellSlot.E))
                {
                    if (BarrelsList.Count(b => b.Barrel.Distance(user) <= Q.Range) > 0 && BarrelsList.Count(b => b.Barrel.Distance(castpos) <= E.Width) < 0)
                    {
                        E.Cast(castpos);
                    }
                }
                Player.IssueOrder(GameObjectOrder.AttackUnit, AABarrel(target));
                return;
            }

            if (Q.IsReady())
            {
                if (ComboMenu.CheckBoxValue(SpellSlot.Q))
                {
                    if (((BarrelsList.Count(b => b.Barrel.Distance(user) <= E.Range) < 1 && !E.IsReady()) || Q.WillKill(target)) && target.IsKillable(Q.Range))
                    {
                        Q.Cast(target);
                    }

                    foreach (var A in BarrelsList.OrderBy(b => b.Barrel.Distance(target)))
                    {
                        if (KillableBarrel(A) != null && KillableBarrel(A).IsValidTarget(Q.Range))
                        {
                            if (pred.IsInRange(KillableBarrel(A), E.Width))
                            {
                                Q.Cast(KillableBarrel(A));
                            }

                            var Secondbarrel = BarrelsList.OrderBy(b => b.Barrel.Distance(target)).FirstOrDefault(b => b.Barrel.NetworkId != KillableBarrel(A).NetworkId && b.Barrel.Distance(KillableBarrel(A)) <= ConnectionRange);
                            if (Secondbarrel != null)
                            {
                                if (pred.IsInRange(Secondbarrel.Barrel, E.Width))
                                {
                                    Q.Cast(KillableBarrel(A));
                                }
                                if (BarrelsList.OrderBy(b => b.Barrel.Distance(target)).Any(b => b.Barrel.NetworkId != Secondbarrel.Barrel.NetworkId && b.Barrel.Distance(Secondbarrel.Barrel) <= ConnectionRange && b.Barrel.CountEnemiesInRange(E.Width) > 0))
                                {
                                    Q.Cast(KillableBarrel(A));
                                }
                            }
                            else
                            {
                                if (BarrelsList.OrderBy(b => b.Barrel.Distance(target)).Any(b => b.Barrel.NetworkId != KillableBarrel(A).NetworkId && b.Barrel.Distance(KillableBarrel(A)) <= ConnectionRange && b.Barrel.CountEnemiesInRange(E.Width) > 0))
                                {
                                    Q.Cast(KillableBarrel(A));
                                }
                            }
                        }
                    }
                }
                if (E.IsReady() && ComboMenu.CheckBoxValue(SpellSlot.E))
                {
                    if (BarrelsList.OrderBy(b => b.Barrel.Distance(target)).Count(b => b.Barrel.IsInRange(target, E.Width)) < 1)
                    {
                        if (BarrelsList.OrderBy(b => b.Barrel.Distance(target)).Count(b => b.Barrel.IsInRange(target, E.Width + ConnectionRange)) > 0)
                        {
                            var targetbarrel = BarrelsList.OrderBy(b => b.Barrel.Distance(target)).FirstOrDefault(b => KillableBarrel(b) != null && (b.Barrel.IsValidTarget(Q.Range) || b.Barrel.IsValidTarget(user.GetAutoAttackRange())) && b.Barrel.IsInRange(target, E.Width + ConnectionRange));
                            if (targetbarrel != null && KillableBarrel(targetbarrel) != null)
                            {
                                var Secondbarrel = BarrelsList.OrderBy(b => b.Barrel.Distance(target)).FirstOrDefault(b => b.Barrel.NetworkId != KillableBarrel(targetbarrel).NetworkId && b.Barrel.Distance(KillableBarrel(targetbarrel)) <= ConnectionRange);
                                castpos = Secondbarrel?.Barrel.Distance(castpos) >= ConnectionRange ? KillableBarrel(targetbarrel).Position.Extend(castpos, ConnectionRange).To3D() : castpos;
                                //Chat.Print("Casted");
                                E.Cast(castpos);
                            }
                        }
                        else
                        {
                            if (E.Handle.Ammo > 1)
                            {
                                if (HPTiming() <= 1000 || target.IsCC())
                                {
                                    E.Cast(castpos);
                                }

                                var circle = new Geometry.Polygon.Circle(castpos, ConnectionRange);
                                var grass = circle.Points.OrderBy(p => p.Distance(castpos)).FirstOrDefault(p => p.IsGrass());
                                if (grass != null)
                                {
                                    E.Cast(grass.To3D());
                                }
                            }
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
            if (Q.IsReady())
            {
                if (E.IsReady() && LaneClearMenu.CheckBoxValue(SpellSlot.E))
                {
                    foreach (var minion in EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(m => m.Health).Where(m => m.IsKillable(E.Range) && BarrelKill(m)))
                    {
                        var pred = E.GetPrediction(minion);
                        if (EntityManager.MinionsAndMonsters.EnemyMinions.Count(e => e.Distance(pred.CastPosition) <= E.Width && BarrelKill(e)) > 1)
                        {
                            if (BarrelsList.Count(b => b.Barrel.IsInRange(pred.CastPosition, E.Width)) < 1
                                || (BarrelsList.Count(b => b.Barrel.IsInRange(pred.CastPosition, ConnectionRange)) > 0 && BarrelsList.Count(b => b.Barrel.IsInRange(pred.CastPosition, E.Width)) < 1))
                            {
                                E.Cast(pred.CastPosition);
                                return;
                            }
                        }
                    }
                }
                if (LaneClearMenu.CheckBoxValue(SpellSlot.Q))
                {
                    var barrel = BarrelsList.OrderByDescending(b => b.Barrel.CountEnemyMinionsInRange(E.Width)).FirstOrDefault(m => KillableBarrel(m) != null && m.Barrel.CountEnemyMinionsInRange(E.Width) > 0 && (KillableBarrel(m).IsValidTarget(Q.Range) || KillableBarrel(m).IsInRange(user, user.GetAutoAttackRange())));
                    if (barrel != null)
                    {
                        if (KillableBarrel(barrel).IsValidTarget(user.GetAutoAttackRange()))
                        {
                            Orbwalker.ForcedTarget = KillableBarrel(barrel);
                        }
                        else
                        {
                            if (KillableBarrel(barrel).IsValidTarget(Q.Range))
                            {
                                Q.Cast(barrel.Barrel);
                            }
                        }
                    }
                    else
                    {
                        foreach (var minion in EntityManager.MinionsAndMonsters.EnemyMinions.OrderByDescending(m => m.Distance(user)).Where(m => m.IsKillable(Q.Range) && Q.WillKill(m) && !BarrelsList.Any(b => b.Barrel.Distance(m) <= E.Width)))
                        {
                            Q.Cast(minion);
                        }
                    }
                }
            }
        }

        public override void JungleClear()
        {
        }

        public override void KillSteal()
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsKillable()))
            {
                if (Q.IsReady() && Q.WillKill(enemy) && enemy.IsKillable(Q.Range) && KillStealMenu.CheckBoxValue(SpellSlot.Q))
                {
                    Q.Cast(enemy);
                }
                if (R.IsReady() && KillStealMenu.CheckBoxValue(SpellSlot.R) && R.WillKill(enemy, KillStealMenu.SliderValue("Rdmg")))
                {
                    R.Cast(enemy);
                }
                if (KillStealMenu.CheckBoxValue(SpellSlot.E))
                {
                    foreach (var a in BarrelsList)
                    {
                        if (BarrelKill(enemy))
                        {
                            if (KillableBarrel(a)?.Distance(enemy) <= E.Width)
                            {
                                Q.Cast(KillableBarrel(a));
                            }
                            if (BarrelsList.Any(b => b.Barrel.Distance(KillableBarrel(a)) <= ConnectionRange && enemy.Distance(b.Barrel) <= E.Width))
                            {
                                Q.Cast(KillableBarrel(a));
                            }
                        }
                    }
                }
            }
        }

        public override void Draw()
        {
            foreach (var spell in SpellList.Where(s => s != R && DrawMenu.CheckBoxValue(s.Slot)))
            {
                Circle.Draw(spell.IsReady() ? SharpDX.Color.Chartreuse : SharpDX.Color.OrangeRed, spell.Range, user);
            }

            foreach (var A in BarrelsList)
            {
                //Circle.Draw(Color.AliceBlue, 325, A.Barrel);
                foreach (var B in BarrelsList.Where(b => b.Barrel.NetworkId != A.Barrel.NetworkId))
                {
                    if (B.Barrel.Distance(A.Barrel) <= ConnectionRange)
                    {
                        Drawing.DrawLine(new Vector2(A.Barrel.ServerPosition.WorldToScreen().X, A.Barrel.ServerPosition.WorldToScreen().Y), new Vector2(B.Barrel.ServerPosition.WorldToScreen().X, B.Barrel.ServerPosition.WorldToScreen().Y), 2, System.Drawing.Color.Red);
                    }
                    //Circle.Draw(Color.AliceBlue, 325, B.Barrel);
                }
            }
            
            foreach (var b in BarrelsList)
            {
                if (KillableBarrel(b) != null)
                {
                    Circle.Draw(EntityManager.Heroes.Enemies.Any(e => e.Distance(b.Barrel) <= E.Width) ? Color.Red : Color.AliceBlue, 325, KillableBarrel(b));
                }
            }
            var target =
                EntityManager.Heroes.Enemies.FirstOrDefault(e => e.IsKillable() &&
                BarrelsList.Any(b => b.Barrel.IsValidTarget(Q.Range) && (KillableBarrel(b)?.Distance(e) <= E.Width || BarrelsList.Any(a => KillableBarrel(b)?.Distance(a.Barrel) <= ConnectionRange && e.Distance(b.Barrel) <= E.Width))))
                ?? TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target == null || !target.IsKillable(E.Range))
                return;

            var castpos = E.GetPrediction(target).CastPosition;

            if (BarrelsList.Count(b => b.Barrel.IsInRange(target, E.Width)) < 1)
            {
                if (BarrelsList.Count(b => b.Barrel.IsInRange(target, E.Width + ConnectionRange)) > 0)
                {
                    var targetbarrel = BarrelsList.FirstOrDefault(b => KillableBarrel(b) != null && (b.Barrel.IsValidTarget(Q.Range) || b.Barrel.IsValidTarget(user.GetAutoAttackRange())) && b.Barrel.IsInRange(target, E.Width + ConnectionRange));
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
