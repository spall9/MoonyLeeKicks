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

namespace MoonyLeeKicks.Insec
{
    internal class LeeSinInsec
    {
        private static class WardJumpPosition
        {
            static float GetSpaceDistToEnemy(float extraRange = 0)
            {
                return LeeSinMenu.insecMenu["wardDistanceToTarget"].Cast<Slider>().CurrentValue + extraRange;
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

            public static Vector2 GetWardJumpPosition(Vector2 allyPos)
            {
                AIHeroClient target = SelectionHandler.GetTarget;

                Vector2 wardPlacePos = allyPos + (target.Position.To2D() - allyPos).Normalized() *
                                   (target.Distance(allyPos) + GetSpaceDistToEnemy());

                bool useMovementPrediction =
                    LeeSinMenu.insecExtensionsMenu["useMovementPrediction"].Cast<CheckBox>().CurrentValue;
                if (useMovementPrediction)
                {
                    var predPos = Prediction.Position.PredictUnitPosition(target, 1000);
                    if (predPos.Distance(wardPlacePos) > target.Distance(wardPlacePos))
                        wardPlacePos = allyPos + (target.Position.To2D() - allyPos).Normalized() *
                                   (target.Distance(allyPos) + GetSpaceDistToEnemy(100));
                }

                Vector3 targetPosition = allyPos.To3D() +
                                         (wardPlacePos.To3D() - allyPos.To3D()).Normalized() *
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
                                             (targetPosition.To2D() - rotatedWardPos).Normalized() * leeSinRKickDistance;
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

        /// <summary>
        /// 2 activations
        /// </summary>
        private static int minEnergy = 80;
        private static int minEnegeryFirstActivation = 50;
        private static int lastInsecCheckTick, LastQ1CastTick;
        private bool moonSecActive;
        private bool _decidedWardKick;
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

        private void GameOnOnUpdate(EventArgs args)
        {
            if (!LeeSinMenu.insecMenu["_insecKey"].Cast<KeyBind>().CurrentValue)
                return;

            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            bool targetValid = TargetSelector.SelectedTarget != null && TargetSelector.SelectedTarget.IsValid;
            bool allyValid = SelectionHandler.LastAllyPosValid;

            if (!allyValid || !targetValid)
                return;

            int updateFrequency = LeeSinMenu.insecMenu["insecFrequency"].Cast<Slider>().CurrentValue;
            if (HasResourcesToInsec() && Environment.TickCount - lastInsecCheckTick >= updateFrequency)
            {
                lastInsecCheckTick = Environment.TickCount;
                CheckInsec();
            }
        }

        private static void ObjAiBaseOnOnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (args.Animation == "Spell1a")
                LastQ1CastTick = Environment.TickCount;

            if (args.Animation == "Spell1b")
            {
                //Last Q Enemy still valid if it was before
                if (GetLastQBuffEnemyHero() != null)
                {
                    QbuffEndTime_hero += 3; //sec
                    extentedQ_hero = true;
                }
            }

            if (args.Animation == "Spell2a")
                ;
        }

        private static AIHeroClient lastEnemyWithQBuff_hero;
        static Obj_AI_Base lastEnemyWithQBuff_object;
        private static float QbuffEndTime_hero, QbuffEndTime_object;
        private static bool extentedQ_hero, extentedQ_object;
        static AIHeroClient GetLastQBuffEnemyHero()
        {
            var currentEnemyWithQBuff = ObjectManager.Get<Obj_AI_Base>()
                .FirstOrDefault(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne")) as AIHeroClient;
            if (currentEnemyWithQBuff != null)
            {
                lastEnemyWithQBuff_hero = currentEnemyWithQBuff;
                QbuffEndTime_hero = lastEnemyWithQBuff_hero.GetBuff("BlindMonkQOne").EndTime;
            }

            if (extentedQ_hero && lastEnemyWithQBuff_hero != null &&
                lastEnemyWithQBuff_hero.Distance(ObjectManager.Player) <= 80)
            {
                QbuffEndTime_hero = 0;
            }

            if (lastEnemyWithQBuff_hero != null && Game.Time >= QbuffEndTime_hero)
            {
                lastEnemyWithQBuff_hero = null;
                extentedQ_hero = false;
            }

            return lastEnemyWithQBuff_hero;
        }

        Obj_AI_Base GetLastQBuffEnemyObject()
        {
            var currentEnemyWithQBuff = ObjectManager.Get<Obj_AI_Base>()
                .FirstOrDefault(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne"));
            if (currentEnemyWithQBuff != null)
            {
                lastEnemyWithQBuff_object = currentEnemyWithQBuff;
                QbuffEndTime_object = lastEnemyWithQBuff_object.GetBuff("BlindMonkQOne").EndTime;
            }

            if (extentedQ_object && lastEnemyWithQBuff_object != null &&
                lastEnemyWithQBuff_object.Distance(ObjectManager.Player) <= 80)
            {
                QbuffEndTime_object = 0;
            }

            if (lastEnemyWithQBuff_object != null && Game.Time >= QbuffEndTime_object)
            {
                lastEnemyWithQBuff_object = null;
                extentedQ_object = false;
            }

            return lastEnemyWithQBuff_object;
        }

        private bool HasResourcesToInsec()
        { 
            var allyAsWard = GetAllyAsWard();
            bool canJump = (SpellManager.CanCastW1 && (WardManager.CanCastWard || allyAsWard != null)) || 
                (SpellManager.FlashReady && !LeeSinMenu.insecExtensionsMenu["dontFlash"].Cast<CheckBox>().CurrentValue);

            bool canInsec = canJump && SpellManager.R.IsReady();
            return canInsec && SelectionHandler.LastAllyPosValid && SelectionHandler.LastTargetValid;
        }

        Obj_AI_Base GetAllyAsWard()
        {
            List<Obj_AI_Base> allyJumps = new List<Obj_AI_Base>();
            if (!SelectionHandler.LastTargetValid || !SelectionHandler.LastAllyPosValid)
                return null;
            
            #region setVars
            var target = SelectionHandler.GetTarget;
            var allyPos = SelectionHandler.GetAllyPos;

            var wardPlacePos = WardJumpPosition.GetWardJumpPosition(allyPos);
            #endregion setVars

            foreach (var allyobj in ObjectManager.Get<Obj_AI_Base>().Where(x =>
                            x.IsValid && x.IsAlly && !x.IsMe && (x is AIHeroClient || x is Obj_AI_Minion)))
            {
                if (allyobj.Distance(wardPlacePos) <= 80)
                {
                    allyJumps.Add(allyobj);
                }
            }

            Obj_AI_Base obj = allyJumps.Any() ? allyJumps.OrderBy(x => x.Distance(wardPlacePos)).First() : null;
            return obj;
        }

        private void AiHeroClientOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && LeeSinMenu.insecMenu["_insecKey"].Cast<KeyBind>().CurrentValue &&
                args.SData.Name.ToLower().Contains("wone"))
            {
                int wspeed = 2000;
            }

            if (!sender.IsMe || args.Slot != SpellSlot.R || !LeeSinMenu.insecMenu["_insecKey"].Cast<KeyBind>().CurrentValue)
                return;

            if (moonSecActive)
            {
                var target = TargetSelector.SelectedTarget;
                var wardPlacePos = SelectionHandler.GetAllyPos + (target.Position.To2D() - SelectionHandler.GetAllyPos).Normalized() *
                               (target.Distance(SelectionHandler.GetAllyPos) - (float)SpellManager.R.Range / 2);
                var flashPos = wardPlacePos.Extend(target, SpellManager.Flash.Range);

                Core.DelayAction(() =>
                {
                    SpellManager.Flash.Cast(flashPos.To3D());
                    moonSecActive = false;
                }, 80);
            }

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
            if (SelectionHandler.LastAllyPosValid)
                new Circle(new ColorBGRA(new Vector4(255, 255, 255, 1)), 100, 2).Draw(SelectionHandler.GetAllyPos.To3D());

            if (!SelectionHandler.LastTargetValid || !SelectionHandler.LastAllyPosValid)
            {
                if (LeeSinMenu.insecMenu["_insecKey"].Cast<KeyBind>().CurrentValue)
                    DrawFailInfo();
                return;
            }

            bool couldInsec = SelectionHandler.LastTargetValid && SelectionHandler.LastAllyPosValid && HasResourcesToInsec();
            if (couldInsec)
            {
                var startpos = SelectionHandler.GetTarget.Position.To2D();
                var endpos = SelectionHandler.GetAllyPos;

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
            else if (LeeSinMenu.insecMenu["_insecKey"].Cast<KeyBind>().CurrentValue)
                DrawFailInfo();

            bool dashDebug = LeeSinMenu.insecMenu["dashDebug"].Cast<KeyBind>().CurrentValue;
            if (dashDebug && SelectionHandler.LastAllyPosValid)
            {
                new Circle(new ColorBGRA(new Vector4(0, 0, 1, 1)), 70, 4).Draw(
                    WardJumpPosition.GetWardJumpPosition(SelectionHandler.GetAllyPos).To3D());
            }
        }

        private void DrawFailInfo()
        {
            try
            {
                Text StatusText = new Text("", new Font("Euphemia", 10F, FontStyle.Bold)) { Color = Color.Red };

                if (TargetSelector.SelectedTarget == null || !TargetSelector.SelectedTarget.IsValid)
                    StatusText.TextValue = "No Insec Target Selected";
                else if (!SelectionHandler.LastAllyPosValid)
                    StatusText.TextValue = "Invalid Insec Ally";
                else if (!HasResourcesToInsec())
                    StatusText.TextValue = "Not enough Spells for Insec";

                StatusText.Position = Player.Instance.Position.WorldToScreen() -
                                      new Vector2((float)StatusText.Bounding.Width / 2, -50);

                StatusText.Draw();
            }
            catch { }
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDown)
            {
                var allyy = ObjectManager.Get<Obj_AI_Base>().Where(x => !x.IsMe && x.IsAlly && x.IsValid &&
                (x is AIHeroClient || x is Obj_AI_Turret)).OrderBy(x => x.Distance(Game.CursorPos)).FirstOrDefault
                    (x => x.Distance(Game.CursorPos) <= 200 && !x.IsMe && x.IsValid);
                if (allyy != null && allyy.IsValid)
                {
                }
            }
        }

        private bool decidedWardKick
        {
            get { return _decidedWardKick; }
            set
            {
                _decidedWardKick = value;
                if (!value)
                    LastWardJumpInsecPosition = default(Vector2);
            }
        }

        private Vector2 LastWardJumpInsecPosition;
        private bool CanWardKick(Vector2 wardPlacePos, AIHeroClient target)
        {
            var allyJump = GetAllyAsWard();
            var allyJumpValid = allyJump != null;
            var canWardJump = (WardManager.CanCastWard || allyJumpValid) && SpellManager.CanCastW1 &&
                me.Mana >= minEnegeryFirstActivation;
            float maxDist = WardManager.CanCastWard ? WardManager.WardRange : SpellManager.W1.Range;

            if (me.Distance(wardPlacePos) <= maxDist && canWardJump)
            {
                decidedWardKick = true;
                LastWardJumpInsecPosition = wardPlacePos;
                //Chat.Print("wardKick");
                if (WardManager.CanCastWard)
                    WardManager.CastWardTo(wardPlacePos.To3D());
                else
                    SpellManager.W1.Cast(allyJump);
                Core.RepeatAction(() => SpellManager.R.Cast(target), 350, 1500);

                Core.DelayAction(() => decidedWardKick = false, 1000);
                return true;
            }

            return false;
        }

        private bool CanFlashKick(Vector2 wardPlacePos, AIHeroClient target)
        {
            if (decidedWardKick)
                return false;

            bool cantWardKick = !CanWardKick(wardPlacePos, target);
            var canFlash = SpellManager.FlashReady;

            if (me.Distance(wardPlacePos) <= SpellManager.Flash.Range && canFlash && cantWardKick)
            {
                //Chat.Print("flashKick");
                if (!LeeSinMenu.insecExtensionsMenu["dontFlash"].Cast<CheckBox>().CurrentValue)
                {
                    SpellManager.Flash.Cast(wardPlacePos.To3D());
                    Core.RepeatAction(() => SpellManager.R.Cast(target), 150, 1500);
                    return true;
                }
            }

            return false;
        }

        private bool CanWardFlashKick(Vector2 wardPlacePos)
        {
            var allyJump = GetAllyAsWard();
            var allyJumpValid = allyJump != null;
            var canFlash = SpellManager.FlashReady;
            var canWardJump = (WardManager.CanCastWard || allyJumpValid) &&
                                me.Mana >= minEnegeryFirstActivation && SpellManager.W1.IsReady();
            float maxWardJumpDist = !allyJumpValid ? WardManager.WardRange : SpellManager.W1.Range;


            bool inRange = me.Distance(wardPlacePos) <= SpellManager.Flash.Range + maxWardJumpDist;



            float distQTargetToWardPos = GetLastQBuffEnemyObject() != null && GetLastQBuffEnemyObject().IsValid ?
                GetLastQBuffEnemyObject().Distance(wardPlacePos) : float.MaxValue;

            bool dontNeedFlash = distQTargetToWardPos <= maxWardJumpDist;
            bool waitForQFirst = (SpellManager.CanCastQ1 || Environment.TickCount - LastQ1CastTick < 1000) &&
                                    LeeSinMenu.insecExtensionsMenu["waitForQBefore_WardFlashKick"].Cast<CheckBox>().CurrentValue;
            bool onlyUseWithQ = GetLastQBuffEnemyObject() == null &&
                LeeSinMenu.insecExtensionsMenu["WardFlashKickOnlyWithQ"].Cast<CheckBox>().CurrentValue;

            if (inRange && canWardJump && canFlash && !dontNeedFlash &&
                !waitForQFirst && !onlyUseWithQ)
            {
                //Chat.Print("wardFLashKick");
                if (WardManager.CanCastWard)
                    if (!LeeSinMenu.insecExtensionsMenu["dontFlash"].Cast<CheckBox>().CurrentValue)
                    {
                        WardManager.CastWardTo(wardPlacePos.To3D());
                        return true;
                    }
                else /*flash -> ally w to reach wardPos*/ if (!LeeSinMenu.insecExtensionsMenu["dontFlash"].Cast<CheckBox>().CurrentValue)
                {
                    SpellManager.Flash.Cast(wardPlacePos.To3D());
                    return true;
                }
            }

            return false;
        }

        private bool CanMoonSec(AIHeroClient target)
        {
            if (!LeeSinMenu.insecMenu["moonSec"].Cast<CheckBox>().CurrentValue)
                return false;

            var wardPlacePos = SelectionHandler.GetAllyPos + (target.Position.To2D() - SelectionHandler.GetAllyPos);

            var canFlash = SpellManager.FlashReady;
            var canWardJump = SpellManager.CanCastW1 && me.Mana >= minEnegeryFirstActivation &&
                              WardManager.CanCastWard;

            if (me.Distance(wardPlacePos) <= WardManager.WardRange && canWardJump && canFlash)
            {
                moonSecActive = true;
                WardManager.CastWardTo(wardPlacePos.To3D());
                Core.RepeatAction(() =>
                {
                    if (me.Distance(target) <= (float)SpellManager.Flash.Range / 2)
                        SpellManager.R.Cast(target);
                    else
                        moonSecActive = false;
                }, 350, 1500);
                return true;
            }

            return false;
        }

        private void CheckInsec()
        {
            if (!me.Spellbook.GetSpell(SpellSlot.Q).Name.Contains("One"))
                SpellManager.Q2.Cast();

            #region setVars
            var target = TargetSelector.SelectedTarget;


            var canFlash = SpellManager.FlashReady;
            var canWardJump = SpellManager.CanCastW1 && me.Mana >= minEnegeryFirstActivation &&
                              WardManager.CanCastWard;
            var canQ = SpellManager.CanCastQ1 && me.Mana >= minEnergy;
            var wardPlacePos = WardJumpPosition.GetWardJumpPosition(SelectionHandler.GetAllyPos);
            bool dontUseFlash = LeeSinMenu.insecExtensionsMenu["dontFlash"].Cast<CheckBox>().CurrentValue;
            #endregion setVars

            /*normal q cast on enemy*/
            var qPred = SpellManager.Q1.GetPrediction(target);
            if (qPred.HitChance >= HitChance.High && canQ)
                SpellManager.Q1.Cast(qPred.CastPosition);


            bool useFlashCorrection = LeeSinMenu.insecExtensionsMenu["useFlashCorrection"].Cast<CheckBox>().CurrentValue;
            int correctionDist = LeeSinMenu.insecExtensionsMenu["flashCorrectionDistance"].Cast<Slider>().CurrentValue;
            if (decidedWardKick && Player.Instance.Distance(LastWardJumpInsecPosition) <= 100 && Player.Instance.Distance(wardPlacePos) > correctionDist 
                && useFlashCorrection)
                decidedWardKick = false;


            /*try insec if possible*/
            if (CanMoonSec(target))
                return;
            if (moonSecActive)
                return;

            if (CanWardKick(wardPlacePos, target))
                return;
            if (CanFlashKick(wardPlacePos, target))
                return;
            if (CanWardFlashKick(wardPlacePos))
                return;


            if (canQ)
            {
                #region searchMinion

                var minList = new List<Obj_AI_Minion>();
                foreach (
                    var minion in
                        EntityManager.MinionsAndMonsters.Combined.Where(
                            x => !x.IsAlly && SpellManager.Q1.GetPrediction(x).CollisionObjects.Length == 0))
                {
                    var qflyTime = me.Distance(minion) / SpellManager.Q1.Speed * 1000 + SpellManager.Q1.CastDelay * 2 +
                                     100;
                    var alive = Prediction.Health.GetPrediction(minion, (int)Math.Ceiling(qflyTime)) >
                                me.GetSpellDamage(minion, SpellSlot.Q) * 2;
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

                    if (targetMinion.Distance(wardPlacePos) <= SpellManager.Flash.Range && canFlash && !dontUseFlash)
                    {
                        SpellManager.Q1.Cast(targetMinion.Position);
                    }
                    /*q minion + wardjump + flash*/
                    if (targetMinion.Distance(wardPlacePos) > SpellManager.Flash.Range &&
                             targetMinion.Distance(wardPlacePos) > WardManager.WardRange && canWardJump && canFlash && !dontUseFlash &&
                             targetMinion.Distance(wardPlacePos) <= SpellManager.Flash.Range + WardManager.WardRange)
                    {
                        SpellManager.Q1.Cast(targetMinion.Position);
                    }
                }
            }
        }
    }
}