using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MGSC;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;


namespace FloatingCombatText
{
    public static class Plugin
    {
        /// <summary>
        /// Handle weapon damage and life changes differently to show crits
        /// </summary>
        public static bool ProcessingDamage = false;
        public static string ModAssemblyName => Assembly.GetExecutingAssembly().GetName().Name;
        public static string ConfigPath => Path.Combine(Application.persistentDataPath, ModAssemblyName, "config.json");
        public static string ModPersistenceFolder => Path.Combine(Application.persistentDataPath, ModAssemblyName);
        public static ModConfig Config { get; private set; }
        
        
        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfig(IModContext context)
        {
            Debug.Log("Updating FloatingCombatText config with missing elements");

            Directory.CreateDirectory(ModPersistenceFolder);

            Config = ModConfig.LoadConfig(ConfigPath);

            new Harmony("Dekar_" + ModAssemblyName).PatchAll();
        }

        public static void CreateWeaponDamageFloatingText(Creature creature, DamageHitInfo hitInfo)
        {
            if (!creature.IsSeenByPlayer)
                return;
     
            var textColor = hitInfo.wasCrit ? Color.yellow : Color.white;
            var text = hitInfo.wasCrit ? hitInfo.finalDmg + "!" : hitInfo.finalDmg.ToString();

            CreateDamageFloatingText(creature, text, textColor);
        }

        public static void CreateDamageFloatingText(Creature creature, string text, Color textColor)
        {
            if (!creature.IsSeenByPlayer)
                return;

            var offsetX = Random.value * 2 * Config.DamageRandomOffsetX - Config.DamageRandomOffsetX + Config.DamagePositionX;
            var offsetY = Random.value * 2 * Config.DamageRandomOffsetY - Config.DamageRandomOffsetY + Config.DamagePositionY;
            var offsetZ = Config.DamagePositionZ;

            CreateFloatingText(creature, text, Config.DamageFontSize, Config.DamageDuration, Config.DamageFloatSpeed, textColor, Color.black, offsetX, offsetY, offsetZ);
        }
        

        public static void CreateFloatingText(Creature creature, string text, float fontSize,
            float duration, float floatSpeed, Color textColor, Color outlineColor, float offsetX, float offsetY,
            float offsetZ)
        {
            var floatingTextGameObject = new GameObject("floatingText");
            floatingTextGameObject.transform.localPosition = creature.gameObject.transform.localPosition + new Vector3(offsetX, offsetY, offsetZ);
            var textComponent = floatingTextGameObject.AddComponent<TextMeshPro>();
            var behaviourComponent = floatingTextGameObject.AddComponent<FloatingTextBehaviour>();

            behaviourComponent.FloatSpeed = floatSpeed;
            behaviourComponent.RemainingTime = duration;
            behaviourComponent.enabled = true;
            
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.lineSpacing = 1;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = textColor;
            textComponent.outlineColor = outlineColor;
            textComponent.outlineWidth = 0.3f;
        }
    }


    [HarmonyPatch(typeof(Creature), "ProcessDamage")]
    public static class Patch_OnProcessDamageCreature
    {
        public static void Prefix(Creature __instance, DamageHitInfo hitInfo)
        {
            Plugin.ProcessingDamage = true;
        }

        public static void Postfix(Creature __instance, DamageHitInfo hitInfo)
        {
            Plugin.CreateWeaponDamageFloatingText(__instance, hitInfo);
            Plugin.ProcessingDamage = false;

        }
    }

    [HarmonyPatch(typeof(Player), "ProcessDamage")]
    public static class Patch_OnProcessDamagePlayer
    {
        public static void Prefix(Player __instance, DamageHitInfo hitInfo)
        {
            Plugin.ProcessingDamage = true;
        }
        public static void Postfix(Player __instance, DamageHitInfo hitInfo)
        {
            Plugin.CreateWeaponDamageFloatingText(__instance, hitInfo);
            Plugin.ProcessingDamage = false;

        }
    }

    [HarmonyPatch(typeof(Creature), "HealthOnValueChanged")]
    public static class Patch_HealthChangedCreature
    {
        public static void Postfix(Creature __instance, int obj)
        {
            if (!Plugin.ProcessingDamage)
            {
                if(obj < 0)
                    Plugin.CreateDamageFloatingText(__instance, (-obj).ToString(), Color.green);
                else if (obj > 0)
                    Plugin.CreateDamageFloatingText(__instance, obj.ToString(), Color.white);
            }
        }
    }

    [HarmonyPatch(typeof(Player), "HealthOnValueChanged")]
    public static class Patch_HealthChangedPlayer
    {
        public static void Postfix(Player __instance, int obj)
        {
            if (!Plugin.ProcessingDamage)
            {
                if (obj < 0)
                    Plugin.CreateDamageFloatingText(__instance, (-obj).ToString(), Color.green);
                else if (obj > 0)
                    Plugin.CreateDamageFloatingText(__instance, obj.ToString(), Color.white);
            }
        }
    }


    [HarmonyPatch(typeof(Creature), nameof(Creature.OnWoundAdded))]
    public static class Patch_OnWoundAdded
    {
        public static void Postfix(Creature __instance, BodyPartWound bodyPartWound)
        {
            if (!__instance.IsSeenByPlayer)
                return;

            var offsetX = Random.value * 2 * Plugin.Config.WoundRandomOffsetX - Plugin.Config.WoundRandomOffsetX + Plugin.Config.WoundPositionX;
            var offsetY = Random.value * 2 * Plugin.Config.WoundRandomOffsetY - Plugin.Config.WoundRandomOffsetY + Plugin.Config.WoundPositionY;
            var offsetZ = Plugin.Config.WoundPositionZ;


            string woundName = GetWoundText(bodyPartWound);

            Plugin.CreateFloatingText(__instance, woundName, Plugin.Config.WoundFontSize, Plugin.Config.WoundDuration, Plugin.Config.WoundFloatSpeed, new Color(0.8f, 0.0f, 0f), new Color(0.3f, 0.0f, 0.0f), offsetX, offsetY, offsetZ);
        }

        /// <summary>
        /// Get the localized text for the wound.
        /// Adapted from MGSC.TooltipFactory.BuildBodyPartWoundTooltip(MGSC.BodyPartWound, MGSC.EffectsController)
        /// </summary>
        /// <param name="bodyPartWound"></param>
        /// <returns></returns>
        private static string GetWoundText(BodyPartWound bodyPartWound)
        {
            ItemPropertyType dmgType = ParseHelper.GetDmgType(bodyPartWound.DmgType);

            BodyPartWound item = bodyPartWound;     //Keeping to match the game's code this is from.

            StringBuilder sb = new StringBuilder();

            foreach (WoundEffect fixableWoundEffect in item.GetFixableWoundEffects())
            {
                string value = string.Empty;
                if (!item.IsFixated && fixableWoundEffect != null)
                {
                    string text = FormatHelper.FormatValue(fixableWoundEffect.ViewValue, fixableWoundEffect.ShowValueFormat);
                    string text2 = Localization.Get("woundeffect." + fixableWoundEffect.EffectId + ".short");
                    value = text + " " + text2;
                }
                //Color color = (item.IsFixated ? Colors.Yellow : Colors.LightRed);
                string natureType = Data.WoundSlots.GetRecord(item.WoundSlotId).NatureType;
                string key = "wound." + item.SlotPositionType + "." + item.DmgType + "." + natureType + ".name";
                                if (item.IsAmputation)
                {
                    key = "wound.amputation." + item.SlotPositionType + "." + item.DmgType + "." + natureType + ".name";
                }
                else if (item.IsMinor)
                {
                    key = "wound.minor." + item.DmgType + "." + natureType + ".name";
                }

                sb.AppendLine(Localization.Get(key));
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }
    }
}
