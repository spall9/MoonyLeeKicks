using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

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
            SetSmite();

            if (me.GetSpellSlotFromName("summonerexhaust") != SpellSlot.Unknown)
                Exhaust = new Spell.Targeted(me.GetSpellSlotFromName("summonerexhaust"), 600);

            Flash = new Spell.Skillshot(me.GetSpellSlotFromName("summonerflash"), 425, SkillShotType.Linear);
        }

        private static void SetSmite()
        {
            int[] SmiteRed = { 3715, 1415, 1414, 1413, 1412 };
            int[] SmiteBlue = { 3706, 1403, 1402, 1401, 1400 };

            SpellSlot smiteSlot;
            if (SmiteBlue.Any(x => ObjectManager.Player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                smiteSlot = ObjectManager.Player.GetSpellSlotFromName("s5_summonersmiteplayerganker");
            else if (SmiteRed.Any(x => ObjectManager.Player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                smiteSlot = ObjectManager.Player.GetSpellSlotFromName("s5_summonersmiteduel");
            else
                smiteSlot = ObjectManager.Player.GetSpellSlotFromName("summonersmite");
            Smite = new Spell.Targeted(smiteSlot, 500);
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
