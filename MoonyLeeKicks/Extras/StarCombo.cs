using System;
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
    internal class StarCombo
    {
        enum MenuEntry
        {
            UseAlly,
            UseWard,
            UseFlash,
            CorrectPos,
            MovementPrediction
        }

        bool Enabled(MenuEntry entry)
        {
            switch (entry)
            {
                case MenuEntry.UseAlly:
                    return LeeSinMenu.starComboMenu["starComboUseAlly"].Cast<CheckBox>().CurrentValue;
                case MenuEntry.UseWard:
                    return LeeSinMenu.starComboMenu["starComboUseWard"].Cast<CheckBox>().CurrentValue;
                case MenuEntry.UseFlash:
                    return LeeSinMenu.starComboMenu["starComboUseFlash"].Cast<CheckBox>().CurrentValue;
                case MenuEntry.CorrectPos:
                    return LeeSinMenu.starComboMenu["correctStarCombo"].Cast<CheckBox>().CurrentValue;
                case MenuEntry.MovementPrediction:
                    return LeeSinMenu.starComboMenu["starComboMovementPrediction"].Cast<CheckBox>().CurrentValue;
            }

            return false;
        }

        enum StarSolutions
        {
            None,
            Ally,
            Ward,
            Flash
        }

        StarSolutions solution = StarSolutions.None;
        bool CanContinueWith(StarSolutions s)
        {
            return s == solution;
        }

        private AIHeroClient me;
        public StarCombo()
        {
            me = ObjectManager.Player;
            Game.OnUpdate += GameOnOnUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;
            AIHeroClient.OnProcessSpellCast += AiHeroClientOnOnProcessSpellCast;
        }

        private string errorString = string.Empty;
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

        private void AiHeroClientOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || args.Slot != SpellSlot.R || !LeeSinMenu.starComboMenu["starComboKey"].Cast<KeyBind>().CurrentValue)
                return;

            var canQ = SpellManager.CanCastQ1;
            if (canQ)
            {
                Core.DelayAction(() => SpellManager.Q1.Cast(args.Target.Position), 250);
            }
            if (SpellManager.ExhaustReady)
            {
                Core.DelayAction(() => SpellManager.Exhaust.Cast(args.Target as Obj_AI_Base), 500);
            }
            if (SpellManager.SmiteReady)
            {
                Core.DelayAction(() => SpellManager.Smite.Cast(args.Target as Obj_AI_Base), 500);
            }
        }

        private bool canWardJump => WardManager.CanWardJump;
        private bool canFlash = SpellManager.FlashReady;

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

                return EntityManager.Heroes.Allies.Any(x => x.IsValid && x.Distance(me) <= SpellManager.W1.Range && x.Distance(target) < SpellManager.R.Range) && me.Mana >= 50 &&
                    SpellManager.CanCastW1;
            }
        }

        private bool hasResources => (SpellManager.R.IsReady() && SpellManager.CanCastQ1 && (canWardJump || canAllyJump || canFlash)) || solution != StarSolutions.None;


        Vector2 PointOnCircle(float radius, float angleInDegrees, Vector2 origin)
        {
            float x = origin.X + (float)(radius * Math.Cos(angleInDegrees * Math.PI / 180));
            float y = origin.Y + (float)(radius * Math.Sin(angleInDegrees * Math.PI / 180));

            return new Vector2(x, y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wardJumping"></param>
        /// <param name="predictPos">if wardjumping / else flashing</param>
        /// <returns></returns>
        Vector2 GetJumpPos(bool wardJumping, bool predictPos = false)
        {
            float maxDist = wardJumping ? WardManager.WardRange : SpellManager.Flash.Range;

            Vector2 targetPos = target.Position.To2D();
            if (predictPos)
            {
                float predictionDist = me.Distance(target) >= WardManager.WardRange
                    ? WardManager.WardRange
                    : me.Distance(target);
                float wSpeed = 2000;
                float flyTime = predictionDist/wSpeed;
                Vector2 predPos = Prediction.Position.PredictUnitPosition(target, (int) flyTime);
                targetPos = predPos;
            }

            if (LeeSinMenu.starComboMenu["starComboMultiR"].Cast<CheckBox>().CurrentValue)
            {
                float leeSinRKickDistance = 700;
                float leeSinRKickWidth = 100;
                var minREnemies = LeeSinMenu.starComboMenu["starComboMultiRHitCount"].Cast<Slider>().CurrentValue;

                Vector2 vecToRotate = new Vector2(200, 0);
                Vector2 op = targetPos;

                for (int currentAngle = 0; currentAngle <= 359; currentAngle++)
                {
                    float stdAngle = 0;
                    Vector2 rotatedWardPos = PointOnCircle(vecToRotate.Length(), stdAngle + currentAngle, op);
                    Vector2 kickEndPos = targetPos +
                                         (targetPos - rotatedWardPos).Normalized()*leeSinRKickDistance;
                    var rect = new Geometry.Polygon.Rectangle(targetPos, kickEndPos, leeSinRKickWidth);

                    if (EntityManager.Heroes.Enemies.Count(x => x.IsValid && rect.IsInside(x)) >= minREnemies &&
                        rotatedWardPos.Distance(me) <= maxDist)
                    {
                        targetPos = rotatedWardPos;
                    }
                }
            }

            return targetPos;
        }

        private void GameOnOnUpdate(EventArgs args)
        {
            if (!LeeSinMenu.starComboMenu["starComboKey"].Cast<KeyBind>().CurrentValue)
            {
                errorString = string.Empty;
                return;
            }

            if (target.HasBuff("BlindMonkQOne") && SpellManager.CanCastQ2)
                SpellManager.Q2.Cast();

            if (!hasResources)
            {
                if (!SpellManager.R.IsReady())
                    errorString = "R not ready";
                else if ((!WardManager.CanCastWard || !SpellManager.FlashReady) && !canAllyJump)
                    errorString = "Cannot dash";
                else if (canAllyJump && (!SpellManager.CanCastW1 || me.Mana < 50))
                    errorString = "Cannot jump to ally";

                return;
            }
            else if (!SelectionHandler.LastTargetValid)
            {
                errorString = "Target not selected";
                return;
            }
            else errorString = string.Empty;

            Orbwalker.OrbwalkTo(Game.CursorPos);

            if (solution == StarSolutions.None)
                SearchForSolution();
            else
                Core.DelayAction(() =>
                {
                    if (solution != StarSolutions.None)
                        solution = StarSolutions.None;
                }, 4000);

            if (CanContinueWith(StarSolutions.Ally))
            {
                if (SpellManager.CanCastW1 && me.Mana >= 50)
                {
                    var ally =
                        EntityManager.Heroes.Allies.Where(
                            x =>
                                x.IsValid && x.Distance(me) <= SpellManager.W1.Range &&
                                x.Distance(target) < SpellManager.R.Range)
                            .OrderBy(x => x.Distance(target))
                            .First();
                    SpellManager.W1.Cast(ally);
                }

                if (target.Distance(me) <= SpellManager.R.Range)
                {
                    SpellManager.R.Cast(target);
                }
            }
            else if (CanContinueWith(StarSolutions.Ward))
            {
                if (canWardJump)
                {
                    var jumpPos = GetJumpPos(true, Enabled(MenuEntry.MovementPrediction)).To3D();
                    WardManager.CastWardTo(jumpPos);
                }

                if (target.Distance(me) <= SpellManager.R.Range)
                {
                    SpellManager.R.Cast(target);
                }
            }
            else if (CanContinueWith(StarSolutions.Flash))
            {
                if (canFlash)
                {
                    SpellManager.Flash.Cast(GetJumpPos(false).To3D());
                }

                if (target.Distance(me) <= SpellManager.R.Range)
                {
                    SpellManager.R.Cast(target);
                }
            }
        }

        private void SearchForSolution()
        {
            if (canAllyJump && Enabled(MenuEntry.UseAlly))
                solution = StarSolutions.Ally;
            else if (canWardJump && target.Distance(me) <= WardManager.WardRange && Enabled(MenuEntry.UseWard))
                solution = StarSolutions.Ward;
            else if (canFlash && target.Distance(me) <= SpellManager.Flash.Range && Enabled(MenuEntry.UseFlash))
                solution = StarSolutions.Flash;
            else if ((canWardJump || canAllyJump) && canFlash && Enabled(MenuEntry.UseFlash))//maybe flash + ward?
            {
                var ally =
                        EntityManager.Heroes.Allies.Where(
                            x => x.IsValid && x.Distance(target) < SpellManager.R.Range)
                            .OrderBy(x => x.Distance(me)).FirstOrDefault();
                bool allyValid = ally != null && !ally.Equals(default(AIHeroClient)) && ally.IsValid;
                if (allyValid && ally.Distance(me) <= SpellManager.W1.Range + SpellManager.Flash.Range && Enabled(MenuEntry.UseAlly))
                {
                    SpellManager.Flash.Cast(me.Position.Extend(ally, SpellManager.Flash.Range).To3D());
                }
                else if (target.Distance(me) <= WardManager.WardRange + SpellManager.Flash.Range && canWardJump &&
                         Enabled(MenuEntry.UseWard))
                {
                    WardManager.CastWardTo(me.Position.Extend(target, WardManager.WardRange).To3D());
                }
            }
        }
    }
}
