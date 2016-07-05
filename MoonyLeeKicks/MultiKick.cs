using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace MoonyLeeKicks
{
    internal class MultiKick
    {
        public MultiKick()
        {
            Game.OnUpdate += OnMultiKickUpdate;
        }

        private void OnMultiKickUpdate(EventArgs args)
        {
            float leeSinRKickDistance = 700;
            float leeSinRKickWidth = 100;
            var minREnemies = LeeSinMenu.multiRMenu["targetAmount"].Cast<Slider>().CurrentValue;

            if (!LeeSinMenu.multiRMenu["multiREnabled"].Cast<CheckBox>().CurrentValue || 
                Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.Combo)
                return;

            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => x.IsValid && 
                x.Distance(ObjectManager.Player) <= SpellManager.R.Range))
            {
                var startPos = enemy.ServerPosition;
                var endPos = ObjectManager.Player.ServerPosition.Extend(
                    startPos,
                    ObjectManager.Player.Distance(enemy) + leeSinRKickDistance).To3D();
                var rectangle = new Geometry.Polygon.Rectangle(startPos, endPos, leeSinRKickWidth);

                if (EntityManager.Heroes.Enemies.Count(x => rectangle.IsInside(x)) >= minREnemies)
                {
                    if (SpellManager.R.IsReady())
                        SpellManager.R.Cast(enemy);
                }
            }
        }
    }
}
