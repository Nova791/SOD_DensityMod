using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using BepInEx.Configuration; 
using HarmonyLib;

namespace DensityMod
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    public class DensityMod : BasePlugin{
        static public ConfigEntry<string> DensityMinConfig;
        static public ConfigEntry<string> DensityMaxConfig;
        static private AcceptableValueList<string> allowedDensity = new AcceptableValueList<string>("low", "medium", "high", "veryHigh");

        static public ConfigEntry<string> LandValueMinConfig;
        static public ConfigEntry<string> LandValueMaxConfig;
        static private AcceptableValueList<string> allowedLandValue = new AcceptableValueList<string>("veryLow", "low", "medium", "high", "veryHigh");

        public static ManualLogSource Logger;

        public override void Load(){

            DensityMinConfig = Config.Bind("Settings",      // The section under which the option is shown
                                         "Minimum Density Size",  // The key of the configuration option in the configuration file
                                         "low", // The default value
                                         "This sets what the smallest density is allowed. Options are: low, medium, high, veryHigh."); // Description of the option to show in the config file

            DensityMaxConfig = Config.Bind("Settings",      // The section under which the option is shown
                                         "Maximum Density Size",  // The key of the configuration option in the configuration file
                                         "veryHigh", // The default value
                                         "This sets what the largest density is allowed. Options are: low, medium, high, veryHigh."); // Description of the option to show in the config file

            LandValueMinConfig = Config.Bind("Settings",      // The section under which the option is shown
                                         "Minimum Land Value",  // The key of the configuration option in the configuration file
                                         "veryLow", // The default value
                                         "This sets what the lowest land value is allowed. Options are: veryLow, low, medium, high, veryHigh."); // Description of the option to show in the config file

            LandValueMaxConfig = Config.Bind("Settings",      // The section under which the option is shown
                                         "Maximum Land Value",  // The key of the configuration option in the configuration file
                                         "veryHigh", // The default value
                                         "This sets what the highest land value is allowed. Options are: veryLow, low, medium, high, veryHigh."); // Description of the option to show in the config file



            if(!Config.Bind("General", "Enabled", true).Value){
                Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is disabled.");
                return;
            }

            Logger = Log;
            allowedDensity.Clamp(DensityMinConfig.BoxedValue);
            allowedDensity.Clamp(DensityMaxConfig.BoxedValue);

            allowedLandValue.Clamp(LandValueMinConfig.BoxedValue);
            allowedLandValue.Clamp(LandValueMaxConfig.BoxedValue);

            //Stops people from breaking things in the config file
            if (!allowedDensity.IsValid(DensityMinConfig.Value)){
                Logger.LogWarning($"Minimum Density not valid, changing value to default: {DensityMinConfig.DefaultValue}");
                DensityMinConfig.BoxedValue = DensityMinConfig.DefaultValue;
            }
            if (!allowedDensity.IsValid(DensityMaxConfig.Value)){
                Logger.LogWarning($"Maximum Density not valid, changing value to default: {DensityMaxConfig.DefaultValue}");
                DensityMaxConfig.BoxedValue = DensityMaxConfig.DefaultValue;
            }

            if (!allowedLandValue.IsValid(LandValueMinConfig.Value)){
                Logger.LogWarning($"Lowest Land Value not valid, changing value to default: {LandValueMinConfig.DefaultValue}");
                LandValueMinConfig.BoxedValue = LandValueMinConfig.DefaultValue;
            }
            if (!allowedLandValue.IsValid(LandValueMaxConfig.Value)){
                Logger.LogWarning($"Highest Land Value not valid, changing value to default: {LandValueMaxConfig.DefaultValue}");
                LandValueMaxConfig.BoxedValue = LandValueMaxConfig.DefaultValue;
            }

            //Also stops people from breaking things in the config file, specifically setting the highest value to be smaller than the lowest.
            if ((int)Enum.Parse(typeof(BuildingPreset.Density), DensityMinConfig.Value) > (int)Enum.Parse(typeof(BuildingPreset.Density), DensityMaxConfig.Value)){
                Logger.LogFatal("Maximum Density Size cannot be less then the Minimum Density Size. Restoring default values now...");
                DensityMinConfig.BoxedValue = DensityMinConfig.DefaultValue;
                DensityMaxConfig.BoxedValue = DensityMaxConfig.DefaultValue;
            }
            if ((int)Enum.Parse(typeof(BuildingPreset.LandValue), LandValueMinConfig.Value) > (int)Enum.Parse(typeof(BuildingPreset.LandValue), LandValueMaxConfig.Value)){
                Logger.LogFatal("Highest Land Value cannot be less then Lowest Land Value. Restoring default values now...");
                LandValueMinConfig.BoxedValue = LandValueMinConfig.DefaultValue;
                LandValueMaxConfig.BoxedValue = LandValueMaxConfig.DefaultValue;
            }

            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            var harmony = new Harmony($"{MyPluginInfo.PLUGIN_GUID}");
            harmony.PatchAll();
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");
        }
    }
}
