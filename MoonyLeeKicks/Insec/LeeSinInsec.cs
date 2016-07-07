using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace MoonyLeeKicks
{
    internal class LeeSinInsec
    {
        private static class WardJumpPosition
        {
            static float GetSpaceDistToEnemy()
            {
                var target = SelectionHandler.GetTarget;
                var predPos = Prediction.Position.PredictUnitPosition(target, 1000);
                var myDirection = ObjectManager.Player.Position.To2D() + 500 * ObjectManager.Player.Direction.To2D().Perpendicular();
                var targetDirection = target.Position.To2D() + 500 * target.Direction.To2D().Perpendicular();
                float angle = (myDirection - ObjectManager.Player.Position.To2D()).AngleBetween(
                    targetDirection - target.Position.To2D());
                bool targetRunningAway = angle <= 10 &&
                    predPos.Distance(ObjectManager.Player.Position) > target.Distance(ObjectManager.Player);
                bool useMovementPrediction =
                    LeeSinMenu.insecConfig["useMovementPrediction"].Cast<CheckBox>().CurrentValue;
                return targetRunningAway && useMovementPrediction ? 300 : LeeSinMenu.insecConfig["wardDistanceToTarget"].Cast<Slider>().CurrentValue;
            }
            /// <summary>
            /// op vec
            /// </summary>
            static Vector2 PointOnCircle(float radius, float angleInDegrees, Vector2 origin)
            {
                float x = origin.X + (float)(radius * Math.Cos(angleInDegrees * Math.PI / 180));
                float y = origin.Y + (float)(radius * Math.Sin(angleInDegrees * Math.PI / 180));

                return new Vector2(x, y);
            }

            public static Vector2 GetWardJumpPosition(Vector2 allyPos, AIHeroClient lastQBuffEnemy)
            {
                AIHeroClient target = SelectionHandler.GetTarget;

                Vector2 wardPlacePos = allyPos + (target.Position.To2D() - allyPos).Normalized() *
                                   (target.Distance(allyPos) + GetSpaceDistToEnemy());

                bool hasQBuff = lastQBuffEnemy != null && lastQBuffEnemy == target && lastQBuffEnemy.IsValid;
                bool attendDash = LeeSinMenu.insecConfig["attendDashes"].Cast<KeyBind>().CurrentValue;
                bool hasDash = target.HasAntiInsecDashReady();
                if (hasDash && attendDash && hasQBuff)
                    wardPlacePos = target.CalculateWardPositionAfterDash(allyPos, GetSpaceDistToEnemy());

                Vector3 targetPosition = allyPos.To3D() +
                                         (wardPlacePos.To3D() - allyPos.To3D()).Normalized()*
                                         (allyPos.Distance(wardPlacePos) - GetSpaceDistToEnemy());

                if (LeeSinMenu.multiRMenu["multiREnabledInsec"].Cast<CheckBox>().CurrentValue)
                {
                    float leeSinRKickDistance = 700;
                    float leeSinRKickWidth = 100;
                    var minREnemies = LeeSinMenu.multiRMenu["targetAmount"].Cast<Slider>().CurrentValue;
                    int rotationAngle = LeeSinMenu.multiRMenu["rotationAngle"].Cast<Slider>().CurrentValue;

                    Vector2 vecToRotate = wardPlacePos - targetPosition.To2D();
                    Vector2 op = targetPosition.To2D();

                    for (int currentAngle = -rotationAngle; currentAngle <= rotationAngle; currentAngle++)
                    {
                        float stdAngle = vecToRotate.AngleBetween(new Vector2(100, 0));
                        Vector2 rotatedWardPos = PointOnCircle(vecToRotate.Length(), stdAngle + currentAngle, op);
                        Vector2 kickEndPos = targetPosition.To2D() +
                                             (targetPosition.To2D() - rotatedWardPos).Normalized()*leeSinRKickDistance;
                        var rect = new Geometry.Polygon.Rectangle(targetPosition.To2D(), kickEndPos, leeSinRKickWidth);

                        if (EntityManager.Heroes.Enemies.Count(x => x.IsValid && rect.IsInside(x)) >= minREnemies)
                        {
                            wardPlacePos = rotatedWardPos;
                        }
                    }

                }

                return wardPlacePos;
            }
        }

        private static class InsecSolution
        {
            static InsecSolutionType lastType = InsecSolutionType.NoSolutionFound;

            /*Ward + Flash is not a solution only, its dicided into 2 parts*/
            public enum InsecSolutionType
            {
                NoSolutionFound,
                WardJump,
                Flash,
                MoonSec
            }
            public static void FoundSolution(InsecSolutionType type)
            {
                lastType = type;
            }

            public static bool GotASolution => lastType != InsecSolutionType.NoSolutionFound;

            public static void ResetSolution()
            {
                lastType = InsecSolutionType.NoSolutionFound;
            }

            public static bool CanContinueSearchingFor(InsecSolutionType type)
            {
                return lastType == InsecSolutionType.NoSolutionFound || lastType == type;
            }
        }

        /// <summary>
        /// 2 activations
        /// </summary>
        private static int minEnergy = 80;
        private static int minEnegeryFirstActivation = 50;
        private static int LastQ1CastTick;
        //TODO: pink ward jump for special champs

        private readonly AIHeroClient me;
        public LeeSinInsec()
        {
            me = ObjectManager.Player;
            Game.OnUpdate += GameOnOnUpdate;

            Obj_AI_Base.OnProcessSpellCast += AiHeroClientOnOnProcessSpellCast;
            Obj_AI_Base.OnPlayAnimation += ObjAiBaseOnOnPlayAnimation;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private void ObjAiBaseOnOnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe || !LeeSinMenu.insecConfig["_insecKey"].Cast<KeyBind>().CurrentValue)
                return;

            if (args.Animation == "Spell1b")
            {
                //Last Q Enemy still valid if it was before
                if (GetLastQBuffEnemyHero() != null)
                    QbuffEndTime_hero += 3;//sec
                if (GetLastQBuffEnemyObject() != null)
                    QbuffEndTime_object += 3;//sec
            }
            if (args.Animation == "Spell1a")
                LastQ1CastTick = Environment.TickCount;
        }

        private int lastInsecCheckTick;
        private void GameOnOnUpdate(EventArgs args)
        {
            GetLastQBuffEnemyHero(); //update
            GetLastQBuffEnemyObject(); //update

            if (!LeeSinMenu.insecConfig["_insecKey"].Cast<KeyBind>().CurrentValue)
                return;

            int updateFrequency = LeeSinMenu.insecConfig["insecFrequency"].Cast<Slider>().CurrentValue;
            if (HasResourcesToInsec() && Environment.TickCount - lastInsecCheckTick >= updateFrequency)
            {
                lastInsecCheckTick = Environment.TickCount;
                CheckInsec();
            }
        }

        private bool HasResourcesToInsec()
        {
            bool canJump = SpellManager.CanCastW1 || SpellManager.FlashReady || (GetAllyAsWard() != null && GetAllyAsWard().IsValid);
            bool canInsec = canJump && SpellManager.R.IsReady();
            return canInsec && SelectionHandler.LastAllyPosValid && SelectionHandler.LastTargetValid;
        }

        Obj_AI_Base GetAllyAsWard()
        {
            var allyPos = SelectionHandler.GetAllyPos;

            List<Obj_AI_Base> allyJumps = new List<Obj_AI_Base>();
            #region setVars
            var target = SelectionHandler.GetTarget;

            var wardPlacePos = WardJumpPosition.GetWardJumpPosition(allyPos, GetLastQBuffEnemyHero());
            #endregion setVars

            foreach (var allyobj in ObjectManager.Get<Obj_AI_Base>().Where(x => 
                            x.IsValid && x.IsAlly && !x.IsMe && (x is AIHeroClient || x is Obj_AI_Minion)))
            {
                if (allyobj.Distance(wardPlacePos) <= 80 && allyobj.Distance(allyPos) > allyPos.Distance(target))
                {
                    allyJumps.Add(allyobj);
                }
            }
            Obj_AI_Base obj = allyJumps.Any() ? allyJumps.OrderBy(x => x.Distance(wardPlacePos)).First() : null;
            return obj;
        }

        private void AiHeroClientOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || args.Slot != SpellSlot.R || !LeeSinMenu.insecConfig["_insecKey"].Cast<KeyBind>().CurrentValue)
                return;

            var allyPos = SelectionHandler.GetAllyPos;

            if (moonSecActive)
            {
                var target = SelectionHandler.GetTarget;
                var wardPlacePos = allyPos + (target.Position.To2D() - allyPos).Normalized()*
                                   (target.Distance(allyPos) - (float) SpellManager.R.Range/2);
                var flashPos = wardPlacePos.Extend(target, SpellManager.Flash.Range);

                Core.DelayAction(() =>
                {
                    SpellManager.Flash.Cast(flashPos.To3D());
                    moonSecActive = false;
                    if (InsecSolution.GotASolution) Core.DelayAction(InsecSolution.ResetSolution, 1600);
                }, 80);
            }
            else if (InsecSolution.GotASolution)
                Core.DelayAction(InsecSolution.ResetSolution, 1600);

            var canQ = SpellManager.CanCastQ1;
            if (canQ)
            {
                Core.DelayAction(() => SpellManager.Q1.Cast(args.Target.Position), 500);
            }
            if (SpellManager.ExhaustReady)
            {
                Core.DelayAction(() => SpellManager.Exhaust.Cast(args.Target as Obj_AI_Base), 500);
            }
            if (SpellManager.SmiteReady)
            {
                Core.DelayAction(() => SpellManager.Smite.Cast(args.Target as Obj_AI_Base), 500);
            }
            if (Item.HasItem(ItemId.Zhonyas_Hourglass) && Item.CanUseItem(ItemId.Zhonyas_Hourglass) && 
                (!moonSecActive || !SpellManager.FlashReady))
            {
                Core.RepeatAction(() => Item.UseItem(ItemId.Zhonyas_Hourglass), 100, 1000);
            }
        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            var allyPos = SelectionHandler.GetAllyPos;

            if (SelectionHandler.LastAllyPosValid)
                new Circle(new ColorBGRA(new Vector4(30, 144, 255, 1)), 100, 2) {Color = Color.DodgerBlue}.Draw(allyPos.To3D());
            if (SelectionHandler.LastTargetValid)
                new Circle(new ColorBGRA(new Vector4(30, 144, 255, 1)), 100, 2) { Color = Color.DodgerBlue }
                .Draw(SelectionHandler.GetTarget.Position);

            if (!SelectionHandler.LastAllyPosValid || !SelectionHandler.LastTargetValid)
            {
                if (LeeSinMenu.insecConfig["_insecKey"].Cast<KeyBind>().CurrentValue)
                    DrawFailInfo();
                return;
            }

            bool couldInsec = HasResourcesToInsec();
            if (couldInsec)
            {
                var startpos = SelectionHandler.GetTarget.Position.To2D();
                var endpos = allyPos;

                Vector2[] arrows = {
                    endpos + (startpos - endpos).Normalized().Rotated(45 * (float)Math.PI / 180) *
                        SelectionHandler.GetTarget.BoundingRadius,
                    endpos + (startpos - endpos).Normalized().Rotated(-45 * (float)Math.PI / 180) *
                        SelectionHandler.GetTarget.BoundingRadius
                };
                var white = Color.FromArgb(255, 255, 255, 255);

                Drawing.DrawLine(startpos.To3D().WorldToScreen(), endpos.To3D().WorldToScreen(), 5, white);
                Drawing.DrawLine(endpos.To3D().WorldToScreen(), arrows[0].To3D().WorldToScreen(), 5, white);
                Drawing.DrawLine(endpos.To3D().WorldToScreen(), arrows[1].To3D().WorldToScreen(), 5, white);
            }
            else if (LeeSinMenu.insecConfig["_insecKey"].Cast<KeyBind>().CurrentValue)
                DrawFailInfo();

            bool dashDebug = LeeSinMenu.insecConfig["dashDebug"].Cast<KeyBind>().CurrentValue;
            if (dashDebug)
            {
                new Circle(new ColorBGRA(new Vector4(0, 0, 255, 1)), 70, 4).Draw(
                    WardJumpPosition.GetWardJumpPosition(allyPos, GetLastQBuffEnemyHero()).To3D());
            }
        }

        private void DrawFailInfo()
        {
            try
            {
                Text StatusText = new Text("", new Font("Euphemia", 10F, FontStyle.Bold)) {Color = Color.Red};

                if (!SelectionHandler.LastTargetValid)
                    StatusText.TextValue = "No Insec Target Selected";
                else if (!SelectionHandler.LastAllyPosValid)
                    StatusText.TextValue = "Invalid Insec Ally Position";
                else if (!HasResourcesToInsec())
                    StatusText.TextValue = "Not enough Spells for Insec";

                StatusText.Position = Player.Instance.Position.WorldToScreen() -
                                      new Vector2((float) StatusText.Bounding.Width/2, -50);

                StatusText.Draw();
            }
            catch { }
        }

        private void CheckWardKick(Vector2 wardPlacePos, AIHeroClient target)
        {
            var allyJump = GetAllyAsWard();
            var allyJumpValid = allyJump != null && allyJump.IsValid;
            var canWardJump = (WardManager.CanCastWard || allyJumpValid) && SpellManager.CanCastW1 &&
                me.Mana >= minEnegeryFirstActivation;
            float maxDist = WardManager.CanCastWard ? WardManager.WardRange : SpellManager.W1.Range;

            if (canWardJump && me.Distance(wardPlacePos) <= maxDist)
            {
                InsecSolution.FoundSolution(InsecSolution.InsecSolutionType.WardJump); 
                if (WardManager.CanCastWard)
                    WardManager.CastWardTo(wardPlacePos.To3D());
                else
                    SpellManager.W1.Cast(allyJump);

                bool useCorrection = LeeSinMenu.insecConfig["correctInsecWithOtherSpells"].Cast<CheckBox>().CurrentValue;
                Core.RepeatAction(() =>
                {
                    if (me.Distance(wardPlacePos) > 80)
                        return;

                    if (target.Distance(me) <= SpellManager.R.Range)
                        SpellManager.R.Cast(target);
                    else if (SpellManager.R.IsReady() && useCorrection)
                        InsecSolution.ResetSolution();
                }, 0, 3000);
            }
        }

        private void CheckFlashKick(Vector2 wardPlacePos, AIHeroClient target)
        {
            var canFlash = SpellManager.FlashReady;

            if (me.Distance(wardPlacePos) <= SpellManager.Flash.Range && canFlash)
            {
                InsecSolution.FoundSolution(InsecSolution.InsecSolutionType.Flash);
                SpellManager.Flash.Cast(wardPlacePos.To3D());

                bool useCorrection = LeeSinMenu.insecConfig["correctInsecWithOtherSpells"].Cast<CheckBox>().CurrentValue;
                Core.RepeatAction(() =>
                {
                    if (me.Distance(wardPlacePos) > 80)
                        return;

                    if (target.Distance(me) <= SpellManager.R.Range)
                        SpellManager.R.Cast(target);
                    else if (SpellManager.R.IsReady() && useCorrection)
                        InsecSolution.ResetSolution();
                }, 0, 3000);
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
                lastEnemyWithQBuff_hero = null;

            return lastEnemyWithQBuff_hero;
        }

        private static Obj_AI_Base lastEnemyWithQBuff_object;
        private static float QbuffEndTime_object;
        static Obj_AI_Base GetLastQBuffEnemyObject()
        {
            var currentEnemyWithQBuff = ObjectManager.Get<Obj_AI_Base>()
                .FirstOrDefault(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne"));
            if (currentEnemyWithQBuff != null)
            {
                lastEnemyWithQBuff_object = currentEnemyWithQBuff;
                QbuffEndTime_object = lastEnemyWithQBuff_object.GetBuff("BlindMonkQOne").EndTime;
            }
            if (lastEnemyWithQBuff_object != null && Game.Time >= QbuffEndTime_object)
                lastEnemyWithQBuff_object = null;

            return lastEnemyWithQBuff_object;
        }

        private void CheckWardFlashKick(Vector2 wardPlacePos)
        {
            var allyJump = GetAllyAsWard();
            var allyJumpValid = allyJump != null && allyJump.IsValid;
            var canFlash = SpellManager.FlashReady;
            var canWardJump = (WardManager.CanCastWard || allyJumpValid) &&
                                me.Mana >= minEnegeryFirstActivation;
            float maxWardJumpDist = !allyJumpValid ? WardManager.WardRange : SpellManager.W1.Range;
            

            bool inRange = me.Distance(wardPlacePos) <= SpellManager.Flash.Range + maxWardJumpDist;
            


            float distQTargetToWardPos = GetLastQBuffEnemyHero() != null ? 
                GetLastQBuffEnemyHero().Distance(wardPlacePos) : float.MaxValue;
            float maxRange = canWardJump ? 600 : 425;
            if (allyJumpValid) maxRange = SpellManager.W1.Range;

            bool dontNeedFlash = distQTargetToWardPos <= maxRange;
            bool waitForQFirst = (SpellManager.CanCastQ1 || Environment.TickCount - LastQ1CastTick < 1000) &&
                                    LeeSinMenu.insecConfig["waitForQBefore_WardFlashKick"].Cast<CheckBox>().CurrentValue;
            bool onlyUseWithQ = GetLastQBuffEnemyObject() == null &&
                LeeSinMenu.insecConfig["WardFlashKickOnlyWithQ"].Cast<CheckBox>().CurrentValue;

            if (inRange && canWardJump && canFlash && !dontNeedFlash && !InsecSolution.GotASolution &&
                !waitForQFirst && !onlyUseWithQ)
            {
                if (WardManager.CanCastWard)
                    WardManager.CastWardTo(wardPlacePos.To3D());
                else /*flash -> ally w to reach wardPos*/
                    SpellManager.Flash.Cast(wardPlacePos.To3D());
            }
        }

        private bool moonSecActive;
        private void CheckMoonSec(AIHeroClient target)
        {
            var allyPos = SelectionHandler.GetAllyPos;

            var wardPlacePos = allyPos + (target.Position.To2D() - allyPos);

            var canFlash = SpellManager.FlashReady;
            var canWardJump = SpellManager.CanCastW1 && me.Mana >= minEnegeryFirstActivation &&
                              WardManager.CanCastWard;
            float distToWardPlacePos = GetLastQBuffEnemyHero() != null && GetLastQBuffEnemyHero().IsValid
                ? GetLastQBuffEnemyHero().Distance(wardPlacePos)
                : me.Distance(wardPlacePos);

            if (distToWardPlacePos <= WardManager.WardRange && canWardJump && canFlash)
            {
                InsecSolution.FoundSolution(InsecSolution.InsecSolutionType.MoonSec);
                moonSecActive = true;
                
                WardManager.CastWardTo(wardPlacePos.To3D());
                Core.RepeatAction(() =>
                {
                    if (me.Distance(target) <= (float)SpellManager.Flash.Range/2)
                        SpellManager.R.Cast(target);
                    else
                        moonSecActive = false;
                }, 350, 1500);
            }
        }

        private void CheckInsec()
        {
            var allyPos = SelectionHandler.GetAllyPos;
            Orbwalker.OrbwalkTo(Game.CursorPos);

            if (SpellManager.CanCastQ2 && Game.Time - WardManager._lastWardJumpTime > 1.5f && 
                !LeeSinMenu.insecConfig["onlyQ2IfNeeded"].Cast<CheckBox>().CurrentValue) //just cast q2, doesnt matter if senseful anymore
                SpellManager.Q2.Cast();

            #region setVars
            var target = SelectionHandler.GetTarget;
            

                var canFlash = SpellManager.FlashReady;
                var canWardJump = SpellManager.CanCastW1 && me.Mana >= minEnegeryFirstActivation &&
                                  WardManager.CanCastWard;
            var wardPlacePos = WardJumpPosition.GetWardJumpPosition(allyPos, GetLastQBuffEnemyHero());
            #endregion setVars

            /*normal q cast on enemy*/
            var qPred = SpellManager.Q1.GetPrediction(target);
            if (qPred.HitChance >= HitChance.High && SpellManager.CanCastQ1)
                SpellManager.Q1.Cast(qPred.CastPosition);
            else if (SpellManager.CanCastQ2 && GetLastQBuffEnemyHero() == target)
                SpellManager.Q2.Cast();

            /*try insec if possible*/
            bool tryMoonSec = LeeSinMenu.insecConfig["moonSec"].Cast<CheckBox>().CurrentValue;
            if (InsecSolution.CanContinueSearchingFor(InsecSolution.InsecSolutionType.MoonSec) && tryMoonSec)
                CheckMoonSec(target);
            if (moonSecActive)
                return;

            if (InsecSolution.CanContinueSearchingFor(InsecSolution.InsecSolutionType.WardJump))
                CheckWardKick(wardPlacePos, target);
            if (InsecSolution.CanContinueSearchingFor(InsecSolution.InsecSolutionType.Flash))
                CheckFlashKick(wardPlacePos, target);

            CheckWardFlashKick(wardPlacePos);

            if (!InsecSolution.GotASolution)
            {
                #region searchMinion

                var minList = new List<Obj_AI_Minion>();
                foreach (
                    var minion in
                        EntityManager.MinionsAndMonsters.Combined.Where(
                            x => !x.IsAlly && SpellManager.Q1.GetPrediction(x).CollisionObjects.Length == 0))
                {
                    var qflyTime = me.Distance(minion)/SpellManager.Q1.Speed*1000 + SpellManager.Q1.CastDelay*2 +
                                     100;
                    var alive = Prediction.Health.GetPrediction(minion, (int) Math.Ceiling(qflyTime)) >
                                me.GetSpellDamage(minion, SpellSlot.Q)*2;
                    if (alive && minion.Distance(me) <= SpellManager.Q1.Range)
                        minList.Add(minion);
                }

                #endregion
                bool canQ2 = SpellManager.CanCastQ2 && Game.Time - WardManager._lastWardJumpTime > 1.5f;

                var targetMinion = minList.OrderBy(x => x.Distance(target)).FirstOrDefault();
                if (targetMinion != null && targetMinion.IsValid)
                {
                    if (targetMinion.Distance(wardPlacePos) <= WardManager.WardRange && canWardJump)
                    {
                        if (canQ2 && GetLastQBuffEnemyObject() == targetMinion)
                            SpellManager.Q2.Cast();
                        else
                            SpellManager.Q1.Cast(targetMinion.Position);
                    }

                    if (targetMinion.Distance(wardPlacePos) <= SpellManager.Flash.Range && canFlash)
                    {
                        if (canQ2 && GetLastQBuffEnemyObject() == targetMinion)
                            SpellManager.Q2.Cast();
                        else
                            SpellManager.Q1.Cast(targetMinion.Position);
                    }
                    /*q minion + wardjump + flash*/
                    if (targetMinion.Distance(wardPlacePos) > SpellManager.Flash.Range &&
                             targetMinion.Distance(wardPlacePos) > WardManager.WardRange && canWardJump && canFlash &&
                             targetMinion.Distance(wardPlacePos) <= SpellManager.Flash.Range + WardManager.WardRange)
                    {
                        if (canQ2 && GetLastQBuffEnemyObject() == targetMinion)
                            SpellManager.Q2.Cast();
                        else
                            SpellManager.Q1.Cast(targetMinion.Position);
                    }
                }
            }
        }
    }
}