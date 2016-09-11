using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Rendering;
using KappAIO.Common;
using SharpDX;
using static KappAIO.Champions.Syndra.SyndraBalls;
using Color = System.Drawing.Color;

namespace KappAIO.Champions.Syndra
{
    internal class Syndra : Base
    {
        public static Spell.Skillshot Q { get; set; }
        public static Spell.Skillshot W { get; set; }
        public static Spell.Skillshot E { get; set; }
        public static Spell.Skillshot Eball { get; set; }
        public static Spell.Targeted R { get; set; }

        private static Text dmg;
        private static float LastE;
        private static float LastW;

        static Syndra()
        {
            Init();
            dmg = new Text(string.Empty, new Font("Tahoma", 9, FontStyle.Bold)) { Color = Color.White };
            Q = new Spell.Skillshot(SpellSlot.Q, 810, SkillShotType.Circular, 600, int.MaxValue, 125) { AllowedCollisionCount = int.MaxValue, DamageType = DamageType.Magical };
            W = new Spell.Skillshot(SpellSlot.W, 1000, SkillShotType.Circular, 350, 1500, 140) { AllowedCollisionCount = int.MaxValue, DamageType = DamageType.Magical };
            E = new Spell.Skillshot(SpellSlot.E, 680, SkillShotType.Cone, 250, 2500, 50) { AllowedCollisionCount = int.MaxValue, DamageType = DamageType.Magical };
            R = new Spell.Targeted(SpellSlot.R, 680);
            Eball = new Spell.Skillshot(SpellSlot.E, 1100, SkillShotType.Linear, 600, 2400, 40) { AllowedCollisionCount = int.MaxValue, DamageType = DamageType.Magical };

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(Eball);
            SpellList.Add(R);

            MenuIni = MainMenu.AddMenu(MenuName, MenuName);
            AutoMenu = MenuIni.AddSubMenu("Auto");
            ComboMenu = MenuIni.AddSubMenu("Combo");
            HarassMenu = MenuIni.AddSubMenu("Harass");
            JungleClearMenu = MenuIni.AddSubMenu("JungleClear");
            LaneClearMenu = MenuIni.AddSubMenu("LaneClear");
            KillStealMenu = MenuIni.AddSubMenu("KillSteal");
            DrawMenu = MenuIni.AddSubMenu("Drawings");

            SpellList.ForEach(
                i =>
                {
                    ComboMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                    if (i != R)
                    {
                        HarassMenu.CreateCheckBox(i.Slot, "Use " + i.Slot, i != E);
                        HarassMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                        HarassMenu.AddSeparator(0);
                        LaneClearMenu.CreateCheckBox(i.Slot, "Use " + i.Slot, i != E);
                        LaneClearMenu.CreateSlider(i.Slot + "hit", i.Slot + " Hit {0} Minions", 3, 1, 20);
                        LaneClearMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                        LaneClearMenu.AddSeparator(0);
                        JungleClearMenu.CreateCheckBox(i.Slot, "Use " + i.Slot, i != E);
                        JungleClearMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                        JungleClearMenu.AddSeparator(0);
                    }
                    KillStealMenu.CreateCheckBox(i.Slot, i.Slot + " KillSteal");
                    DrawMenu.CreateCheckBox(i.Slot, "Draw " + i.Slot);
                });

            AutoMenu.CreateCheckBox("QEgap", "Auto QE Anti-Gapcloser");
            AutoMenu.CreateCheckBox("QEint", "Auto QE Interrupter");
            AutoMenu.CreateCheckBox("Egap", "Auto E Anti-Gapcloser");
            AutoMenu.CreateCheckBox("Eint", "Auto E Interrupter");
            AutoMenu.CreateCheckBox("Wunk", "Auto W Unkillable Minions");

            ComboMenu.CreateCheckBox("QE", "Use QE");
            HarassMenu.CreateCheckBox("QE", "Use QE");
            KillStealMenu.CreateCheckBox("QE", "QE KillSteal");

            DrawMenu.CreateCheckBox("dmg", "Draw Combo Damage");
            DrawMenu.CreateCheckBox("balls", "Draw Balls");

            MenuList.Add(HarassMenu);
            MenuList.Add(LaneClearMenu);
            MenuList.Add(JungleClearMenu);

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Orbwalker.OnUnkillableMinion += Orbwalker_OnUnkillableMinion;
        }

        private static void Orbwalker_OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (W.IsReady() && W.WillKill(target) && target.IsKillable(W.Range) && AutoMenu.CheckBoxValue("Wunk"))
            {
                W.Cast(target);
            }
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (sender == null || !sender.IsEnemy || !sender.IsKillable()) return;

            if (AutoMenu.CheckBoxValue("QEint") && Q.IsReady() && E.IsReady() && sender.IsKillable(1200))
            {
                QE(sender);
            }
            else
            {
                if (E.IsReady() && AutoMenu.CheckBoxValue("Eint"))
                {
                    if (SelectBall(sender) != null && E.IsInRange(SelectBall(sender)))
                    {
                        Eball.Cast(SelectBall(sender));
                        return;
                    }
                    if (sender.IsKillable(E.Range))
                    {
                        E.Cast(sender, 25);
                    }
                }
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if(sender == null || !sender.IsEnemy || !sender.IsKillable()) return;

            if (AutoMenu.CheckBoxValue("QEgap") && Q.IsReady() && E.IsReady() && sender.IsKillable(1200))
            {
                QE(sender);
            }
            else
            {
                if (E.IsReady() && AutoMenu.CheckBoxValue("Egap"))
                {
                    if (SelectBall(sender) != null && E.IsInRange(SelectBall(sender)))
                    {
                        Eball.Cast(SelectBall(sender));
                        return;
                    }
                    if (sender.IsKillable(E.Range))
                    {
                        E.Cast(sender, 25);
                    }
                }
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe) return;

            if(args.Slot == SpellSlot.W && W.Handle.ToggleState == 1)
                args.Process = Core.GameTickCount - LastE > 150 + Game.Ping;
            if(args.Slot == SpellSlot.W)
                args.Process = Core.GameTickCount - LastW > 100 + Game.Ping;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            if(args.Slot == SpellSlot.E)
                LastE = Core.GameTickCount;
            if (args.Slot == SpellSlot.W)
                LastW = Core.GameTickCount;
        }

        public override void Active()
        {
            if (R.Level == 3 && R.Range != 755)
            {
                R.Range = 755;
            }
        }

        public override void Combo()
        {
            var Qtarget = Q.GetTarget();
            var Wtarget = W.GetTarget();
            var Etarget = E.GetTarget();
            //var Rtarget = EntityManager.Heroes.Enemies.OrderByDescending(TargetSelector.GetPriority).FirstOrDefault(t => t.IsKillable(R.Range) && RDamage(t) >= t.TotalShieldHealth());
            if (SelectBall(Etarget) == null)
            {
                Etarget = EntityManager.Heroes.Enemies.OrderByDescending(TargetSelector.GetPriority).FirstOrDefault(t => (BallsList.Any() ? BallsList.Any(b => b.IsInRange(t, Eball.Range) && E.IsInRange(b)) : t.IsKillable(1200)) && t.IsKillable());
            }
            var FullCombotarget = EntityManager.Heroes.Enemies.OrderByDescending(TargetSelector.GetPriority).FirstOrDefault(e => ComboDamage(e, true) >= e.Health && e.IsKillable(R.Range));

            if (W.Handle.ToggleState != 1 && Wtarget != null && W.IsReady() && Wtarget.IsKillable(W.Range))
            {
                W.Cast(Wtarget);
            }

            if (FullCombotarget != null && FullCombotarget.IsKillable())
            {
                if (Q.IsReady() && FullCombotarget.IsKillable(Q.Range) && ComboMenu.CheckBoxValue(SpellSlot.Q) && user.Mana >= Q.Mana() + W.Mana() + E.Mana() + R.Mana())
                {
                    Q.Cast(FullCombotarget, 45);
                }
                if (E.IsReady() && ComboMenu.CheckBoxValue(SpellSlot.E) && user.Mana >= W.Mana() + E.Mana() + R.Mana())
                {
                    if (SelectBall(FullCombotarget) != null && E.IsInRange(SelectBall(FullCombotarget)))
                    {
                        Eball.Cast(SelectBall(FullCombotarget));
                        return;
                    }
                    if (FullCombotarget.IsKillable(E.Range))
                    {
                        E.Cast(FullCombotarget, 25);
                        return;
                    }
                }
                if (W.IsReady() && FullCombotarget.IsKillable(W.Range) && ComboMenu.CheckBoxValue(SpellSlot.W) && user.Mana >= W.Mana() + R.Mana())
                {
                    WCast(FullCombotarget);
                }
                if (R.IsReady() && FullCombotarget.IsKillable(R.Range) && ComboMenu.CheckBoxValue(SpellSlot.R) && !(Q.IsReady() && W.IsReady() && E.IsReady()))
                {
                    R.Cast(FullCombotarget);
                }
            }

            if (E.IsReady() && Etarget != null && SelectBall(Etarget) != null && E.IsInRange(SelectBall(Etarget)) && ComboMenu.CheckBoxValue(SpellSlot.E))
            {
                Eball.Cast(SelectBall(Etarget));
                return;
            }

            if (Etarget != null && Q.IsReady() && E.IsReady() && ComboMenu.CheckBoxValue("QE"))
            {
                QE(Etarget);
            }

            if (Qtarget != null && Q.IsReady() && Qtarget.IsKillable(Q.Range) && ComboMenu.CheckBoxValue(SpellSlot.Q))
            {
                Q.Cast(Qtarget, 30);
            }
            
            if (Etarget != null && E.IsReady() && ComboMenu.CheckBoxValue(SpellSlot.E))
            {
                if (Etarget.IsKillable(E.Range) && user.HealthPercent <= 20)
                {
                    E.Cast(Etarget, 25);
                    return;
                }
            }

            if (Wtarget != null && W.IsReady() && Wtarget.IsKillable(W.Range) && ComboMenu.CheckBoxValue(SpellSlot.W))
            {
                W.Cast(Wtarget);
            }
        }

        public override void Flee()
        {
        }

        public override void Harass()
        {
            var Qtarget = Q.GetTarget();
            var Wtarget = W.GetTarget();
            var Etarget = E.GetTarget();

            if (SelectBall(Etarget) == null)
            {
                Etarget = EntityManager.Heroes.Enemies.OrderByDescending(TargetSelector.GetPriority).FirstOrDefault(t => (BallsList.Any() ? BallsList.Any(b => b.IsInRange(t, Eball.Range) && E.IsInRange(b)) : t.IsKillable(1200)) && t.IsKillable());
            }

            if (Etarget != null && Q.IsReady() && E.IsReady() && HarassMenu.CheckBoxValue("QE") && HarassMenu.CompareSlider("Emana", user.ManaPercent))
            {
                QE(Etarget);
            }

            if (Wtarget != null && W.IsReady() && Wtarget.IsKillable(W.Range) && HarassMenu.CheckBoxValue(SpellSlot.W) && HarassMenu.CompareSlider("Wmana", user.ManaPercent))
            {
                WCast(Wtarget);
                return;
            }
            if (Qtarget != null && Q.IsReady() && Qtarget.IsKillable(Q.Range) && HarassMenu.CheckBoxValue(SpellSlot.Q) && HarassMenu.CompareSlider("Qmana", user.ManaPercent))
            {
                Q.Cast(Qtarget, 30);
                return;
            }
            if (Etarget != null && E.IsReady() && HarassMenu.CheckBoxValue(SpellSlot.E) && HarassMenu.CompareSlider("Emana", user.ManaPercent))
            {
                if (SelectBall(Etarget) != null && E.IsInRange(SelectBall(Etarget)))
                {
                    Eball.Cast(SelectBall(Etarget));
                    return;
                }
                if (Etarget.IsKillable(E.Range) && user.HealthPercent <= 20)
                {
                    E.Cast(Etarget, 25);
                }
            }
        }

        public override void LastHit()
        {
        }

        public override void LaneClear()
        {
            if (Q.IsReady() && LaneClearMenu.CheckBoxValue(SpellSlot.Q) && LaneClearMenu.CompareSlider("Qmana", user.ManaPercent))
            {
                var qminions = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(Q.LaneMinions(), Q.Width * 2, (int)Q.Range, Q.CastDelay, Q.Speed);
                if (qminions.HitNumber >= LaneClearMenu.SliderValue("Qhit"))
                {
                    Q.Cast(qminions.CastPosition);
                }
            }

            if (W.IsReady() && LaneClearMenu.CheckBoxValue(SpellSlot.W) && LaneClearMenu.CompareSlider("Wmana", user.ManaPercent))
            {
                var wminions = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(W.LaneMinions(), W.Width * 2, (int)W.Range, W.CastDelay, W.Speed);
                if (wminions.HitNumber + 1 >= LaneClearMenu.SliderValue("Whit"))
                {
                    WCast(wminions.CastPosition);
                }
            }

            if (E.IsReady() && LaneClearMenu.CheckBoxValue(SpellSlot.E) && LaneClearMenu.CompareSlider("Emana", user.ManaPercent))
            {
                foreach (var ball in BallsList)
                {
                    var Eminions = EntityManager.MinionsAndMonsters.GetLineFarmLocation(Eball.LaneMinions(), Eball.Width, (int)Eball.Range, ball.ServerPosition.Extend(user, 100));
                    if (Eminions.HitNumber >= LaneClearMenu.SliderValue("Ehit"))
                    {
                        Eball.Cast(ball.ServerPosition);
                    }
                }
            }
        }

        public override void JungleClear()
        {
            foreach (var mob in Extentions.BigJungleMobs)
            {
                if (Q.IsReady() && mob.IsKillable(Q.Range) && JungleClearMenu.CheckBoxValue(SpellSlot.Q) && JungleClearMenu.CompareSlider("Qmana", user.ManaPercent))
                {
                    Q.Cast(mob);
                    return;
                }

                if (W.IsReady() && mob.IsKillable(W.Range) && JungleClearMenu.CheckBoxValue(SpellSlot.W) && JungleClearMenu.CompareSlider("Wmana", user.ManaPercent))
                {
                    WCast(mob);
                    return;
                }

                if (E.IsReady() && mob.IsKillable(E.Range) && JungleClearMenu.CheckBoxValue(SpellSlot.E) && JungleClearMenu.CompareSlider("Emana", user.ManaPercent))
                {
                    E.Cast(mob);
                    return;
                }
            }
        }

        public override void KillSteal()
        {
            foreach (var target in EntityManager.Heroes.Enemies.OrderByDescending(TargetSelector.GetPriority).Where(e => e.IsKillable()))
            {
                if (Q.IsReady() && E.IsReady() && target.IsKillable(1200) && KillStealMenu.CheckBoxValue("QE") && Eball.WillKill(target))
                {
                    QE(target);
                }

                if (W.IsReady() && W.WillKill(target) && target.IsKillable(W.Range) && KillStealMenu.CheckBoxValue(SpellSlot.W))
                {
                    WCast(target);
                    return;
                }
                if (Q.IsReady() && Q.WillKill(target) && target.IsKillable(Q.Range) && KillStealMenu.CheckBoxValue(SpellSlot.Q))
                {
                    Q.Cast(target, 30);
                    return;
                }
                if (E.IsReady() && E.WillKill(target) && KillStealMenu.CheckBoxValue(SpellSlot.E))
                {
                    if (SelectBall(target) != null)
                    {
                        Eball.Cast(SelectBall(target));
                        return;
                    }
                    if (target.IsKillable(E.Range))
                    {
                        E.Cast(target, 25);
                        return;
                    }
                }
                if (R.IsReady() && target.IsKillable(R.Range) && RDamage(target) >= target.Health && KillStealMenu.CheckBoxValue(SpellSlot.R))
                {
                    R.Cast(target);
                    return;
                }
            }
        }

        public override void Draw()
        {
            foreach (var obj in EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget() && DrawMenu.CheckBoxValue("dmg")))
            {
                float x = obj.HPBarPosition.X;
                float y = obj.HPBarPosition.Y;
                dmg.Color = Color.White;
                if (ComboDamage(obj, true) >= obj.Health)
                {
                    dmg.Color = Color.Red;
                }
                dmg.TextValue = (int)ComboDamage(obj, true) + " / " + (int)obj.Health;
                dmg.Position = new Vector2(x, y);
                dmg.Draw();
            }
            
            if (DrawMenu.CheckBoxValue("balls"))
            {
                foreach (var ball in BallsList.Where(b => b != null && E.IsInRange(b)))
                {
                    Circle.Draw(SharpDX.Color.AliceBlue, ball.BoundingRadius + 25, ball);

                    if (E.IsReady())
                    {
                        var start = ball.ServerPosition.Extend(user.ServerPosition, 100).To3D();
                        var end = user.ServerPosition.Extend(ball.ServerPosition, Eball.Range).To3D();

                        new Geometry.Polygon.Rectangle(start, end, Eball.Width).Draw(Color.AliceBlue);
                    }
                }
            }

            foreach (var spell in SpellList.Where(s => DrawMenu.CheckBoxValue(s.Slot)))
            {
                Circle.Draw(spell.IsReady() ? SharpDX.Color.Chartreuse : SharpDX.Color.OrangeRed, spell.Range, user);
            }
        }

        protected static void QE(Obj_AI_Base target)
        {
            if (Q.IsReady() && E.IsReady() && user.Mana >= Q.Handle.SData.Mana + E.Handle.SData.Mana)
            {
                var castpos = Eball.GetPrediction(target).CastPosition;
                if(Q.Cast(Q.IsInRange(castpos) ? castpos : user.ServerPosition.Extend(castpos, E.Range).To3D()))
                {
                    Eball.Cast(castpos);
                }
            }
        }

        protected static void WCast(Obj_AI_Base target)
        {
            if (W.Handle.ToggleState == 1)
            {
                var pick = EntityManager.MinionsAndMonsters.CombinedAttackable.FirstOrDefault(m => m.IsValidTarget(W.Range) && m.Health > 5) ?? BallsList.FirstOrDefault(b => W.IsInRange(b));
                if (pick != null)
                {
                    W.Cast(pick);
                }
            }
            else
            {
                W.Cast(target);
            }
        }

        protected static void WCast(Vector3 target)
        {
            if (W.Handle.ToggleState == 1)
            {
                var pick = EntityManager.MinionsAndMonsters.CombinedAttackable.FirstOrDefault(m => m.IsValidTarget(W.Range) && m.Health > 5) ?? BallsList.FirstOrDefault(b => W.IsInRange(b));
                if (pick != null)
                {
                    W.Cast(pick);
                }
            }
            else
            {
                W.Cast(target);
            }
        }
    }
}
