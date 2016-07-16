using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace MoonyLeeKicks
{
    static class LeeSinMenu
    {
        public static void AddStringList(this Menu m, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            var mode = m.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ": " + values[mode.CurrentValue];
            mode.OnValueChange += delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
            {
                sender.DisplayName = displayName + ": " + values[args.NewValue];
            };
        }

        public static Menu config,
            comboMenu, harassMenu, waveClearMenu, jungleClearMenu, miscMenu,
            insecMenu, insecExtensionsMenu, multiRMenu, starComboMenu, bubbaKushMenu, smiteMenu,
            DashAnalysisMenu;

        private static Menu helpMenu;
        public static void Init()
        {
            config = MainMenu.AddMenu("MoonyLeeSin", "__MoonyLeeSin");
            config.AddGroupLabel("Most intelligent Lee Script");
            config.AddGroupLabel("by DanThePman");

            comboMenu = config.AddSubMenu("Combo", "ComboMenu");
            comboMenu.AddGroupLabel("Gank Combo");
            comboMenu.Add("useQ", new CheckBox("Use Q"));
            comboMenu.Add("useWGap", new CheckBox("Use W To GapClose"));
            comboMenu.Add("noWAtQ2", new CheckBox("Don't GapClose If Q2 Possible"));
            comboMenu.Add("noWAtQ1Fly", new CheckBox("Wait For Q1 Before GapClose With W"));
            comboMenu.Add("useE", new CheckBox("Use E"));
            comboMenu.Add("useRKs", new CheckBox("Killsteal R", false));
            comboMenu.Add("useItems", new CheckBox("Use Tiamat/Hydra"));
            comboMenu.AddSeparator();
            comboMenu.AddGroupLabel("Fight Combo");
            comboMenu.Add("useQFight", new CheckBox("Use Q"));
            comboMenu.Add("useWFight", new CheckBox("Use W"));
            comboMenu.Add("useEFight", new CheckBox("Use E"));
            comboMenu.Add("useRFight", new CheckBox("Use R"));
            comboMenu.Add("useItemsFight", new CheckBox("Use Tiamat/Hydra"));
            comboMenu.AddSeparator();
            comboMenu.AddStringList("currentComboMethod", "Current Combo Style", new [] {"Gank", "Fight"}, 0);
            comboMenu.Add("comboSytleSwitch",
                new KeyBind("Switch Combo Style (Toggle)", false, KeyBind.BindTypes.PressToggle));

            harassMenu = config.AddSubMenu("Harass", "HarassMenu");
            harassMenu.Add("useQ", new CheckBox("Use Q1 Harass"));
            harassMenu.Add("useE", new CheckBox("Use E Harass"));


            waveClearMenu = config.AddSubMenu("WaveClear", "WaveClearMenu");
            waveClearMenu.Add("useQ", new CheckBox("Use Q WaveClear"));
            waveClearMenu.Add("useW", new CheckBox("Use W WaveClear"));
            waveClearMenu.Add("useE", new CheckBox("Use E WaveClear"));
            waveClearMenu.Add("useItems", new CheckBox("Use Tiamat/Hydra WaveClear"));

            jungleClearMenu = config.AddSubMenu("JungleClear", "JungleClearMenu");
            jungleClearMenu.Add("useQ", new CheckBox("Use Q JungleClear"));
            jungleClearMenu.Add("useW", new CheckBox("Use W JungleClear"));
            jungleClearMenu.Add("useE", new CheckBox("Use E JungleClear"));
            jungleClearMenu.Add("useItems", new CheckBox("Use Tiamat/Hydra JungleClear"));

            miscMenu = config.AddSubMenu("Misc", "MiscMenu");
            miscMenu.Add("useWardJump", new CheckBox("Wardjump In Flee Mode"));
            miscMenu.Add("useWardJumpMaxRange", new CheckBox("Use For Max Range"));
            miscMenu.Add("useRKs_General", new CheckBox("Killsteal R If Possible", false));





            insecMenu = config.AddSubMenu("Insec", "LeeSinInsec");
            insecMenu.Add("insecFrequency", new Slider("Update delay in ms", 0, 0, 500));
            insecMenu.AddLabel("Inscrease To Get More Fps");
            insecMenu.Add("wardDistanceToTarget", new Slider("Ward Distance To Enemy", 230, 200, 300));
            insecMenu.AddSeparator();

            insecMenu.Add("_insecKey", new KeyBind("Lee Sin Insec Key", false, KeyBind.BindTypes.HoldActive));
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
            insecExtensionsMenu.Add("attendDashes", new CheckBox("Enabled"));
            insecExtensionsMenu.AddLabel("Only Calculates Extra Range If The Target Has Q Buff. Ignores If Jumps Over Minions");
            insecExtensionsMenu.Add("dashForcecastMethod", new ComboBox("Dash Prediction Method", 1, 
                "Prospective Forecast", "Wait For Dash Cast"));
            insecExtensionsMenu.AddSeparator();
            insecExtensionsMenu.Add("dashInfo__Elo", new ComboBox("Target Elo", 2, "Bronze+", "Gold+", "Plat+"));
            insecExtensionsMenu.AddSeparator();
            insecExtensionsMenu.Add("useDashAnalysis", new CheckBox("Use Dash Analysis Data To Predict Dashes"));
            insecExtensionsMenu.AddLabel("TURN OFF IN HIGH ELO");
            insecExtensionsMenu.AddSeparator();
            insecExtensionsMenu.Add("minPobabilityToDash", new Slider("Minimum Enemy Dash Probability to Assume A Dash", 70));
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



            DashAnalysisMenu = config.AddSubMenu("DashAnalysis", "DashAnalysisMenu");



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
            helpMenu.AddGroupLabel("Found A Bug? Scroll down");
            helpMenu.AddGroupLabel("How to insec");
            helpMenu.AddLabel("1. Make sure that insec spells are ready (at least R->W->Ward or R->Flash)", 35);
            helpMenu.AddLabel("2. Select enemy (blue circle)", 35);
            helpMenu.AddLabel("3. Select ally (blue circle 2)", 35);
            helpMenu.AddLabel("Afterwards, a white arrow from the enemy to the target will be drawn", 35);
            helpMenu.AddLabel("4. Press insec key and hold it down", 35);
            helpMenu.AddLabel("5. To cancel the insec, release the button", 35);
            helpMenu.AddSeparator();
            helpMenu.AddGroupLabel("Error?");
            helpMenu.AddGroupLabel("For Insec:");
            helpMenu.AddLabel("If your spells are not ready or you have not selected a valid target/ally", 35);
            helpMenu.AddLabel("          then a red font will be drawn below your hero to inform you.", 35);
            helpMenu.AddSeparator();
            helpMenu.AddGroupLabel("For Jungle- or WaveClear: Do not bind them to the same key");
            helpMenu.AddSeparator();
            helpMenu.AddGroupLabel("For Bubba Kush: Select a target and make sure your distance to it is <= WardRange");
            helpMenu.AddSeparator();
            helpMenu.AddGroupLabel("Anything still does not work: Reload the addon");
        }
    }
}
