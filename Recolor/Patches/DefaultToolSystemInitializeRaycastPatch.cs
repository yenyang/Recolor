// <copyright file="DefaultToolSystemInitializeRaycastPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Patches
{
    using Game.Common;
    using Game.Input;
    using Game.Net;
    using Game.Tools;
    using HarmonyLib;
    using Unity.Entities;

    /// <summary>
    ///  Patches Initialize Raycast of default tool system to allow selecting subelement nelane fences.
    /// </summary>
    [HarmonyPatch(typeof(DefaultToolSystem), "InitializeRaycast")]
    public class DefaultToolSystemInitializeRaycastPatch
    {
        /// <summary>
        /// Patches Initialize Raycast of default tool system to allow selecting subelement nelane fences.
        /// </summary>
        public static void Postfix()
        {
            DefaultToolSystem defaultToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DefaultToolSystem>();
            ProxyAction fenceSelectorAction = Mod.Instance.Settings.GetAction(Settings.Setting.FenceSelectorModeActionName);
            if (fenceSelectorAction.IsPressed())
            {
                ToolRaycastSystem toolRaycastSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolRaycastSystem>();
                toolRaycastSystem.typeMask = TypeMask.Lanes;
                toolRaycastSystem.netLayerMask = Layer.Fence;
                toolRaycastSystem.utilityTypeMask = UtilityTypes.Fence;
                toolRaycastSystem.raycastFlags = RaycastFlags.SubElements | RaycastFlags.NoMainElements | RaycastFlags.Markers | RaycastFlags.EditorContainers;
            }
        }
    }
}
