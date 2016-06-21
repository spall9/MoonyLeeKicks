using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace MoonyLeeKicks
{
    static class LeeSinMenu
    {
        public static Menu config;
        public static Menu insecConfig;
        public static Menu multiRMenu;
        public static void Init()
        {
            config = MainMenu.AddMenu("MoonyLeeSin", "__MoonyLeeSin");
            config.AddGroupLabel("Combo");
            config.Add("moonyLee_useQ", new CheckBox("Use Q Combo"));
            config.Add("moonyLee_useWGap", new CheckBox("Use W to GapClose Combo"));
            config.Add("moonyLee_useE", new CheckBox("Use E Combo"));
            config.Add("moonyLee_useRKs", new CheckBox("Killsteal R Combo", false));
            config.Add("moonyLee_useItems", new CheckBox("Use Tiamat/Hydra Combo"));
            config.AddSeparator();
            config.AddGroupLabel("WaveClear");
            config.Add("moonyLee_useQWC", new CheckBox("Use Q WaveClear"));
            config.Add("moonyLee_useWWC", new CheckBox("Use W WaveClear"));
            config.Add("moonyLee_useEWC", new CheckBox("Use E WaveClear"));
            config.Add("moonyLee_useItemsWC", new CheckBox("Use Tiamat/Hydra WaveClear"));
            config.AddSeparator();
            config.AddGroupLabel("JungleClear");
            config.Add("moonyLee_useQJC", new CheckBox("Use Q JungleClear"));
            config.Add("moonyLee_useWJC", new CheckBox("Use W JungleClear"));
            config.Add("moonyLee_useEJC", new CheckBox("Use E JungleClear"));
            config.Add("moonyLee_useItemsJC", new CheckBox("Use Tiamat/Hydra JungleClear"));
            config.AddSeparator();
            config.AddGroupLabel("Misc");
            config.Add("moonyLee_useWardJump", new CheckBox("Wardjump in Flee Mode"));
            config.Add("moonyLee_useRKs_General", new CheckBox("Killsteal R if possible", false));

            insecConfig = config.AddSubMenu("MoonyInsec", "LeeSinInsec");
            insecConfig.AddGroupLabel("Insec");
            insecConfig.Add("insecFrequency", new Slider("Update delay in ms", 0, 0, 500));
            insecConfig.AddLabel("Inscrease to get more fps");
            insecConfig.Add("wardDistanceToTarget",
                new Slider("Ward distance to enemy", 230, 50, 300));
            insecConfig.Add("attendDashes", new KeyBind("Attend dashes", true, KeyBind.BindTypes.PressToggle));
            insecConfig.AddLabel("Only calculates extra range if target has Q Buff. Ignores if jumps over minions");

            insecConfig.AddSeparator(10);
            insecConfig.Add("_insecKey", new KeyBind("LeeSinInsec Key", false, KeyBind.BindTypes.HoldActive));
            insecConfig.Add("moonSec", new CheckBox("Enable MoonSec", false));
            insecConfig.AddLabel("^ For Swag purpose only ^");
            insecConfig.AddSeparator(10);
            insecConfig.AddGroupLabel("Drawings");
            insecConfig.Add("dashDebug", new KeyBind("Draw WardJump Position (Toggle)", true, KeyBind.BindTypes.PressToggle));

            multiRMenu = config.AddSubMenu("Multiple R", "multiRMoonyLeeSin");
            multiRMenu.Add("multiREnabled", new CheckBox("Use In Combo", false));
            multiRMenu.Add("targetAmount", new Slider("Minimum targets", 2, 2, 5));
            multiRMenu.AddGroupLabel("Insec");
            multiRMenu.Add("multiREnabledInsec", new CheckBox("Enable in InsecMode"));
            multiRMenu.Add("rotationAngle", new Slider("Kick angle [in Degrees]", 40, 0, 90));
            multiRMenu.AddLabel("45° => The Addon is allowed to kick up to 45° sidewards during the insec if multiple targets get hit");
        }
    }
}
