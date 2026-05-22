// <copyright file="VisualCustomizeSectionHasMeshColorsPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Patches
{
    using Game.UI.InGame;
    using HarmonyLib;

    /// <summary>
    /// Patches Visual CustomizeSection HasMeshColors to return false if option enabled was enabled at startup.
    /// </summary>
    [HarmonyPatch(typeof(VisualCustomizeSection), "HasMeshColors")]
    public class VisualCustomizeSectionHasMeshColorsPatch
    {
        /// <summary>
        /// Patches Visual CustomizeSection HasMeshColors to return false if option was enabled at startup.
        /// </summary>
        /// <param name="__result">Used to override the result of original method.</param>
        /// <returns>True so that the original method runs, false to not.</returns>
        public static bool Prefix(ref bool __result)
        {
            if (Mod.Instance.HideVanillaVisualCustomizeSection)
            {
                __result = false;

                return false;
            }

            return true;
        }
    }
}
