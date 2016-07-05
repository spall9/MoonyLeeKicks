using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace MoonyLeeKicks
{
    internal static class ChampionDashes
    {
        static readonly List<DashInfo> DashInfos = new List<DashInfo>();
        public static bool HasAntiInsecDashReady(this AIHeroClient h)
        {
            var dashInfo = DashInfos.FirstOrDefault(x => x.ChampionName == h.ChampionName);
            if (dashInfo == null)
                return false;

            bool isLeBlanc = h.ChampionName.Contains("LeBlanc");
            bool spellReady = h.Spellbook.GetSpell(dashInfo.slot).CooldownExpires - Game.Time <= 0 && 
                h.Mana >= h.Spellbook.GetSpell(dashInfo.slot).SData.Mana;
            //Distortion

            if (!isLeBlanc)
                return spellReady;
            else
            {
                bool rReady = h.Spellbook.GetSpell(SpellSlot.R).CooldownExpires - Game.Time <= 0 &&
                    h.Mana >= h.Spellbook.GetSpell(SpellSlot.R).SData.Mana;
                bool wAsR = h.Spellbook.GetSpell(SpellSlot.R).SData.Name.ToLower().Contains("distortion");

                if (rReady && wAsR)
                    return true;

                return spellReady;
            }
        }

        public static Vector2 CalculateWardPositionAfterDash(this AIHeroClient target, Vector2 SelectedAllyPos,
            float normalDistance)
        {
            var dashInfo = DashInfos.FirstOrDefault(x => x.ChampionName == target.ChampionName);
            if (dashInfo == null)
                return Vector2.Zero;

            return dashInfo.CalculateNewWardPosition(SelectedAllyPos, normalDistance);
        }

        public static void Init()
        {
            DashInfos.Add(new DashInfo(SpellSlot.Q, "Aatrox", 650));
            DashInfos.Add(new DashInfo(SpellSlot.R, "Ahri", 450));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Caitlyn", 400));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Corki", 600));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Ekko", 325));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Ezreal", 475));
            DashInfos.Add(new DashInfo(SpellSlot.Q, "Fiora", 400));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Fizz", 400));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Gragas", 600));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Graves", 425));
            DashInfos.Add(new DashInfo(SpellSlot.E, "JarvanIV", 830));
            DashInfos.Add(new DashInfo(SpellSlot.Q, "Kalista", 300));
            DashInfos.Add(new DashInfo(SpellSlot.E, "KhaZix", 700));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Kindred", 340));
            DashInfos.Add(new DashInfo(SpellSlot.W, "LeBlanc", 600));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Lucian", 425));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Quinn", 525));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Riven", 325));
            DashInfos.Add(new DashInfo(SpellSlot.Q, "Sejuani", 650));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Shen", 600));
            DashInfos.Add(new DashInfo(SpellSlot.W, "Tristana", 900));
            DashInfos.Add(new DashInfo(SpellSlot.E, "Tryndamere", 660));
            DashInfos.Add(new DashInfo(SpellSlot.Q, "Vayne", 300));
            DashInfos.Add(new DashInfo(SpellSlot.W, "Zed", 700));
        }
    }
    internal class DashInfo
    {
        public DashInfo(SpellSlot _slot, string _ChampName, float _DashDist)
        {
            slot = _slot;
            ChampionName = _ChampName;
            DashDistance = _DashDist;
        }
        public SpellSlot slot;
        public string ChampionName;

        private float _DashDistance;
        public float DashDistance
        {
            get
            {
                if (ChampionName == "KhaZix" &&
                    EntityManager.Heroes.Enemies.First(x => x.IsEnemy && x.ChampionName == "KhaZix")
                        .Spellbook.GetSpell(SpellSlot.E)
                        .SData.Name.ToLower()
                        .Contains("evolved"))
                {
                    return 900;
                }

                return _DashDistance;
            }
            set { _DashDistance = value; }
        }

        /// <summary>
        /// Take Dash in count
        /// </summary>
        public Vector2 CalculateNewWardPosition(Vector2 SelectedAllyPos, float normalDistance)
        {
            var me = ObjectManager.Player;
            var target = EntityManager.Heroes.Enemies.FirstOrDefault(x => x.ChampionName == ChampionName);

            Vector2 meTarget = target.Position.To2D() - me.Position.To2D();
            float meTargetDist = me.Distance(target);
            Vector2 dashEndPosOP = me.Position.To2D() + meTarget.Normalized()*(meTargetDist + DashDistance);

            Vector2 allyDashEndPos = dashEndPosOP - SelectedAllyPos;
            float allyDashEndPos_Distance = SelectedAllyPos.Distance(dashEndPosOP);
            Vector2 wardPos = SelectedAllyPos +
                              allyDashEndPos.Normalized()*(allyDashEndPos_Distance + normalDistance);
            return wardPos;
        }
    }
}
