﻿using System;
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
                return LeeSinMenu.insecConfig["wardDistanceToTarget"].Cast<Slider>().CurrentValue;
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

            public static Vector2 GetWardJumpPosition(Obj_AI_Base ally, AIHeroClient lastQBuffEnemy)
            {
                AIHeroClient target = TargetSelector.SelectedTarget;
                Vector2 wardPlacePos = ally.Position.To2D() + (target.Position.To2D() - ally.Position.To2D()).Normalized() *
                                   (target.Distance(ally) + GetSpaceDistToEnemy());

                bool hasQBuff = lastQBuffEnemy != null && lastQBuffEnemy == target && lastQBuffEnemy.IsValid;
                bool attendDash = LeeSinMenu.insecConfig["attendDashes"].Cast<KeyBind>().CurrentValue;
                bool hasDash = target.HasAntiInsecDashReady();
                if (hasDash && attendDash && hasQBuff)
                    wardPlacePos = target.CalculateWardPositionAfterDash(ally, GetSpaceDistToEnemy());

                Vector3 targetPosition = ally.Position +
                                         (wardPlacePos.To3D() - ally.Position).Normalized()*
                                         (ally.Distance(wardPlacePos) - GetSpaceDistToEnemy());

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

            public static bool GotASolution
            {
                get { return lastType != InsecSolutionType.NoSolutionFound; }
            }

            public static void ResetSolution()
            {
                lastType = InsecSolutionType.NoSolutionFound;
            }

            public static bool CanContinueSearchingFor(InsecSolutionType type)
            {
                return lastType == InsecSolutionType.NoSolutionFound || lastType == type;
            }
        }

        private static Obj_AI_Base ally;
        /// <summary>
        /// 2 activations
        /// </summary>
        private static int minEnergy = 80;
        private static int minEnegeryFirstActivation = 50;
        //TODO: pink ward jump for special champs

        private readonly AIHeroClient me = ObjectManager.Player;
        public LeeSinInsec()
        {
            Game.OnUpdate += GameOnOnUpdate;

            Game.OnWndProc += Game_OnWndProc;
            Obj_AI_Base.OnProcessSpellCast += AiHeroClientOnOnProcessSpellCast;
            Obj_AI_Base.OnPlayAnimation += ObjAiBaseOnOnPlayAnimation;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private void ObjAiBaseOnOnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe && args.Animation == "Spell1b" && LeeSinMenu.insecConfig["_insecKey"].Cast<KeyBind>().CurrentValue)
            {
                //Last Q Enemy still valid if it was before
                if (GetLastQBuffEnemy() != null)
                    QbuffEndTime += 3;//sec
            }
        }

        private int lastInsecCheckTick;
        private void GameOnOnUpdate(EventArgs args)
        {
            GetLastQBuffEnemy(); //update

            bool targetValid = TargetSelector.SelectedTarget != null && TargetSelector.SelectedTarget.IsValid;
            bool allyValid = ally != null && ally.IsValid;

            if (!allyValid || !targetValid)
                return;

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
            return canInsec;
        }

        Obj_AI_Base GetAllyAsWard()
        {
            List<Obj_AI_Base> allyJumps = new List<Obj_AI_Base>();
            #region setVars
            var target = TargetSelector.SelectedTarget;
            if (target == null || !target.IsValid || ally == null || !ally.IsValid)
                return null;

            var wardPlacePos = WardJumpPosition.GetWardJumpPosition(ally, GetLastQBuffEnemy());
            #endregion setVars

            foreach (var allyobj in ObjectManager.Get<Obj_AI_Base>().Where(x => 
                            x.IsValid && x.IsAlly && !x.IsMe && (x is AIHeroClient || x is Obj_AI_Minion)))
            {
                if (allyobj.Distance(wardPlacePos) <= 80 && allyobj.Distance(ally) > ally.Distance(target))
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

            if (moonSecActive)
            {
                var target = TargetSelector.SelectedTarget;
                var wardPlacePos = ally.Position.To2D() + (target.Position.To2D() - ally.Position.To2D()).Normalized()*
                                   (target.Distance(ally) - (float) SpellManager.R.Range/2);
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
            if (ally != null && ally.IsValid)
                new Circle(new ColorBGRA(new Vector4(255, 255, 255, 1)), 100, 2).Draw(ally.Position);

            if (ally == null || TargetSelector.SelectedTarget == null || !ally.IsValid || !TargetSelector.SelectedTarget.IsValid)
            {
                if (LeeSinMenu.insecConfig["_insecKey"].Cast<KeyBind>().CurrentValue)
                    DrawFailInfo();
                return;
            }

            bool couldInsec = TargetSelector.SelectedTarget.IsValid && ally.IsValid && HasResourcesToInsec();
            if (couldInsec)
            {
                var startpos = TargetSelector.SelectedTarget.Position.To2D();
                var endpos = ally.Position.To2D();

                Vector2[] arrows = {
                    endpos + (startpos - endpos).Normalized().Rotated(45 * (float)Math.PI / 180) *
                        TargetSelector.SelectedTarget.BoundingRadius,
                    endpos + (startpos - endpos).Normalized().Rotated(-45 * (float)Math.PI / 180) *
                        TargetSelector.SelectedTarget.BoundingRadius
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
                    WardJumpPosition.GetWardJumpPosition(ally, GetLastQBuffEnemy()).To3D());
            }
        }

        private void DrawFailInfo()
        {
            try
            {
                Text StatusText = new Text("", new Font("Euphemia", 10F, FontStyle.Bold)) {Color = Color.Red};

                if (TargetSelector.SelectedTarget == null || !TargetSelector.SelectedTarget.IsValid)
                    StatusText.TextValue = "No Insec Target Selected";
                else if (ally == null || !ally.IsValid)
                    StatusText.TextValue = "Invalid Insec Ally";
                else if (!HasResourcesToInsec())
                    StatusText.TextValue = "Not enough Spells for Insec";

                StatusText.Position = Player.Instance.Position.WorldToScreen() -
                                      new Vector2((float) StatusText.Bounding.Width/2, -50);

                StatusText.Draw();
            }
            catch { }
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint) WindowMessages.LeftButtonDown)
            {
                var allyy = ObjectManager.Get<Obj_AI_Base>().Where(x => !x.IsMe && x.IsAlly && x.IsValid && 
                (x is AIHeroClient || x is Obj_AI_Turret)).OrderBy(x => x.Distance(Game.CursorPos)).FirstOrDefault
                    (x => x.Distance(Game.CursorPos) <= 200 && !x.IsMe && x.IsValid);
                if (allyy != null && allyy.IsValid)
                {
                    ally = allyy;
                }
            }
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

        private static AIHeroClient lastEnemyWithQBuff;
        private static float QbuffEndTime;
        static AIHeroClient GetLastQBuffEnemy()
        {
            var currentEnemyWithQBuff = ObjectManager.Get<Obj_AI_Base>()
                .FirstOrDefault(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne")) as AIHeroClient;
            if (currentEnemyWithQBuff != null)
            {
                lastEnemyWithQBuff = currentEnemyWithQBuff;
                QbuffEndTime = lastEnemyWithQBuff.GetBuff("BlindMonkQOne").EndTime;
            }
            if (lastEnemyWithQBuff != null && Game.Time >= QbuffEndTime)
                lastEnemyWithQBuff = null;

            return lastEnemyWithQBuff;
        }

        private void CheckWardFlashKick(Vector2 wardPlacePos, AIHeroClient target)
        {
            var allyJump = GetAllyAsWard();
            var allyJumpValid = allyJump != null && allyJump.IsValid;
            var canFlash = SpellManager.FlashReady;
            var canWardJump = (WardManager.CanCastWard || allyJumpValid) &&
                                me.Mana >= minEnegeryFirstActivation;
            float maxWardJumpDist = !allyJumpValid ? WardManager.WardRange : SpellManager.W1.Range;
            

            bool inRange = me.Distance(wardPlacePos) <= SpellManager.Flash.Range + maxWardJumpDist;
            


            float distQTargetToWardPos = GetLastQBuffEnemy() != null ? 
                GetLastQBuffEnemy().Distance(wardPlacePos) : float.MaxValue;
            float maxRange = canWardJump ? 600 : 425;
            if (maxRange > 0 && allyJumpValid) maxRange = SpellManager.W1.Range;

            bool dontNeedFlash = distQTargetToWardPos <= maxRange && canWardJump;
            bool waitForQFirst = SpellManager.CanCastQ1 && /*me.Mana >= minEnergy &&*/
                                    LeeSinMenu.insecConfig["waitForQBefore_WardFlashKick"].Cast<CheckBox>().CurrentValue;
            if (inRange && canWardJump && canFlash && !dontNeedFlash && !InsecSolution.GotASolution &&
                !waitForQFirst)
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
            var wardPlacePos = ally.Position.To2D() + (target.Position.To2D() - ally.Position.To2D());

            var canFlash = SpellManager.FlashReady;
            var canWardJump = SpellManager.CanCastW1 && me.Mana >= minEnegeryFirstActivation &&
                              WardManager.CanCastWard;
            float distToWardPlacePos = GetLastQBuffEnemy() != null && GetLastQBuffEnemy().IsValid
                ? GetLastQBuffEnemy().Distance(wardPlacePos)
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
                return;
            }
        }

        private void CheckInsec()
        {
            if (!me.Spellbook.GetSpell(SpellSlot.Q).Name.Contains("One") && Game.Time - WardManager._lastWardJumpTime > 1.5f)
                SpellManager.Q2.Cast();
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            #region setVars
                var target = TargetSelector.SelectedTarget;
            

                var canFlash = SpellManager.FlashReady;
                var canWardJump = SpellManager.CanCastW1 && me.Mana >= minEnegeryFirstActivation &&
                                  WardManager.CanCastWard;
                var canQ = SpellManager.CanCastQ1 && me.Mana >= minEnergy;
            var wardPlacePos = WardJumpPosition.GetWardJumpPosition(ally, GetLastQBuffEnemy());
            #endregion setVars

            /*normal q cast on enemy*/
            var qPred = SpellManager.Q1.GetPrediction(target);
            if (qPred.HitChance >= HitChance.High && canQ)
                SpellManager.Q1.Cast(qPred.CastPosition);

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

            CheckWardFlashKick(wardPlacePos, target);

            if (canQ && !InsecSolution.GotASolution)
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

                var targetMinion = minList.OrderBy(x => x.Distance(target)).FirstOrDefault();
                if (targetMinion != null && targetMinion.IsValid)
                {
                    if (targetMinion.Distance(wardPlacePos) <= WardManager.WardRange && canWardJump)
                    {
                        SpellManager.Q1.Cast(targetMinion.Position);
                    }

                    if (targetMinion.Distance(wardPlacePos) <= SpellManager.Flash.Range && canFlash)
                    {
                        SpellManager.Q1.Cast(targetMinion.Position);
                    }
                    /*q minion + wardjump + flash*/
                    if (targetMinion.Distance(wardPlacePos) > SpellManager.Flash.Range &&
                             targetMinion.Distance(wardPlacePos) > WardManager.WardRange && canWardJump && canFlash &&
                             targetMinion.Distance(wardPlacePos) <= SpellManager.Flash.Range + WardManager.WardRange)
                    {
                        SpellManager.Q1.Cast(targetMinion.Position);
                    }
                }
            }
        }
    }
}