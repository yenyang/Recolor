// <copyright file="ColorPickerToolSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Common;
    using Game.Input;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A tool for picking colors from meshes.
    /// </summary>
    public partial class ColorPickerToolSystem : ToolBaseSystem
    {
        private ProxyAction m_ApplyAction;
        private ILog m_Log;
        private Entity m_PreviousRaycastedEntity;
        private Entity m_PreviousSelectedEntity;
        private EntityQuery m_HighlightedQuery;
        private SelectedInfoPanelColorFieldsSystem m_SelectedInfoPanelColorFieldsSystem;
        private ToolOutputBarrier m_Barrier;
        private GenericTooltipSystem m_GenericTooltipSystem;

        /// <inheritdoc/>
        public override string toolID => "ColorPickerTool";

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
            m_ToolRaycastSystem.collisionMask = Game.Common.CollisionMask.OnGround | Game.Common.CollisionMask.Overground;
            m_ToolRaycastSystem.typeMask = Game.Common.TypeMask.MovingObjects | Game.Common.TypeMask.StaticObjects;
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
            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            m_Log.Info($"{nameof(ColorPickerToolSystem)}.{nameof(OnCreate)}");
            m_SelectedInfoPanelColorFieldsSystem = World.GetOrCreateSystemManaged<SelectedInfoPanelColorFieldsSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_HighlightedQuery = SystemAPI.QueryBuilder()
                .WithAll<Highlighted>()
                .WithNone<Deleted, Temp, Game.Common.Overridden>()
                .Build();
            m_GenericTooltipSystem = World.GetOrCreateSystemManaged<GenericTooltipSystem>();
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_ApplyAction.shouldBeEnabled = true;
            m_Log.Debug($"{nameof(ColorPickerToolSystem)}.{nameof(OnStartRunning)}");
            m_GenericTooltipSystem.ClearTooltips();
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            m_ApplyAction.shouldBeEnabled = false;
            EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
            EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
            m_PreviousRaycastedEntity = Entity.Null;
            m_Log.Debug($"{nameof(ColorPickerToolSystem)}.{nameof(OnStopRunning)}");
            m_GenericTooltipSystem.ClearTooltips();
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            m_GenericTooltipSystem.RegisterIconTooltip("ColorPickerToolIcon", "coui://uil/Standard/PickerPipette.svg");

            if (!GetRaycastResult(out Entity currentRaycastEntity, out RaycastHit hit) || !EntityManager.TryGetBuffer(currentRaycastEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                buffer.AddComponent<BatchesUpdated>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                buffer.RemoveComponent<Highlighted>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                m_PreviousRaycastedEntity = Entity.Null;
                return inputDeps;
            }

            if (currentRaycastEntity != m_PreviousRaycastedEntity)
            {
                m_PreviousRaycastedEntity = currentRaycastEntity;
                buffer.AddComponent<BatchesUpdated>(m_HighlightedQuery, EntityQueryCaptureMode.AtRecord);
                buffer.RemoveComponent<Highlighted>(m_HighlightedQuery, EntityQueryCaptureMode.AtRecord);
            }

            if (m_HighlightedQuery.IsEmptyIgnoreFilter)
            {
                buffer.AddComponent<BatchesUpdated>(currentRaycastEntity);
                buffer.AddComponent<Highlighted>(currentRaycastEntity);
                m_PreviousRaycastedEntity = currentRaycastEntity;
            }

            if (!m_ApplyAction.WasPerformedThisFrame())
            {
                return inputDeps;
            }

            m_SelectedInfoPanelColorFieldsSystem.ChangeSelectedEntityColorSet(meshColorBuffer[0].m_ColorSet, buffer);
            m_ToolSystem.activeTool = m_DefaultToolSystem;
            return inputDeps;
        }
    }
}
