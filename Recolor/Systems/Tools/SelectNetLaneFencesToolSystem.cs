// <copyright file="SelectNetLaneFencesToolSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Tools
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Common;
    using Game.Input;
    using Game.Net;
    using Game.Prefabs;
    using Game.Tools;
    using Recolor;
    using Recolor.Settings;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Tool for selecting net lane fences and hedges.
    /// </summary>
    public partial class SelectNetLaneFencesToolSystem : ToolBaseSystem
    {
        private ToolOutputBarrier m_ToolOutputBarrier;
        private ILog m_Log;
        private Entity m_PreviousRaycastedEntity;
        private EntityQuery m_HighlightedQuery;
        private GenericTooltipSystem m_TooltipSystem;
        private ProxyAction m_ActivateAction;

        /// <inheritdoc/>
        public override string toolID => "Select Net Lane Fences Tool";

        /// <inheritdoc/>
        public override PrefabBase GetPrefab()
        {
            return null;
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
            base.OnCreate();
            Enabled = false;
            m_Log = Mod.Instance.Log;
            m_Log.Info($"[{nameof(SelectNetLaneFencesToolSystem)}] {nameof(OnCreate)}");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_TooltipSystem = World.GetExistingSystemManaged<GenericTooltipSystem>();
            m_ActivateAction = Mod.Instance.Settings.GetAction(Setting.FenceSelectorModeActionName);
            m_ToolSystem.EventToolChanged += (tool) => m_ActivateAction.shouldBeEnabled = tool == m_DefaultToolSystem;
            m_HighlightedQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
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

            m_ActivateAction.shouldBeEnabled = true;

            m_ActivateAction.onInteraction += (_, _) => m_ToolSystem.activeTool = this;
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            applyAction.enabled = true;
            m_Log.Debug($"{nameof(SelectNetLaneFencesToolSystem)}.{nameof(OnStartRunning)}");
            m_TooltipSystem.RegisterTooltip("SelectANetLaneFence", Game.UI.Tooltip.TooltipColor.Info, LocaleEN.MouseTooltipKey("SelectANetLaneFence"), "Select a NetLane Fence.");
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
            EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
            m_PreviousRaycastedEntity = Entity.Null;
            m_TooltipSystem.ClearTooltips();
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
            bool raycastFlag = GetRaycastResult(out Entity currentRaycastEntity, out RaycastHit _);
            bool hasOwnerComponentFlag = EntityManager.TryGetComponent(currentRaycastEntity, out Owner _);
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

            if (applyAction.WasPressedThisFrame())
            {
                m_ToolSystem.selected = currentRaycastEntity;
                m_ToolSystem.activeTool = m_DefaultToolSystem;
            }

            return inputDeps;
        }
    }
}
