using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace MoonyLeeKicks.Insec
{
    static class SelectionHandler
    {
        public static bool LastAllyPosValid => (LastSelectedAlly != null && LastSelectedAlly.IsValid) || 
            LastMouseSpot != Vector2.Zero;
        public static bool LastTargetValid => LastSelectedTarget != null && LastSelectedTarget.IsValid;
        private static Obj_AI_Base LastSelectedAlly { get; set; }
        private static AIHeroClient LastSelectedTarget { get; set; }

        private static Vector2 LastMouseSpot = Vector2.Zero;

        public static void InitListening()
        {
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDown)
            {
                bool onMouseSpot = LeeSinMenu.insecExtensionsMenu["insecToMouseSpot"].Cast<CheckBox>().CurrentValue;

                var allyy = ObjectManager.Get<Obj_AI_Base>().Where(x => !x.IsMe && x.IsAlly && x.IsValid &&
                (x is AIHeroClient || x is Obj_AI_Turret)).OrderBy(x => x.Distance(Game.CursorPos)).FirstOrDefault
                    (x => x.Distance(Game.CursorPos) <= 200);

                var target = EntityManager.Heroes.Enemies.Where(x => x.IsValid).
                        OrderBy(x => x.Distance(Game.CursorPos)).FirstOrDefault
                    (x => x.Distance(Game.CursorPos) <= 200);

                bool allyValid = allyy != null && allyy.IsValid && !allyy.Equals(default(Obj_AI_Base));
                bool targetValid = target != null && target.IsValid && !target.Equals(default(AIHeroClient));
                bool bothValid = allyValid && targetValid;

                if (allyValid && !bothValid)
                {
                    LastSelectedAlly = allyy;
                }
                else if (targetValid)
                {
                    LastSelectedTarget = target;
                }
                else if (onMouseSpot) //both unvalid
                {
                    LastMouseSpot = Game.CursorPos.To2D();
                    LastSelectedAlly = null;
                }
            }
        }

        /// <summary>
        /// returns Vector2.Zero if no pos is found
        /// </summary>
        /// <returns></returns>
        public static Vector2 GetAllyPos => LastSelectedAlly?.Position.To2D() ?? LastMouseSpot;

        public static AIHeroClient GetTarget => LastTargetValid ? LastSelectedTarget : null;
    }
}
