// <copyright file="ColorPainterToolSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems
{
    using System;
    using System.Linq;
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
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using static Recolor.Systems.SelectedInfoPanelColorFieldsSystem;

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
        private SelectedInfoPanelColorFieldsSystem m_SelectedInfoPanelColorFieldsSystem;
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
            if (!m_CustomColorVariationSystem.TryGetCustomColorVariation(prefabEntity, index, out CustomColorVariations customColorVariation))
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
                m_ToolRaycastSystem.typeMask = Game.Common.TypeMask.MovingObjects | Game.Common.TypeMask.StaticObjects;
                m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubBuildings | RaycastFlags.SubElements;
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
            m_SelectedInfoPanelColorFieldsSystem = World.GetOrCreateSystemManaged<SelectedInfoPanelColorFieldsSystem>();
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
                else if (!m_SelectedInfoPanelColorFieldsSystem.SingleInstance && m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(currentRaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet colorSet))
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
                else if (!m_SelectedInfoPanelColorFieldsSystem.SingleInstance && m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(currentRaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet colorSet) && m_SelectedInfoPanelColorFieldsSystem.TryGetVanillaColorSet(assetSeasonIdentifier, out ColorSet VanillaColorSet))
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

        private void ChangeInstanceColorSet(ColorSet colorSet, ref EntityCommandBuffer buffer, Entity entity)
        {
            if (m_SelectedInfoPanelColorFieldsSystem.SingleInstance && !EntityManager.HasComponent<Game.Objects.Plant>(entity) && EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                if (!EntityManager.HasBuffer<CustomMeshColor>(entity))
                {
                    DynamicBuffer<CustomMeshColor> newBuffer = EntityManager.AddBuffer<CustomMeshColor>(entity);
                    foreach (MeshColor meshColor in meshColorBuffer)
                    {
                        newBuffer.Add(new CustomMeshColor(meshColor));
                    }
                }

                if (!EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer))
                {
                    return;
                }

                int length = meshColorBuffer.Length;
                if (EntityManager.HasComponent<Tree>(entity))
                {
                    length = Math.Min(4, meshColorBuffer.Length);
                }

                for (int i = 0; i < length; i++)
                {
                    CustomMeshColor customMeshColor = customMeshColorBuffer[i];
                    customMeshColor.m_ColorSet = colorSet;
                    customMeshColorBuffer[i] = customMeshColor;
                    buffer.AddComponent<BatchesUpdated>(entity);
                }
            }
        }

        private void ChangeColorVariation(ColorSet colorSet, ref EntityCommandBuffer buffer, Entity entity, AssetSeasonIdentifier assetSeasonIdentifier)
        {
            if (!EntityManager.HasBuffer<CustomMeshColor>(entity))
            {
                if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) || !EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    return;
                }

                int length = subMeshBuffer.Length;
                if (EntityManager.HasComponent<Tree>(entity))
                {
                    length = Math.Min(4, subMeshBuffer.Length);
                }

                for (int i = 0; i < length; i++)
                {
                    if (!EntityManager.TryGetBuffer(subMeshBuffer[i].m_SubMesh, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < assetSeasonIdentifier.m_Index)
                    {
                        continue;
                    }

                    ColorVariation colorVariation = colorVariationBuffer[assetSeasonIdentifier.m_Index];
                    colorVariation.m_ColorSet = colorSet;
                    colorVariationBuffer[assetSeasonIdentifier.m_Index] = colorVariation;
                    buffer.AddComponent<BatchesUpdated>(entity);
                }

                return;
            }
        }

        private void GenerateOrUpdateCustomColorVariationEntity(Entity instanceEntity, ref EntityCommandBuffer buffer, AssetSeasonIdentifier assetSeasonIdentifier)
        {
            if (!EntityManager.TryGetComponent(instanceEntity, out PrefabRef prefabRef) || !EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < assetSeasonIdentifier.m_Index)
            {
                return;
            }

            ColorSet colorSet = colorVariationBuffer[assetSeasonIdentifier.m_Index].m_ColorSet;
            if (!EntityManager.HasComponent<Game.Objects.Tree>(instanceEntity))
            {
                m_CustomColorVariationSystem.CreateOrUpdateCustomColorVariationEntity(buffer, subMeshBuffer[0].m_SubMesh, colorSet, assetSeasonIdentifier.m_Index);
            }
            else
            {
                int length = Math.Min(4, subMeshBuffer.Length);
                for (int i = 0; i < length; i++)
                {
                    if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[i].m_SubMesh, out PrefabBase prefabBase))
                    {
                        continue;
                    }

                    AssetSeasonIdentifier currentAssetSeasonIdentifier = new AssetSeasonIdentifier()
                    {
                        m_Index = assetSeasonIdentifier.m_Index,
                        m_PrefabID = prefabBase.GetPrefabID(),
                        m_Season = assetSeasonIdentifier.m_Season,
                    };

                    m_CustomColorVariationSystem.CreateOrUpdateCustomColorVariationEntity(buffer, subMeshBuffer[i].m_SubMesh, colorSet, currentAssetSeasonIdentifier.m_Index);
                }
            }

            EntityQuery prefabRefQuery = SystemAPI.QueryBuilder()
                .WithAll<PrefabRef>()
                .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                .Build();

            NativeArray<Entity> entities = prefabRefQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity e in entities)
            {
                if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer) && currentSubMeshBuffer[0].m_SubMesh == subMeshBuffer[0].m_SubMesh)
                {
                    buffer.AddComponent<BatchesUpdated>(e);
                }
            }
        }

        private void DeleteCustomColorVariationEntity(Entity instanceEntity, ref EntityCommandBuffer buffer, AssetSeasonIdentifier assetSeasonIdentifier)
        {
            if (!EntityManager.TryGetComponent(instanceEntity, out PrefabRef prefabRef) || !EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }


            if (!EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < assetSeasonIdentifier.m_Index)
            {
                return;
            }

            if (!EntityManager.HasComponent<Game.Objects.Tree>(instanceEntity))
            {
                m_CustomColorVariationSystem.DeleteCustomColorVariationEntity(buffer, subMeshBuffer[0].m_SubMesh);
            }
            else
            {
                int length = Math.Min(4, subMeshBuffer.Length);
                for (int i = 0; i < length; i++)
                {
                    if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[i].m_SubMesh, out PrefabBase prefabBase))
                    {
                        continue;
                    }

                    m_CustomColorVariationSystem.DeleteCustomColorVariationEntity(buffer, subMeshBuffer[i].m_SubMesh);
                }
            }

            EntityQuery prefabRefQuery = SystemAPI.QueryBuilder()
                .WithAll<PrefabRef>()
                .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                .Build();

            NativeArray<Entity> entities = prefabRefQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity e in entities)
            {
                if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer) && currentSubMeshBuffer[0].m_SubMesh == subMeshBuffer[0].m_SubMesh)
                {
                    buffer.AddComponent<BatchesUpdated>(e);
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct ToolRadiusJob : IJob
        {
            public OverlayRenderSystem.Buffer m_OverlayBuffer;
            public float3 m_Position;
            public float m_Radius;

            /// <summary>
            /// Draws tool radius.
            /// </summary>
            public void Execute()
            {
                m_OverlayBuffer.DrawCircle(new UnityEngine.Color(.52f, .80f, .86f, 1f), default, m_Radius / 20f, 0, new float2(0, 1), m_Position, m_Radius * 2f);
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct ChangeMeshColorWithinRadiusJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public ColorSet m_ApplyColorSet;
            public EntityCommandBuffer buffer;
            public float m_Radius;
            public float3 m_Position;

            /// <summary>
            /// Executes job which will change state or prefab for trees within a radius.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (CheckForWithinRadius(m_Position, transformNativeArray[i].m_Position, m_Radius))
                    {
                        Entity currentEntity = entityNativeArray[i];

                        DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(currentEntity);
                        meshColorBuffer.Add(new MeshColor() { m_ColorSet = m_ApplyColorSet });
                        DynamicBuffer<CustomMeshColor> customMeshColors = buffer.AddBuffer<CustomMeshColor>(currentEntity);
                        customMeshColors.Add(new CustomMeshColor() { m_ColorSet = m_ApplyColorSet });

                        buffer.AddComponent<BatchesUpdated>(currentEntity);
                    }
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
            {
                float minRadius = 1f;
                radius = Mathf.Max(radius, minRadius);
                position.y = cursorPosition.y;
                if (Unity.Mathematics.math.distance(cursorPosition, position) < radius)
                {
                    return true;
                }

                return false;
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct ChangeVehicleMeshColorWithinRadiusJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;
            public ColorSet m_ApplyColorSet;
            public EntityCommandBuffer buffer;
            public float m_Radius;
            public float3 m_Position;

            /// <summary>
            /// Executes job which will change state or prefab for trees within a radius.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<InterpolatedTransform> interpolatedTransformNativeArray = chunk.GetNativeArray(ref m_InterpolatedTransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (CheckForWithinRadius(m_Position, interpolatedTransformNativeArray[i].m_Position, m_Radius))
                    {
                        Entity currentEntity = entityNativeArray[i];

                        DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(currentEntity);
                        meshColorBuffer.Add(new MeshColor() { m_ColorSet = m_ApplyColorSet });
                        DynamicBuffer<CustomMeshColor> customMeshColors = buffer.AddBuffer<CustomMeshColor>(currentEntity);
                        customMeshColors.Add(new CustomMeshColor() { m_ColorSet = m_ApplyColorSet });

                        buffer.AddComponent<BatchesUpdated>(currentEntity);
                    }
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
            {
                float minRadius = 1f;
                radius = Mathf.Max(radius, minRadius);
                position.y = cursorPosition.y;
                if (Unity.Mathematics.math.distance(cursorPosition, position) < radius)
                {
                    return true;
                }

                return false;
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct ResetMeshColorWithinRadiusJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public EntityCommandBuffer buffer;
            public float m_Radius;
            public float3 m_Position;

            /// <summary>
            /// Executes job which will change state or prefab for trees within a radius.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (CheckForWithinRadius(m_Position, transformNativeArray[i].m_Position, m_Radius))
                    {
                        Entity currentEntity = entityNativeArray[i];
                        buffer.RemoveComponent<CustomMeshColor>(currentEntity);
                        buffer.AddComponent<BatchesUpdated>(currentEntity);
                    }
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
            {
                float minRadius = 1f;
                radius = Mathf.Max(radius, minRadius);
                position.y = cursorPosition.y;
                if (Unity.Mathematics.math.distance(cursorPosition, position) < radius)
                {
                    return true;
                }

                return false;
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct ResetVehicleMeshColorWithinRadiusJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;
            public EntityCommandBuffer buffer;
            public float m_Radius;
            public float3 m_Position;

            /// <summary>
            /// Executes job which will change state or prefab for trees within a radius.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<InterpolatedTransform> interpolatedTransformNativeArray = chunk.GetNativeArray(ref m_InterpolatedTransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (CheckForWithinRadius(m_Position, interpolatedTransformNativeArray[i].m_Position, m_Radius))
                    {
                        Entity currentEntity = entityNativeArray[i];
                        buffer.RemoveComponent<CustomMeshColor>(currentEntity);
                        buffer.AddComponent<BatchesUpdated>(currentEntity);
                    }
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
            {
                float minRadius = 1f;
                radius = Mathf.Max(radius, minRadius);
                position.y = cursorPosition.y;
                if (Unity.Mathematics.math.distance(cursorPosition, position) < radius)
                {
                    return true;
                }

                return false;
            }
        }

    }
}
