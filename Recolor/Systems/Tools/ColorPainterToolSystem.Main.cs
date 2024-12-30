// <copyright file="ColorPainterToolSystem.Main.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Tools
{
    using System;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Buildings;
    using Game.Common;
    using Game.Input;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Game.Vehicles;
    using Recolor.Domain;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
    using Recolor.Systems.SelectedInfoPanel;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using static Recolor.Systems.SelectedInfoPanel.SIPColorFieldsSystem;

    /// <summary>
    /// A tool for painting colors onto meshes.
    /// </summary>
    public partial class ColorPainterToolSystem : ToolBaseSystem
    {
        private ProxyAction m_ApplyAction;
        private ProxyAction m_SecondaryApplyAction;
        private ILog m_Log;
        private Entity m_PreviousRaycastedEntity;
        private Entity m_PreviousSelectedEntity;
        private EntityQuery m_HighlightedQuery;
        private SIPColorFieldsSystem m_SelectedInfoPanelColorFieldsSystem;
        private CustomColorVariationSystem m_CustomColorVariationSystem;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private ToolOutputBarrier m_Barrier;
        private GenericTooltipSystem m_GenericTooltipSystem;
        private ColorPainterUISystem m_ColorPainterUISystem;
        private EntityQuery m_BuildingMeshColorQuery;
        private EntityQuery m_VehicleMeshColorQuery;
        private EntityQuery m_ParkedVehicleMeshColorQuery;
        private EntityQuery m_PropMeshColorQuery;
        private EntityQuery m_BuildingCustomMeshColorQuery;
        private EntityQuery m_VehicleCustomMeshColorQuery;
        private EntityQuery m_ParkedVehicleCustomMeshColorQuery;
        private EntityQuery m_PropCustomMeshColorQuery;

        /// <inheritdoc/>
        public override string toolID => "ColorPainterTool";

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

        /// <summary>
        /// So tree controller can check if something in this save game has custom color variation.
        /// </summary>
        /// <param name="prefabEntity">Prefab entity for submesh.</param>
        /// <param name="index">color variation index.</param>
        /// <returns>True if has custom color variation, false if not.</returns>
        public bool HasCustomColorVariation(Entity prefabEntity, int index)
        {
            if (!m_CustomColorVariationSystem.TryGetCustomColorVariation(prefabEntity, index, out CustomColorVariations _))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single || m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker)
            {
                m_ToolRaycastSystem.collisionMask = Game.Common.CollisionMask.OnGround | Game.Common.CollisionMask.Overground;
                m_ToolRaycastSystem.typeMask = Game.Common.TypeMask.MovingObjects | Game.Common.TypeMask.StaticObjects | TypeMask.Lanes;
                m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubBuildings | RaycastFlags.SubElements;
                m_ToolRaycastSystem.netLayerMask = Game.Net.Layer.Fence;
                m_ToolRaycastSystem.utilityTypeMask = Game.Net.UtilityTypes.Fence;
            }
            else if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius)
            {
                m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
            }
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
            m_Log.Info($"{nameof(ColorPainterToolSystem)}.{nameof(OnCreate)}");
            m_SelectedInfoPanelColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();
            m_CustomColorVariationSystem = World.GetOrCreateSystemManaged<CustomColorVariationSystem>();
            m_ColorPainterUISystem = World.GetOrCreateSystemManaged<ColorPainterUISystem>();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_HighlightedQuery = SystemAPI.QueryBuilder()
                .WithAll<Highlighted>()
                .WithNone<Deleted, Temp, Game.Common.Overridden>()
                .Build();
            m_GenericTooltipSystem = World.GetOrCreateSystemManaged<GenericTooltipSystem>();

            m_BuildingMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Building, MeshColor, Game.Objects.Transform>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_VehicleMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Vehicle, MeshColor, InterpolatedTransform>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_ParkedVehicleMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Vehicle, MeshColor, Game.Objects.Transform, ParkedCar>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_PropMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Objects.Object, Game.Objects.Static, MeshColor, Game.Objects.Transform>()
                .WithNone<Temp, Deleted, Game.Common.Overridden, Tree, Plant, Building>()
                .Build();

            m_BuildingCustomMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Building, MeshColor, Game.Objects.Transform, CustomMeshColor>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_VehicleCustomMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Vehicle, MeshColor, InterpolatedTransform, CustomMeshColor>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_ParkedVehicleCustomMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Vehicle, MeshColor, Game.Objects.Transform, ParkedCar, CustomMeshColor>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_PropCustomMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Objects.Object, Game.Objects.Static, MeshColor, Game.Objects.Transform, CustomMeshColor>()
                .WithNone<Temp, Deleted, Game.Common.Overridden, Tree, Plant, Building>()
                .Build();

            m_ApplyAction = Mod.Instance.Settings.GetAction(Mod.PainterApplyMimicAction);

            m_SecondaryApplyAction = Mod.Instance.Settings.GetAction(Mod.PainterSecondaryApplyMimicAction);
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_ApplyAction.shouldBeEnabled = true;
            m_SecondaryApplyAction.shouldBeEnabled = true;
            m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(OnStartRunning)}");
            m_GenericTooltipSystem.ClearTooltips();
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            m_ApplyAction.shouldBeEnabled = false;
            m_SecondaryApplyAction.shouldBeEnabled = false;
            EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
            EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
            m_PreviousRaycastedEntity = Entity.Null;
            m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(OnStopRunning)}");
            m_GenericTooltipSystem.ClearTooltips();
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint)
            {
                m_GenericTooltipSystem.RemoveTooltip("ColorPickerToolIcon");
                m_GenericTooltipSystem.RemoveTooltip("ResetToolIcon");
                m_GenericTooltipSystem.RegisterIconTooltip("ColorPainterToolIcon", "coui://uil/Colored/ColorPalette.svg");
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker)
            {
                m_GenericTooltipSystem.RemoveTooltip("ColorPainterToolIcon");
                m_GenericTooltipSystem.RemoveTooltip("ResetToolIcon");
                m_GenericTooltipSystem.RegisterIconTooltip("ColorPickerToolIcon", "coui://uil/Standard/PickerPipette.svg");
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Reset)
            {
                m_GenericTooltipSystem.RemoveTooltip("ColorPainterToolIcon");
                m_GenericTooltipSystem.RemoveTooltip("ColorPickerToolIcon");
                m_GenericTooltipSystem.RegisterIconTooltip("ResetToolIcon", "coui://uil/Standard/Reset.svg");
            }

            bool raycastResult = GetRaycastResult(out Entity currentRaycastEntity, out RaycastHit hit);

            if ((m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Reset && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
                || (m_SecondaryApplyAction.WasPerformedThisFrame() && m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single))
            {
                if (!raycastResult
                || !EntityManager.HasBuffer<MeshColor>(currentRaycastEntity)
                || (!EntityManager.HasBuffer<CustomMeshColor>(currentRaycastEntity) && m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                || (!m_SelectedInfoPanelColorFieldsSystem.SingleInstance && m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(currentRaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet colorSet) && m_SelectedInfoPanelColorFieldsSystem.MatchesEntireVanillaColorSet(colorSet, assetSeasonIdentifier))
                || (hit.m_HitPosition.x == 0 && hit.m_HitPosition.y == 0 && hit.m_HitPosition.z == 0))
                {
                    buffer.AddComponent<BatchesUpdated>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                    buffer.RemoveComponent<Highlighted>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                    m_PreviousRaycastedEntity = Entity.Null;
                    return inputDeps;
                }
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
            {
                if (!raycastResult
                    || !EntityManager.HasBuffer<MeshColor>(currentRaycastEntity)
                    || (EntityManager.HasComponent<Plant>(currentRaycastEntity) && m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                    || (EntityManager.HasBuffer<CustomMeshColor>(currentRaycastEntity) && !m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                    || (hit.m_HitPosition.x == 0 && hit.m_HitPosition.y == 0 && hit.m_HitPosition.z == 0))
                {
                    buffer.AddComponent<BatchesUpdated>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                    buffer.RemoveComponent<Highlighted>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                    m_PreviousRaycastedEntity = Entity.Null;

                    if (EntityManager.HasComponent<Plant>(currentRaycastEntity) && m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                    {
                        m_GenericTooltipSystem.RegisterTooltip("SingleInstancePlantWarning", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.MouseTooltipKey("SingleInstancePlantWarning"), "Single instance color changes for plants is not currently supported.");
                    }
                    else
                    {
                        m_GenericTooltipSystem.RemoveTooltip("SingleInstancePlantWarning");
                    }

                    if (EntityManager.HasBuffer<CustomMeshColor>(currentRaycastEntity) && !m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                    {
                        m_GenericTooltipSystem.RegisterTooltip("HasCustomMeshColorWarning", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.MouseTooltipKey("HasCustomMeshColorWarning"), "Cannot change color variation on this because it has custom instance colors.");
                    }
                    else
                    {
                        m_GenericTooltipSystem.RemoveTooltip("HasCustomMeshColorWarning");
                    }

                    return inputDeps;
                }
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker)
            {
                if (!raycastResult
                    || !EntityManager.HasBuffer<MeshColor>(currentRaycastEntity)
                    || (hit.m_HitPosition.x == 0 && hit.m_HitPosition.y == 0 && hit.m_HitPosition.z == 0))
                {
                    buffer.AddComponent<BatchesUpdated>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                    buffer.RemoveComponent<Highlighted>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                    m_PreviousRaycastedEntity = Entity.Null;
                    return inputDeps;
                }
            }

            m_GenericTooltipSystem.RemoveTooltip("SingleInstancePlantWarning");
            m_GenericTooltipSystem.RemoveTooltip("HasCustomMeshColorWarning");

            if (currentRaycastEntity != m_PreviousRaycastedEntity && !m_HighlightedQuery.IsEmptyIgnoreFilter)
            {
                m_PreviousRaycastedEntity = currentRaycastEntity;
                buffer.AddComponent<BatchesUpdated>(m_HighlightedQuery, EntityQueryCaptureMode.AtRecord);
                buffer.RemoveComponent<Highlighted>(m_HighlightedQuery, EntityQueryCaptureMode.AtRecord);
            }

            if (m_HighlightedQuery.IsEmptyIgnoreFilter && (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single || m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker))
            {
                buffer.AddComponent<BatchesUpdated>(currentRaycastEntity);
                buffer.AddComponent<Highlighted>(currentRaycastEntity);
                m_PreviousRaycastedEntity = currentRaycastEntity;
            }

            float radius = m_ColorPainterUISystem.Radius;
            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius && m_ColorPainterUISystem.ToolMode != ColorPainterUISystem.PainterToolMode.Picker)
            {
                ToolRadiusJob toolRadiusJob = new ()
                {
                    m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle),
                    m_Position = new Vector3(hit.m_HitPosition.x, hit.m_Position.y, hit.m_HitPosition.z),
                    m_Radius = radius,
                };
                inputDeps = IJobExtensions.Schedule(toolRadiusJob, JobHandle.CombineDependencies(inputDeps, outJobHandle));
                m_OverlayRenderSystem.AddBufferWriter(inputDeps);
            }

            if (!m_ApplyAction.WasPerformedThisFrame() && !m_ApplyAction.IsPressed() && !m_SecondaryApplyAction.WasPerformedThisFrame() && !m_SecondaryApplyAction.IsPressed())
            {
                return inputDeps;
            }

            if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single && m_ApplyAction.WasPerformedThisFrame())
            {
                if (m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                {
                    ChangeInstanceColorSet(m_ColorPainterUISystem.ColorSet, ref buffer, currentRaycastEntity);
                }
                else if (!m_SelectedInfoPanelColorFieldsSystem.SingleInstance && m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(currentRaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet _))
                {
                    ChangeColorVariation(m_ColorPainterUISystem.ColorSet, ref buffer, currentRaycastEntity, assetSeasonIdentifier);
                    GenerateOrUpdateCustomColorVariationEntity(currentRaycastEntity, ref buffer, assetSeasonIdentifier);
                }
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius && m_ApplyAction.IsPressed())
            {
                ChangeMeshColorWithinRadiusJob changeMeshColorWithinRadiusJob = new ()
                {
                    m_EntityType = SystemAPI.GetEntityTypeHandle(),
                    m_Position = hit.m_HitPosition,
                    m_Radius = radius,
                    m_TransformType = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true),
                    m_ApplyColorSet = m_ColorPainterUISystem.ColorSet,
                    buffer = m_Barrier.CreateCommandBuffer(),
                };

                if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Building)
                {
                    inputDeps = JobChunkExtensions.Schedule(changeMeshColorWithinRadiusJob, m_BuildingMeshColorQuery, inputDeps);
                    m_Barrier.AddJobHandleForProducer(inputDeps);
                }
                else if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Props)
                {
                    inputDeps = JobChunkExtensions.Schedule(changeMeshColorWithinRadiusJob, m_PropMeshColorQuery, inputDeps);
                    m_Barrier.AddJobHandleForProducer(inputDeps);
                }
                else if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Vehicles)
                {
                    inputDeps = JobChunkExtensions.Schedule(changeMeshColorWithinRadiusJob, m_ParkedVehicleMeshColorQuery, inputDeps);
                    m_Barrier.AddJobHandleForProducer(inputDeps);
                }

                if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Vehicles)
                {
                    ChangeVehicleMeshColorWithinRadiusJob changeVehicleMeshColorWithinRadiusJob = new()
                    {
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_Position = hit.m_HitPosition,
                        m_Radius = radius,
                        m_InterpolatedTransformType = SystemAPI.GetComponentTypeHandle<InterpolatedTransform>(isReadOnly: true),
                        m_ApplyColorSet = m_ColorPainterUISystem.ColorSet,
                        buffer = m_Barrier.CreateCommandBuffer(),
                    };
                    inputDeps = JobChunkExtensions.Schedule(changeVehicleMeshColorWithinRadiusJob, m_VehicleMeshColorQuery, inputDeps);
                    m_Barrier.AddJobHandleForProducer(inputDeps);
                }
            }
            else if ((m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Reset && m_ApplyAction.WasPerformedThisFrame() && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
                    || (m_SecondaryApplyAction.WasPerformedThisFrame() && m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single))
            {
                if (m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                {
                    buffer.RemoveComponent<CustomMeshColor>(currentRaycastEntity);
                    buffer.AddComponent<BatchesUpdated>(currentRaycastEntity);
                }
                else if (!m_SelectedInfoPanelColorFieldsSystem.SingleInstance && m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(currentRaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet _) && m_SelectedInfoPanelColorFieldsSystem.TryGetVanillaColorSet(assetSeasonIdentifier, out ColorSet VanillaColorSet))
                {
                    ChangeColorVariation(VanillaColorSet, ref buffer, currentRaycastEntity, assetSeasonIdentifier);
                    DeleteCustomColorVariationEntity(currentRaycastEntity, ref buffer, assetSeasonIdentifier);
                }
            }
            else if ((m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Reset && m_ApplyAction.IsPressed() && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius)
                || (m_SecondaryApplyAction.IsPressed() && m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius))
            {
                ResetMeshColorWithinRadiusJob resetCustomMeshColorWithinRadiusJob = new ()
                {
                    m_EntityType = SystemAPI.GetEntityTypeHandle(),
                    m_Position = hit.m_HitPosition,
                    m_Radius = radius,
                    m_TransformType = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true),
                    buffer = m_Barrier.CreateCommandBuffer(),
                };

                if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Building)
                {
                    inputDeps = JobChunkExtensions.Schedule(resetCustomMeshColorWithinRadiusJob, m_BuildingCustomMeshColorQuery, inputDeps);
                    m_Barrier.AddJobHandleForProducer(inputDeps);
                }
                else if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Props)
                {
                    inputDeps = JobChunkExtensions.Schedule(resetCustomMeshColorWithinRadiusJob, m_PropCustomMeshColorQuery, inputDeps);
                    m_Barrier.AddJobHandleForProducer(inputDeps);
                }
                else if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Vehicles)
                {
                    inputDeps = JobChunkExtensions.Schedule(resetCustomMeshColorWithinRadiusJob, m_ParkedVehicleCustomMeshColorQuery, inputDeps);
                    m_Barrier.AddJobHandleForProducer(inputDeps);
                }

                if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Vehicles)
                {
                    ResetVehicleMeshColorWithinRadiusJob resetVehicleMeshColorWithinRadiusJob = new ()
                    {
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_Position = hit.m_HitPosition,
                        m_Radius = radius,
                        m_InterpolatedTransformType = SystemAPI.GetComponentTypeHandle<InterpolatedTransform>(isReadOnly: true),
                        buffer = m_Barrier.CreateCommandBuffer(),
                    };
                    inputDeps = JobChunkExtensions.Schedule(resetVehicleMeshColorWithinRadiusJob, m_VehicleCustomMeshColorQuery, inputDeps);
                    m_Barrier.AddJobHandleForProducer(inputDeps);
                }
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker && m_ApplyAction.WasPerformedThisFrame() && EntityManager.TryGetBuffer(currentRaycastEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) && meshColorBuffer.Length > 0)
            {
                m_ColorPainterUISystem.ColorSet = meshColorBuffer[0].m_ColorSet;
                m_ColorPainterUISystem.ToolMode = ColorPainterUISystem.PainterToolMode.Paint;
            }

            return inputDeps;
        }
    }
}
