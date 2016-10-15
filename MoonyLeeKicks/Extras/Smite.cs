using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu.Values;
using MoonyLeeKicks.Insec;

namespace MoonyLeeKicks.Extras
{
    class Smite
    {
        private AIHeroClient me;

        IEnumerable<string> LargeMonsters = new []
        {
            "SRU_Blue", "SRU_Red"
        };

        IEnumerable<string> EpicMonsters = new[]
        {
            "TT_Spiderboss", "SRU_Baron", "SRU_RiftHerald",
            "SRU_Dragon_Elder", "SRU_Dragon_Air", "SRU_Dragon_Earth",
            "SRU_Dragon_Fire", "SRU_Dragon_Water"
        };

        int GetSmiteDamage()
        {
            int[] CalcSmiteDamage =
            {
                20 * ObjectManager.Player.Level + 370,
                30 * ObjectManager.Player.Level + 330,
                40 * ObjectManager.Player.Level + 240,
                50 * ObjectManager.Player.Level + 100
            };

            return CalcSmiteDamage.Max();
        }

        float GetQDamage(Obj_AI_Base monster)
        { 
            if (SpellManager.CanCastQ1 && me.Mana >= 80)
                return me.GetSpellDamage(monster, SpellSlot.Q)*2;

            if (SpellManager.CanCastQ1 && me.Mana >= 50)
                return me.GetSpellDamage(monster, SpellSlot.Q);

            if (SpellManager.CanCastQ2 && me.Mana >= 30)
                return me.GetSpellDamage(monster, SpellSlot.Q);

            return 0;
        }

        float GetSmiteDamageOnHero()
        {
            if (SpellManager.Smite.Slot == me.GetSpellSlotFromName("s5_summonersmiteduel"))
            {
                return SpellManager.SmiteReady ? 54 + 6 * me.Level : 0;
            }

            if (SpellManager.Smite.Slot == me.GetSpellSlotFromName("s5_summonersmiteplayerganker"))
            {
                return SpellManager.SmiteReady ? 20 + 8 * me.Level : 0;
            }

            return 0;
        }

        public Smite()
        {
            me = ObjectManager.Player;
            Game.OnUpdate += GameOnOnUpdate;
        }

        private void GameOnOnUpdate(EventArgs args)
        {
            if (!LeeSinMenu.smiteMenu["smiteToggleKey"].Cast<KeyBind>().CurrentValue || !SpellManager.SmiteReady)
                return;

            bool smiteLarge = LeeSinMenu.smiteMenu["useSmiteLargeChamps"].Cast<CheckBox>().CurrentValue;
            bool smiteEpic = LeeSinMenu.smiteMenu["useForEpicCamps"].Cast<CheckBox>().CurrentValue;
            float maxDist = (SpellManager.CanCastQ1 && me.Mana >= 80) || 
                (SpellManager.CanCastQ2 && me.Mana >= 30) ? SpellManager.Q1.Range : SpellManager.Smite.Range;

            List<string> targetMonsters = new List<string>();
            if (smiteLarge) targetMonsters.AddRange(LargeMonsters);
            if (smiteEpic) targetMonsters.AddRange(EpicMonsters);

            foreach (Obj_AI_Minion smiteable in 
                EntityManager.MinionsAndMonsters.GetJungleMonsters().
                Where(x => targetMonsters.Any(tm => x.Name.Contains(tm)) && !x.Name.Contains("Mini") && x.IsValid && !x.IsDead && 
                    x.Health <= GetSmiteDamage() + GetQDamage(x) && x.Distance(me.Position) <= maxDist).
                    OrderBy(x => EpicMonsters.Contains(x.Name)))
            {
                if (smiteable.Health <= GetSmiteDamage() && smiteable.Distance(me) <= SpellManager.Smite.Range)
                    SpellManager.Smite.Cast(smiteable);
                else if (SpellManager.CanCastQ1 && SpellManager.Q1.GetPrediction(smiteable).CollisionObjects.Length == 0)
                    SpellManager.Q1.Cast(smiteable.ServerPosition);
                else if (SpellManager.CanCastQ2)
                    SpellManager.Q2.Cast();
            }

            bool comboSmite = Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo &&
                              LeeSinMenu.smiteMenu["useSmiteQCombo"].Cast<CheckBox>().CurrentValue;
            bool insecSmite = LeeSinMenu.insecMenu["_insecKey"].Cast<KeyBind>().CurrentValue &&
                              LeeSinMenu.smiteMenu["useSmiteQInsec"].Cast<CheckBox>().CurrentValue &&
                              SelectionHandler.LastTargetValid;
            if ((comboSmite || insecSmite) && SpellManager.CanCastQ1 && me.Mana >= 50)
            {
                var target = comboSmite
                    ? (TargetSelector.GetTarget(1000, DamageType.Magical) ??
                       TargetSelector.GetTarget(1000, DamageType.Physical))
                    : SelectionHandler.GetTarget;

                var pred = SpellManager.Q1.GetPrediction(target);
                List<Obj_AI_Base> collisions =
                    pred.CollisionObjects.Where(x => !(x is AIHeroClient)).ToList();

                if (collisions.Count == 1 &&
                    collisions[0].Distance(me) <= SpellManager.Smite.Range &&
                    collisions[0].Health <= GetSmiteDamage()
                    && target.IsValid)
                {
                    SpellManager.Q1.Cast(pred.CastPosition);
                    Core.RepeatAction(() => SpellManager.Smite.Cast(pred.CollisionObjects[0]),
                        Math.Max(50, SpellManager.Q1.CastDelay - SpellManager.Smite.CastDelay), 1500);
                }
            }

            if (LeeSinMenu.smiteMenu["useSmiteKs"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var smiteableHero in 
                    EntityManager.Heroes.Enemies.Where(x => x.IsValid && !x.IsDead && x.Distance(me) <= SpellManager.Smite.Range 
                        && x.Health < GetSmiteDamageOnHero()).OrderBy(x => x.Health))
                {
                    SpellManager.Smite.Cast(smiteableHero);
                }
            }
        }
    }
}
