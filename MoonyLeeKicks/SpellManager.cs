using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;

namespace MoonyLeeKicks
{
    class SpellManager
    {
        public static Spell.Skillshot Q1, E1, E2;
        public static Spell.Targeted R, W1;
        public static Spell.Active Q2, W2;
        public static Spell.Targeted Smite, Exhaust;
        public static Spell.Skillshot Flash;

        private static readonly AIHeroClient me = ObjectManager.Player;

        public static void Init()
        {
            Q1 = new Spell.Skillshot(SpellSlot.Q, 1100, SkillShotType.Linear, 250, 1800, 60)
            {
                AllowedCollisionCount = 0
            };
            Q2 = new Spell.Active(SpellSlot.Q, 1300);

            W1 = new Spell.Targeted(SpellSlot.W, 1200);
            W2 = new Spell.Active(SpellSlot.W, 700);

            E1 = new Spell.Skillshot(SpellSlot.E, 350, SkillShotType.Linear, 250, int.MaxValue, 100)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E2 = new Spell.Skillshot(SpellSlot.E, 675, SkillShotType.Linear, 250, int.MaxValue, 100)
            {
                AllowedCollisionCount = int.MaxValue
            };

            R = new Spell.Targeted(SpellSlot.R, 375);

            if (me.GetSpellSlotFromName("smite") != SpellSlot.Unknown)
                Smite = new Spell.Targeted(me.GetSpellSlotFromName("smite"), 500);
            if (me.GetSpellSlotFromName("summonerexhaust") != SpellSlot.Unknown)
                Exhaust = new Spell.Targeted(me.GetSpellSlotFromName("summonerexhaust"), 600);

            Flash = new Spell.Skillshot(me.GetSpellSlotFromName("summonerflash"), 425, SkillShotType.Linear);
        }

        public static void CastW(Vector3 pos)
        {
            W1.Cast(pos);
            Core.DelayAction(() => W2.Cast(), 1000);
        }

        public static bool CanCastW1
        {
            get
            {
                return me.Spellbook.GetSpell(SpellSlot.W).IsReady &&
                       me.Spellbook.GetSpell(SpellSlot.W).Name.Contains("One");
            }
        }
        public static bool CanCastW2
        {
            get
            {
                return me.Spellbook.GetSpell(SpellSlot.W).IsReady &&
                       me.Spellbook.GetSpell(SpellSlot.W).Name.Contains("Two");
            }
        }
        public static bool CanCastQ1
        {
            get
            {
                return me.Spellbook.GetSpell(SpellSlot.Q).IsReady &&
                       me.Spellbook.GetSpell(SpellSlot.Q).Name.Contains("One");
            }
        }
        public static bool CanCastQ2
        {
            get
            {
                return me.Spellbook.GetSpell(SpellSlot.Q).IsReady &&
                       !me.Spellbook.GetSpell(SpellSlot.Q).Name.Contains("One");
            }
        }
        public static bool CanCastE1
        {
            get
            {
                return me.Spellbook.GetSpell(SpellSlot.E).IsReady &&
                       me.Spellbook.GetSpell(SpellSlot.E).Name.Contains("One");
            }
        }
        public static bool CanCastE2
        {
            get
            {
                return me.Spellbook.GetSpell(SpellSlot.E).IsReady &&
                       !me.Spellbook.GetSpell(SpellSlot.E).Name.Contains("One");
            }
        }
        public static bool SmiteReady
        {
            get
            {
                return Smite != null && Smite.IsReady();
            }
        }

        public static bool ExhaustReady
        {
            get
            {
                return Exhaust != null && Exhaust.IsReady();
            }
        }

        public static bool FlashReady
        {
            get
            {
                return Flash != null && Flash.IsReady();
            }
        }
    }
}
