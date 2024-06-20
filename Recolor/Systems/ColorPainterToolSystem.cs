// <copyright file="ColorPainterToolSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
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
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using static Recolor.Systems.SelectedInfoPanelColorFieldsSystem;

    /// <summary>
    /// A tool for painting colors onto meshes.
    /// </summary>
    public partial class ColorPainterToolSystem : ToolBaseSystem
    {
        private ProxyAction m_ApplyAction;
        private ILog m_Log;
        private Entity m_PreviousRaycastedEntity;
        private Entity m_PreviousSelectedEntity;
        private EntityQuery m_HighlightedQuery;
        private SelectedInfoPanelColorFieldsSystem m_SelectedInfoPanelColorFieldsSystem;
        private ToolOutputBarrier m_Barrier;
        private GenericTooltipSystem m_GenericTooltipSystem;
        private ColorPainterUISystem m_ColorPainterUISystem;
        private EntityQuery m_BuildingMeshColorQuery;
        private EntityQuery m_VehicleMeshColorQuery;
        private EntityQuery m_PropMeshColorQuery;

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

        /// <inheritdoc/>
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
            {
                m_ToolRaycastSystem.collisionMask = Game.Common.CollisionMask.OnGround | Game.Common.CollisionMask.Overground;
                m_ToolRaycastSystem.typeMask = Game.Common.TypeMask.MovingObjects | Game.Common.TypeMask.StaticObjects;
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
            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            m_Log.Info($"{nameof(ColorPainterToolSystem)}.{nameof(OnCreate)}");
            m_SelectedInfoPanelColorFieldsSystem = World.GetOrCreateSystemManaged<SelectedInfoPanelColorFieldsSystem>();
            m_ColorPainterUISystem = World.GetOrCreateSystemManaged<ColorPainterUISystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_HighlightedQuery = SystemAPI.QueryBuilder()
                .WithAll<Highlighted>()
                .WithNone<Deleted, Temp, Game.Common.Overridden>()
                .Build();
            m_GenericTooltipSystem = World.GetOrCreateSystemManaged<GenericTooltipSystem>();

            m_BuildingMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Building, MeshColor>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_VehicleMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Vehicle, MeshColor>()
                .WithNone<Temp, Deleted, Game.Common.Overridden>()
                .Build();

            m_PropMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Objects.Object, Game.Objects.Static, MeshColor>()
                .WithNone<Temp, Deleted, Game.Common.Overridden, Tree, Plant>()
                .Build();
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_ApplyAction.shouldBeEnabled = true;
            m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(OnStartRunning)}");
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
            m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(OnStopRunning)}");
            m_GenericTooltipSystem.ClearTooltips();
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            m_GenericTooltipSystem.RegisterIconTooltip("ColorPainterToolIcon", "coui://uil/Colored/ColorPalette.svg");

            if (!GetRaycastResult(out Entity currentRaycastEntity, out RaycastHit hit)
                || ((!EntityManager.HasBuffer<MeshColor>(currentRaycastEntity) || (EntityManager.HasComponent<Plant>(currentRaycastEntity) && m_SelectedInfoPanelColorFieldsSystem.SingleInstance)) && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
                || (EntityManager.HasBuffer<CustomMeshColor>(currentRaycastEntity) && !m_SelectedInfoPanelColorFieldsSystem.SingleInstance && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single))
            {
                buffer.AddComponent<BatchesUpdated>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                buffer.RemoveComponent<Highlighted>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                m_PreviousRaycastedEntity = Entity.Null;
                return inputDeps;
            }

            if (currentRaycastEntity != m_PreviousRaycastedEntity && !m_HighlightedQuery.IsEmptyIgnoreFilter)
            {
                m_PreviousRaycastedEntity = currentRaycastEntity;
                buffer.AddComponent<BatchesUpdated>(m_HighlightedQuery, EntityQueryCaptureMode.AtRecord);
                buffer.RemoveComponent<Highlighted>(m_HighlightedQuery, EntityQueryCaptureMode.AtRecord);
            }

            if (m_HighlightedQuery.IsEmptyIgnoreFilter && m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
            {
                buffer.AddComponent<BatchesUpdated>(currentRaycastEntity);
                buffer.AddComponent<Highlighted>(currentRaycastEntity);
                m_PreviousRaycastedEntity = currentRaycastEntity;
            }

            if (!m_ApplyAction.WasPerformedThisFrame())
            {
                return inputDeps;
            }

            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single)
            {
                if (m_SelectedInfoPanelColorFieldsSystem.SingleInstance)
                {
                    ChangeInstanceColorSet(m_ColorPainterUISystem.ColorSet, ref buffer, currentRaycastEntity);
                }
                else if (!m_SelectedInfoPanelColorFieldsSystem.SingleInstance && m_SelectedInfoPanelColorFieldsSystem.GetAssetSeasonIdentifier(currentRaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet colorSet))
                {
                    ChangeColorVariation(m_ColorPainterUISystem.ColorSet, ref buffer, currentRaycastEntity, assetSeasonIdentifier);
                    SaveColorSet(currentRaycastEntity, ref buffer, assetSeasonIdentifier);
                }
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

        private void SaveColorSet(Entity instanceEntity, ref EntityCommandBuffer buffer, AssetSeasonIdentifier assetSeasonIdentifier)
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
                m_SelectedInfoPanelColorFieldsSystem.TrySaveCustomColorSet(colorSet, assetSeasonIdentifier);
            }
            else
            {
                for (int i = 0; i < 4; i++)
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

                    m_SelectedInfoPanelColorFieldsSystem.TrySaveCustomColorSet(colorSet, assetSeasonIdentifier);
                }
            }

            EntityQuery prefabRefQuery = SystemAPI.QueryBuilder()
                .WithAll<PrefabRef>()
                .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                .Build();

            NativeArray<Entity> entities = prefabRefQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity e in entities)
            {
                if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && currentPrefabRef.m_Prefab == prefabRef.m_Prefab)
                {
                    buffer.AddComponent<BatchesUpdated>(e);
                }
            }
        }
    }
}
