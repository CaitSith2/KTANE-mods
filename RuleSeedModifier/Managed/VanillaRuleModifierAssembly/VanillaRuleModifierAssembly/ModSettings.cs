using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace VanillaRuleModifierAssembly
{
    public class ModuleSettings
    {
        private const int DefaultSeed = 6502;

        public int SettingsVersion;
        public string HowToUse0 = "Don't Touch this value. It is used by the mod internally to determine if there are new settings to be saved.";

    
        public bool ResetToDefault = false;
        public string HowToReset = "Changing this setting to true will reset ALL your setting back to default.";

        //Add your own settings here.  If you wish to have explanations, define them as strings similar to as shown above.
        //Make sure those strings are JSON compliant.
        public int RuleSeed = DefaultSeed;

        public string HowToUse1 = "Sets the seed that will be used to generate the ruleset.";
        public string HowToUse2 = "1 = Vanilla";
        public string HowToUse3 = DefaultSeed + " = VanillaRuleModifier Default Seed";        
    }

    public class ModSettings
    {
        public readonly int ModSettingsVersion = 2;
        public ModuleSettings Settings = new ModuleSettings();

        public string ModuleName { get; }
        //Update this line each time you make changes to the Settings version.

        public ModSettings(KMModSettings settings)
        {
            var fileName = Path.GetFileName(settings.SettingsPath);
            if (fileName != null) ModuleName = fileName.Replace("-settings.txt", "");
        }

        public ModSettings(KMBombModule module)
        {
            ModuleName = module.ModuleType;
        }

        public ModSettings(KMNeedyModule module)
        {
            ModuleName = module.ModuleType;
        }

        public ModSettings(string moduleName)
        {
            ModuleName = moduleName;
        }

        private string GetModSettingsPath(bool directory)
        {
            var modSettingsDirectory = Path.Combine(Application.persistentDataPath, "Modsettings");
            return directory ? modSettingsDirectory : Path.Combine(modSettingsDirectory, ModuleName + "-settings.txt");
        }

        public bool WriteSettings()
        {
            Debug.LogFormat("Writing Settings File: {0}", GetModSettingsPath(false));
            try
            {
                if (!Directory.Exists(GetModSettingsPath(true)))
                {
                    Directory.CreateDirectory(GetModSettingsPath(true));
                }

                Settings.SettingsVersion = ModSettingsVersion;
                var settings = JsonConvert.SerializeObject(Settings, Formatting.Indented, new StringEnumConverter());
                File.WriteAllText(GetModSettingsPath(false), settings);
                Debug.LogFormat("New settings = {0}", settings);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogFormat("Failed to Create settings file due to Exception:\n{0}\nStack Trace:\n{1}", ex.Message,
                    ex.StackTrace);
                return false;
            }
        }

        public bool ReadSettings()
        {
            var modSettings = GetModSettingsPath(false);
            try
            {
                if (File.Exists(modSettings))
                {
                    var settings = File.ReadAllText(modSettings);
                    Settings = JsonConvert.DeserializeObject<ModuleSettings>(settings, new StringEnumConverter());
                    bool forcewrite = Settings.RuleSeed < 0;
                    Settings.RuleSeed = Settings.RuleSeed == int.MinValue 
                        ? 0 
                        : Mathf.Abs(Settings.RuleSeed);

                    if (Settings.SettingsVersion != ModSettingsVersion || forcewrite)
                        return WriteSettings();
                    if (!Settings.ResetToDefault) return true;
                    Settings = new ModuleSettings();
                    return WriteSettings();
                }
                Settings = new ModuleSettings();
                return WriteSettings();
            }
            catch (Exception ex)
            {
                Debug.LogFormat(
                    "Settings not loaded due to Exception:\n{0}\nStack Trace:\n{1}\nLoading default settings instead.",
                    ex.Message, ex.StackTrace);
                Settings = new ModuleSettings();
                return WriteSettings();
            }
        }
    }
}