using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace FloatingCombatText
{
    public class ModConfig
    {
        public float DamageFontSize { get; set; } = 1f;
        public float DamageDuration { get; set; } = 2f;
        public float DamageFloatSpeed { get; set; } = 0.1f;
        public float DamagePositionX { get; set; } = 0f;
        public float DamagePositionY { get; set; } = 0.2f;
        public float DamagePositionZ { get; set; } = -2f;
        public float DamageRandomOffsetX { get; set; } = 0.10f;
        public float DamageRandomOffsetY { get; set; } = 0.15f;
        public float WoundFontSize { get; set; } = 1f;
        public float WoundDuration { get; set; } = 2f;
        public float WoundFloatSpeed { get; set; } = 0.1f;
        public float WoundPositionX { get; set; } = 0f;
        public float WoundPositionY { get; set; } = 0.1f;
        public float WoundPositionZ { get; set; } = -2f;
        public float WoundRandomOffsetX { get; set; } = 0.0f;
        public float WoundRandomOffsetY { get; set; } = 0.1f;

        public static ModConfig LoadConfig(string configPath)
        {
            ModConfig config;

            JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            };

            if (File.Exists(configPath))
            {
                try
                {
                    string sourceJson = File.ReadAllText(configPath);

                    config = JsonConvert.DeserializeObject<ModConfig>(sourceJson, serializerSettings);

                    //Add any new elements that have been added since the last mod version the user had.
                    string upgradeConfig = JsonConvert.SerializeObject(config, serializerSettings);

                    if (upgradeConfig != sourceJson)
                    {
                        Debug.Log("Updating config with missing elements");
                        //re-write
                        File.WriteAllText(configPath, upgradeConfig);
                    }


                    return config;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error parsing configuration. Ignoring config file and using defaults");
                    Debug.LogException(ex);

                    //Not overwriting in case the user just made a typo.
                    config = new ModConfig();
                    return config;
                }
            }

            config = new ModConfig();

            string json = JsonConvert.SerializeObject(config, serializerSettings);
            File.WriteAllText(configPath, json);

            return config;
        }
    }
}
