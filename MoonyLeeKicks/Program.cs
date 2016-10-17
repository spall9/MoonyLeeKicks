using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using MoonyLeeKicks.Extras;
using MoonyLeeKicks.Insec;
using SharpDX;

namespace MoonyLeeKicks
{
    class Program
    {
        private static AIHeroClient me;

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
                    me = ObjectManager.Player;
                    LeeSinMenu.Init();
                    SelectionHandler.InitListening();
                    WardManager.Init();
                    SpellManager.Init();

                    new MultiKick();
                    new LeeSinInsec();
                    new StarCombo();
                    new BubbaKush();
                    new Smite();

                    Game.OnUpdate += LeeSinOnUpdate;
                    Obj_AI_Base.OnPlayAnimation += ObjAiBaseOnOnPlayAnimation;
                }
            };
        }

        private static void ObjAiBaseOnOnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (args.Animation == "Spell1a")
                LastQ1CastTick = Environment.TickCount;

            if (args.Animation == "Spell1b")
            {
                LastQ2Tick = Environment.TickCount;
                //Last Q Enemy still valid if it was before
                if (GetLastQBuffEnemyHero() != null)
                {
                    QbuffEndTime_hero += 3; //sec
                    extentedQ_hero = true;
                }
            }
        }

        private static AIHeroClient lastEnemyWithQBuff_hero;
        private static int LastQ1CastTick;
        static int LastQ2Tick;
        private static float QbuffEndTime_hero;
        private static bool extentedQ_hero;
        static AIHeroClient GetLastQBuffEnemyHero()
        {
            var currentEnemyWithQBuff = ObjectManager.Get<Obj_AI_Base>()
                .FirstOrDefault(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne")) as AIHeroClient;
            if (currentEnemyWithQBuff != null)
            {
                lastEnemyWithQBuff_hero = currentEnemyWithQBuff;
                QbuffEndTime_hero = lastEnemyWithQBuff_hero.GetBuff("BlindMonkQOne").EndTime;
            }

            if (extentedQ_hero && lastEnemyWithQBuff_hero != null &&
                lastEnemyWithQBuff_hero.Distance(ObjectManager.Player) <= 80)
            {
                QbuffEndTime_hero = 0;
            }

            if (lastEnemyWithQBuff_hero != null && Game.Time >= QbuffEndTime_hero)
            {
                lastEnemyWithQBuff_hero = null;
                extentedQ_hero = false;
            }

            return lastEnemyWithQBuff_hero;
        }

        private static void LeeSinOnUpdate(EventArgs args)
        {
            GetLastQBuffEnemyHero(); //update

            if (LeeSinMenu.comboMenu["comboSytleSwitch"].Cast<KeyBind>().CurrentValue)
            {
                int comboMethod = LeeSinMenu.comboMenu["currentComboMethod"].Cast<Slider>().CurrentValue;
                LeeSinMenu.comboMenu["currentComboMethod"].Cast<Slider>().CurrentValue = comboMethod == 0 ? 1 : 0;
                LeeSinMenu.comboMenu["comboSytleSwitch"].Cast<KeyBind>().CurrentValue = false;
            }

            if (SpellManager.R.IsReady() && LeeSinMenu.comboMenu["useRKs_General"].Cast<CheckBox>().CurrentValue)
                foreach (AIHeroClient killableEnemy in EntityManager.Heroes.Enemies.Where(x => x.IsValid && 
                    me.GetSpellDamage(x, SpellSlot.R) > x.Health && x.Distance(me) <= SpellManager.R.Range))
                {
                    SpellManager.R.Cast(killableEnemy);
                }

            switch (Orbwalker.ActiveModesFlags)
            {
                    case Orbwalker.ActiveModes.Combo:
                    int comboMethod = LeeSinMenu.comboMenu["currentComboMethod"].Cast<Slider>().CurrentValue;
                    if (comboMethod == 0)
                        GankCombo();
                    else
                        FightCombo();
                    break;
                    case Orbwalker.ActiveModes.Harass:
                        Harass();
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

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical) ?? TargetSelector.GetTarget(1000, DamageType.Physical);
            if (target != null && target.IsValid)
            {
                var qPred = SpellManager.Q1.GetPrediction(target);

                if (SpellManager.CanCastQ1 && me.Mana >= 50 && qPred.HitChance >= HitChance.High && 
                    LeeSinMenu.harassMenu["useQ"].Cast<CheckBox>().CurrentValue)
                    SpellManager.Q1.Cast(qPred.CastPosition);

                if (SpellManager.CanCastE1 && me.Mana >= 50 && target.Distance(me) <= SpellManager.E1.Range &&
                    LeeSinMenu.harassMenu["useE"].Cast<CheckBox>().CurrentValue)
                    SpellManager.E1.Cast(me.Position);
            }
        }

        private static int lastWFightCombo, FightComboSpellCastTick;
        private static void FightCombo()
        {
            if (Environment.TickCount - FightComboSpellCastTick <= 500)
                return;

            bool useQ = LeeSinMenu.comboMenu["useQFight"].Cast<CheckBox>().CurrentValue;
            bool useW = LeeSinMenu.comboMenu["useWFight"].Cast<CheckBox>().CurrentValue;
            bool useE = LeeSinMenu.comboMenu["useEFight"].Cast<CheckBox>().CurrentValue;
            bool useR = LeeSinMenu.comboMenu["useRFight"].Cast<CheckBox>().CurrentValue;

            var target = TargetSelector.GetTarget(1000, DamageType.Magical) ?? TargetSelector.GetTarget(1000, DamageType.Physical);
            if (target == null || !target.IsValid)
                return;

            var qPred = SpellManager.Q1.GetPrediction(target);

            bool cannotAA = !Orbwalker.CanAutoAttack || Orbwalker.CanBeAborted || target.Distance(me) > me.GetAutoAttackRange();
            bool qProcess = !me.Spellbook.GetSpell(SpellSlot.Q).Name.Contains("One");
            bool wProcess = !me.Spellbook.GetSpell(SpellSlot.W).Name.Contains("One");
            bool eProcess = !me.Spellbook.GetSpell(SpellSlot.E).Name.Contains("One");
            bool anythingProcessed = qProcess || wProcess || eProcess;

            int maxPassiveStacks = me.Level < 6 ? 0 : 1;
            bool wNotReady = !SpellManager.CanCastW1 && !SpellManager.CanCastW2;

            if (!anythingProcessed && cannotAA && useQ && wNotReady && SpellManager.CanCastQ1 &&
                me.Mana >= 50 && qPred.HitChance >= HitChance.High)
            {
                SpellManager.Q1.Cast(qPred.CastPosition);
                FightComboSpellCastTick = Environment.TickCount;
                return;
            }
            if (cannotAA && useQ && SpellManager.CanCastQ2 && PassiveStacks <= maxPassiveStacks)
            {
                SpellManager.Q2.Cast();
                FightComboSpellCastTick = Environment.TickCount;
                return;
            }

            if (!anythingProcessed && cannotAA && useW && SpellManager.CanCastW1 && Environment.TickCount - lastWFightCombo >= 1000 &&
                target.Distance(me) <= me.GetAutoAttackRange())
            {
                SpellManager.W1.Cast(me);
                lastWFightCombo = Environment.TickCount;
                FightComboSpellCastTick = Environment.TickCount + 500;
                return;
            }
            if (cannotAA && useW && SpellManager.CanCastW2 && PassiveStacks <= maxPassiveStacks &&
                target.Distance(me) <= me.GetAutoAttackRange())
            {
                SpellManager.W2.Cast();
                FightComboSpellCastTick = Environment.TickCount;
                return;
            }

            if (!anythingProcessed && cannotAA && useE && SpellManager.CanCastE1 &&
                target.Distance(me) <= SpellManager.E1.Range)
            {
                SpellManager.E1.Cast(me.Position);
                FightComboSpellCastTick = Environment.TickCount;
                return;
            }
            if (cannotAA && useE && SpellManager.CanCastE2 && PassiveStacks <= maxPassiveStacks &&
                target.Distance(me) <= SpellManager.E2.Range)
            {
                SpellManager.E2.Cast(me.Position);
                FightComboSpellCastTick = Environment.TickCount;
                return;
            }

            if (useR && SpellManager.CanCastQ2 && GetLastQBuffEnemyHero().Equals(target) &&
                SpellManager.R.IsReady() && target.Distance(me) <= SpellManager.R.Range
                && PassiveStacks <= maxPassiveStacks && cannotAA)
                SpellManager.R.Cast(target);

            if (useR && me.GetSpellDamage(target, SpellSlot.R) > target.Health && target.Distance(me) <= SpellManager.R.Range)
                SpellManager.R.Cast(target);

            if (cannotAA)
                UseItems(LeeSinMenu.comboMenu["useItemsFight"].Cast<CheckBox>().CurrentValue, target);
        }

        static Obj_AI_Base GetAllyAsWard(Vector2 pos)
        {
            IEnumerable<Obj_AI_Base> allyJumps = 
                ObjectManager.Get<Obj_AI_Base>().Where(x => x.IsValid && x.IsAlly && !x.IsMe && 
                    x.Distance(pos) <= 80 && (x is AIHeroClient || x is Obj_AI_Minion)).ToList();

            Obj_AI_Base obj = allyJumps.Any() ? allyJumps.OrderBy(x => x.Distance(pos)).First() : null;
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
                bool useQ = LeeSinMenu.clearMenu["useQJ"].Cast<CheckBox>().CurrentValue;
                bool useW = LeeSinMenu.clearMenu["useWJ"].Cast<CheckBox>().CurrentValue;
                bool useE = LeeSinMenu.clearMenu["useEJ"].Cast<CheckBox>().CurrentValue;

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

                UseItems(LeeSinMenu.clearMenu["useItemsJ"].Cast<CheckBox>().CurrentValue, targetMinion);
            }
        }

        private static int lastWCastWaveClear;
        private static void WaveClear()
        {
            bool useQ = LeeSinMenu.clearMenu["useQW"].Cast<CheckBox>().CurrentValue;
            bool useW = LeeSinMenu.clearMenu["useWW"].Cast<CheckBox>().CurrentValue;
            bool useE = LeeSinMenu.clearMenu["useEW"].Cast<CheckBox>().CurrentValue;

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

                    if (useW && SpellManager.CanCastW1 && Environment.TickCount - lastWCastWaveClear >= 100 &&
                        targetMinion.Distance(me) <= me.GetAutoAttackRange())
                    {
                        SpellManager.W1.Cast(me);
                        lastWCastWaveClear = Environment.TickCount;
                    }
                    if (useW && SpellManager.CanCastW2 && PassiveStacks <= maxPassiveStacks &&
                        targetMinion.Distance(me) <= me.GetAutoAttackRange())
                        SpellManager.W2.Cast();

                    if (useE && SpellManager.CanCastE1 && 
                        EntityManager.MinionsAndMonsters.EnemyMinions.Count(x => x.IsValid && x.Distance(me) <= 350) >= 2)
                        SpellManager.E1.Cast(me.Position);
                    if (useE && SpellManager.CanCastE2 && PassiveStacks <= maxPassiveStacks)
                        SpellManager.E2.Cast(me.Position);

                    UseItems(LeeSinMenu.clearMenu["useItemsW"].Cast<CheckBox>().CurrentValue, targetMinion);
                }
                catch { }
            }
        }

        private static void Flee()
        {
            bool canWard = WardManager.CanCastWard;
            bool enoughMana = me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
            bool doWardJump = LeeSinMenu.comboMenu["useWardJump"].Cast<CheckBox>().CurrentValue;
            bool maxRange = LeeSinMenu.comboMenu["useWardJumpMaxRange"].Cast<CheckBox>().CurrentValue;

            Vector2 maxRangeJumpPos = me.Position.To2D() +
                            (Game.CursorPos.To2D() - me.Position.To2D()).Normalized() * WardManager.WardRange;
            Vector3 mousePos = me.Distance(Game.CursorPos.To2D()) > WardManager.WardRange ? 
                me.Position.Extend(Game.CursorPos, WardManager.WardRange).To3D() : Game.CursorPos;

            Obj_AI_Base allyobj = GetAllyAsWard(maxRange ? maxRangeJumpPos : mousePos.To2D());
            bool allyobjValid = allyobj != null && allyobj.IsValid;

            if (canWard && enoughMana && doWardJump && !allyobjValid)
            {
                WardManager.CastWardTo(maxRange ? maxRangeJumpPos.To3D() : mousePos);
            }
            else if (enoughMana && doWardJump && allyobjValid)
            {
                SpellManager.W1.Cast(allyobj);
                Core.DelayAction(() => { if (SpellManager.CanCastW2) SpellManager.W2.Cast(); }, 1000);
            }
        }

        private static void GankCombo()
        {
            bool useQ = LeeSinMenu.comboMenu["useQ"].Cast<CheckBox>().CurrentValue;
            bool useW = LeeSinMenu.comboMenu["useWGap"].Cast<CheckBox>().CurrentValue;
            bool useE = LeeSinMenu.comboMenu["useE"].Cast<CheckBox>().CurrentValue;
            bool ksR = LeeSinMenu.comboMenu["useRKs"].Cast<CheckBox>().CurrentValue;
            bool useItems = LeeSinMenu.comboMenu["useItems"].Cast<CheckBox>().CurrentValue;

            var target = TargetSelector.GetTarget(1000, DamageType.Magical) ?? TargetSelector.GetTarget(1000, DamageType.Physical);

            if (target == null || !target.IsValid || target.IsDead)
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

            bool canQFly = ((SpellManager.CanCastQ1 && target.Distance(me) <= 1300) || Environment.TickCount-LastQ1CastTick <= 3000) &&
                LeeSinMenu.comboMenu["noWAtQ1Fly"].Cast<CheckBox>().CurrentValue;
            bool q2 =
                (Environment.TickCount - LastQ2Tick <= 2000 || GetLastQBuffEnemyHero() != null) &&
                LeeSinMenu.comboMenu["noWAtQ2"].Cast<CheckBox>().CurrentValue;
            if (target.Distance(me) > me.GetAutoAttackRange() && useW && !canQFly && !q2)
            {
                //w gap
                bool canWard = WardManager.CanCastWard;
                bool canW = SpellManager.W1.IsReady() && me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana;

                var allyobj = ObjectManager.Get<Obj_AI_Base>().
                    Where(x => x != null && x.IsAlly && !x.IsMe && x.IsValid && (x is Obj_AI_Minion || x is AIHeroClient))
                    .OrderBy(x => x.Distance(me))
                    .FirstOrDefault(x => x.Distance(target) <= me.GetAutoAttackRange());
                var allyobjValid = allyobj != null && allyobj.IsValid;

                if (allyobjValid && canW)
                    SpellManager.W1.Cast(allyobj);
                else if (!allyobjValid && canWard && canW)
                    WardManager.CastWardTo(target.Position);
            }

            if (ksR && SpellManager.R.IsReady() && me.Distance(target) <= SpellManager.R.Range &&
                me.GetSpellDamage(target, SpellSlot.R) > target.Health)
                SpellManager.R.Cast(target);

            UseItems(useItems, target);
        }

        private static void UseItems(bool useItems, Obj_AI_Base target)
        {
            bool hasHydra = Item.HasItem(ItemId.Ravenous_Hydra);
            bool hasTiamat = Item.HasItem(ItemId.Tiamat);
            if (useItems && (hasHydra || hasTiamat) && Orbwalker.CanMove && target.Distance(me) <= 400)
            {
                Item.UseItem(hasTiamat ? ItemId.Tiamat : ItemId.Ravenous_Hydra);
            }
        }
    }
}
