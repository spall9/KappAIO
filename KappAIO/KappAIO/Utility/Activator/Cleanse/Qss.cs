using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Spells;
using KappAIO.Common;
using static KappAIO.Utility.Activator.Database;

namespace KappAIO.Utility.Activator.Cleanse
{
    internal class Qss
    {
        private static readonly List<Item> SelfQss = new List<Item> { Quicksilver_Sash, Mercurial_Scimitar };

        private static readonly List<Item> AllyQss = new List<Item> { Mikaels };

        private static readonly List<BuffType> BuffsToQss = new List<BuffType>
        {
            BuffType.Blind, BuffType.Charm, BuffType.Fear, BuffType.Flee, BuffType.Knockback, BuffType.Knockup, BuffType.NearSight,
            BuffType.Poison, BuffType.Polymorph, BuffType.Sleep, BuffType.Slow, BuffType.Snare, BuffType.Silence, BuffType.Stun,
            BuffType.Suppression, BuffType.Taunt
        };

        private class SaveBuffs
        {
            public readonly AIHeroClient Owner;
            public readonly BuffType buff;

            public SaveBuffs(AIHeroClient owner, BuffType type)
            {
                this.Owner = owner;
                this.buff = type;
            }
        }

        private static readonly List<SaveBuffs> SavedBuffs = new List<SaveBuffs>();

        private static Menu Clean;

        public static void Init()
        {
            try
            {
                Clean = Load.MenuIni.AddSubMenu("Qss");
                Clean.CreateCheckBox("ally", "Qss Allies");
                Clean.AddSeparator(0);
                Clean.AddGroupLabel("Items");
                Clean.CreateCheckBox("Cleanse", "Use Summoner Cleanse");
                SelfQss.ForEach(i => Clean.CreateCheckBox(i.Id.ToString(), "Use " + i.ItemInfo.Name));
                AllyQss.ForEach(i => Clean.CreateCheckBox(i.Id.ToString(), "Use " + i.ItemInfo.Name));
                Clean.AddSeparator(0);
                Clean.AddGroupLabel("Buffs To Qss");
                BuffsToQss.ForEach(b => Clean.CreateCheckBox(b.ToString(), "Use On " + b));

                Game.OnTick += Game_OnTick;
                Obj_AI_Base.OnBuffGain += Obj_AI_Base_OnBuffGain;
                Obj_AI_Base.OnBuffLose += Obj_AI_Base_OnBuffLose;
            }
            catch (Exception ex)
            {
                Logger.Send("Activator Qss Error While Init", ex, Logger.LogLevel.Error);
            }
        }

        private static void Game_OnTick(EventArgs args)
        {
            try
            {
                if (Player.Instance.IsDead)
                {
                    SavedBuffs.RemoveAll(b => b.Owner.IsMe);
                    return;
                }

                foreach (var saved in SavedBuffs.Where(a => a.Owner != null && Clean.CheckBoxValue(a.buff.ToString()) && a.Owner.IsKillable()))
                {
                    CastQss(saved.Owner);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Send("Activator Qss Error At Game_OnTick", ex, Logger.LogLevel.Error);
            }
        }

        private static void CastQss(Obj_AI_Base target)
        {
            try
            {
                foreach (var i in SelfQss.Where(a => a.ItemReady(Clean)))
                {
                    if (target.IsMe)
                    {
                        i.Cast();
                        return;
                    }
                }
                foreach (var i in AllyQss.Where(a => a.ItemReady(Clean)))
                {
                    if (target.IsMe || (target.IsAlly && !target.IsMe && Clean.CheckBoxValue("ally")))
                    {
                        i.Cast(target);
                        return;
                    }
                }

                if (target.IsMe && SummonerSpells.Cleanse.IsReady() && Clean.CheckBoxValue("Cleanse"))
                {
                    SummonerSpells.Cleanse.Cast();
                }
            }
            catch (Exception ex)
            {
                Logger.Send("Activator Qss Error At CastQss", ex, Logger.LogLevel.Error);
            }
        }

        private static void Obj_AI_Base_OnBuffLose(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            try
            {
                var caster = sender as AIHeroClient;
                if (caster == null || !caster.IsAlly || !BuffsToQss.Contains(args.Buff.Type))
                    return;
                SavedBuffs.Remove(new SaveBuffs(caster, args.Buff.Type));
            }
            catch (Exception ex)
            {
                Logger.Send("Activator Qss Error At Obj_AI_Base_OnBuffLose", ex, Logger.LogLevel.Error);
            }
        }

        private static void Obj_AI_Base_OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            try
            {
                var caster = sender as AIHeroClient;
                if (caster == null || !caster.IsAlly || !BuffsToQss.Contains(args.Buff.Type))
                    return;
                SavedBuffs.Add(new SaveBuffs(caster, args.Buff.Type));
            }
            catch (Exception ex)
            {
                Logger.Send("Activator Qss Error At Obj_AI_Base_OnBuffGain", ex, Logger.LogLevel.Error);
            }
        }
    }
}
