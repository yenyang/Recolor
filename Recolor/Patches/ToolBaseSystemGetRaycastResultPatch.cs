// <copyright file="ToolBaseSystemGetRaycastResultPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Patches
{
    using System;
    using Colossal.Entities;
    using Game;
    using Game.Common;
    using Game.Input;
    using Game.Prefabs;
    using Game.Tools;
    using HarmonyLib;
    using Recolor.Extensions;
    using Unity.Entities;

    /// <summary>
    /// Patches ToolBaseSystem GetRaycastResult to alter raycast results.
    /// </summary>
    [HarmonyPatch(typeof(ToolBaseSystem), "GetRaycastResult", new Type[] { typeof(Entity), typeof(RaycastHit) }, new ArgumentType[] { ArgumentType.Out, ArgumentType.Out })]

    public class ToolBaseSystemGetRaycastResultPatch
    {
        /// <summary>
        /// Patches ToolBaseSystem GetRaycastResult to alter raycast results.
        /// </summary>
        /// <param name="entity">Entity that is being raycasted.</param>
        /// <param name="hit">The resulting raycast hit.</param>
        /// <returns>True is not actually patching method. False if patching method.</returns>
        public static bool Prefix(out Entity entity, out RaycastHit hit)
        {
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            DefaultToolSystem defaultToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DefaultToolSystem>();
            if (toolSystem.activeTool != defaultToolSystem || !toolSystem.actionMode.IsGame())
            {
                entity = Entity.Null;
                hit = default;
                return true;
            }

            ToolRaycastSystem toolRaycastSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolRaycastSystem>();
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            bool raycastHitSomething = toolRaycastSystem.GetRaycastResult(out var result);

            Mod.Instance.Log.Debug($"{nameof(ToolBaseSystemGetRaycastResultPatch)}.{nameof(Prefix)} m_HitEntity = {result.m_Hit.m_HitEntity.Index}:{result.m_Hit.m_HitEntity.Version}");
            Mod.Instance.Log.Debug($"{nameof(ToolBaseSystemGetRaycastResultPatch)}.{nameof(Prefix)} m_Owner = {result.m_Owner.Index}:{result.m_Owner.Version}");

            if (toolSystem.EntityManager.TryGetComponent(result.m_Hit.m_HitEntity, out PrefabRef prefabRef) &&
                prefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase) &&
                prefabBase is NetLaneGeometryPrefab)
            {
                Mod.Instance.Log.Debug($"{nameof(ToolBaseSystemGetRaycastResultPatch)}.{nameof(Prefix)} Hit a fence prefab");
                defaultToolSystem.EntityManager.AddComponent<Highlighted>(result.m_Hit.m_HitEntity);
                defaultToolSystem.EntityManager.AddComponent<BatchesUpdated>(result.m_Hit.m_HitEntity);
                ProxyAction defaultToolApplyMimic = Mod.Instance.Settings.GetAction(Mod.SelectNetLaneFencesToolApplyMimicAction);
                if (defaultToolApplyMimic.WasPerformedThisFrame())
                {
                    toolSystem.selected = result.m_Hit.m_HitEntity;

                    Mod.Instance.Log.Debug($"{nameof(ToolBaseSystemGetRaycastResultPatch)}.{nameof(Prefix)} selected a fence prefab");
                }
            }

            if (raycastHitSomething && !toolSystem.EntityManager.HasComponent<Deleted>(result.m_Owner))
            {
                entity = result.m_Owner;
                hit = result.m_Hit;
                return true;
            }

            entity = Entity.Null;
            hit = default;
            return true;
        }
    }
}
