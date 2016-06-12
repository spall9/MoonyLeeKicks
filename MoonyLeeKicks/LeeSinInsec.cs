using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace MoonyLeeKicks
{
    internal class LeeSinInsec
    {
        private static AIHeroClient ally;
        //TODO: pink ward jump for special champs

        private readonly Menu config;
        private readonly AIHeroClient me = ObjectManager.Player;

        public LeeSinInsec(ref Menu mainMenu)
        {
            config = mainMenu.AddSubMenu("MoonyInsec", "LeeSinInsec");
            config.AddLabel("Kicks to allies | Uses Ward > Flash");
            config.AddSeparator(10);
            config.Add("wardDistanceToTarget",
                new Slider("Ward distance to enemy", 300, 50, (int) SpellManager.R.Range - 1));
            config.Add("attendDashes", new CheckBox("Attend dashes"));

            config.Add("_insecKey", new KeyBind("LeeSinInsec Key", false, KeyBind.BindTypes.HoldActive));
            config.Add("moonSec", new CheckBox("Enable MoonSec", false));
            config.AddLabel("^ For Swag purpose only ^");

            Game.OnUpdate += GameOnOnUpdate;

            Game.OnWndProc += Game_OnWndProc;
            Obj_AI_Base.OnProcessSpellCast += AiHeroClientOnOnProcessSpellCast;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private void GameOnOnUpdate(EventArgs args)
        {
            if (ally == null || TargetSelector.SelectedTarget == null)
                return;

            bool cantInsec = !ally.IsValid || !TargetSelector.SelectedTarget.IsValid || !config["_insecKey"].Cast<KeyBind>().CurrentValue;
            if (cantInsec)
                return;

            float minMana = !SpellManager.FlashReady
                    ? me.Spellbook.GetSpell(SpellSlot.Q).SData.Mana +
                      me.Spellbook.GetSpell(SpellSlot.W).SData.Mana
                    : me.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;

            bool canJump = SpellManager.CanCastW1 || SpellManager.FlashReady || GetAllyAsWard() != null;

            if (canJump && SpellManager.R.IsReady() && me.Mana >= minMana)
            {
                CheckInsec();
            }
        }

        Obj_AI_Base GetAllyAsWard()
        {
            List<Obj_AI_Base> allyJumps = new List<Obj_AI_Base>();
            #region setVars
            var target = TargetSelector.SelectedTarget;
            var hasDash = Gapcloser.GapCloserList.Where(spell => spell.ChampName == target.ChampionName)
                .Any(gap => gap.SkillType != Gapcloser.GapcloserType.Targeted && target.Spellbook.GetSpell(gap.SpellSlot).CooldownExpires - Game.Time <= 0);


            var wardDist = config["wardDistanceToTarget"].Cast<Slider>().CurrentValue;
            if (hasDash && target.HasBuff("BlindMonkQOne") && config["attendDashes"].Cast<CheckBox>().CurrentValue)
            {
                List<string> falseGapCloseChamps = new List<string>
                {
                    "Amumu",
                };
                if (!falseGapCloseChamps.Contains(target.ChampionName))
                    wardDist += (int)SpellManager.Flash.Range;
            }
            var wardPlacePos = ally.Position.To2D() +
                               (target.Position.To2D() - ally.Position.To2D()).Normalized() *
                               (target.Distance(ally) + wardDist);
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
            if (!sender.IsMe || args.Slot != SpellSlot.R)
                return;

            if (moonSecActive)
            {
                var target = TargetSelector.SelectedTarget;
                var wardPlacePos = ally.Position.To2D() + (target.Position.To2D() - ally.Position.To2D()).Normalized() *
                               (target.Distance(ally) - (float)SpellManager.R.Range / 2);
                var flashPos = wardPlacePos.Extend(target, SpellManager.Flash.Range);

                Core.RepeatAction(() =>
                {
                    SpellManager.Flash.Cast(flashPos.To3D());
                    moonSecActive = false;
                    ally = null;
                }, 80, 2000);
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
        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            try
            {
                if (ally.IsValid && ally != null)
                    new Circle(new ColorBGRA(new Vector4(255, 255, 255, 1)), 100, 2).Draw(ally.Position);
            }
            catch
            {
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint) WindowMessages.LeftButtonDown)
            {
                var allyy = EntityManager.Heroes.Allies.OrderBy(x => x.Distance(Game.CursorPos)).FirstOrDefault
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
            var canWardJump = WardManager.CanCastWard || allyJump != null;
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
            var canWardJump = SpellManager.CanCastW1 && me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana &&
                              WardManager.CanCastWard;

            if (me.Distance(wardPlacePos) <= SpellManager.Flash.Range && !canWardJump && canFlash)
            {
                SpellManager.Flash.Cast(wardPlacePos.To3D());
                Core.RepeatAction(() => SpellManager.R.Cast(target), 350, 1500);
                ally = null;
                return true;
            }

            return false;
        }

        private bool CanWardFlashKick(Vector2 wardPlacePos)
        {
            var allyJump = GetAllyAsWard();
            var canWardJump = (WardManager.CanCastWard || allyJump != null) && me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
            float maxWardJumpDist = WardManager.CanCastWard ? WardManager.WardRange : SpellManager.W1.Range;

            var canFlash = SpellManager.FlashReady;

            bool inRange = me.Distance(wardPlacePos) <= SpellManager.Flash.Range + maxWardJumpDist;


            bool q1Casted = SpellManager.CanCastQ2 &&
                            ObjectManager.Get<Obj_AI_Base>().Any(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne"));
            float distQTargetToWardPos = ObjectManager.Get<Obj_AI_Base>().Any(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne")) ?
                EntityManager.Heroes.Enemies.FirstOrDefault(x => x.IsEnemy && x.IsValid && x.HasBuff("BlindMonkQOne"))
                .Distance(wardPlacePos) : 0;
            float maxRange = canWardJump ? 600 : 425;
            if (canWardJump && allyJump != null) maxRange = SpellManager.W1.Range;

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
            var wardPlacePos = ally.Position.To2D() + (target.Position.To2D() - ally.Position.To2D()).Normalized() *
                               (target.Distance(ally) - (float)SpellManager.R.Range/4);

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

        private void CheckInsec()
        {

            if (!me.Spellbook.GetSpell(SpellSlot.Q).Name.Contains("One"))
                SpellManager.Q2.Cast();
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            #region setVars
            var target = TargetSelector.SelectedTarget;
            var hasDash = Gapcloser.GapCloserList.Where(spell => spell.ChampName == target.ChampionName)
                .Any(gap => gap.SkillType != Gapcloser.GapcloserType.Targeted && target.Spellbook.GetSpell(gap.SpellSlot).CooldownExpires - Game.Time <= 0);

            var canFlash = SpellManager.FlashReady;
            var canWardJump = SpellManager.CanCastW1 && me.Mana >= me.Spellbook.GetSpell(SpellSlot.W).SData.Mana &&
                              WardManager.CanCastWard;
            var canQ = SpellManager.CanCastQ1;
            var wardDist = config["wardDistanceToTarget"].Cast<Slider>().CurrentValue;
            if (hasDash && target.HasBuff("BlindMonkQOne") && config["attendDashes"].Cast<CheckBox>().CurrentValue)
            {
                List<string> falseGapCloseChamps = new List<string>
                {
                    "Amumu",
                };
                if (!falseGapCloseChamps.Contains(target.ChampionName))
                wardDist += (int)SpellManager.Flash.Range;
            }
            var wardPlacePos = ally.Position.To2D() +
                               (target.Position.To2D() - ally.Position.To2D()).Normalized()*
                               (target.Distance(ally) + wardDist);
#endregion setVars

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
                if (targetMinion != null)
                {
                    if (targetMinion.Distance(wardPlacePos) <= WardManager.WardRange && canWardJump)
                    {
                        SpellManager.Q1.Cast(targetMinion.Position);
                    }
                    else if (targetMinion.Distance(wardPlacePos) <= SpellManager.Flash.Range &&
                             !canWardJump && canFlash)
                    {
                        SpellManager.Q1.Cast(targetMinion.Position);
                    }
                    /*q minion + wardjump + flash*/
                    else if (targetMinion.Distance(wardPlacePos) > SpellManager.Flash.Range &&
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