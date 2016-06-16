using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace MoonyLeeKicks
{
    static class LeeSinSmite
    {
        static readonly string[] SmiteableUnits =
        {
            "SRU_Red", "SRU_Blue", "SRU_Dragon", "SRU_Baron"
        };

        private static readonly AIHeroClient me = ObjectManager.Player;
        public static void Init()
        {
            Game.OnUpdate += OnSmiteUpdate;
        }

        static int GetSmiteDamage()
        {
            if (SpellManager.Smite == null || !SpellManager.Smite.IsReady())
            {
                return 0;
            }
            int level = me.Level;
            int[] smitedamage =
            {
                20*level + 370,
                30*level + 330,
                40*level + 240,
                50*level + 100
            };
            return smitedamage.Max();
        }

        private static void OnSmiteUpdate(EventArgs args)
        {
            bool enabled = LeeSinMenu.smiteMenu["useSmite"].Cast<KeyBind>().CurrentValue;
            if (!enabled || !SpellManager.SmiteReady)
                return;

            foreach (var mob in EntityManager.MinionsAndMonsters.Monsters.Where(x => x.IsValid && x.Distance(me) <= 
                    SpellManager.Q1.Range && x.Health <= GetSmiteDamage()))
            {
                if (SmiteableUnits.Any(x => mob.BaseSkinName.Contains(x)))
                {
                    bool enable =
                        LeeSinMenu.smiteMenu["useSmite" + SmiteableUnits.FirstOrDefault(x => mob.BaseSkinName.Contains(x))]
                            .Cast<CheckBox>().CurrentValue;
                    if (enable)
                        SpellManager.Smite.Cast(mob);
                }
            }
        }
    }
}
