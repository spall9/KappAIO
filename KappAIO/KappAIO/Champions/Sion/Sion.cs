using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Rendering;
using KappAIO.Common;
using SharpDX;
using static KappAIO.Champions.Sion.Stuff;
using Color = System.Drawing.Color;

namespace KappAIO.Champions.Sion
{
    internal class Sion : Base
    {
        private static Vector3 RStartPos;
        private static Vector3 REndPos;
        private static Vector3 RDirection;
        
        internal static bool IsCastingR { get { return Player.Instance.HasBuff("SionR"); } }
        private static bool IsChargingQ;
        private static float LastQTick;
        internal static Vector3 LastQPos;
        
        public static Spell.Skillshot Q { get; }
        public static Spell.Active W { get; }
        public static Spell.Skillshot E { get; }
        public static Spell.Skillshot R { get; }

        static Sion()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 740, SkillShotType.Linear, 250, int.MaxValue, 200, DamageType.Physical) {AllowedCollisionCount = int.MaxValue};
            W = new Spell.Active(SpellSlot.W, 500);
            E = new Spell.Skillshot(SpellSlot.E, 750, SkillShotType.Linear, 250, 1000, 70) { AllowedCollisionCount = -1 };
            R = new Spell.Skillshot(SpellSlot.R, 850, SkillShotType.Linear, 250, 950, 300) { AllowedCollisionCount = -1 };
            SpellList.Add(Q);
            SpellList.Add(E);
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
                        HarassMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                        HarassMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                        HarassMenu.AddSeparator(0);
                        LaneClearMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                        LaneClearMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                        LaneClearMenu.AddSeparator(0);
                        JungleClearMenu.CreateCheckBox(i.Slot, "Use " + i.Slot);
                        JungleClearMenu.CreateSlider(i.Slot + "mana", i.Slot + " Mana Manager {0}%", 60);
                        JungleClearMenu.AddSeparator(0);
                        KillStealMenu.CreateCheckBox(i.Slot, i.Slot + " KillSteal");
                    }
                    DrawMenu.CreateCheckBox(i.Slot, "Draw " + i.Slot);
                });

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnStopCast += Spellbook_OnStopCast;
        }

        private static void Spellbook_OnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            if (sender.IsMe)
                IsChargingQ = false;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if(!sender.IsMe || sender.IsZombie)
                return;

            if (args.Slot == SpellSlot.Q)
            {
                if (!IsChargingQ)
                {
                    LastQPos = args.End;
                    LastQTick = Core.GameTickCount;
                }
                IsChargingQ = !IsChargingQ;
            }

            if (args.Slot == SpellSlot.R)
            {
                RStartPos = args.Start;
                REndPos = args.End;
            }
        }

        public override void Active()
        {
            if (IsCastingR)
            {
                var test = RStartPos.Extend(user.ServerPosition, RStartPos.Distance(user) + R.Range);
                //var direction = user.ServerPosition - (RStartPos - REndPos);
                RDirection = user.ServerPosition.Extend(test, R.Range).To3D();
            }

            if (Core.GameTickCount - LastQTick > 3500 && IsChargingQ)
            {
                IsChargingQ = false;
            }
        }

        public override void Combo()
        {
            var QTarget = Q.GetTarget();
            var ETarget = E.GetTarget();
            var EextendedTarget = TargetSelector.GetTarget(EntityManager.Heroes.Enemies.Where(e => e.IsKillable(MaxERange)), DamageType.Magical);
            var RTarget = EntityManager.Heroes.Enemies.OrderBy(e => e.Distance(Player.Instance)).FirstOrDefault(e => e.IsKillable(R.Range) && R.WillKill(e));
            
            if (E.IsReady() && ComboMenu.CheckBoxValue("E"))
            {
                if (ETarget != null && ETarget.IsKillable())
                {
                    E.Cast(ETarget, 30);
                }
                else
                {
                    if (EextendedTarget != null)
                    {
                        var cannontarget = ExtendETarget(EextendedTarget);
                        if (cannontarget != null)
                            E.Cast(cannontarget);
                    }
                }
                return;
            }
            
            if (RTarget != null && R.IsReady())
            {
                R.Cast(RTarget);
                if (IsCastingR)
                {
                    Player.Instance.Spellbook.UpdateChargeableSpell(SpellSlot.R, RTarget.ServerPosition, false);
                    Player.ForceIssueOrder(GameObjectOrder.MoveTo, RTarget.ServerPosition, false);
                    Player.IssueOrder(GameObjectOrder.MoveTo, RTarget.ServerPosition, false);
                }
                return;
            }

            if (Q.IsReady() && ComboMenu.CheckBoxValue("Q"))
            {
                QCast(QTarget);
            }
        }

        public override void Flee()
        {
        }

        public override void Harass()
        {
            var QTarget = Q.GetTarget();
            var ETarget = E.GetTarget();
            var EextendedTarget = TargetSelector.GetTarget(EntityManager.Heroes.Enemies.Where(e => e.IsKillable(MaxERange)), DamageType.Magical);

            if (E.IsReady() && HarassMenu.CheckBoxValue("E") && Player.Instance.ManaPercent > HarassMenu.SliderValue("Emana"))
            {
                if (ETarget != null && ETarget.IsKillable())
                {
                    E.Cast(ETarget, 30);
                }
                else
                {
                    if (EextendedTarget != null)
                    {
                        var cannontarget = ExtendETarget(EextendedTarget);
                        if (cannontarget != null)
                            E.Cast(cannontarget);
                    }
                }
                return;
            }
            if (Q.IsReady() && HarassMenu.CheckBoxValue("Q") && Player.Instance.ManaPercent > HarassMenu.SliderValue("Qmana"))
            {
                QCast(QTarget);
            }
        }

        public override void LastHit()
        {
        }

        public override void LaneClear()
        {
            var qfarm = Q.GetBestLinearCastPosition(Q.LaneMinions());
            var efarm = E.GetBestLinearCastPosition(E.LaneMinions());

            if (qfarm.HitNumber >= LaneClearMenu.SliderValue("Qhit") && (!IsChargingQ && Player.Instance.ManaPercent > LaneClearMenu.SliderValue("Qmana") || IsChargingQ))
            {
                QCast(qfarm.CastPosition, qfarm.HitNumber);
            }

            if (efarm.HitNumber > LaneClearMenu.SliderValue("Ehit") && E.IsReady() && Player.Instance.ManaPercent > LaneClearMenu.SliderValue("Emana"))
            {
                E.Cast(efarm.CastPosition);
            }
        }

        public override void JungleClear()
        {
            var qfarm = Q.GetBestLinearCastPosition(Q.JungleMinions());
            var efarm = E.GetBestLinearCastPosition(E.JungleMinions());

            if (qfarm.HitNumber >= 1 && (!IsChargingQ && Player.Instance.ManaPercent > JungleClearMenu.SliderValue("Qmana") || IsChargingQ))
            {
                QCast(qfarm.CastPosition, qfarm.HitNumber);
            }

            if (efarm.HitNumber >= 1 && E.IsReady() && Player.Instance.ManaPercent > JungleClearMenu.SliderValue("Emana"))
            {
                E.Cast(efarm.CastPosition);
            }
        }

        public override void KillSteal()
        {
            var ETarget = E.GetKillStealTarget();
            var EextendedTarget = TargetSelector.GetTarget(EntityManager.Heroes.Enemies.Where(e => e.IsKillable(MaxERange) && E.WillKill(e)), DamageType.Magical);

            if (E.IsReady() && KillStealMenu.CheckBoxValue("E"))
            {
                if (ETarget != null && ETarget.IsKillable())
                {
                    E.Cast(ETarget, 30);
                }
                else
                {
                    if (EextendedTarget != null)
                    {
                        var cannontarget = ExtendETarget(EextendedTarget);
                        if (cannontarget != null)
                            E.Cast(cannontarget);
                    }
                }
                return;
            }
        }

        public override void Draw()
        {
            foreach (var spell in SpellList.Where(s => DrawMenu.CheckBoxValue(s.Slot)))
            {
                Circle.Draw(spell.IsReady() ? SharpDX.Color.Chartreuse : SharpDX.Color.OrangeRed, spell.Range, user);
            }

            //EndPos(Game.CursorPos).DrawCircle(100, SharpDX.Color.AliceBlue);
            if (LastQPos != null && IsChargingQ)
                QRectangle(LastQPos).Draw(Color.AliceBlue, 2);

            /*if(IsCastingR)
                new Geometry.Polygon.Rectangle(user.ServerPosition, RDirection, R.Width).Draw(Color.AliceBlue, 2);*/
        }

        private static void QCast(AIHeroClient target)
        {
            if(!Q.IsReady())
                return;

            if (target != null && !IsChargingQ && target.IsKillable(Q.Range * 0.9f))
            {
                Q.Cast(target);
            }

            if(LastQPos == null || LastQPos == Vector3.Zero)
                return;

            var QRect = QRectangle(LastQPos);
            var QTarget = EntityManager.Heroes.Enemies.FirstOrDefault(e => e.IsKillable(Q.Range) && QRect.IsInside(e.PrediectPosition(250 + Game.Ping)));

            if (IsChargingQ)
            {
                if (QTarget == null)
                {
                    CancelQ();
                }
            }
        }

        private static void QCast(Vector3 pos, int StartHits)
        {
            if (!Q.IsReady())
                return;

            if (!IsChargingQ)
            {
                Q.Cast(pos);
            }

            if (LastQPos == null || LastQPos == Vector3.Zero)
                return;

            var QRect = QRectangle(LastQPos);
            var QHits = EntityManager.Enemies.Count(e => e.IsKillable() && QRect.IsInside(e.PrediectPosition(250 + Game.Ping)));

            if (IsChargingQ)
            {
                if (QHits < StartHits)
                {
                    CancelQ();
                }
            }
        }

        private static void CancelQ()
        {
            Player.Instance.Spellbook.UpdateChargeableSpell(SpellSlot.Q, Game.CursorPos, true);
        }

        private static Obj_AI_Base ExtendETarget(Obj_AI_Base target)
        {
            return EntityManager.MinionsAndMonsters.CombinedAttackable.FirstOrDefault(e => e.IsKillable() && ERectangle(e).IsInside(target));
        }
    }
}
