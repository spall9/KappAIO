using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
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
        private static float LastW;
        private static float LastQE;

        private static Menu UltMenu;

        private static bool IsTryingToQE { get { return Core.GameTickCount - LastQE < 250; } }

        static Syndra()
        {
            Init();
            dmg = new Text(string.Empty, new Font("Tahoma", 9, FontStyle.Bold)) { Color = Color.White };
            Q = new Spell.Skillshot(SpellSlot.Q, 810, SkillShotType.Circular, 600, int.MaxValue, 125) { AllowedCollisionCount = int.MaxValue, DamageType = DamageType.Magical };
            W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 350, 1500, 140) { AllowedCollisionCount = int.MaxValue, DamageType = DamageType.Magical };
            E = new Spell.Skillshot(SpellSlot.E, 680, SkillShotType.Cone, 250, 2500, 50) { AllowedCollisionCount = int.MaxValue, DamageType = DamageType.Magical };
            R = new Spell.Targeted(SpellSlot.R, 680, DamageType.Magical);
            Eball = new Spell.Skillshot(SpellSlot.E, 1100, SkillShotType.Linear, 250, 2400, 40) { AllowedCollisionCount = int.MaxValue, DamageType = DamageType.Magical };

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
            UltMenu = MenuIni.AddSubMenu("R BlackList");

            SpellList.ForEach(
                i =>
                {
                    ComboMenu.CreateCheckBox(i.Slot, "Use " + i.Slot, i.Slot != SpellSlot.E);
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
            AutoMenu.CreateCheckBox("fleeE", "Flee E");
            AutoMenu.CreateKeyBind("QEkey", "QE To Mouse", false, KeyBind.BindTypes.HoldActive);

            ComboMenu.CreateCheckBox("QE", "Use QE");
            ComboMenu.CreateCheckBox("Eball", "Use E on Balls");

            HarassMenu.CreateCheckBox("QE", "Use QE");
            HarassMenu.CreateCheckBox("Eball", "Use E on Balls");
            HarassMenu.CreateKeyBind("auto", "Auto Harass", false, KeyBind.BindTypes.PressToggle);

            KillStealMenu.CreateCheckBox("QE", "QE KillSteal");

            DrawMenu.CreateCheckBox("dmg", "Draw Combo Damage");
            DrawMenu.CreateCheckBox("balls", "Draw Balls");

            UltMenu.AddGroupLabel("Targets To Not Use R On:");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                UltMenu.CreateCheckBox(enemy.Name(), "Dont Ult " + enemy.Name(), false);
            }

            MenuList.Add(HarassMenu);
            MenuList.Add(LaneClearMenu);
            MenuList.Add(JungleClearMenu);

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Orbwalker.OnUnkillableMinion += Orbwalker_OnUnkillableMinion;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (sender == null || !sender.IsEnemy)
                return;

            var caster = sender as AIHeroClient;

            if(caster == null || !caster.IsKillable())
                return;

            if (AutoMenu.CheckBoxValue("QEint") && Q.IsReady() && E.IsReady() && Eball.IsInRange(caster))
            {
                QE(caster);
                return;
            }

            if (AutoMenu.CheckBoxValue("Eint") && E.IsReady())
            {
                if (E.IsInRange(caster))
                    ECast(caster);
                else
                    EBall(caster);
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if(sender == null || !sender.IsEnemy || !sender.IsKillable())
                return;

            if (AutoMenu.CheckBoxValue("QEgap") && Q.IsReady() && E.IsReady() && Eball.IsInRange(sender))
            {
                QE(sender);
                return;
            }

            if (AutoMenu.CheckBoxValue("Egap") && E.IsReady())
            {
                if (E.IsInRange(sender))
                    ECast(sender);
                else
                    EBall(sender);
            }
        }

        private static void Orbwalker_OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if(Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || target == null || !W.IsReady())
                return;

            if (target.IsKillable(W.Range) && W.WillKill(target) && AutoMenu.CheckBoxValue("Wunk"))
            {
                W.Cast(target);
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if(!sender.Owner.IsMe)
                return;

            if (IsTryingToQE)
            {
                if(args.Slot == SpellSlot.Q)
                    E.Cast(args.EndPosition);

                if (args.Slot == SpellSlot.W)
                    args.Process = false;
            }
        }

        public override void Active()
        {
            if (AutoMenu.KeyBindValue("QEkey"))
            {
                QE(Game.CursorPos);
            }

            if (HarassMenu.KeyBindValue("auto"))
            {
                this.Harass();
            }
        }

        public override void Combo()
        {
            var qtarget = Q.GetTarget();
            var wtarget = W.GetTarget();
            var etarget = E.GetTarget();
            var eballtarget = Eball.GetTarget();
            var rtargets = EntityManager.Heroes.Enemies.OrderByDescending(e => e.Health).Where(e => RWillKill(e) && e.IsKillable(R.Range) && !UltMenu.CheckBoxValue(e.Name()));
            var rtarget = TargetSelector.GetTarget(rtargets, DamageType.Magical);

            if (Q.IsReady() && E.IsReady() && eballtarget != null && eballtarget.IsKillable(Eball.Range) && ComboMenu.CheckBoxValue("QE"))
            {
                QE(eballtarget);
            }

            if (Eball.IsReady() && eballtarget != null && ComboMenu.CheckBoxValue("Eball"))
            {
                EBall(eballtarget);
            }

            if (Q.IsReady() && qtarget != null && qtarget.IsKillable() && ComboMenu.CheckBoxValue("Q"))
            {
                Q.Cast(qtarget, 30);
            }

            if (etarget != null && E.IsReady() && wtarget.IsKillable() && ComboMenu.CheckBoxValue("E"))
            {
                ECast(etarget);
            }

            if (wtarget != null && W.IsReady() && ComboMenu.CheckBoxValue("W") && wtarget.IsKillable())
            {
                WCast(wtarget);
            }

            if (R.IsReady() && rtarget != null && ComboMenu.CheckBoxValue("R"))
            {
                RCast(rtarget);
            }
        }

        public override void Flee()
        {
            if (AutoMenu.CheckBoxValue("fleeE") && E.IsReady())
            {
                var etarget = EntityManager.Heroes.Enemies.OrderBy(e => e.Distance(Player.Instance)).FirstOrDefault(e => e.IsKillable());
                if (etarget != null)
                    ECast(etarget);
            }
        }

        public override void Harass()
        {
            var qtarget = Q.GetTarget();
            var wtarget = W.GetTarget();
            var etarget = E.GetTarget();
            var eballtarget = Eball.GetTarget();
            var qmanacheck = Player.Instance.ManaPercent > HarassMenu.SliderValue("Qmana");
            var wmanacheck = Player.Instance.ManaPercent > HarassMenu.SliderValue("Wmana");
            var emanacheck = Player.Instance.ManaPercent > HarassMenu.SliderValue("Emana");

            if (Q.IsReady() && E.IsReady() && qmanacheck && emanacheck && eballtarget != null && eballtarget.IsKillable(Eball.Range) && HarassMenu.CheckBoxValue("QE"))
            {
                QE(eballtarget);
            }

            if (Eball.IsReady() && emanacheck && eballtarget != null && HarassMenu.CheckBoxValue("Eball"))
            {
                EBall(eballtarget);
            }

            if (Q.IsReady() && qtarget != null && qmanacheck && qtarget.IsKillable() && HarassMenu.CheckBoxValue("Q"))
            {
                Q.Cast(qtarget, 30);
            }

            if (etarget != null && E.IsReady() && emanacheck && wtarget.IsKillable() && HarassMenu.CheckBoxValue("E"))
            {
                ECast(etarget);
            }

            if (wtarget != null && W.IsReady() && wmanacheck && HarassMenu.CheckBoxValue("W") && wtarget.IsKillable())
            {
                WCast(wtarget);
            }
        }

        public override void LastHit()
        {
        }

        public override void LaneClear()
        {
            var qmanacheck = Player.Instance.ManaPercent > LaneClearMenu.SliderValue("Qmana");
            var wmanacheck = Player.Instance.ManaPercent > LaneClearMenu.SliderValue("Wmana");
            var emanacheck = Player.Instance.ManaPercent > LaneClearMenu.SliderValue("Emana");
            var qhits = LaneClearMenu.SliderValue("Qhit");
            var whits = LaneClearMenu.SliderValue("Whit");
            var ehits = LaneClearMenu.SliderValue("Ehit");
            var QBestFarmLoc = Q.GetBestCircularCastPosition(Q.LaneMinions());
            var WBestFarmLoc = W.GetBestCircularCastPosition(W.LaneMinions());
            var EBestFarmLoc = E.GetBestConeCastPosition(E.LaneMinions());

            if (qmanacheck && Q.IsReady() && QBestFarmLoc.HitNumber >= qhits)
            {
                Q.Cast(QBestFarmLoc.CastPosition);
            }
            if (wmanacheck && W.IsReady() && (WBestFarmLoc.HitNumber >= whits || W.ToggleState != 1))
            {
                WCast(WBestFarmLoc.CastPosition);
            }
            if (emanacheck && W.IsReady() && EBestFarmLoc.HitNumber >= ehits)
            {
                ECast(EBestFarmLoc.CastPosition);
            }
        }

        public override void JungleClear()
        {
            var qmanacheck = Player.Instance.ManaPercent > JungleClearMenu.SliderValue("Qmana");
            var wmanacheck = Player.Instance.ManaPercent > JungleClearMenu.SliderValue("Wmana");
            var emanacheck = Player.Instance.ManaPercent > JungleClearMenu.SliderValue("Emana");
            var QBestFarmLoc = Q.GetBestCircularCastPosition(Q.JungleMinions());
            var WBestFarmLoc = W.GetBestCircularCastPosition(W.JungleMinions());
            var EBestFarmLoc = E.GetBestConeCastPosition(E.JungleMinions());

            if (qmanacheck && Q.IsReady())
            {
                Q.Cast(QBestFarmLoc.CastPosition);
            }
            if (wmanacheck && W.IsReady())
            {
                WCast(WBestFarmLoc.CastPosition);
            }
            if (emanacheck && E.IsReady())
            {
                ECast(EBestFarmLoc.CastPosition);
            }
        }

        public override void KillSteal()
        {
            var qtarget = Q.GetKillStealTarget();
            var wtarget = W.GetKillStealTarget();
            var etarget = E.GetKillStealTarget();
            var eballtarget = Eball.GetKillStealTarget();
            var rtarget = EntityManager.Heroes.Enemies.OrderBy(TargetSelector.GetPriority).FirstOrDefault(o => o.IsKillable(R.Range) && RWillKill(o));

            if (qtarget != null && Q.IsReady() && KillStealMenu.CheckBoxValue("Q"))
            {
                Q.Cast(qtarget, 30);
                return;
            }
            if (wtarget != null && W.IsReady() && KillStealMenu.CheckBoxValue("W"))
            {
                WCast(wtarget);
                return;
            }
            if (etarget != null && E.IsReady() && KillStealMenu.CheckBoxValue("E"))
            {
                ECast(etarget);
                return;
            }
            if (eballtarget != null && Q.IsReady() && E.IsReady() && KillStealMenu.CheckBoxValue("QE"))
            {
                QE(etarget);
                return;
            }
            if (rtarget != null && R.IsReady() && KillStealMenu.CheckBoxValue("R"))
            {
                RCast(rtarget);
            }
        }

        public override void Draw()
        {
            if (DrawMenu.CheckBoxValue("dmg"))
            {
                foreach (var obj in EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget()))
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
        
        private static bool WCast(Vector3 pos)
        {
            if (Core.GameTickCount - LastW < 250 + Game.Ping || !W.IsReady() || !W.IsInRange(pos))
            {
                return false;
            }

            if (W.ToggleState == 1)
            {
                var wtarget = ObjectManager.Get<Obj_AI_Minion>().OrderBy(m => m.Distance(pos)).FirstOrDefault(m => m.IsValidForW());
                if (wtarget != null && W.Cast(wtarget))
                {
                    LastW = Core.GameTickCount;
                    return true;
                }
            }
            else
            {
                return W.Cast(pos);
            }

            return false;
        }

        private static bool WCast(Obj_AI_Base target)
        {
            if (target == null)
            {
                return false;
            }

            var pred = W.GetPrediction(target);
            return W.IsReady() && pred.HitChancePercent > 30 && target.IsKillable(W.Range) && WCast(pred.CastPosition);
        }

        private static bool ECast(Vector3 pos)
        {
            if (!E.IsReady())
            {
                return false;
            }

            if (Q.IsReady() && ComboMenu.CheckBoxValue("QE") && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                return QE(pos);
            }

            return E.Cast(pos);
        }

        private static bool ECast(Obj_AI_Base target)
        {
            if (!E.IsReady() || target == null || !target.IsKillable(E.Range))
            {
                return false;
            }

            return ECast(E.GetPrediction(target).CastPosition);
        }

        private static void RCast(AIHeroClient target)
        {
            if (R.IsReady() && target.IsKillable(R.Range) && !UltMenu.CheckBoxValue(target.Name()))
            {
                R.Cast(target);
            }
        }

        private static bool QE(Vector3 pos)
        {
            if (!E.IsInRange(pos))
            {
                pos = Player.Instance.ServerPosition.Extend(pos, E.Range - 75).To3D();
            }

            if (Q.IsReady())
            {
                if (E.IsReady() && E.IsInRange(pos))
                {
                    if (Q.Cast(pos))
                    {
                        LastQE = Core.GameTickCount;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool QE(Obj_AI_Base target)
        {
            var qemana = Player.Instance.Mana > Q.ManaCost + E.ManaCost;
            if (qemana && Q.IsReady() && E.IsReady() && target.IsKillable(Eball.Range))
            {
                var pred = Eball.GetPrediction(target);
                return pred.HitChance >= HitChance.Low && QE(pred.CastPosition);
            }
            return false;
        }

        private static bool EBall(Vector3 pos)
        {
            if (!Eball.IsReady() || SelectBall(pos) == null)
            {
                return false;
            }

            return Eball.Cast(SelectBall(pos));
        }

        private static bool EBall(Obj_AI_Base target)
        {
            return target != null && target.IsKillable(Eball.Range) && EBall(Eball.GetPrediction(target).CastPosition);
        }
    }
}
