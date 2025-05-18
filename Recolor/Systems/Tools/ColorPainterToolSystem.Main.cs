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
    using Recolor.Systems.SingleInstance;
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
        private ILog m_Log;
        private Entity m_PreviousRaycastedEntity;
        private Entity m_PreviousSelectedEntity;
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
        private EntityQuery m_DefinitionGroup;
        private State m_State;

        /// <summary>
        /// Enum for state of the tool.
        /// </summary>
        public enum State
        {
            /// <summary>
            /// Mouse up.
            /// </summary>
            Default,

            /// <summary>
            /// Left mouse down in paint mode.
            /// </summary>
            Painting,

            /// <summary>
            /// Right mouse down in paint mode, or left mouse down in reset mode.
            /// </summary>
            Reseting,

            /// <summary>
            /// Left mouse down in picker mode,
            /// </summary>
            Picking,
        }

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
            m_DefinitionGroup = GetDefinitionQuery();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();

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
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            applyAction.enabled = true;
            secondaryApplyAction.enabled = true;
            m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(OnStartRunning)}");
            m_GenericTooltipSystem.ClearTooltips();
            m_State = State.Default;
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            m_PreviousRaycastedEntity = Entity.Null;
            m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(OnStopRunning)}");
            m_GenericTooltipSystem.ClearTooltips();
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            m_State = State.Default;

            if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint)
            {
                m_GenericTooltipSystem.RemoveTooltip("ColorPickerToolIcon");
                m_GenericTooltipSystem.RemoveTooltip("ResetToolIcon");
                m_GenericTooltipSystem.RegisterIconTooltip("ColorPainterToolIcon", "coui://ui-mods/images/format_painter.svg");
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

            if ((m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Reset &&
                m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single) ||
               (secondaryApplyAction.WasReleasedThisFrame() &&
                m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint &&
                m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single))
            {
                m_State = State.Reseting;

                if (!raycastResult
                || !EntityManager.HasBuffer<MeshColor>(currentRaycastEntity)
                || (!EntityManager.HasBuffer<CustomMeshColor>(currentRaycastEntity) && m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                || (!m_SelectedInfoPanelColorFieldsSystem.SingleInstance && m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(currentRaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet colorSet) && m_SelectedInfoPanelColorFieldsSystem.MatchesEntireVanillaColorSet(colorSet, assetSeasonIdentifier))
                || (hit.m_HitPosition.x == 0 && hit.m_HitPosition.y == 0 && hit.m_HitPosition.z == 0))
                {
                    m_PreviousRaycastedEntity = Entity.Null;
                    return Clear(inputDeps);
                }
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint &&
                     m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
            {
                if (!raycastResult
                    || !EntityManager.HasBuffer<MeshColor>(currentRaycastEntity)
                    || (EntityManager.HasComponent<Plant>(currentRaycastEntity) && m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                    || (EntityManager.HasBuffer<CustomMeshColor>(currentRaycastEntity) && !m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                    || (hit.m_HitPosition.x == 0 && hit.m_HitPosition.y == 0 && hit.m_HitPosition.z == 0))
                {
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

                    return Clear(inputDeps);
                }
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker)
            {
                if (!raycastResult
                    || !EntityManager.HasBuffer<MeshColor>(currentRaycastEntity)
                    || (hit.m_HitPosition.x == 0 && hit.m_HitPosition.y == 0 && hit.m_HitPosition.z == 0))
                {
                    m_PreviousRaycastedEntity = Entity.Null;
                    return Clear(inputDeps);
                }
            }

            m_GenericTooltipSystem.RemoveTooltip("SingleInstancePlantWarning");
            m_GenericTooltipSystem.RemoveTooltip("HasCustomMeshColorWarning");

            if (EntityManager.HasComponent<Game.Creatures.Creature>(currentRaycastEntity))
            {
                m_PreviousRaycastedEntity = Entity.Null;
                return Clear(inputDeps);
            }

            if (currentRaycastEntity != m_PreviousRaycastedEntity)
            {
                m_PreviousRaycastedEntity = currentRaycastEntity;
            }

            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single ||
                m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker)
            {
                UpdateDefinitions(inputDeps);
            }

            if ((m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint ||
               m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Reset) &&
               m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius &&
               (!raycastResult || (hit.m_HitPosition.x == 0 && hit.m_HitPosition.y == 0 && hit.m_HitPosition.z == 0)))
            {
                return Clear(inputDeps);
            }

            float radius = m_ColorPainterUISystem.Radius;
            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius &&
                m_ColorPainterUISystem.ToolMode != ColorPainterUISystem.PainterToolMode.Picker)
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

            if (!applyAction.WasReleasedThisFrame() && !applyAction.IsPressed() && !secondaryApplyAction.WasReleasedThisFrame() && !secondaryApplyAction.IsPressed())
            {
                return Clear(inputDeps);
            }

            if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint &&
                m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single &&
                applyAction.WasReleasedThisFrame())
            {
                m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(OnUpdate)}  GetAllowApply() = {GetAllowApply()}");
                if (m_SelectedInfoPanelColorFieldsSystem.SingleInstance && GetAllowApply())
                {
                    applyMode = ApplyMode.Apply;
                    return UpdateDefinitions(inputDeps);
                    //ChangeInstanceColorSet(m_ColorPainterUISystem.RecolorSet, ref buffer, currentRaycastEntity);
                }
                else if (!m_SelectedInfoPanelColorFieldsSystem.SingleInstance && m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(currentRaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet _))
                {
                    ChangeColorVariation(m_ColorPainterUISystem.RecolorSet, ref buffer, currentRaycastEntity, assetSeasonIdentifier);
                    GenerateOrUpdateCustomColorVariationEntity(currentRaycastEntity, ref buffer, assetSeasonIdentifier);
                } else
                {
                    applyMode = ApplyMode.Clear;
                    return UpdateDefinitions(inputDeps);
                }
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint &&
                     m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius &&
                    (applyAction.IsPressed() ||
                    (secondaryApplyAction.IsPressed() &&
                   (!m_ColorPainterUISystem.RecolorSet.States[0] ||
                    !m_ColorPainterUISystem.RecolorSet.States[1] ||
                    !m_ColorPainterUISystem.RecolorSet.States[2]))))
            {
                
            }
            else if ((m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Reset &&
                      applyAction.WasReleasedThisFrame() &&
                      m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single) ||
                      (secondaryApplyAction.WasReleasedThisFrame() &&
                      m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint &&
                      m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single))
            {
                if (m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                {
                }
                else if (!m_SelectedInfoPanelColorFieldsSystem.SingleInstance &&
                         m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(currentRaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet _) &&
                         m_SelectedInfoPanelColorFieldsSystem.TryGetVanillaColorSet(assetSeasonIdentifier, out ColorSet VanillaColorSet))
                {
                    ChangeColorVariation(new RecolorSet(VanillaColorSet), ref buffer, currentRaycastEntity, assetSeasonIdentifier);
                    DeleteCustomColorVariationEntity(currentRaycastEntity, ref buffer, assetSeasonIdentifier);
                }
            }
            else if ((m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Reset &&
                      applyAction.IsPressed() && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius) ||
                     (secondaryApplyAction.IsPressed() &&
                      m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint &&
                      m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius))
            {
                
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker &&
                     applyAction.WasReleasedThisFrame() &&
                     EntityManager.TryGetBuffer(currentRaycastEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) &&
                     meshColorBuffer.Length > 0)
            {
                m_ColorPainterUISystem.ColorSet = meshColorBuffer[0].m_ColorSet;
                m_ColorPainterUISystem.ToolMode = ColorPainterUISystem.PainterToolMode.Paint;
            }

            return Clear(inputDeps);
        }
    }
}
