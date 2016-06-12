using System;
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

                    config = MainMenu.AddMenu("MoonyLeeSin", "__MoonyLeeSin");
                    config.AddLabel("WardJump in FleeMode");
                    config.AddSeparator(10);
                    config.Add("moonyLee_useQ", new CheckBox("Use Q Combo"));
                    config.AddSeparator();
                    config.Add("moonyLee_useQWC", new CheckBox("Use Q WaveClear"));
                    config.Add("moonyLee_useWWC", new CheckBox("Use W WaveClear"));
                    config.Add("moonyLee_useEWC", new CheckBox("Use E WaveClear"));
                    config.AddSeparator();
                    config.Add("moonyLee_useQJC", new CheckBox("Use Q JungleClear"));
                    config.Add("moonyLee_useWJC", new CheckBox("Use W JungleClear"));
                    config.Add("moonyLee_useEJC", new CheckBox("Use E JungleClear"));
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

        private static int lastWCastJungleClear;
        private static void JungleClear()
        {
            var targetMinion = 
                EntityManager.MinionsAndMonsters.Monsters.Where(x => x.Distance(me) <= 500 && x.IsValid).
                    OrderByDescending(x => x.Health).FirstOrDefault();

            if (targetMinion != null)
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

            if (targetMinion != null)
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
                                (Game.CursorPos.To2D() - me.Position.To2D()).Normalized()*WardManager.WardRange;

            var allyobj =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(x => x.IsValid && (x is AIHeroClient || x is Obj_AI_Minion))
                    .OrderBy(x => x.Distance(jumpPos))
                    .FirstOrDefault();

            if (allyobj != null)
            {
                if (allyobj.Distance(jumpPos) <= 80)
                {
                    SpellManager.W1.Cast(allyobj);
                    Core.DelayAction(() => SpellManager.W2.Cast(), 1000);
                }
            }
            else if (WardManager.CanCastWard && me.Mana >= me.Spellbook.GetSpell(SpellManager.W1.Slot).SData.Mana)
            {
                WardManager.CastWardTo(jumpPos.To3D());
            }
        }

        private static void Combo()
        {
            bool useQ = config["moonyLee_useQ"].Cast<CheckBox>().CurrentValue;

            var target = TargetSelector.SelectedTarget ?? TargetSelector.GetTarget(1000, DamageType.Magical) ??
                         TargetSelector.GetTarget(1000, DamageType.Physical);

            if (useQ && target != null)
            {
                var qPred = SpellManager.Q1.GetPrediction(target);
                if (qPred.HitChance >= HitChance.High)
                    SpellManager.Q1.Cast(qPred.CastPosition);
            }
        }
    }
}
