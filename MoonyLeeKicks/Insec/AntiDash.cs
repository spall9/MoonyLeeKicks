using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace MoonyLeeKicks.Insec
{
    static class AntiDash
    {
        private static AIHeroClient me => ObjectManager.Player;
        enum Elo
        {
            Low,
            AveragePlus,
            High
        }

        enum DashForecast
        {
            LinearlyAway,
            ToBase,
            ToTeam,
            LinearyAwayWallCollision
        }

        static bool enabled => LeeSinMenu.insecExtensionsMenu["attendDashes"].Cast<CheckBox>().CurrentValue;

        private static bool useForecast
            => LeeSinMenu.insecExtensionsMenu["dashForcecastMethod"].Cast<ComboBox>().CurrentValue == 0;

        private static bool useAnalysis
            => LeeSinMenu.insecExtensionsMenu["useDashAnalysis"].Cast<CheckBox>().CurrentValue;

        private static int minProb
            => LeeSinMenu.insecExtensionsMenu["minPobabilityToDash"].Cast<Slider>().CurrentValue;

        private static bool waitForCast => !useForecast;

        private static Elo targetElo
            =>
                LeeSinMenu.insecExtensionsMenu["dashInfo__Elo"].Cast<ComboBox>().SelectedIndex == 0
                    ? Elo.Low
                    : (LeeSinMenu.insecExtensionsMenu["dashInfo__Elo"].Cast<ComboBox>().SelectedIndex == 1 ?
                            Elo.AveragePlus : Elo.High);

        static Vector2 CalculateWardPositionAfterDash_Forecasted(float normalDistance)
        {
            AIHeroClient target = SelectionHandler.GetTarget;
            Vector2 SelectedAllyPos = SelectionHandler.GetAllyPos;

            var dashInfo = ChampionDashes.DashInfos.FirstOrDefault(x => x.ChampionName == target.ChampionName);
            if (dashInfo == null || !enabled)
                return Vector2.Zero;

            Vector2 dashEndPos = Vector2.Zero;

            //
            Vector2 meTarget = target.Position.To2D() - me.Position.To2D();
            float meTargetDist = me.Distance(target);
            Vector2 dashEndPosOP_LinearlyAway = me.Position.To2D() + meTarget.Normalized() * (meTargetDist + dashInfo.DashDistance);
            //
            Vector2 targetBase = ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy).Position.To2D() - target.Position.To2D();
            Vector2 dashEndPosOP_ToBase = target.Position.To2D() + targetBase.Normalized()*dashInfo.DashDistance;
            //
            Vector2 targetTeam = GetAlliedEnemiesCenter() - target.Position.To2D();
            Vector2 dashEndPosOP_ToTeam = target.Position.To2D() + targetTeam.Normalized() * dashInfo.DashDistance;

            var forecast = GetDashForecast(dashEndPosOP_LinearlyAway, dashEndPosOP_ToBase, dashEndPosOP_ToTeam);

            switch (forecast)
            {
                case DashForecast.LinearlyAway:
                    dashEndPos = dashEndPosOP_LinearlyAway;
                    break;
                case DashForecast.ToBase:
                    dashEndPos = dashEndPosOP_ToBase;
                    break;
                case DashForecast.ToTeam:
                    dashEndPos = dashEndPosOP_ToTeam;
                    break;
            }

            if (forecast == DashForecast.LinearyAwayWallCollision)
            {
                var targetPos2d = target.Position.To2D();
                var enemyFountainVec = ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy).Position.To2D();
                float startAngle =
                    (dashEndPosOP_LinearlyAway - targetPos2d).AngleBetween(new Vector2(1, 0));
                float vecMagnitude = (dashEndPosOP_LinearlyAway - targetPos2d).Length();
                float sign = PointOnCircle(vecMagnitude, startAngle + 1, targetPos2d).Distance(enemyFountainVec) <
                             dashEndPosOP_LinearlyAway.Distance(enemyFountainVec)
                    ? +1
                    : -1;

                for (float currentAngle = startAngle; Math.Abs(currentAngle - startAngle) <= 180 ; currentAngle += sign)
                {
                    var newLinearDashPos = PointOnCircle(vecMagnitude, currentAngle, targetPos2d);
                    if (!newLinearDashPos.IsWall())
                    {
                        dashEndPos = newLinearDashPos;
                        break;
                    }
                }
            }

            if (!useForecast)
                dashEndPos = dashEndPosOP_LinearlyAway;
            Vector2 allyDashEndPos = dashEndPos - SelectedAllyPos;
            float allyDashEndPos_Distance = SelectedAllyPos.Distance(dashEndPos);
            Vector2 wardPos = SelectedAllyPos +
                              allyDashEndPos.Normalized() * (allyDashEndPos_Distance + normalDistance);

            return wardPos;
        }

        static DashForecast GetDashForecast(Vector2 linearDashVec, Vector2 baseDashVec, Vector2 teamDashVec)
        {
            AIHeroClient target = SelectionHandler.GetTarget;
            var otherEnemiesPos = ObjectManager.Get<Obj_AI_Base>().Where(x => (x is AIHeroClient || x is Obj_AI_Turret) &&
                x != target && !x.IsDead && x.Distance(target) <= 1000 
                && x.Distance(target) < x.Distance(me)).Select(x => x.Position.To2D()).ToList();
            Vector2 cCenter;

            try
            {
                float cRadius;
                MEC.FindMinimalBoundingCircle(otherEnemiesPos, out cCenter, out cRadius);
            }
            catch { return otherEnemiesPos.Count >= 2 && !teamDashVec.IsWall() ? DashForecast.ToTeam : DashForecast.LinearlyAway; }

            Vector2 baseVec = ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy).Position.To2D();
            float angle_CenterBase = cCenter.AngleBetween(baseVec);
            if (angle_CenterBase <= 10 && !baseDashVec.IsWall())
                return DashForecast.ToBase;

            if (otherEnemiesPos.Count >= 2 && !teamDashVec.IsWall())
                return DashForecast.ToTeam;

            if (!linearDashVec.IsWall())
                return DashForecast.LinearlyAway;

            return DashForecast.LinearyAwayWallCollision;
        }

        static Vector2 GetAlliedEnemiesCenter()
        {
            var otherEnemiesPos = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.Distance(SelectionHandler.GetTarget) <= 1000
                && x.Distance(SelectionHandler.GetTarget) < x.Distance(me)).Select(x => x.Position.To2D()).ToList();
            Vector2 cCenter;
            float cRadius;
            MEC.FindMinimalBoundingCircle(otherEnemiesPos, out cCenter, out cRadius);
            return cCenter;
        }

        static Vector2 PointOnCircle(float radius, float angleInDegrees, Vector2 origin)
        {
            float x = origin.X + (float)(radius * Math.Cos(angleInDegrees * Math.PI / 180));
            float y = origin.Y + (float)(radius * Math.Sin(angleInDegrees * Math.PI / 180));

            return new Vector2(x, y);
        }

        private static bool dashGotCasted;
        private static Vector2 LastDashCastPos;
        public static void ObjAiBaseOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (LeeSinInsec.InsecSolution.lastType == LeeSinInsec.InsecSolution.InsecSolutionType.WaitForDashCast)
            {
                var target = SelectionHandler.GetTarget;
                var dashInfo = ChampionDashes.DashInfos.FirstOrDefault(x => x.ChampionName == target.ChampionName);
                if (dashInfo == null) //target switched, new one has no dash
                {
                    LeeSinInsec.InsecSolution.ResetSolution();
                    dashGotCasted = false;
                    return;
                }

                if (sender == target && args.Slot == dashInfo.slot)
                {
                    LastDashCastPos = args.End.To2D();
                    dashGotCasted = true;
                    Core.DelayAction(() => dashGotCasted = false, 3000);
                }
            }
        }

        public static Vector2 GetDashWardPos(float normalDistance, AIHeroClient lastQBuffEnemy)
        {
            var dashAnalysis = DashAnalysis.Enemies.FirstOrDefault(x => x.Hero.NetworkId == SelectionHandler.GetTarget.NetworkId);
            if (dashAnalysis == null)
                return Vector2.Zero;

            if ((useAnalysis && dashAnalysis.DashProbability < minProb) || (useAnalysis && targetElo == Elo.High))
                return Vector2.Zero;

            bool lowElo = targetElo == Elo.Low && !useAnalysis;
            if (!enabled || lowElo)
                return Vector2.Zero;

            var target = SelectionHandler.GetTarget;
            bool hasQBuff = lastQBuffEnemy != null && lastQBuffEnemy == target && lastQBuffEnemy.IsValid;
            bool hasDash = target.HasAntiInsecDashReady();

            if (hasQBuff && hasDash && useForecast)
                return CalculateWardPositionAfterDash_Forecasted(normalDistance);

            if (hasQBuff && hasDash && waitForCast)
                LeeSinInsec.InsecSolution.FoundSolution(LeeSinInsec.InsecSolution.InsecSolutionType.WaitForDashCast);

            if (!dashGotCasted)
                return Vector2.Zero;

            return SelectionHandler.GetAllyPos.Extend(LastDashCastPos,
                SelectionHandler.GetAllyPos.Distance(LastDashCastPos) + normalDistance);
        }
    }
}
