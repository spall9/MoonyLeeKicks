using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace MoonyLeeKicks
{
    static class LeeSinMenu
    {
        public static Menu config;
        public static Menu insecConfig;
        public static Menu multiRMenu;
        private static Menu guideMenu;
        public static Menu userMenu;
        public static void Init()
        {
            config = MainMenu.AddMenu("MoonyLeeSin", "__MoonyLeeSin");
            config.Add("unloadExtensions", new CheckBox("Dont load User extensions (reload addon)", false));
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
                new Slider("Ward distance to enemy", 230, 200, 300));
            insecConfig.AddSeparator(10);
            insecConfig.Add("attendDashes", new KeyBind("Attend dashes", true, KeyBind.BindTypes.PressToggle));
            insecConfig.AddLabel("Only calculates extra range if target has Q Buff. Ignores if jumps over minions");
            insecConfig.Add("waitForQBefore_WardFlashKick",
                new CheckBox("Don't do instant insec", false));
            insecConfig.AddLabel("Wait for using Q before instantly do Ward->Flash->Kick");
            insecConfig.Add("correctInsecWithOtherSpells",
                new CheckBox("Correct insec with other spells like flash"));
            insecConfig.AddLabel("If your end position behind the enemy is inaccurate");
            insecConfig.Add("useMovementPrediction", new CheckBox("Use movement prediction"));
            insecConfig.AddLabel("If the target is running away, the ward distance to it increases");

            insecConfig.AddSeparator(10);
            insecConfig.Add("_insecKey", new KeyBind("LeeSinInsec Key", false, KeyBind.BindTypes.HoldActive));
            insecConfig.Add("moonSec", new CheckBox("Enable MoonSec", false));
            insecConfig.AddLabel("^ For Swag purpose only ^");
            insecConfig.AddSeparator(10);
            insecConfig.AddGroupLabel("Drawings");
            insecConfig.Add("dashDebug", new KeyBind("Draw WardJump Position (Toggle)", true, KeyBind.BindTypes.PressToggle));

            multiRMenu = config.AddSubMenu("Multiple R", "multiRMoonyLeeSin");
            multiRMenu.Add("multiREnabled", new CheckBox("Use In Combo (R only)", false));
            multiRMenu.Add("targetAmount", new Slider("Minimum targets", 2, 2, 5));
            multiRMenu.AddSeparator();
            multiRMenu.AddGroupLabel("Insec");
            multiRMenu.Add("multiREnabledInsec", new CheckBox("Enable in InsecMode"));
            multiRMenu.Add("rotationAngle", new Slider("Kick angle [in Degrees]", 30, 0, 90));
            multiRMenu.AddLabel("45° => The Addon is allowed to kick up to 45° sidewards during the insec if multiple targets get hit");

            userMenu = config.AddSubMenu("UserWishes", "UserWishes");
            userMenu.AddGroupLabel("Insec extensions");
            userMenu.Add("insecToMouseSpot", new CheckBox("Enable Insec to mouse spot", false));
            userMenu.AddLabel("Click on ground");

            userMenu.AddSeparator();
            userMenu.AddGroupLabel("Star Combo");
            userMenu.Add("starComboKey", new KeyBind("Star Combo", false, KeyBind.BindTypes.HoldActive));
            userMenu.AddLabel("Select enemy");
            userMenu.AddSeparator(10);
            userMenu.Add("starComboMultiR", new CheckBox("Try multiple R"));
            userMenu.Add("starComboMultiRHitCount", new Slider("Min enemies hit in star combo", 2, 2, 5));
            userMenu.AddSeparator(10);
            userMenu.Add("starComboUseWard", new CheckBox("Use ward"));
            userMenu.Add("starComboUseFlash", new CheckBox("Use flash"));
            userMenu.Add("starComboUseAlly", new CheckBox("Use allies to jump"));
            userMenu.AddLabel("Prefers Ward/Ally over flash");
            userMenu.AddSeparator(10);
            userMenu.Add("starComboMovementPrediction", new CheckBox("Use movement prediction"));
            userMenu.AddLabel("For wardjumps");

            userMenu.AddSeparator();
            userMenu.AddGroupLabel("Bubba kush");
            userMenu.Add("bubbaKey", new KeyBind("Bubba kush", false, KeyBind.BindTypes.HoldActive));
            userMenu.Add("useAlliesBubba", new CheckBox("Use allies"));
            userMenu.Add("useMovementPredictionBubba1", new CheckBox("Use movement prediction for target"));
            userMenu.Add("useMovementPredictionBubba2", new CheckBox("Use movement prediction for rest enemies"));

            guideMenu = config.AddSubMenu("Help", "helpMenu");
            guideMenu.AddGroupLabel("How to insec");
            guideMenu.AddLabel("1. Make sure that insec spells are ready (at least R->W->Ward or R->Flash)", 35);
            guideMenu.AddLabel("2. Select enemy (blue circle)", 35);
            guideMenu.AddLabel("3. Select ally (blue circle 2)", 35);
            guideMenu.AddLabel("Afterwards, a white arrow from the enemy to the target will be drawn", 35);
            guideMenu.AddLabel("4. Press insec key and hold it down", 35);
            guideMenu.AddLabel("5. To cancel the insec, release the button", 35);
            guideMenu.AddSeparator(10);
            guideMenu.AddGroupLabel("Error?");
            guideMenu.AddLabel("At insec: If your spells are not ready or you have not selected a valid target/ally", 35);
            guideMenu.AddLabel("          then a red font will be drawn below your hero to inform you.", 35);
            guideMenu.AddSeparator(5);
            guideMenu.AddLabel("Jungle- or WaveClear does not work: Do not bind them to the same key", 35);
            guideMenu.AddSeparator(5);
            guideMenu.AddLabel("Anything still does not work: Reload the addon", 35);
        }
    }
}
