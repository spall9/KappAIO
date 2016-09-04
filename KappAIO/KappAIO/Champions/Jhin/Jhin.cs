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
using static KappAIO.Champions.Jhin.JhinStuff;
using Color = System.Drawing.Color;

namespace KappAIO.Champions.Jhin
{
    internal class Jhin : Base
    {
        internal static int CurrentRShot;
        private static bool IsCastingR;
        private static Vector3 LastRPosition;
        private static bool RTap;

        public static Spell.Targeted Q { get; }
        public static Spell.Skillshot W { get; }
        public static Spell.Skillshot E { get; }
        public static Spell.Skillshot R { get; }

        static Jhin()
        {
            Q = new Spell.Targeted(SpellSlot.Q, 600) { DamageType = DamageType.Physical };
            W = new Spell.Skillshot(SpellSlot.W, 2500, SkillShotType.Linear, 750, 5000, 40) {AllowedCollisionCount = -1, DamageType = DamageType.Physical };
            E = new Spell.Skillshot(SpellSlot.E, 750, SkillShotType.Circular, 250, 1600, 300) { DamageType = DamageType.Magical };
            R = new Spell.Skillshot(SpellSlot.R, 3500, SkillShotType.Linear, 200, 4500, 80) { AllowedCollisionCount = -1, DamageType = DamageType.Physical };
            SpellList.Add(Q);
            SpellList.Add(E);
            SpellList.Add(W);
            //SpellList.Add(R);

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
                    HarassMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                    HarassMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                    HarassMenu.AddSeparator(0);
                    LaneClearMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                    LaneClearMenu.CreateSlider(i.Slot + "hit", i.Slot + " Hit {0} Minions", 3, 1, 20);
                    LaneClearMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                    LaneClearMenu.AddSeparator(0);
                    JungleClearMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                    JungleClearMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                    JungleClearMenu.AddSeparator(0);
                    KillStealMenu.CreateCheckBox(i.Slot, i.Slot + " KillSteal");
                    DrawMenu.CreateCheckBox(i.Slot, "Draw " + i.Slot);
                });

            AutoMenu.CreateCheckBox("Qunk", "Q UnKillable Minions");
            AutoMenu.CreateCheckBox("AutoW", "Auto W Targets With Buff");
            AutoMenu.CreateCheckBox("WGap", "W Gap Closers");
            AutoMenu.AddGroupLabel("R Settings");
            AutoMenu.Add("Rmode", new ComboBox("R Mode", 0, "Auto R", "On Tap R"));
            AutoMenu.CreateCheckBox("R", "Use R");
            AutoMenu.CreateCheckBox("RKS", "R Kill Steal");
            AutoMenu.CreateCheckBox("Rmouse", "Focus Targets Near Mouse", false);
            AutoMenu.CreateCheckBox("Commands", "Block All Commands While Casting R", false);
            AutoMenu.CreateSlider("RHit", "R HitChance {0}%", 45);
            AutoMenu.CreateSlider("MouseRange", "Focus Near Mouse Radius {0}", 700, 150, 1250);
            AutoMenu.CreateKeyBind("RTap", "R Tap Key", false, KeyBind.BindTypes.HoldActive, 'S').OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args) { RTap = args.NewValue; };

            ComboMenu.CreateCheckBox("WAA", "W If Target is Out Of AA Range");
            ComboMenu.CreateCheckBox("WBUFF", "W Snare Targets Only");
            
            DrawMenu.CreateCheckBox("RSector", "Draw R Sector", false);
            DrawMenu.CreateCheckBox("Notifications", "Enable Notifications");

            Player.OnIssueOrder += Player_OnIssueOrder;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalker.OnUnkillableMinion += Orbwalker_OnUnkillableMinion;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && args.Slot == SpellSlot.E)
            {
                if (ObjectManager.Get<Obj_AI_Minion>().Any(o => !o.IsDead && o.IsValid && o.BaseSkinName.Equals("JhinTrap") && o.IsAlly && o.Distance(args.EndPosition) <= 400))
                {
                    args.Process = false;
                }
            }
        }

        private static void Orbwalker_OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (target.IsKillable(Q.Range) && Q.IsReady() && Q.WillKill(target) && AutoMenu.CheckBoxValue("Qunk") && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Q.Cast(target);
            }
        }

        private static void Player_OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (sender.IsMe && IsCastingR && AutoMenu.CheckBoxValue("Commands"))
            {
                args.Process = false;
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if(sender == null || !sender.IsKillable(W.Range) || !W.IsReady() || !sender.HasJhinEBuff()) return;
            if (e.End.IsInRange(user, 600) && AutoMenu.CheckBoxValue("WGap"))
            {
                W.Cast(sender);
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.Equals(FirstR))
                {
                    IsCastingR = true;
                    LastRPosition = args.End;
                }
                if (args.SData.Name.Equals("JhinRShot"))
                {
                    RTap = false;
                    CurrentRShot++;
                }
            }
        }

        public override void Active()
        {
            Orbwalker.DisableMovement = IsCastingR;
            Orbwalker.DisableAttacking = IsCastingR;

            if (!user.Spellbook.IsChanneling && !user.Spellbook.IsCharging && !user.Spellbook.IsCastingSpell)
            {
                IsCastingR = false;
                CurrentRShot = 0;
            }

            if (IsCastingR && AutoMenu.CheckBoxValue("R") && LastRPosition != null)
            {
                var target = AutoMenu.CheckBoxValue("Rmouse")
                                 ? EntityManager.Heroes.Enemies.OrderBy(h => h.Distance(Game.CursorPos))
                                       .FirstOrDefault(e => e != null && e.IsKillable(R.Range) && e.IsInRange(Game.CursorPos, AutoMenu.SliderValue("MouseRange")) && JhinRSector(LastRPosition).IsInside(e))
                                 : EntityManager.Heroes.Enemies.OrderBy(t => t.TotalShieldHealth() / TotalRDamage(t))
                                       .FirstOrDefault(e => e != null && e.IsKillable(R.Range) && JhinRSector(LastRPosition).IsInside(e));

                if (target != null && target.IsKillable(R.Range))
                {
                    if (AutoMenu.ComboBoxValue("Rmode") == 0)
                    {
                        R.Cast(target, AutoMenu.SliderValue("RHit"));
                    }
                    else
                    {
                        if (RTap)
                        {
                            R.Cast(target, AutoMenu.SliderValue("RHit"));
                        }
                    }
                }
            }

            if(IsCastingR) return;

            if (AutoMenu.CheckBoxValue("AutoW") && W.IsReady())
            {
                foreach (var target in EntityManager.Heroes.Enemies.Where(e => e.IsKillable(W.Range) && e.HasJhinEBuff()))
                {
                    W.Cast(target, 45);
                }
            }
        }

        public override void Combo()
        {
            if (IsCastingR || Orbwalker.IsAutoAttacking) return;

            var qtarget = Q.GetTarget();
            var wtarget = W.GetTarget();
            var etarget = E.GetTarget();

            if (qtarget != null && ComboMenu.CheckBoxValue(SpellSlot.Q))
            {
                if (Q.IsReady() && qtarget.IsKillable(Q.Range))
                {
                    Q.Cast(qtarget);
                }
            }
            if (etarget != null && ComboMenu.CheckBoxValue(SpellSlot.E))
            {
                if (E.IsReady() && etarget.IsKillable(E.Range) && etarget.IsCC())
                {
                    E.Cast(etarget, HitChance.High);
                }
            }

            if (wtarget != null && ComboMenu.CheckBoxValue(SpellSlot.W) && wtarget.IsKillable(W.Range))
            {
                var useW = ((ComboMenu.CheckBoxValue("WBUFF") && wtarget.HasJhinEBuff()) || !ComboMenu.CheckBoxValue("WBUFF"))
                           && ((!user.IsInAutoAttackRange(wtarget) && ComboMenu.CheckBoxValue("WAA")) || !ComboMenu.CheckBoxValue("WAA"));
                if (useW)
                {
                    W.Cast(wtarget, HitChance.Low);
                }
            }
        }

        public override void Flee()
        {
        }

        public override void Harass()
        {
            if (IsCastingR || Orbwalker.IsAutoAttacking) return;

            var qtarget = Q.GetTarget();
            var wtarget = W.GetTarget();
            var etarget = E.GetTarget();

            if (qtarget != null && HarassMenu.CheckBoxValue(SpellSlot.Q) && HarassMenu.CompareSlider("Qmana", user.ManaPercent))
            {
                if (Q.IsReady() && qtarget.IsKillable(Q.Range))
                {
                    Q.Cast(qtarget);
                }
            }
            if (etarget != null && HarassMenu.CheckBoxValue(SpellSlot.E) && HarassMenu.CompareSlider("Emana", user.ManaPercent))
            {
                if (E.IsReady() && etarget.IsKillable(E.Range) && etarget.IsCC())
                {
                    E.Cast(etarget, HitChance.High);
                }
            }
            if (wtarget != null && HarassMenu.CheckBoxValue(SpellSlot.W) && HarassMenu.CompareSlider("Wmana", user.ManaPercent) && wtarget.IsKillable(W.Range))
            {
                W.Cast(wtarget, HitChance.Low);
            }
        }

        public override void LastHit()
        {
            if (IsCastingR) return;
        }

        public override void LaneClear()
        {
            if (IsCastingR || Orbwalker.IsAutoAttacking) return;

            if (W.IsReady() && LaneClearMenu.CheckBoxValue(SpellSlot.W) && LaneClearMenu.CompareSlider("Wmana", user.ManaPercent))
            {
                var minions = W.LaneMinions();
                var farmloc = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, W.Width, (int)W.Range);
                if (farmloc.HitNumber >= LaneClearMenu.SliderValue("Whit"))
                    W.Cast(farmloc.CastPosition);
            }

            if (Q.IsReady() && LaneClearMenu.CheckBoxValue(SpellSlot.Q) && LaneClearMenu.CompareSlider("Qmana", user.ManaPercent))
            {
                var qminion = Q.LaneMinions().OrderByDescending(m => m.CountEnemyMinionsInRange(450)).FirstOrDefault(m => m.CountEnemyMinionsInRange(450) >= LaneClearMenu.SliderValue("Qhit"));
                if (qminion != null)
                {
                    Q.Cast(qminion);
                }
            }

            if (E.IsReady() && LaneClearMenu.CheckBoxValue(SpellSlot.E) && LaneClearMenu.CompareSlider("Emana", user.ManaPercent))
            {
                var minions = E.LaneMinions();
                var farmloc = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minions, E.Width, (int)E.Range);
                if (farmloc.HitNumber >= LaneClearMenu.SliderValue("Ehit"))
                    E.Cast(farmloc.CastPosition);
            }
        }

        public override void JungleClear()
        {
            if (IsCastingR || Orbwalker.IsAutoAttacking) return;

            var jgtarget = Extentions.BigJungleMobs.FirstOrDefault(m => m.IsKillable(Q.Range + 75));
            if (jgtarget != null)
            {
                if (W.IsReady() && JungleClearMenu.CheckBoxValue(SpellSlot.W) && JungleClearMenu.CompareSlider("Wmana", user.ManaPercent))
                {
                    W.Cast(jgtarget);
                    return;
                }

                if (Q.IsReady() && JungleClearMenu.CheckBoxValue(SpellSlot.Q) && JungleClearMenu.CompareSlider("Qmana", user.ManaPercent))
                {
                    Q.Cast(jgtarget);
                    return;
                }

                if (E.IsReady() && JungleClearMenu.CheckBoxValue(SpellSlot.E) && JungleClearMenu.CompareSlider("Emana", user.ManaPercent))
                {
                    E.Cast(jgtarget);
                }
            }
        }

        public override void KillSteal()
        {
            foreach (var target in EntityManager.Heroes.Enemies.Where(t => t != null))
            {
                if (IsCastingR && R.IsReady() && AutoMenu.CheckBoxValue("RKS"))
                {
                    if (target.IsKillable(R.Range) && CurrentRDamage(target) >= target.TotalShieldHealth() && JhinRSector(LastRPosition).IsInside(target))
                    {
                        R.Cast(target, AutoMenu.SliderValue("RHit"));
                        return;
                    }
                }
                
                if (IsCastingR) return;

                if (W.IsReady() && KillStealMenu.CheckBoxValue(SpellSlot.W) && W.WillKill(target))
                {
                    W.Cast(target, 50);
                    return;
                }

                if (Q.IsReady() && KillStealMenu.CheckBoxValue(SpellSlot.Q) && Q.WillKill(target))
                {
                    Q.Cast(target);
                    return;
                }
            }
        }

        public override void Draw()
        {
            foreach (var spell in SpellList.Where(s => DrawMenu.CheckBoxValue(s.Slot)))
            {
                Circle.Draw(spell.IsReady() ? SharpDX.Color.Chartreuse : SharpDX.Color.OrangeRed, spell.Range, user);
            }

            if (IsCastingR && LastRPosition != null)
            {
                if (AutoMenu.CheckBoxValue("Rmouse"))
                {
                    Circle.Draw(SharpDX.Color.Goldenrod, AutoMenu.SliderValue("MouseRange"), Game.CursorPos);
                }
                if(DrawMenu.CheckBoxValue("RSector"))
                    JhinRSector(LastRPosition).Draw(Color.AliceBlue, 2);
            }
            
            if (DrawMenu.CheckBoxValue("Notifications") && R.IsReady())
            {
                var i = 0f;
                foreach (var t in EntityManager.Heroes.Enemies.Where(e => e.IsKillable()))
                {
                    if (t != null && t.IsKillable())
                    {
                        var totalRDamage = TotalRDamage(t);

                        if (totalRDamage >= t.TotalShieldHealth())
                        {
                            i += 0.02f;
                            Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * (0.4f + i), Color.YellowGreen, (int)(t.TotalShieldHealth() / (totalRDamage / 4)) + " x Ult can kill: " + t.ChampionName + " have: " + (int)t.TotalShieldHealth() + "HP");
                            Extentions.DrawLine(t.Position, user.Position, 6, Color.Yellow);
                        }
                    }
                }
            }
        }
    }
}
