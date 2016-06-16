using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace MoonyLeeKicks
{
    static class LeeSinMenu
    {
        public static Menu config;
        public static Menu insecConfig;
        public static Menu multiRMenu;
        public static Menu smiteMenu;
        public static void Init()
        {
            config = MainMenu.AddMenu("MoonyLeeSin", "__MoonyLeeSin");
            config.Add("moonyLee_useQ", new CheckBox("Use Q Combo"));
            config.Add("moonyLee_useWGap", new CheckBox("Use W to GapClose"));
            config.Add("moonyLee_useE", new CheckBox("Use E Combo"));
            config.Add("moonyLee_useRKs", new CheckBox("Killsteal R Combo", false));
            config.AddSeparator();
            config.Add("moonyLee_useQWC", new CheckBox("Use Q WaveClear"));
            config.Add("moonyLee_useWWC", new CheckBox("Use W WaveClear"));
            config.Add("moonyLee_useEWC", new CheckBox("Use E WaveClear"));
            config.AddSeparator();
            config.Add("moonyLee_useQJC", new CheckBox("Use Q JungleClear"));
            config.Add("moonyLee_useWJC", new CheckBox("Use W JungleClear"));
            config.Add("moonyLee_useEJC", new CheckBox("Use E JungleClear"));
            config.AddSeparator();
            config.Add("moonyLee_useWardJump", new CheckBox("Wardjump in Flee Mode"));
            config.Add("moonyLee_useRKs_General", new CheckBox("Killsteal R if possible", false));

            insecConfig = config.AddSubMenu("MoonyInsec", "LeeSinInsec");
            insecConfig.AddGroupLabel("Insec");
            insecConfig.Add("wardDistanceToTarget",
                new Slider("Ward distance to enemy", 230, 50, 300));
            insecConfig.Add("attendDashes", new KeyBind("Attend dashes", true, KeyBind.BindTypes.PressToggle));
            insecConfig.AddLabel("Only calculates extra range if target has Q Buff. Ignores if jumps over minions");

            insecConfig.AddSeparator(10);
            insecConfig.Add("_insecKey", new KeyBind("LeeSinInsec Key", false, KeyBind.BindTypes.HoldActive));
            insecConfig.Add("moonSec", new CheckBox("Enable MoonSec", false));
            insecConfig.AddLabel("^ For Swag purpose only ^");
            insecConfig.AddSeparator(10);
            insecConfig.AddGroupLabel("Performance");
            insecConfig.Add("smoothFps", new CheckBox("Use Multitasking to prevent FPS-Drops", false));
            insecConfig.Add("smoothFpsBuffer", new Slider("Buffer Tick", 100, 100, 1000));
            insecConfig.AddLabel("Increase this value, if Multitasking is enabled, to gain more Fps");
            insecConfig.Add("smoothFpsMaxTasks", new Slider("Maximum Tasks", 100, 1, 1000));
            insecConfig.AddLabel("Decrease this value, if Multitasking is enabled, to gain more Fps");
            insecConfig.AddSeparator(10);
            insecConfig.AddGroupLabel("Drawings");
            insecConfig.Add("dashDebug", new KeyBind("Draw WardJump Position", false, KeyBind.BindTypes.HoldActive));

            multiRMenu = config.AddSubMenu("Multiple R", "multiRMoonyLeeSin");
            multiRMenu.Add("multiREnabled", new CheckBox("Use In Combo", false));
            multiRMenu.Add("targetAmount", new Slider("Minimum targets", 2, 2, 5));
            multiRMenu.AddGroupLabel("Insec");
            multiRMenu.Add("multiREnabledInsec", new CheckBox("Enable in InsecMode"));
            multiRMenu.Add("rotationAngle", new Slider("Kick angle [in Degrees]", 40, 0, 90));
            multiRMenu.AddLabel("45° => The Addon is allowed to kick up to 45° sidewards during the insec if multiple targets get hit");

            smiteMenu = config.AddSubMenu("Smite", "smiteMenuMoonyLeeSin");
            smiteMenu.Add("useSmite", new KeyBind("Use Smite", false, KeyBind.BindTypes.PressToggle));
            foreach (var baseSkinName in new [] {"SRU_Red", "SRU_Blue", "SRU_Dragon", "SRU_Baron"})
            {
                smiteMenu.Add("useSmite" + baseSkinName, new CheckBox(baseSkinName.Replace("SRU_", "")));
            }
        }
    }
}
