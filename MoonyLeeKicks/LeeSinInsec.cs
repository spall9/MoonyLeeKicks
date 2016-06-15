using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace MoonyLeeKicks
{
    internal class LeeSinInsec
    {
        private static Obj_AI_Base ally;
        //TODO: pink ward jump for special champs

        private readonly Menu config;
        private readonly AIHeroClient me = ObjectManager.Player;

        public LeeSinInsec(ref Menu mainMenu)
        {
            config = mainMenu.AddSubMenu("MoonyInsec", "LeeSinInsec");
            config.AddGroupLabel("Insec");
            config.Add("wardDistanceToTarget",
                new Slider("Ward distance to enemy", 230, 50, 300));
            config.Add("attendDashes", new KeyBind("Attend dashes", true, KeyBind.BindTypes.PressToggle));
            config.AddLabel("Only calculates extra range if target has Q Buff. Ignores if jumps over minions");

            config.AddSeparator(10);
            config.Add("_insecKey", new KeyBind("LeeSinInsec Key", false, KeyBind.BindTypes.HoldActive));
            config.Add("moonSec", new CheckBox("Enable MoonSec", false));
            config.AddLabel("^ For Swag purpose only ^");
            config.AddSeparator(10);
            config.AddGroupLabel("Performance");
            config.Add("smoothFps", new CheckBox("Use Multitasking to prevent FPS-Drops", false));
            config.Add("smoothFpsBuffer", new Slider("Buffer Tick", 100, 100, 1000));
            config.AddLabel("Increase this value, if Multitasking is enabled, to gain more Fps");
            config.Add("smoothFpsMaxTasks", new Slider("Maximum Tasks", 100, 1, 1000));
            config.AddLabel("Decrease this value, if Multitasking is enabled, to gain more Fps");
            config.AddSeparator(10);
            config.AddGroupLabel("Drawings");
            config.Add("dashDebug", new KeyBind("Draw WardJump Position", false, KeyBind.BindTypes.HoldActive));

            Game.OnUpdate += GameOnOnUpdate;

            Game.OnWndProc += Game_OnWndProc;
            Obj_AI_Base.OnProcessSpellCast += AiHeroClientOnOnProcessSpellCast;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private Tuple<Vector2, Vector2> dashDebugTuple;
        private int lastInsecCallTick = 0;
        readonly List<Task> InsecTaskList = new List<Task>(); 
        private void GameOnOnUpdate(EventArgs args)
        {
            InsecTaskList.RemoveAll(x => x.IsCompleted);

            if (ally == null || TargetSelector.SelectedTarget == null)
                return;

            CheckDashDebug();

            bool cantInsec = !ally.IsValid || !TargetSelector.SelectedTarget.IsValid || !config["_insecKey"].Cast<KeyBind>().CurrentValue;
            if (cantInsec)
                return;

            bool canInsec = HasResourcesToInsec();
            if (canInsec)
            {
                bool smoothFps = config["smoothFps"].Cast<CheckBox>().CurrentValue;
                int smoothBuffer = config["smoothFpsBuffer"].Cast<Slider>().CurrentValue;
                int maxTasks = config["smoothFpsMaxTasks"].Cast<Slider>().CurrentValue;
                if (smoothFps && InsecTaskList.Count <= maxTasks && Environment.TickCount - lastInsecCallTick >= smoothBuffer)
                {
                    lastInsecCallTick = Environment.TickCount;
                    try
                    {
                        var t = Task.Factory.StartNew(() =>
                        {
                            try { CheckInsec(); } catch { }
                        });
                        InsecTaskList.Add(t);
                    }
                    catch { }
                    
                }
                else if (!smoothFps)
                    CheckInsec();
            }
        }

        private void CheckDashDebug()
        {
            bool dashDebug = config["dashDebug"].Cast<KeyBind>().CurrentValue;
            if (dashDebug)
            {
                var wardPlacePos = ally.Position.To2D() + (TargetSelector.SelectedTarget.Position.To2D() - ally.Position.To2D()).Normalized() *
                                   (TargetSelector.SelectedTarget.Distance(ally) + GetSpaceDistToEnemy());
                dashDebugTuple = new Tuple<Vector2, Vector2>(wardPlacePos, TargetSelector.SelectedTarget.HasAntiInsecDashReady() ?
                    TargetSelector.SelectedTarget.CalculateWardPositionAfterDash(ally, GetSpaceDistToEnemy()) : Vector2.Zero);
            }
        }

        private bool HasResourcesToInsec()
        {
            float minMana = !SpellManager.FlashReady
                                ? me.Spellbook.GetSpell(SpellSlot.Q).SData.Mana +
                                  me.Spellbook.GetSpell(SpellSlot.W).SData.Mana
                                : me.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;

            bool canJump = SpellManager.CanCastW1 || SpellManager.FlashReady || (GetAllyAsWard() != null && GetAllyAsWard().IsValid);
            bool canInsec = canJump && SpellManager.R.IsReady() && me.Mana >= minMana;
            return canInsec;
        }

        Obj_AI_Base GetAllyAsWard()
        {
            List<Obj_AI_Base> allyJumps = new List<Obj_AI_Base>();
            #region setVars
            var target = TargetSelector.SelectedTarget;
            if (target == null || !target.IsValid || ally == null || !ally.IsValid)
                return null;

            var wardPlacePos = ally.Position.To2D() +
                               (target.Position.To2D() - ally.Position.To2D()).Normalized() *
                               (target.Distance(ally) + GetSpaceDistToEnemy());
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
            if (!sender.IsMe || args.Slot != SpellSlot.R || !config["_insecKey"].Cast<KeyBind>().CurrentValue)
                return;

            if (moonSecActive)
            {
                var target = TargetSelector.SelectedTarget;
                var wardPlacePos = ally.Position.To2D() + (target.Position.To2D() - ally.Position.To2D()).Normalized() *
                               (target.Distance(ally) - (float)SpellManager.R.Range / 2);
                var flashPos = wardPlacePos.Extend(target, SpellManager.Flash.Range);

                Core.DelayAction(() =>
                {
                    SpellManager.Flash.Cast(flashPos.To3D());
                    moonSecActive = false;
                    ally = null;
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
            if (ally != null && ally.IsValid)
                new Circle(new ColorBGRA(new Vector4(255, 255, 255, 1)), 100, 2).Draw(ally.Position);

            if (ally == null || TargetSelector.SelectedTarget == null)
            {
                if (config["_insecKey"].Cast<KeyBind>().CurrentValue)
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
            else if (config["_insecKey"].Cast<KeyBind>().CurrentValue)
                DrawFailInfo();

            bool dashDebug = config["dashDebug"].Cast<KeyBind>().CurrentValue;
            if (dashDebug && dashDebugTuple != null)
            {
                new Circle(new ColorBGRA(new Vector4(0, 0, 255, 1)), 70, 4).Draw(dashDebugTuple.Item1.To3D());
                new Circle(new ColorBGRA(new Vector4(0, 0, 255, 1)), 70, 4).Draw(dashDebugTuple.Item2.To3D());
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

        private static void Game_OnWndProc(WndEventArgs args)
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

        private bool CanWardKick(Vector2 wardPlacePos, AIHeroClient target)
        {
            var allyJump = GetAllyAsWard();
            var allyJumpValid = allyJump != null && allyJump.IsValid;
            var canWardJump = (WardManager.CanCastWard || allyJumpValid) && SpellManager.CanCastW1 &&
                me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
            float maxDist = WardManager.CanCastWard ? WardManager.WardRange : SpellManager.W1.Range;

            if (me.Distance(wardPlacePos) <= maxDist && canWardJump)
            {
                if (WardManager.CanCastWard)
                    WardManager.CastWardTo(wardPlacePos.To3D());
                else
                    SpellManager.W1.Cast(allyJump);
                Core.RepeatAction(() => SpellManager.R.Cast(target), 350, 1500);
                ally = null;
                return true;
            }

            return false;
        }

        private bool CanFlashKick(Vector2 wardPlacePos, AIHeroClient target)
        {
            var canFlash = SpellManager.FlashReady;

            if (me.Distance(wardPlacePos) <= SpellManager.Flash.Range && canFlash)
            {
                SpellManager.Flash.Cast(wardPlacePos.To3D());
                Core.RepeatAction(() => SpellManager.R.Cast(target), 150, 1500);
                ally = null;
                return true;
            }

            return false;
        }

        private bool CanWardFlashKick(Vector2 wardPlacePos)
        {
            var allyJump = GetAllyAsWard();
            var allyJumpValid = allyJump != null && allyJump.IsValid;
            var canWardJump = (WardManager.CanCastWard || allyJumpValid) &&
                                me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
            float maxWardJumpDist = allyJump == null ? WardManager.WardRange : SpellManager.W1.Range;

            var canFlash = SpellManager.FlashReady;

            bool inRange = me.Distance(wardPlacePos) <= SpellManager.Flash.Range + maxWardJumpDist;


            bool q1Casted = SpellManager.CanCastQ2;
            float distQTargetToWardPos =
                ObjectManager.Get<Obj_AI_Base>().Any(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne"))
                    ? ObjectManager.Get<Obj_AI_Base>().First(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne")).Distance(wardPlacePos)
                    : float.MaxValue;
            float maxRange = canWardJump ? 600 : 425;
            if (maxRange > 0 && allyJumpValid) maxRange = SpellManager.W1.Range;

            bool dontNeedFlash = q1Casted && distQTargetToWardPos <= maxRange && canWardJump;

            if (inRange && canWardJump && canFlash && !dontNeedFlash)
            {
                if (WardManager.CanCastWard)
                    WardManager.CastWardTo(wardPlacePos.To3D());
                else /*flash -> ally w to reach wardPos*/
                    SpellManager.Flash.Cast(wardPlacePos.To3D());
                return true;
            }

            return false;
        }

        private bool moonSecActive = false;
        private bool CanMoonSec(AIHeroClient target)
        {
            var wardPlacePos = ally.Position.To2D() + (target.Position.To2D() - ally.Position.To2D());

            var canFlash = SpellManager.FlashReady;
            var canWardJump = SpellManager.CanCastW1 && me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana &&
                              WardManager.CanCastWard;

            if (me.Distance(wardPlacePos) <= WardManager.WardRange && canWardJump && canFlash)
            {
                moonSecActive = true;
                WardManager.CastWardTo(wardPlacePos.To3D());
                Core.RepeatAction(() =>
                {
                    if (me.Distance(target) <= (float)SpellManager.Flash.Range/2)
                        SpellManager.R.Cast(target);
                    else
                        moonSecActive = false;
                }, 350, 1500);
                return true;
            }

            return false;
        }

        float GetSpaceDistToEnemy()
        {
            return config["wardDistanceToTarget"].Cast<Slider>().CurrentValue;
        }

        private void CheckInsec()
        {
            if (!me.Spellbook.GetSpell(SpellSlot.Q).Name.Contains("One"))
                SpellManager.Q2.Cast();
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            #region setVars
                var target = TargetSelector.SelectedTarget;
            

                var canFlash = SpellManager.FlashReady;
                var canWardJump = SpellManager.CanCastW1 && me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana &&
                                  WardManager.CanCastWard;
                var canQ = SpellManager.CanCastQ1;
                var wardPlacePos = ally.Position.To2D() + (target.Position.To2D() - ally.Position.To2D()).Normalized()*
                                   (target.Distance(ally) + GetSpaceDistToEnemy());
            #endregion setVars

            bool hasQBuff = target.HasBuff("BlindMonkQOne");
            bool attendDash = config["attendDashes"].Cast<KeyBind>().CurrentValue;
            bool hasDash = target.HasAntiInsecDashReady();
            if (hasDash && attendDash && hasQBuff)
                wardPlacePos = target.CalculateWardPositionAfterDash(ally, GetSpaceDistToEnemy());

            /*normal q cast on enemy*/
            var qPred = SpellManager.Q1.GetPrediction(target);
            if (qPred.HitChance >= HitChance.High && canQ)
                SpellManager.Q1.Cast(qPred.CastPosition);

            /*try insec if possible*/
            if (CanMoonSec(target) && config["moonSec"].Cast<CheckBox>().CurrentValue)
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
                float qflyTime;
                foreach (
                    var minion in
                        EntityManager.MinionsAndMonsters.Combined.Where(
                            x => !x.IsAlly && SpellManager.Q1.GetPrediction(x).CollisionObjects.Length == 0))
                {
                    qflyTime = me.Distance(minion)/SpellManager.Q1.Speed*1000 + SpellManager.Q1.CastDelay*2 +
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