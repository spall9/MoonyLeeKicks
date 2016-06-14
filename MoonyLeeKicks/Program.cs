﻿using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace MoonyLeeKicks
{
    class Program
    {
        private static Menu config;
        private static AIHeroClient me = ObjectManager.Player;

        public static string PassiveName = "blindmonkpassive_cosmetic";
        public static int PassiveStacks
        {
            get
            {
                return ObjectManager.Player.HasBuff(PassiveName) ? ObjectManager.Player.GetBuff(PassiveName).Count : 0;
            }
        }

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += eventArgs =>
            {
                //config = MainMenu.AddMenu("MoonyLeeSin", "MoonyLeeSin");
                if (ObjectManager.Player.ChampionName == "LeeSin")
                {
                    WardManager.Init();
                    SpellManager.Init();
                    ChampionDashes.Init();

                    config = MainMenu.AddMenu("MoonyLeeSin", "__MoonyLeeSin");
                    config.Add("moonyLee_useQ", new CheckBox("Use Q Combo"));
                    config.Add("moonyLee_useW", new CheckBox("Use W to GapClose"));
                    config.Add("moonyLee_useE", new CheckBox("Use E Combo"));
                    config.AddSeparator();
                    config.Add("moonyLee_useQWC", new CheckBox("Use Q WaveClear"));
                    config.Add("moonyLee_useWWC", new CheckBox("Use W WaveClear"));
                    config.Add("moonyLee_useEWC", new CheckBox("Use E WaveClear"));
                    config.AddSeparator();
                    config.Add("moonyLee_useQJC", new CheckBox("Use Q JungleClear"));
                    config.Add("moonyLee_useWJC", new CheckBox("Use W JungleClear"));
                    config.Add("moonyLee_useEJC", new CheckBox("Use E JungleClear"));
                    config.AddSeparator();
                    config.Add("moonyLee_useWardJump", new CheckBox("Wardjump in Flee Mode"));
                    new LeeSinInsec(ref config);

                    Game.OnUpdate += LeeSinOnUpdate;
                }
            };
        }

        private static void LeeSinOnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveModesFlags)
            {
                    case Orbwalker.ActiveModes.Combo:
                    Combo();
                    break;
                    case Orbwalker.ActiveModes.Flee:
                    Flee();
                    break;
                    case Orbwalker.ActiveModes.LaneClear:
                    WaveClear();
                    break;
                    case Orbwalker.ActiveModes.JungleClear:
                    JungleClear();
                    break;
            }
        }

        static Obj_AI_Base GetAllyAsWard(Vector2 wardPlacePos)
        {
            List<Obj_AI_Base> allyJumps = new List<Obj_AI_Base>();

            foreach (var allyobj in ObjectManager.Get<Obj_AI_Base>().Where(x =>
                            x.IsValid && x.IsAlly && !x.IsMe && (x is AIHeroClient || x is Obj_AI_Minion)))
            {
                if (allyobj.Distance(wardPlacePos) <= 80)
                {
                    allyJumps.Add(allyobj);
                }
            }
            Obj_AI_Base obj = allyJumps.Any() ? allyJumps.OrderBy(x => x.Distance(wardPlacePos)).First() : null;
            return obj;
        }

        private static int lastWCastJungleClear;
        private static void JungleClear()
        {
            var targetMinion = 
                EntityManager.MinionsAndMonsters.Monsters.Where(x => x.Distance(me) <= 500 && x.IsValid).
                    OrderByDescending(x => x.Health).FirstOrDefault();

            if (targetMinion != null && targetMinion.IsValid)
            {
                bool useQ = config["moonyLee_useQJC"].Cast<CheckBox>().CurrentValue;
                bool useW = config["moonyLee_useWJC"].Cast<CheckBox>().CurrentValue;
                bool useE = config["moonyLee_useEJC"].Cast<CheckBox>().CurrentValue;

                int maxPassiveStacks = me.Level < 6 ? 0 : 1;
                maxPassiveStacks = me.Level > 15 ? 2 : maxPassiveStacks;
                bool wNotReady = !SpellManager.CanCastW1 && !SpellManager.CanCastW2;
                bool qNotReady = !SpellManager.CanCastQ1 && !SpellManager.CanCastQ2;

                if (useQ && wNotReady && SpellManager.CanCastQ1 &&
                    me.Mana >= me.Spellbook.GetSpell(SpellManager.Q1.Slot).SData.Mana)
                    SpellManager.Q1.Cast(targetMinion.Position);
                if (useQ && SpellManager.CanCastQ2 && PassiveStacks <= maxPassiveStacks)
                    SpellManager.Q2.Cast();

                if (useW && SpellManager.CanCastW1 && Environment.TickCount - lastWCastJungleClear >= 100)
                {
                    SpellManager.W1.Cast(me);
                    lastWCastJungleClear = Environment.TickCount;
                }
                if (useW && SpellManager.CanCastW2 && PassiveStacks <= maxPassiveStacks)
                    SpellManager.W2.Cast();

                if (useE && SpellManager.CanCastE1 && qNotReady &&
                    targetMinion.Distance(me) <= 350)
                    SpellManager.E1.Cast(me.Position);
                if (useE && SpellManager.CanCastE2 && PassiveStacks <= maxPassiveStacks)
                    SpellManager.E2.Cast(me.Position);
            }
        }

        private static int lastWCastWaveClear;
        private static void WaveClear()
        {
            bool useQ = config["moonyLee_useQWC"].Cast<CheckBox>().CurrentValue;
            bool useW = config["moonyLee_useQWC"].Cast<CheckBox>().CurrentValue;
            bool useE = config["moonyLee_useQWC"].Cast<CheckBox>().CurrentValue;

            var targetMinion =
                EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.Distance(me) <= 500 && x.IsValid).OrderByDescending(x => x.Health).FirstOrDefault();

            if (targetMinion != null && targetMinion.IsValid)
            {
                try
                {
                    int maxPassiveStacks = me.Level < 6 ? 0 : 1;
                    maxPassiveStacks = me.Level > 15 ? 2 : maxPassiveStacks;
                    bool wNotReady = !SpellManager.CanCastW1 && !SpellManager.CanCastW2;

                    if (useQ && wNotReady && SpellManager.CanCastQ1 &&
                        me.Mana >= me.Spellbook.GetSpell(SpellManager.Q1.Slot).SData.Mana)
                        SpellManager.Q1.Cast(targetMinion.Position);
                    if (useQ && SpellManager.CanCastQ2 && PassiveStacks <= maxPassiveStacks)
                        SpellManager.Q2.Cast();

                    if (useW && SpellManager.CanCastW1 && Environment.TickCount - lastWCastWaveClear >= 100)
                    {
                        SpellManager.W1.Cast(me);
                        lastWCastWaveClear = Environment.TickCount;
                    }
                    if (useW && SpellManager.CanCastW2 && PassiveStacks <= maxPassiveStacks)
                        SpellManager.W2.Cast();

                    if (useE && SpellManager.CanCastE1 && 
                        EntityManager.MinionsAndMonsters.EnemyMinions.Count(x => x.IsValid && x.Distance(me) <= 350) >= 2)
                        SpellManager.E1.Cast(me.Position);
                    if (useE && SpellManager.CanCastE2 && PassiveStacks <= maxPassiveStacks)
                        SpellManager.E2.Cast(me.Position);
                }
                catch { }
            }
        }

        private static void Flee()
        {
            Vector2 jumpPos = me.Position.To2D() +
                            (Game.CursorPos.To2D() - me.Position.To2D()).Normalized() * WardManager.WardRange;

            Obj_AI_Base allyobj = GetAllyAsWard(jumpPos);
            bool canWard = WardManager.CanCastWard;
            bool enoughMana = me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
            bool doWardJump = config["moonyLee_useWardJump"].Cast<CheckBox>().CurrentValue;

            if (canWard && enoughMana && doWardJump && allyobj == null)
            {
                WardManager.CastWardTo(jumpPos.To3D());
            }
            else if (enoughMana && doWardJump && allyobj != null && allyobj.IsValid)
            {
                SpellManager.W1.Cast(allyobj);
                Core.DelayAction(() => { if (SpellManager.CanCastW2) SpellManager.W2.Cast(); }, 1000);
            }
        }

        private static void Combo()
        {
            bool useQ = config["moonyLee_useQ"].Cast<CheckBox>().CurrentValue;
            bool useW = config["moonyLee_useW"].Cast<CheckBox>().CurrentValue;
            bool useE = config["moonyLee_useE"].Cast<CheckBox>().CurrentValue;


            var target = TargetSelector.SelectedTarget ?? TargetSelector.GetTarget(1000, DamageType.Magical) ??
                         TargetSelector.GetTarget(1000, DamageType.Physical);

            if (target == null || !target.IsValid)
                return;

            if (useQ && SpellManager.CanCastQ1 && Orbwalker.CanMove)
            {
                var qPred = SpellManager.Q1.GetPrediction(target);
                if (qPred.HitChance >= HitChance.High)
                    SpellManager.Q1.Cast(qPred.CastPosition);
            }
            if (useQ && SpellManager.CanCastQ2 && Orbwalker.CanMove)
                SpellManager.Q2.Cast();

            if (useE && target.Distance(me) <= SpellManager.E1.Range && SpellManager.CanCastE1 && Orbwalker.CanMove)
                SpellManager.E1.Cast(me.Position);

            if (SpellManager.CanCastE2 && Orbwalker.CanMove)
                SpellManager.E2.Cast(me.Position);

            if (target.Distance(me) > me.GetAutoAttackRange() && useW)
            {
                //w gap
                bool canWard = WardManager.CanCastWard;
                bool canW = SpellManager.W1.IsReady() && me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana;

                var allyobj = ObjectManager.Get<Obj_AI_Base>().
                    Where(x => x != null && x.IsAlly && !x.IsMe && x.IsValid && (x is Obj_AI_Minion || x is AIHeroClient))
                    .OrderBy(x => x.Distance(me))
                    .FirstOrDefault(x => x.Distance(target) <= me.GetAutoAttackRange());

                if (allyobj != null && canW)
                    SpellManager.W1.Cast(allyobj);
                else if (allyobj == null && canWard && canW)
                    WardManager.CastWardTo(target.Position);
            }
        }
    }
}
