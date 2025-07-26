// <copyright file="ColorPainterToolSystem.Main.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Tools
{
    using System;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Audio.Radio;
    using Game.Buildings;
    using Game.Citizens;
    using Game.Common;
    using Game.Creatures;
    using Game.Input;
    using Game.Net;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Game.Vehicles;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
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
        private Entity m_RaycastEntity;
        private Entity m_PreviousRaycastEntity;
        private SIPColorFieldsSystem m_SelectedInfoPanelColorFieldsSystem;
        private CustomColorVariationSystem m_CustomColorVariationSystem;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private ToolOutputBarrier m_Barrier;
        private GenericTooltipSystem m_GenericTooltipSystem;
        private ColorPainterUISystem m_ColorPainterUISystem;
        private EntityQuery m_BuildingMeshColorQuery;
        private EntityQuery m_VehicleMeshColorQuery;
        private EntityQuery m_ParkedVehicleMeshColorQuery;
        private EntityQuery m_NetLanesMeshColorQuery;
        private EntityQuery m_PropMeshColorQuery;
        private EntityQuery m_DefinitionGroup;
        private State m_State;
        private float m_TimeLastReset;
        private float m_TimeLastApplied;
        private float3 m_LastRaycastPosition;
        private NativeList<Entity> m_SelectedEntities;
        private EntityQuery m_ResetBuildingMeshColorQuery;
        private EntityQuery m_ResetVehicleMeshColorQuery;
        private EntityQuery m_ResetParkedVehicleMeshColorQuery;
        private EntityQuery m_ResetNetLanesMeshColorQuery;
        private EntityQuery m_ResetPropMeshColorQuery;


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

            /// <summary>
            /// Right mouse release when left mouse down.
            /// </summary>
            Canceling,


        }

        /// <summary>
        /// Gets the state of the tool.
        /// </summary>
        public State CurrentState
        {
            get { return m_State; }
            private set { m_State = value; }
        }

        /// <summary>
        /// Gets the racyast entity.
        /// </summary>
        public Entity RaycastEntity
        {
            get { return m_RaycastEntity; }
            private set { m_RaycastEntity = value; }
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
            if ((m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single ||
                 m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker) &&
                 m_SelectedInfoPanelColorFieldsSystem.ShowPaletteChoices &&
                 m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.NetLanes)
            {
                m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
                m_ToolRaycastSystem.typeMask = TypeMask.Lanes;
                m_ToolRaycastSystem.netLayerMask = Layer.Fence;
                m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.Fence;
                m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.NoMainElements;
            }
            else if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single || m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker)
            {
                m_ToolRaycastSystem.collisionMask = Game.Common.CollisionMask.OnGround | Game.Common.CollisionMask.Overground;
                m_ToolRaycastSystem.typeMask = Game.Common.TypeMask.MovingObjects | Game.Common.TypeMask.StaticObjects | TypeMask.Lanes;
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
            m_SelectedInfoPanelColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();
            m_CustomColorVariationSystem = World.GetOrCreateSystemManaged<CustomColorVariationSystem>();
            m_ColorPainterUISystem = World.GetOrCreateSystemManaged<ColorPainterUISystem>();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_DefinitionGroup = GetDefinitionQuery();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_TimeLastApplied = UnityEngine.Time.time;

            m_SelectedEntities = new NativeList<Entity>(Allocator.Persistent);

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

            m_NetLanesMeshColorQuery = SystemAPI.QueryBuilder()
               .WithAll<Game.Net.Curve, MeshColor, Owner>()
               .WithNone<Temp, Deleted, Game.Common.Overridden>()
               .Build();

            m_PropMeshColorQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Game.Objects.Object>(),
                    ComponentType.ReadOnly<MeshColor>(),
                    ComponentType.ReadOnly<Game.Objects.Static>(),
                    ComponentType.ReadOnly<Game.Objects.Transform>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Common.Overridden>(),
                    ComponentType.ReadOnly<Plant>(),
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<Quantity>(),
                    ComponentType.ReadOnly<Owner>(),
                },
            });

            m_ResetBuildingMeshColorQuery = SystemAPI.QueryBuilder()
               .WithAll<Building, MeshColor, Game.Objects.Transform, CustomMeshColor>()
               .WithNone<Temp, Deleted, Game.Common.Overridden>()
               .Build();

            m_ResetVehicleMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Vehicle, MeshColor, InterpolatedTransform, CustomMeshColor>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_ResetParkedVehicleMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Vehicle, MeshColor, Game.Objects.Transform, ParkedCar, CustomMeshColor>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_ResetNetLanesMeshColorQuery = SystemAPI.QueryBuilder()
               .WithAll<Game.Net.Curve, MeshColor, Owner, CustomMeshColor>()
               .WithNone<Temp, Deleted, Game.Common.Overridden>()
               .Build();

            m_ResetPropMeshColorQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Game.Objects.Object>(),
                    ComponentType.ReadOnly<MeshColor>(),
                    ComponentType.ReadOnly<Game.Objects.Static>(),
                    ComponentType.ReadOnly<Game.Objects.Transform>(),
                    ComponentType.ReadOnly<CustomMeshColor>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Common.Overridden>(),
                    ComponentType.ReadOnly<Plant>(),
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<Quantity>(),
                    ComponentType.ReadOnly<Owner>(),
                },
            });

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
            m_RaycastEntity = Entity.Null;
            m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(OnStopRunning)}");
            m_GenericTooltipSystem.ClearTooltips();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_SelectedEntities.Dispose();
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            if (m_State == State.Painting)
            {
                m_GenericTooltipSystem.RemoveTooltip("ColorPickerToolIcon");
                m_GenericTooltipSystem.RemoveTooltip("ResetToolIcon");
                m_GenericTooltipSystem.RegisterIconTooltip("ColorPainterToolIcon", "coui://ui-mods/images/format_painter.svg");
            }
            else if (m_State == State.Picking)
            {
                m_GenericTooltipSystem.RemoveTooltip("ColorPainterToolIcon");
                m_GenericTooltipSystem.RemoveTooltip("ResetToolIcon");
                m_GenericTooltipSystem.RegisterIconTooltip("ColorPickerToolIcon", "coui://uil/Standard/PickerPipette.svg");
            }
            else if (m_State == State.Reseting)
            {
                m_GenericTooltipSystem.RemoveTooltip("ColorPainterToolIcon");
                m_GenericTooltipSystem.RemoveTooltip("ColorPickerToolIcon");
                m_GenericTooltipSystem.RegisterIconTooltip("ResetToolIcon", "coui://uil/Standard/Reset.svg");
            }

            Entity previousRaycastEntity = m_RaycastEntity;
            bool raycastResult = GetRaycastResult(out m_RaycastEntity, out RaycastHit hit);

            if (m_RaycastEntity != previousRaycastEntity)
            {
                m_TimeLastReset = 0f;
            }

            if (EntityManager.HasComponent<Game.Creatures.Creature>(m_RaycastEntity))
            {
                m_TimeLastReset = 0f;
                m_RaycastEntity = Entity.Null;
                return Clear(inputDeps);
            }

            if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint &&
                     m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
            {
                if (!raycastResult ||
                    !EntityManager.HasBuffer<MeshColor>(m_RaycastEntity) ||
                   (EntityManager.HasComponent<Plant>(m_RaycastEntity) &&
                    m_SelectedInfoPanelColorFieldsSystem.SingleInstance) ||
                   (EntityManager.HasBuffer<CustomMeshColor>(m_RaycastEntity) &&
                    !m_SelectedInfoPanelColorFieldsSystem.SingleInstance))
                    {
                        m_TimeLastReset = 0f;
                        if (EntityManager.HasComponent<Plant>(m_RaycastEntity) && m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                        {
                            m_GenericTooltipSystem.RegisterTooltip("SingleInstancePlantWarning", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.MouseTooltipKey("SingleInstancePlantWarning"), "Single instance color changes for plants is not currently supported.");
                        }
                        else
                        {
                            m_GenericTooltipSystem.RemoveTooltip("SingleInstancePlantWarning");
                        }

                        if (EntityManager.HasBuffer<CustomMeshColor>(m_RaycastEntity) && !m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                        {
                            m_GenericTooltipSystem.RegisterTooltip("HasCustomMeshColorWarning", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.MouseTooltipKey("HasCustomMeshColorWarning"), "Cannot change color variation on this because it has custom instance colors.");
                        }
                        else
                        {
                            m_GenericTooltipSystem.RemoveTooltip("HasCustomMeshColorWarning");
                        }

                        m_RaycastEntity = Entity.Null;
                        return Clear(inputDeps);
                    }
            }

            State previousState = m_State;
            if (m_State != State.Canceling &&
                applyAction.IsPressed() &&
                secondaryApplyAction.WasReleasedThisFrame())
            {
                m_State = State.Canceling;
                return Clear(inputDeps);
            }
            else if (m_State == State.Canceling &&
                     applyAction.IsPressed())
            {
                return Clear(inputDeps);
            }
            else if (m_State == State.Canceling &&
                    !applyAction.IsPressed())
            {
                m_State = State.Default;
                return Clear(inputDeps);
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker)
            {
                m_State = State.Picking;
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Reset ||
                     secondaryApplyAction.IsPressed() ||
                     secondaryApplyAction.WasReleasedThisFrame() ||
                     UnityEngine.Time.time < m_TimeLastReset + 0.5f)
            {
                m_State = State.Reseting;
            }
            else
            {
                m_State = State.Painting;
            }

            if (!raycastResult ||
              (hit.m_HitPosition.x == 0 &&
               hit.m_HitPosition.y == 0 &&
               hit.m_HitPosition.z == 0 &&
               m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius))
            {
                m_TimeLastReset = 0f;
                applyMode = ApplyMode.Clear;
                return Clear(inputDeps);
            }


            if (m_State != previousState)
            {
                return Clear(inputDeps);
            }

            m_GenericTooltipSystem.RemoveTooltip("SingleInstancePlantWarning");
            m_GenericTooltipSystem.RemoveTooltip("HasCustomMeshColorWarning");

            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius &&
                m_ColorPainterUISystem.ToolMode != ColorPainterUISystem.PainterToolMode.Picker)
            {
                ToolRadiusJob toolRadiusJob = new ()
                {
                    m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle),
                    m_Position = new Vector3(hit.m_HitPosition.x, hit.m_Position.y, hit.m_HitPosition.z),
                    m_Radius = m_ColorPainterUISystem.Radius,
                };
                inputDeps = IJobExtensions.Schedule(toolRadiusJob, JobHandle.CombineDependencies(inputDeps, outJobHandle));
                m_OverlayRenderSystem.AddBufferWriter(inputDeps);

                m_LastRaycastPosition = new Vector3(hit.m_HitPosition.x, hit.m_Position.y, hit.m_HitPosition.z);
            }

            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius &&
                m_TimeLastApplied > UnityEngine.Time.time - 0.5f)
            {
                return Clear(inputDeps);
            }
            else if (!applyAction.IsPressed() &&
                     !secondaryApplyAction.IsPressed() &&
                      m_SelectedEntities.Length > 0)
            {
                m_SelectedEntities.Clear();
            }

            if (applyAction.WasReleasedThisFrame() ||
                (secondaryApplyAction.WasReleasedThisFrame() && m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint))
            {
                return Apply(inputDeps);
            }
            else if (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker ||
                     m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
            {
                if (m_RaycastEntity != m_PreviousRaycastEntity &&
                    m_PreviousRaycastEntity != Entity.Null)
                {
                    m_State = State.Default;
                    m_PreviousRaycastEntity = Entity.Null;
                    return Clear(inputDeps);
                }
                else if (m_PreviousRaycastEntity == Entity.Null &&
                       (m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker ||
                        m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single) &&
                      ((m_State == State.Picking &&
                      (!m_SelectedInfoPanelColorFieldsSystem.ShowPaletteChoices ||
                        EntityManager.HasBuffer<AssignedPalette>(m_RaycastEntity))) ||
                        (m_State == State.Reseting &&
                        EntityManager.HasComponent<CustomMeshColor>(m_RaycastEntity)) ||
                       (m_State == State.Painting &&
                       (!m_SelectedInfoPanelColorFieldsSystem.ShowPaletteChoices ||
                       (MatchingCategory() &&
                        MatchingFilter())))))
                {
                    m_PreviousRaycastEntity = m_RaycastEntity;
                    return UpdateDefinitions(inputDeps);
                }
                else
                {
                    applyMode = ApplyMode.None;
                    return inputDeps;
                }
            }
            else if (applyAction.IsPressed() ||
                    (secondaryApplyAction.IsPressed() && m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint))
            {
                applyMode = ApplyMode.None;
                return UpdateDefinitions(inputDeps);
            }
            else
            {
                applyMode = ApplyMode.None;
                return inputDeps;
            }
        }
    }
}
