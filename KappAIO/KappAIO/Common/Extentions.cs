using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace KappAIO.Common
{
    internal static class Extentions
    {
        /// <summary>
        ///     Returns true if the Item IsReady.
        /// </summary>
        public static bool ItemReady(this Item item, Menu menu)
        {
            return item != null && item.IsOwned(Player.Instance) && item.IsReady() && menu.CheckBoxValue(item.Id.ToString());
        }

        /// <summary>
        ///     Returns if no orbwalker modes are active
        /// </summary>
        public static bool NoModesActive
        {
            get
            {
                return !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)
                       && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear)
                       && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee);
            }
        }

        /// <summary>
        ///     Supported Jungle Mobs.
        /// </summary>
        public static string[] Junglemobs = new []
        {
            "SRU_Dragon_Air", "SRU_Dragon_Earth", "SRU_Dragon_Fire", "SRU_Dragon_Water",
            "SRU_Dragon_Elder", "SRU_Baron", "SRU_Gromp", "SRU_Krug", "SRU_Razorbeak",
            "Sru_Crab", "SRU_Murkwolf", "SRU_Blue", "SRU_Red", "SRU_RiftHerald",
            "TT_NWraith", "TT_NWolf", "TT_NGolem", "TT_Spiderboss", "AscXerath"
        };

        /// <summary>
        ///     Returns Supported Jungle Mobs.
        /// </summary>
        public static IEnumerable<Obj_AI_Minion> SupportedJungleMobs
        {
            get
            {
                return EntityManager.MinionsAndMonsters.GetJungleMonsters().Where(m => Junglemobs.Any(j => j.Equals(m.BaseSkinName)));
            }
        }

        /// <summary>
        ///     Returns true if target Is CC'D.
        /// </summary>
        public static bool IsCC(this Obj_AI_Base target)
        {
            return !target.CanMove || target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Fear)
                   || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Suppression) || target.HasBuffOfType(BuffType.Taunt)
                   || target.HasBuffOfType(BuffType.Sleep);
        }

        /// <summary>
        ///     Returns true if you can deal damage to the target.
        /// </summary>
        public static bool IsKillable(this Obj_AI_Base target, float range)
        {
            return !target.HasBuff("kindredrnodeathbuff") && !target.Buffs.Any(b => b.Name.ToLower().Contains("fioraw")) && !target.HasBuff("JudicatorIntervention") && !target.IsZombie
                   && !target.HasBuff("ChronoShift") && !target.HasBuff("UndyingRage") && !target.IsInvulnerable && !target.IsZombie && !target.HasBuff("bansheesveil") && !target.IsDead
                   && !target.IsPhysicalImmune && target.Health > 0 && !target.HasBuffOfType(BuffType.Invulnerability) && !target.HasBuffOfType(BuffType.PhysicalImmunity) && target.IsValidTarget(range);
        }

        /// <summary>
        ///     Returns true if you can deal damage to the target.
        /// </summary>
        public static bool IsKillable(this Obj_AI_Base target)
        {
            return !target.HasBuff("kindredrnodeathbuff") && !target.Buffs.Any(b => b.Name.ToLower().Contains("fioraw")) && !target.HasBuff("JudicatorIntervention") && !target.IsZombie
                   && !target.HasBuff("ChronoShift") && !target.HasBuff("UndyingRage") && !target.IsInvulnerable && !target.IsZombie && !target.HasBuff("bansheesveil") && !target.IsDead
                   && !target.IsPhysicalImmune && target.Health > 0 && !target.HasBuffOfType(BuffType.Invulnerability) && !target.HasBuffOfType(BuffType.PhysicalImmunity) && target.IsValidTarget();
        }

        /// <summary>
        ///     Returns true if you can deal damage to the target (AIHeroClient).
        /// </summary>
        public static bool IsKillable(this AIHeroClient target)
        {
            return !target.HasBuff("kindredrnodeathbuff") && !target.Buffs.Any(b => b.Name.ToLower().Contains("fioraw")) && !target.HasBuff("JudicatorIntervention") && !target.IsZombie
                   && !target.HasBuff("ChronoShift") && !target.HasBuff("UndyingRage") && !target.HasUndyingBuff() && !target.IsInvulnerable && !target.IsZombie && !target.HasBuff("bansheesveil") && !target.IsDead
                   && !target.IsPhysicalImmune && target.Health > 0 && !target.HasBuffOfType(BuffType.Invulnerability) && !target.HasBuffOfType(BuffType.PhysicalImmunity) && target.IsValidTarget();
        }

        /// <summary>
        ///     Casts spell with selected hitchance.
        /// </summary>
        public static void Cast(this Spell.Skillshot spell, Obj_AI_Base target, HitChance hitChance)
        {
            if (target != null && spell.IsReady() && target.IsKillable(spell.Range))
            {
                var pred = spell.GetPrediction(target);
                if (pred.HitChance >= hitChance || target.IsCC())
                {
                    spell.Cast(pred.CastPosition);
                }
            }
        }

        /// <summary>
        ///     Creates a checkbox.
        /// </summary>
        public static CheckBox CreateCheckBox(this Menu m, string id, string name, bool defaultvalue = true)
        {
            return m.Add(id, new CheckBox(name, defaultvalue));
        }

        /// <summary>
        ///     Creates a checkbox.
        /// </summary>
        public static CheckBox CreateCheckBox(this Menu m, SpellSlot slot, string name, bool defaultvalue = true)
        {
            return m.Add(slot.ToString(), new CheckBox(name, defaultvalue));
        }

        /// <summary>
        ///     Creates a slider.
        /// </summary>
        public static Slider CreateSlider(this Menu m, string id, string name, int defaultvalue = 0, int MinValue = 0, int MaxValue = 100)
        {
            return m.Add(id, new Slider(name, defaultvalue, MinValue, MaxValue));
        }

        /// <summary>
        ///     Returns CheckBox Value.
        /// </summary>
        public static bool CheckBoxValue(this Menu m, string id)
        {
            return m[id].Cast<CheckBox>().CurrentValue;
        }

        /// <summary>
        ///     Returns CheckBox Value.
        /// </summary>
        public static bool CheckBoxValue(this Menu m, SpellSlot slot)
        {
            return m[slot.ToString()].Cast<CheckBox>().CurrentValue;
        }

        /// <summary>
        ///     Returns Slider Value.
        /// </summary>
        public static int SliderValue(this Menu m, string id)
        {
            return m[id].Cast<Slider>().CurrentValue;
        }

        /// <summary>
        ///     Returns true if the value is >= the slider.
        /// </summary>
        public static bool CompareSlider(this Menu m, string id, float value)
        {
            return value >= m[id].Cast<Slider>().CurrentValue;
        }

        /// <summary>
        ///     Returns true if the spell will kill the target.
        /// </summary>
        public static bool WillKill(this Spell.SpellBase spell, Obj_AI_Base target, float MultiplyDmgBy = 1)
        {
            return Player.Instance.GetSpellDamage(target, spell.Slot) * MultiplyDmgBy >= Prediction.Health.GetPrediction(target, (int)(spell.CastDelay + (Player.Instance.Distance(target) / spell.Handle.SData.MissileSpeed) * 1000));
        }

        /// <summary>
        ///     Returns true if the target is big minion (Siege / Super Minion).
        /// </summary>
        public static bool IsBigMinion(this Obj_AI_Base target)
        {
            return target.BaseSkinName.ToLower().Contains("siege") || target.BaseSkinName.ToLower().Contains("super");
        }

        /// <summary>
        ///     Returns true if the target is big minion (Siege / Super Minion).
        /// </summary>
        public static Vector3 PrediectPosition(this Obj_AI_Base target, int Time)
        {
            return Prediction.Position.PredictUnitPosition(target, Time).To3D();
        }
    }
}
