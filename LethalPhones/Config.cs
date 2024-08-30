using BepInEx.Configuration;

namespace Scoops
{
    public class Config
    {
        private static string loadedPreferredNumber;
        public static string PreferredNumber => loadedPreferredNumber ?? preferredNumber.Value;

        public static ConfigEntry<float> recordingStartDist;
        public static ConfigEntry<float> backgroundVoiceDist;
        public static ConfigEntry<float> eavesdropDist;
        public static ConfigEntry<float> backgroundSoundMod;
        public static ConfigEntry<float> voiceSoundMod;
        public static ConfigEntry<float> deathHangupTime;

        public static ConfigEntry<bool> savePhoneNumbers;
        public static ConfigEntry<bool> enablePreferredNumbers;
        public static ConfigEntry<bool> saveLocalPreferredNumber;
        public static ConfigEntry<string> preferredNumber;

        public static ConfigEntry<int> maxPhoneBugs;
        public static ConfigEntry<float> chancePhoneBug;
        public static ConfigEntry<float> minPhoneBugInterval;
        public static ConfigEntry<float> maxPhoneBugInterval;

        public Config(ConfigFile cfg)
        {
            voiceSoundMod = cfg.Bind(
                    "General",
                    "voiceSoundMod",
                    0f,
                    "All voices on calls have their volume adjusted by this value."
            );
            backgroundSoundMod = cfg.Bind(
                    "General",
                    "backgroundSoundMod",
                    -0.1f,
                    "All background noises on calls have their volume adjusted by this value."
            );
            recordingStartDist = cfg.Bind(
                    "General",
                    "recordingStartDist",
                    15f,
                    "Disables phones while in this distance to the person you're calling."
            );
            backgroundVoiceDist = cfg.Bind(
                    "General",
                    "backgroundVoiceDist",
                    20f,
                    "The distance at which you can hear other players in the background of a call."
            );
            eavesdropDist = cfg.Bind(
                    "General",
                    "eavesdropDist",
                    5f,
                    "The distance at which you can listen in on someone else's call."
            );
            deathHangupTime = cfg.Bind(
                    "General",
                    "deathHangupTime",
                    0.5f,
                    "The time it takes (in seconds) for a call to auto-hangup after death."
            );

            maxPhoneBugs = cfg.Bind(
                    "Enemies.HoardingBugs",                                             // Config section
                    "maxPhoneBugs",                                                     // Key of this config
                    1,                                                                  // Default value
                    "Maximum number of Hoarding Bugs that can spawn with phones."       // Description
            );
            chancePhoneBug = cfg.Bind(
                    "Enemies.HoardingBugs",
                    "chancePhoneBug",
                    0.1f,
                    "The chance (0 - 1) that a Hoarding Bug will be spawned with a phone."
            );
            minPhoneBugInterval = cfg.Bind(
                    "Enemies.HoardingBugs",
                    "minPhoneBugInterval",
                    10f,
                    "The shortest time (in seconds) between calls from each Hoarding Bug."
            );
            maxPhoneBugInterval = cfg.Bind(
                    "Enemies.HoardingBugs",
                    "maxPhoneBugInterval",
                    100f,
                    "The longest time (in seconds) between calls from each Hoarding Bug."
            );

            savePhoneNumbers = cfg.Bind(
                    "PhoneNumbers",
                    "savePhoneNumbers",
                    true,
                    "Remembers phone numbers assigned to each player for the file."
            );
            enablePreferredNumbers = cfg.Bind(
                    "PhoneNumbers",
                    "enablePreferredNumbers",
                    false,
                    "Attempts to assign players a user-specified phone number when they join a lobby which has enabled this feature."
            );
            saveLocalPreferredNumber = cfg.Bind(
                    "PhoneNumbers",
                    "saveLocalPreferredNumber",
                    false,
                    "When enabled, saves the preferred phone number specified in this config locally, or deletes the locally saved number if the config option is empty."
            );
            preferredNumber = cfg.Bind(
                    "PhoneNumbers",
                    "preferredNumber",
                    "",
                    "Attempts to assign you this phone number (4 digits) when you join a lobby which has enabled this feature.\nIf a preferred phone number has been saved locally (see above option), that will be used instead."
            );

            // Support changing preferred phone number on main menu via a config editor e.g. LethalConfig
            preferredNumber.SettingChanged += (_, _) => SavePreferredNumber();
            saveLocalPreferredNumber.SettingChanged += (_, _) => SavePreferredNumber();

            // Immediately update preferred phone number on launch
            LoadPreferredNumber();
            SavePreferredNumber();
        }

        private void SavePreferredNumber()
        {
            if (!saveLocalPreferredNumber.Value)
            {
                return;
            }

            if (string.IsNullOrEmpty(preferredNumber.Value))
            {
                ES3.DeleteKey($"{PluginInfo.PLUGIN_GUID}_PreferredNumber", "LCGeneralSaveData");

                loadedPreferredNumber = null;
                return;
            }

            ES3.Save($"{PluginInfo.PLUGIN_GUID}_PreferredNumber", preferredNumber.Value, "LCGeneralSaveData");

            loadedPreferredNumber = preferredNumber.Value;
        }

        private void LoadPreferredNumber()
        {
            if (!ES3.KeyExists($"{PluginInfo.PLUGIN_GUID}_PreferredNumber", "LCGeneralSaveData"))
            {
                loadedPreferredNumber = null;
                return;
            }

            loadedPreferredNumber = ES3.Load<string>($"{PluginInfo.PLUGIN_GUID}_PreferredNumber", "LCGeneralSaveData");
        }
    }
}
