using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace MoonyLeeKicks.Insec
{
    /// <summary>
    /// Init after ChampionDashes.cs
    /// </summary>
    static class DashAnalysis
    {
        public class DashAnalysisContainer
        {
            public AIHeroClient Hero;
            public string InfoText => $"{Hero.ChampionName} : " + 
                (DashProbability.ToString().Contains("NaN") ? "Unknown" : DashProbability + " %");
            public string MenuId => Hero.ChampionName + ".DashAnalysisId";

            public bool NotEnoughData => DashProbability == float.NaN;
            /// <summary>
            /// amount of Q2's used on the hero
            /// </summary>
            public int Q2Count { get; set; }

            public int DashCount { get; set; }

            public DashInfo dashInfo
            {
                get { return ChampionDashes.DashInfos.FirstOrDefault(x => x.ChampionName == Hero.ChampionName); }
            }

            public DashAnalysisContainer(AIHeroClient hero)
            {
                Hero = hero;
            }

            /// <summary>
            /// Probability of the hero to dash
            /// </summary>
            public float DashProbability => Q2Count > 0 ? DashCount/Q2Count*100 : float.NaN;
        }

        public static List<DashAnalysisContainer> enemies = new List<DashAnalysisContainer>(); 
        public static void Init()
        {
            enemies = EntityManager.Heroes.Enemies.
                Where(x => ChampionDashes.DashInfos.Any(dashInf => dashInf.ChampionName == x.ChampionName)).
                    Select(x => new DashAnalysisContainer(x)).ToList();


            LeeSinMenu.DashAnalysisMenu.AddGroupLabel("Probability Of The Players To Dash Away In Insec");
            foreach (DashAnalysisContainer dashAnalysisContainer in enemies)
            {
                LeeSinMenu.DashAnalysisMenu.Add(dashAnalysisContainer.MenuId, new CheckBox(dashAnalysisContainer.InfoText, false));
                LeeSinMenu.DashAnalysisMenu.AddSeparator();
            }

            Obj_AI_Base.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
            Obj_AI_Base.OnPlayAnimation += ObjAiBaseOnOnPlayAnimation;
        }

        private static void ObjAiBaseOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (GetLastQBuffEnemyHero() == null || !(sender is AIHeroClient) || sender.IsAlly ||
                !((AIHeroClient)sender).HasAntiInsecDashReady())
                return;

            DashAnalysisContainer container = enemies.First(x => x.Hero.NetworkId == sender.NetworkId);
            DashInfo dashInfo = container.dashInfo;

            if (args.Slot == dashInfo.slot)//dash cast
            {
                container.DashCount++;
                ReplaceContainerInList(container);
            }
        }

        private static AIHeroClient lastEnemyWithQBuff_hero;
        private static float QbuffEndTime_hero;
        static AIHeroClient GetLastQBuffEnemyHero()
        {
            var currentEnemyWithQBuff = ObjectManager.Get<Obj_AI_Base>()
                .FirstOrDefault(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne")) as AIHeroClient;
            if (currentEnemyWithQBuff != null)
            {
                lastEnemyWithQBuff_hero = currentEnemyWithQBuff;
                QbuffEndTime_hero = lastEnemyWithQBuff_hero.GetBuff("BlindMonkQOne").EndTime;
            }

            if (lastEnemyWithQBuff_hero != null && Game.Time >= QbuffEndTime_hero)
            {
                lastEnemyWithQBuff_hero = null;
            }

            return lastEnemyWithQBuff_hero;
        }

        static void ObjAiBaseOnOnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (args.Animation == "Spell1b")
            {
                //Last Q Enemy still valid if it was before
                if (GetLastQBuffEnemyHero() != null)
                {
                    QbuffEndTime_hero += 3; //sec
                    DashAnalysisContainer container = enemies.First(x => x.Hero.NetworkId == GetLastQBuffEnemyHero().NetworkId);
                    container.Q2Count++;
                    ReplaceContainerInList(container);
                }
            }
        }

        static void ReplaceContainerInList(DashAnalysisContainer c)
        {
            var itemToRemove = enemies.First(x => x.Hero.NetworkId == c.Hero.NetworkId);
            enemies.Remove(itemToRemove);

            LeeSinMenu.DashAnalysisMenu[c.MenuId].Cast<CheckBox>().DisplayName = c.InfoText;
            enemies.Add(c);
        }
    }
}
