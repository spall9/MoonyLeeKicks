using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace MoonyLeeKicks
{
    static class LeeSinMenu
    {
        public static Menu config, insecMenu, insecExtensionsMenu, multiRMenu, starComboMenu, bubbaKushMenu, smiteMenu;

        private static Menu helpMenu;
        public static void Init()
        {
            config = MainMenu.AddMenu("MoonyLeeSin", "__MoonyLeeSin");
            config.AddGroupLabel("Combo");
            config.Add("moonyLee_useQ", new CheckBox("Use Q Combo"));
            config.Add("moonyLee_useWGap", new CheckBox("Use W To GapClose Combo"));
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
            config.Add("moonyLee_useWardJump", new CheckBox("Wardjump In Flee Mode"));
            config.Add("moonyLee_useWardJumpMaxRange", new CheckBox("Use For Max Range"));
            config.Add("moonyLee_useRKs_General", new CheckBox("Killsteal R If Possible", false));





            insecMenu = config.AddSubMenu("Insec", "LeeSinInsec");
            insecMenu.Add("insecFrequency", new Slider("Update delay in ms", 0, 0, 500));
            insecMenu.AddLabel("Inscrease To Get More Fps");
            insecMenu.Add("wardDistanceToTarget", new Slider("Ward Distance To Enemy", 230, 200, 300));
            insecMenu.AddSeparator();

            insecMenu.Add("_insecKey", new KeyBind("LeeSinInsec Key", false, KeyBind.BindTypes.HoldActive));
            insecMenu.Add("moonSec", new CheckBox("Enable MoonSec", false));
            insecMenu.AddLabel("^ For Fancy Looking Purpose Only (Could Mess Up!) ^");
            insecMenu.AddSeparator();

            insecMenu.AddGroupLabel("Drawings");
            insecMenu.Add("dashDebug", new KeyBind("Draw WardJump Position (Toggle)", true, KeyBind.BindTypes.PressToggle));





            insecExtensionsMenu = config.AddSubMenu("InsecExtensions", "InsecExtensionsMenu");
            insecExtensionsMenu.Add("insecToMouseSpot", new CheckBox("Enable Insec To Mouse Spot", false));
            insecExtensionsMenu.AddLabel("Click On Ground");
            insecExtensionsMenu.AddSeparator();

            insecExtensionsMenu.AddGroupLabel("Anti Dash");
            insecExtensionsMenu.Add("attendDashes", new KeyBind("Attend dashes", true, KeyBind.BindTypes.PressToggle));
            insecExtensionsMenu.AddLabel("Only Calculates Extra Range If The Target Has Q Buff. Ignores If Jumps Over Minions");
            insecExtensionsMenu.Add("automatedDashForecast", new CheckBox("Automatic Dash Forecast (BETA)"));
            insecExtensionsMenu.AddLabel("Automatically Decides If And Where The Target Would Dash To");
            insecExtensionsMenu.AddSeparator();
            insecExtensionsMenu.Add("dashInfo__Elo", new ComboBox("Target Elo", 1, "Bronze+", "Gold+"));
            insecExtensionsMenu.AddSeparator();
            insecExtensionsMenu.AddGroupLabel("________________________________________________");
            insecExtensionsMenu.AddSeparator();


            insecExtensionsMenu.Add("waitForQBefore_WardFlashKick", new CheckBox("Do Not Execute Instant Insec", false));
            insecExtensionsMenu.AddLabel("Wait For Using Q Before Instantly Do Ward->Flash->Kick");
            insecExtensionsMenu.AddLabel("(Doesn't Matter If The Q Hits)");
            insecExtensionsMenu.AddSeparator();
            insecExtensionsMenu.Add("WardFlashKickOnlyWithQ", new CheckBox("Only Enable Ward->Flash->Kick If the Q hit", false));
            insecExtensionsMenu.AddSeparator();

            //insecExtensionsMenu.Add("correctInsecWithOtherSpells", new CheckBox("Correct Insec With Other Spells (e.g. Flash)"));
            //insecExtensionsMenu.AddLabel("If Your End Position Behind The Enemy is Inaccurate");
            //insecExtensionsMenu.AddSeparator();

            insecExtensionsMenu.Add("useMovementPrediction", new CheckBox("Use Movement Prediction"));
            insecExtensionsMenu.AddLabel("If The Target Is Running Away, The Ward Distance To It Increases");
            insecExtensionsMenu.AddSeparator();

            insecExtensionsMenu.Add("onlyQ2IfNeeded", new CheckBox("Better Q2 Checks"));
            insecExtensionsMenu.AddLabel("Checks Again If Q2 Makes Sense Or If The Target Is Too Far Away");
            insecExtensionsMenu.AddSeparator();
            




            multiRMenu = config.AddSubMenu("Multiple R", "multiRMoonyLeeSin");
            multiRMenu.Add("multiREnabled", new CheckBox("Use In Combo (R Only)", false));
            multiRMenu.Add("targetAmount", new Slider("Minimum Targets", 2, 2, 5));
            multiRMenu.AddSeparator();
            multiRMenu.AddGroupLabel("Insec");
            multiRMenu.Add("multiREnabledInsec", new CheckBox("Enable in InsecMode"));
            multiRMenu.Add("rotationAngle", new Slider("Kick angle [in Degrees]", 30, 0, 90));
            multiRMenu.AddLabel("45° => The Addon Is Allowed To Kick Up To 45° Sidewards During The Insec If Multiple Targets Get Hit");





            starComboMenu = config.AddSubMenu("Star Combo", "StarComboMenu");
            starComboMenu.Add("starComboKey", new KeyBind("Star Combo", false, KeyBind.BindTypes.HoldActive));
            starComboMenu.AddLabel("Select Enemy");
            starComboMenu.AddSeparator();

            starComboMenu.Add("starComboMultiR", new CheckBox("Try Multiple R"));
            starComboMenu.Add("starComboMultiRHitCount", new Slider("Min Enemies Hit In Star Combo", 2, 2, 5));
            starComboMenu.AddSeparator();

            starComboMenu.Add("starComboUseWard", new CheckBox("Use Ward"));
            starComboMenu.Add("starComboUseFlash", new CheckBox("Use Flash"));
            starComboMenu.Add("starComboUseAlly", new CheckBox("Use Allies To Jump"));
            starComboMenu.AddLabel("Prefers Ward/Ally Over Flash");
            starComboMenu.AddSeparator();

            starComboMenu.Add("starComboMovementPrediction", new CheckBox("Use Movement Prediction"));
            starComboMenu.AddLabel("For Wardjumps");





            bubbaKushMenu = config.AddSubMenu("BubbaKush", "bubbaKushMenu");
            bubbaKushMenu.Add("bubbaKey", new KeyBind("Bubba Kush", false, KeyBind.BindTypes.HoldActive));
            bubbaKushMenu.AddLabel("Select Enemy");
            bubbaKushMenu.AddSeparator();

            bubbaKushMenu.Add("useAlliesBubba", new CheckBox("Use Allies"));
            bubbaKushMenu.Add("useMovementPredictionBubba1", new CheckBox("Use Movement Prediction For Target"));
            bubbaKushMenu.Add("useMovementPredictionBubba2", new CheckBox("Use Movement Prediction For Rest Enemies"));
            bubbaKushMenu.AddSeparator();

            bubbaKushMenu.Add("betterCalculationBubba", new CheckBox("Use More Precise Calculations"));
            bubbaKushMenu.AddLabel("Attends The Distance Of Hitted Enemies To The Ultimate-Rectangle-Hitbox Edge");




            smiteMenu = config.AddSubMenu("Smite", "SmiteMenu");
            smiteMenu.Add("smiteToggleKey", new KeyBind("Use Smite (Toggle)", true, KeyBind.BindTypes.PressToggle));
            smiteMenu.AddSeparator();
            smiteMenu.Add("useSmiteLargeChamps", new CheckBox("Use For Large Camps"));
            smiteMenu.AddLabel("Blue, Red");
            smiteMenu.AddSeparator();

            smiteMenu.Add("useForEpicCamps", new CheckBox("Use For Epic Camps"));
            smiteMenu.AddLabel("All Dragons, Baron, Rift Herald, Spider Boss");
            smiteMenu.AddSeparator();

            smiteMenu.Add("useSmiteKs", new CheckBox("Use Smite To Ks"));
            smiteMenu.AddSeparator();

            smiteMenu.Add("useSmiteQCombo", new CheckBox("Use Smite->Q in Combo"));
            smiteMenu.Add("useSmiteQInsec", new CheckBox("Use Smite->Q in Insec "));



            helpMenu = config.AddSubMenu("Help", "helpMenu");
            helpMenu.AddGroupLabel("How to insec");
            helpMenu.AddLabel("1. Make sure that insec spells are ready (at least R->W->Ward or R->Flash)", 35);
            helpMenu.AddLabel("2. Select enemy (blue circle)", 35);
            helpMenu.AddLabel("3. Select ally (blue circle 2)", 35);
            helpMenu.AddLabel("Afterwards, a white arrow from the enemy to the target will be drawn", 35);
            helpMenu.AddLabel("4. Press insec key and hold it down", 35);
            helpMenu.AddLabel("5. To cancel the insec, release the button", 35);
            helpMenu.AddSeparator(10);
            helpMenu.AddGroupLabel("Error?");
            helpMenu.AddLabel("At insec: If your spells are not ready or you have not selected a valid target/ally", 35);
            helpMenu.AddLabel("          then a red font will be drawn below your hero to inform you.", 35);
            helpMenu.AddSeparator(5);
            helpMenu.AddLabel("Jungle- or WaveClear does not work: Do not bind them to the same key", 35);
            helpMenu.AddSeparator(5);
            helpMenu.AddLabel("Anything still does not work: Reload the addon", 35);
        }
    }
}
