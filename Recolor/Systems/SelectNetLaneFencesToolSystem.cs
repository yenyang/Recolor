// <copyright file="SubElementBulldozerTool.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolr.Systems
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Areas;
    using Game.Buildings;
    using Game.Common;
    using Game.Input;
    using Game.Net;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Recolor;
    using Recolor.Systems;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Tool for removing subelements. For debuggin use --burst-disable-compilation launch parameter.
    /// </summary>
    public partial class SelectNetLaneFencesToolSystem : ToolBaseSystem
    {
        private ProxyAction m_ApplyAction;
        private BulldozeToolSystem m_BulldozeToolSystem;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private EntityQuery m_OwnedQuery;
        private ILog m_Log;
        private Entity m_PreviousRaycastedEntity;
        private EntityQuery m_HighlightedQuery;
        private GenericTooltipSystem m_TooltipSystem;

        /// <inheritdoc/>
        public override string toolID => "Select Net Lane Fences Tool";

        /// <inheritdoc/>
        public override PrefabBase GetPrefab()
        {
            return m_BulldozeToolSystem.GetPrefab();
        }

        /// <inheritdoc/>
        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        /// <inheritdoc/>
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
            m_ToolRaycastSystem.typeMask = TypeMask.Lanes;
            m_ToolRaycastSystem.netLayerMask = Layer.Fence;
            m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.Fence;
            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.NoMainElements;
        }

        /// <summary>
        /// For stopping the tool. Probably with esc key.
        /// </summary>
        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            Enabled = false;
            m_Log = Mod.Instance.Log;
            m_Log.Info($"[{nameof(SelectNetLaneFencesToolSystem)}] {nameof(OnCreate)}");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_TooltipSystem = World.GetExistingSystemManaged<GenericTooltipSystem>();
            m_BulldozeToolSystem = World.GetOrCreateSystemManaged<BulldozeToolSystem>();
            base.OnCreate();
            m_OwnedQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Owner>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Overridden>(),
                    },
                },
            });
            m_HighlightedQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Highlighted>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Overridden>(),
                    },
                },
            });
            RequireForUpdate(m_OwnedQuery);

            m_ApplyAction = Mod.Instance.Settings.GetAction(Mod.SelectNetLaneFencesToolApplyMimicAction);
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            m_ApplyAction.shouldBeEnabled = true;
            m_Log.Debug($"{nameof(SelectNetLaneFencesToolSystem)}.{nameof(OnStartRunning)}");
            m_TooltipSystem.RegisterTooltip(,"Select a NetLane Fence.");
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            m_ApplyAction.shouldBeEnabled = false;
            EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
            EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
            m_PreviousRaycastedEntity = Entity.Null;
            m_TooltipSystem.ClearTooltips();
            base.OnStopRunning();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            bool raycastFlag = GetRaycastResult(out Entity currentRaycastEntity, out RaycastHit hit);
            bool hasOwnerComponentFlag = EntityManager.TryGetComponent(currentRaycastEntity, out Owner owner);
            EntityCommandBuffer buffer = m_ToolOutputBarrier.CreateCommandBuffer();

            // This section handles highlight removal.
            if (m_PreviousRaycastedEntity != currentRaycastEntity || !raycastFlag || currentRaycastEntity == Entity.Null)
            {
                EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
                EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
                m_PreviousRaycastedEntity = currentRaycastEntity;
            }

            if (!raycastFlag || currentRaycastEntity == Entity.Null)
            {
                return inputDeps;
            }

            if (m_HighlightedQuery.IsEmptyIgnoreFilter && raycastFlag && hasOwnerComponentFlag)
            {
                buffer.AddComponent<Highlighted>(currentRaycastEntity);
                buffer.AddComponent<BatchesUpdated>(currentRaycastEntity);
            }

            if (m_ApplyAction.WasPressedThisFrame())
            {
                m_ToolSystem.selected = currentRaycastEntity;
            }

            return inputDeps;
        }
    }
}
