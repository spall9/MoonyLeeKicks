using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using MoonyLeeKicks.Insec;
using SharpDX;

namespace MoonyLeeKicks.Extras
{
    public static class PolygonExtensions
    {
        public static Geometry.Polygon GetDetailedPolygon(this Geometry.Polygon.Rectangle p)
        {
            Geometry.Polygon detailedRectangle = new Geometry.Polygon();
            for (int i = 0; i < p.Points.Count; i += 2)
            {
                var point = p.Points[i];
                var nextPoint = i == p.Points.Count - 1 ? p.Points[0] : p.Points[i + 1];
                detailedRectangle.Add(nextPoint);

                for (float scaling = 1; scaling >= 0; scaling -= 0.1f)
                {
                    var detailedPoint = point + (nextPoint - point) * scaling;
                    detailedRectangle.Add(detailedPoint);
                }
                detailedRectangle.Add(point);
            }
            return detailedRectangle;
        }
    }
    internal class BubbaKush
    {
        private AIHeroClient me;
        private bool enabled => LeeSinMenu.bubbaKushMenu["bubbaKey"].Cast<KeyBind>().CurrentValue;
        private bool hasResources => SpellManager.R.IsReady() && SpellManager.FlashReady && WardManager.CanCastWard;

        private bool targetValid = (TargetSelector.SelectedTarget != null && TargetSelector.SelectedTarget.IsValid) ||
                                   SelectionHandler.LastTargetValid;

        AIHeroClient target
        {
            get
            {
                return TargetSelector.SelectedTarget != null && TargetSelector.SelectedTarget.IsValid
                    ? TargetSelector.SelectedTarget
                    : SelectionHandler.GetTarget;
            }
        }

        bool canAllyJump
        {
            get
            {
                if (!targetValid)
                    return false;

                return EntityManager.Heroes.Allies.Any(x => x.IsValid && x.Distance(me) <= SpellManager.W1.Range && x.Distance(target) < 100) && me.Mana >= 50 &&
                    SpellManager.CanCastW1 && LeeSinMenu.bubbaKushMenu["useAlliesBubba"].Cast<CheckBox>().CurrentValue;
            }
        }

        private bool foundSolution;
        private string errorString = string.Empty;
        public BubbaKush()
        {
            me = ObjectManager.Player;
            Game.OnUpdate += GameOnOnUpdate;
            AIHeroClient.OnProcessSpellCast += OnProcessSpellCast;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            if (errorString == string.Empty)
                return;

            Text StatusText = new Text("", new Font("Euphemia", 10F, FontStyle.Bold))
            {
                Color = System.Drawing.Color.Red,
                TextValue = errorString
            };

            StatusText.Position = Player.Instance.Position.WorldToScreen() -
                                  new Vector2((float)StatusText.Bounding.Width / 2, -50);

            StatusText.Draw();
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || args.Slot != SpellSlot.R || !LeeSinMenu.bubbaKushMenu["bubbaKey"].Cast<KeyBind>().CurrentValue)
                return;

            Vector2 flashPos = GetFlashPos();
            if (!flashPos.IsZero)
                Core.RepeatAction(() =>
                {
                    if (SpellManager.FlashReady)
                        SpellManager.Flash.Cast(flashPos.To3D());
                }, 80, 1000);
            else
            {
                args.Process = false;
                foundSolution = false;
            }
        }

        private void GameOnOnUpdate(EventArgs args)
        {
            if (!enabled)
            {
                errorString = string.Empty;
                return;
            }

            Orbwalker.OrbwalkTo(Game.CursorPos);

            if (!hasResources && !foundSolution)
            {
                if (!SpellManager.R.IsReady())
                    errorString = "R not ready";
                else if (!WardManager.CanCastWard && !canAllyJump)
                    errorString = "Ward not ready and no ally jump";
                else if (!SpellManager.FlashReady)
                    errorString = "No flash ready";

                return;
            }
            else if (!SelectionHandler.LastTargetValid)
            {
                errorString = "Target not selected";
                return;
            }
            else errorString = string.Empty;

            if (EntityManager.Heroes.Enemies.Any(x => x.IsValid && !x.IsDead && x.Distance(target) <= 700))
            {
                CheckQCast();
                CheckWardJump();
            }

            if (foundSolution)
            {
                if (target.Distance(me) <= SpellManager.R.Range && SpellManager.R.IsReady() && SpellManager.R.CanCast(target))
                    SpellManager.R.Cast(target);
                Core.DelayAction(() => foundSolution = false, 3000);
            }
        }

        Vector2 PointOnCircle(float radius, float angleInDegrees, Vector2 origin)
        {
            float x = origin.X + (float)(radius * Math.Cos(angleInDegrees * Math.PI / 180));
            float y = origin.Y + (float)(radius * Math.Sin(angleInDegrees * Math.PI / 180));

            return new Vector2(x, y);
        }

        private int ExtraNeededRange => LeeSinMenu.insecMenu["extraRangeBuffer"].Cast<Slider>().CurrentValue;

        private void CheckQCast()
        {
            var minList = new List<Obj_AI_Minion>();
            foreach (
                var minion in
                    EntityManager.MinionsAndMonsters.Combined.Where(
                        x => !x.IsAlly && SpellManager.Q1.GetPrediction(x).CollisionObjects.Length == 0))
            {
                var qflyTime = me.Distance(minion) / SpellManager.Q1.Speed * 1000 + SpellManager.Q1.CastDelay * 2 +
                                 100;
                var alive = Prediction.Health.GetPrediction(minion, (int)Math.Ceiling(qflyTime)) >
                            me.GetSpellDamage(minion, SpellSlot.Q);
                if (alive && minion.Distance(me) <= SpellManager.Q1.Range)
                    minList.Add(minion);
            }

            var targetMinion = minList.OrderBy(x => x.Distance(SelectionHandler.GetTarget)).FirstOrDefault();
            if (targetMinion != null && targetMinion.IsValid)
            {
                if (targetMinion.Distance(SelectionHandler.GetTarget) <= WardManager.WardRange + 
                    ExtraNeededRange && WardManager.CanWardJump)
                {
                    if (targetMinion.HasBuff("BlindMonkQOne"))
                        SpellManager.Q2.Cast();
                    else
                        SpellManager.Q1.Cast(targetMinion.Position);
                }
            }
        }

        /// <returns></returns>
        void CheckWardJump()
        {
            float wSpeed = 2000;

            if (canAllyJump)
            {
                var ally = EntityManager.Heroes.Allies.Where(
                    x =>
                        x.IsValid && x.Distance(me) <= SpellManager.W1.Range &&
                        x.Distance(target) < 100).OrderBy(x => x.Distance(me)).First();
                float predictionDist = me.Distance(ally);
                
                float flyTime = predictionDist / wSpeed;
                Vector2 predPos = Prediction.Position.PredictUnitPosition(target, (int)flyTime);

                if (predPos.Distance(ally) <= SpellManager.R.Range)
                {
                    foundSolution = true;
                    SpellManager.W1.Cast(ally);
                }
            }

            if (LeeSinMenu.bubbaKushMenu["useMovementPredictionBubba1"].Cast<CheckBox>().CurrentValue 
                && WardManager.CanWardJump)
            {
                float predictionDist = me.Distance(target) >= WardManager.WardRange
                    ? WardManager.WardRange
                    : me.Distance(target);
                float flyTime = predictionDist / wSpeed;
                Vector2 predPos = Prediction.Position.PredictUnitPosition(target, (int)flyTime);

                if (me.Distance(predPos) <= WardManager.WardRange)
                {
                    foundSolution = true;
                    WardManager.CastWardTo(predPos.To3D());
                }
            }
            else if (WardManager.CanWardJump)
            {
                Vector2 targetPos = target.Position.To2D();
                if (me.Distance(targetPos) <= WardManager.WardRange)
                {
                    foundSolution = true;
                    WardManager.CastWardTo(targetPos.To3D());
                }
            }
        }

        /// <summary>
        /// returns the distance to the closest edge point of the multi kick rectangle
        /// </summary>
        /// <returns></returns>
        float GetEdgeValue(Geometry.Polygon.Rectangle multiKickRect, Vector2 position)
        {
            return multiKickRect.GetDetailedPolygon().Points.OrderBy(x => x.Distance(position)).First().Distance(position);
        }

        Vector2 GetFlashPos()
        {
            var targetPos = target.Position.To2D();
            Vector2 bestPos = Vector2.Zero;
            bool useBetterCalculations = LeeSinMenu.bubbaKushMenu["betterCalculationBubba"].Cast<CheckBox>().CurrentValue;

            bool predictAllEnemiesPos = LeeSinMenu.bubbaKushMenu["useMovementPredictionBubba2"].Cast<CheckBox>().CurrentValue;
            IEnumerable<Vector2> enemiesPos = predictAllEnemiesPos
                ? from enemy in EntityManager.Heroes.Enemies
                  where enemy.IsValid && enemy.IsHPBarRendered
                  let predPos = Prediction.Position.PredictUnitPosition(enemy, SpellManager.R.CastDelay)
                  select predPos : EntityManager.Heroes.Enemies.Where(x => x.IsValid && x.IsHPBarRendered).
                    Select(x => x.Position.To2D());
            ;

            float leeSinRKickDistance = 700;
            float leeSinRKickWidth = 100;
            var minREnemies = 2;

            int maxHits = 0;
            float bestEdgeValue = 0;

            Vector2 vecToRotate = new Vector2(200, 0);
            Vector2 op = targetPos;

            for (int currentAngle = 0; currentAngle <= 359; currentAngle++)
            {
                float stdAngle = 0;
                Vector2 rotatedWardPos = PointOnCircle(vecToRotate.Length(), stdAngle + currentAngle, op);
                Vector2 kickEndPos = targetPos +
                                     (targetPos - rotatedWardPos).Normalized() * leeSinRKickDistance;

                var rect = new Geometry.Polygon.Rectangle(targetPos, kickEndPos, leeSinRKickWidth);
                int hits = enemiesPos.Count(x => rect.IsInside(x));
                float edgeValue = enemiesPos.Where(x => rect.IsInside(x)).Select(x => GetEdgeValue(rect, x)).Sum();

                bool betterEdgeValue = hits == maxHits && edgeValue > bestEdgeValue && useBetterCalculations;
                if (hits >= minREnemies && rotatedWardPos.Distance(me) <= SpellManager.Flash.Range)
                {
                    if (hits > maxHits || betterEdgeValue)
                    {
                        maxHits = hits;
                        bestPos = rotatedWardPos;
                        bestEdgeValue = edgeValue;
                    }
                }
            }

            return bestPos;
        }
    }
}
