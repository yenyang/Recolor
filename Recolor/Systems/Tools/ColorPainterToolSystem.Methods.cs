// <copyright file="ColorPainterToolSystem.Methods.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
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
    using Recolor.Domain.Palette;
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
    using UnityEngine.Windows;
    using static Game.Rendering.OverlayRenderSystem;
    using static Recolor.Domain.Palette.PaletteFilterTypeData;
    using static Recolor.Systems.SelectedInfoPanel.SIPColorFieldsSystem;

    /// <summary>
    /// A tool for painting colors onto meshes.
    /// </summary>
    public partial class ColorPainterToolSystem : ToolBaseSystem
    {
        /// <summary>
        /// Changes the instance color of an entity.
        /// </summary>
        /// <param name="recolorSet">Color set and states.</param>
        /// <param name="buffer">ECB from appropriate system update phase.</param>
        /// <param name="entity">Subject entity.</param>
        public void ChangeInstanceColorSet(RecolorSet recolorSet, ref EntityCommandBuffer buffer, Entity entity)
        {
            if (!EntityManager.HasComponent<Game.Objects.Plant>(entity) &&
                EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                if (!EntityManager.HasBuffer<CustomMeshColor>(entity))
                {
                    DynamicBuffer<CustomMeshColor> newBuffer = EntityManager.AddBuffer<CustomMeshColor>(entity);
                    foreach (MeshColor meshColor in meshColorBuffer)
                    {
                        newBuffer.Add(new CustomMeshColor(meshColor));
                    }

                    if (!EntityManager.HasBuffer<MeshColorRecord>(entity))
                    {
                        DynamicBuffer<MeshColorRecord> meshColorRecordBuffer = EntityManager.AddBuffer<MeshColorRecord>(entity);
                        foreach (MeshColor meshColor in meshColorBuffer)
                        {
                            meshColorRecordBuffer.Add(new MeshColorRecord(meshColor));
                        }
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
                    customMeshColor.m_ColorSet = CompileColorSet(recolorSet, meshColorBuffer[0].m_ColorSet);
                    customMeshColorBuffer[i] = customMeshColor;
                }

                buffer.AddComponent<BatchesUpdated>(entity);
                m_SelectedInfoPanelColorFieldsSystem.AddBatchesUpdatedToSubElements(entity, buffer);
            }
        }

        private ColorSet CompileColorSet(RecolorSet recolorSet, ColorSet originalColors)
        {
            ColorSet colorSet = originalColors;
            if (recolorSet.States[0])
            {
                colorSet.m_Channel0 = recolorSet.Channels[0];
            }

            if (recolorSet.States[1])
            {
                colorSet.m_Channel1 = recolorSet.Channels[1];
            }

            if (recolorSet.States[2])
            {
                colorSet.m_Channel2 = recolorSet.Channels[2];
            }

            return colorSet;
        }

        /// <summary>
        /// Resets custom mesh colors of an entity.
        /// </summary>
        /// <param name="recolorSet">Colorset and toggles.</param>
        /// <param name="buffer">ECB from appropriate system update phase.</param>
        /// <param name="entity">Subject entity.</param>
        private void ResetInstanceColors(RecolorSet recolorSet, ref EntityCommandBuffer buffer, Entity entity)
        {
            if (EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) &&
                EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) &&
                EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer) &&
                meshColorRecordBuffer.Length > 0 &&
                customMeshColorBuffer.Length > 0 &&
                meshColorBuffer.Length > 0)
            {
                bool matchesVanillaColorSet = true;
                RecolorSet newRecolorSet = new(customMeshColorBuffer[0].m_ColorSet);
                for (int i = 0; i < 3; i++)
                {
                    if (recolorSet.States[i])
                    {
                        newRecolorSet.Channels[i] = meshColorRecordBuffer[0].m_ColorSet[i];
                    }
                    else
                    {
                        newRecolorSet.ToggleChannel((uint)i);
                    }

                    if (newRecolorSet.Channels[i] != meshColorRecordBuffer[0].m_ColorSet[i])
                    {
                        matchesVanillaColorSet = false;
                    }
                }

                if (matchesVanillaColorSet)
                {
                    buffer.RemoveComponent<CustomMeshColor>(entity);
                    buffer.RemoveComponent<MeshColorRecord>(entity);
                    buffer.AddComponent<BatchesUpdated>(entity);
                    m_SelectedInfoPanelColorFieldsSystem.AddBatchesUpdatedToSubElements(entity, buffer);
                }
                else
                {
                    ChangeInstanceColorSet(newRecolorSet, ref buffer, entity);
                }
            }
            else
            {
                buffer.RemoveComponent<CustomMeshColor>(entity);
                buffer.RemoveComponent<MeshColorRecord>(entity);
                buffer.AddComponent<BatchesUpdated>(entity);
                m_SelectedInfoPanelColorFieldsSystem.AddBatchesUpdatedToSubElements(entity, buffer);
            }
        }

        /// <summary>
        /// Changes color variation of a prefab.
        /// </summary>
        /// <param name="recolorSet">Colors and states.</param>
        /// <param name="buffer">ECB from approrpiate system update phase.</param>
        /// <param name="entity">Instance Entity</param>
        /// <param name="assetSeasonIdentifier">Asset Season identifier.</param>
        private void ChangeColorVariation(RecolorSet recolorSet, ref EntityCommandBuffer buffer, Entity entity, AssetSeasonIdentifier assetSeasonIdentifier)
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
                    colorVariation.m_ColorSet = CompileColorSet(recolorSet, colorVariationBuffer[i].m_ColorSet);
                    colorVariationBuffer[assetSeasonIdentifier.m_Index] = colorVariation;
                }

                buffer.AddComponent<BatchesUpdated>(entity);
                m_SelectedInfoPanelColorFieldsSystem.AddBatchesUpdatedToSubElements(entity, buffer);

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

                    AssetSeasonIdentifier currentAssetSeasonIdentifier = new ()
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
                    m_SelectedInfoPanelColorFieldsSystem.AddBatchesUpdatedToSubElements(e, buffer);
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
                    if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[i].m_SubMesh, out PrefabBase _))
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
                    m_SelectedInfoPanelColorFieldsSystem.AddBatchesUpdatedToSubElements(e, buffer);
                }
            }
        }

        private JobHandle UpdateDefinitions(JobHandle inputDeps)
        {
            JobHandle jobHandle = DestroyDefinitions(m_DefinitionGroup, m_Barrier, inputDeps);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single ||
                m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Picker)
            {
                CreateDefinitionJob createDefinitionJob = new CreateDefinitionJob()
                {
                    buffer = buffer,
                    m_InstanceEntity = m_RaycastEntity,
                    m_TransformData = SystemAPI.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
                    m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                    m_OwnerLookup = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true),
                    m_CurveLookup = SystemAPI.GetComponentLookup<Game.Net.Curve>(isReadOnly: true),
                    m_EditorContainterLookup = SystemAPI.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true),
                    m_PseudoRandomSeedLookup = SystemAPI.GetComponentLookup<Game.Common.PseudoRandomSeed>(isReadOnly: true),
                };
                inputDeps = createDefinitionJob.Schedule(inputDeps);
                m_Barrier.AddJobHandleForProducer(inputDeps);
            }
            else
            {
                CreateDefinitionsWithRadiusOfTransform createDefinitionsWithRadiusOfTransform = new CreateDefinitionsWithRadiusOfTransform()
                {
                    m_EntityType = SystemAPI.GetEntityTypeHandle(),
                    m_Position = m_LastRaycastPosition,
                    buffer = m_Barrier.CreateCommandBuffer(),
                    m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                    m_Radius = m_ColorPainterUISystem.Radius,
                    m_TransformType = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true),
                    m_SelectedEntities = m_SelectedEntities,
                    m_OwnerLookup = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true),
                    m_TransformLookup = SystemAPI.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
                    m_PseudoRandomSeedLookup = SystemAPI.GetComponentLookup<Game.Common.PseudoRandomSeed>(isReadOnly: true),
                };

                if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Building)
                {
                    if (m_State == State.Painting)
                    {
                        inputDeps = createDefinitionsWithRadiusOfTransform.Schedule(m_BuildingMeshColorQuery, inputDeps);
                    }
                    else if (m_State == State.Reseting)
                    {
                        inputDeps = createDefinitionsWithRadiusOfTransform.Schedule(m_ResetBuildingMeshColorQuery, inputDeps);
                    }
                }
                else if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Props)
                {
                    if (m_State == State.Painting)
                    {
                        inputDeps = createDefinitionsWithRadiusOfTransform.Schedule(m_PropMeshColorQuery, inputDeps);
                    }
                    else if (m_State == State.Reseting)
                    {
                        inputDeps = createDefinitionsWithRadiusOfTransform.Schedule(m_ResetPropMeshColorQuery, inputDeps);
                    }
                }
                else if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Vehicles)
                {
                    CreateDefinitionsWithRadiusOfInterpolatedTransform createDefinitionsWithRadiusOfInterpolatedTransform = new CreateDefinitionsWithRadiusOfInterpolatedTransform()
                    {
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_Position = m_LastRaycastPosition,
                        buffer = m_Barrier.CreateCommandBuffer(),
                        m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                        m_Radius = m_ColorPainterUISystem.Radius,
                        m_InterpolatedTransformType = SystemAPI.GetComponentTypeHandle<InterpolatedTransform>(isReadOnly: true),
                        m_SelectedEntities = m_SelectedEntities,
                        m_OwnerLookup = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true),
                        m_PseudoRandomSeedLookup = SystemAPI.GetComponentLookup<Game.Common.PseudoRandomSeed>(isReadOnly: true),
                    };
                    if (m_State == State.Painting)
                    {
                        inputDeps = createDefinitionsWithRadiusOfTransform.Schedule(m_ParkedVehicleMeshColorQuery, inputDeps);
                        inputDeps = createDefinitionsWithRadiusOfInterpolatedTransform.Schedule(m_VehicleMeshColorQuery, inputDeps);
                    }
                    else if (m_State == State.Reseting)
                    {
                        inputDeps = createDefinitionsWithRadiusOfTransform.Schedule(m_ResetParkedVehicleMeshColorQuery, inputDeps);
                        inputDeps = createDefinitionsWithRadiusOfInterpolatedTransform.Schedule(m_ResetVehicleMeshColorQuery, inputDeps);
                    }
                }
                else if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.NetLanes)
                {
                    CreateDefinitionsWithinRadiusOfCurve createDefinitionsWithinRadiusOfCurve = new CreateDefinitionsWithinRadiusOfCurve()
                    {
                        m_CurveType = SystemAPI.GetComponentTypeHandle<Game.Net.Curve>(isReadOnly: true),
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_EditorContainerLookup = SystemAPI.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true),
                        m_OwnerLookup = SystemAPI.GetComponentLookup<Game.Common.Owner>(isReadOnly: true),
                        m_SelectedEntities = m_SelectedEntities,
                        m_Position = m_LastRaycastPosition,
                        m_PrefabRefLookup = SystemAPI.GetComponentLookup<Game.Prefabs.PrefabRef>(isReadOnly: true),
                        m_PseudoRandomSeedLookup = SystemAPI.GetComponentLookup<Game.Common.PseudoRandomSeed>(isReadOnly: true),
                        m_Radius = m_ColorPainterUISystem.Radius,
                        buffer = m_Barrier.CreateCommandBuffer(),
                    };

                    if (m_State == State.Painting)
                    {
                        inputDeps = createDefinitionsWithinRadiusOfCurve.Schedule(m_NetLanesMeshColorQuery, inputDeps);
                    }
                    else if (m_State == State.Reseting)
                    {
                        inputDeps = createDefinitionsWithinRadiusOfCurve.Schedule(m_ResetNetLanesMeshColorQuery, inputDeps);
                    }
                }

                m_Barrier.AddJobHandleForProducer(inputDeps);
            }

            return inputDeps;
        }

        private JobHandle Clear(JobHandle inputDeps)
        {
            applyMode = ApplyMode.Clear;
            inputDeps = DestroyDefinitions(m_DefinitionGroup, m_Barrier, inputDeps);
            m_SelectedEntities.Clear();
            m_PreviousRaycastEntity = Entity.Null;
            return inputDeps;
        }

        private JobHandle Apply(JobHandle inputDeps)
        {
            applyMode = ApplyMode.Apply;
            m_PreviousRaycastEntity = Entity.Null;
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            if (m_State == State.Picking)
            {
                m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(Apply)} Picking.");
                if (m_SelectedInfoPanelColorFieldsSystem.ShowPaletteChoices &&
                    EntityManager.TryGetBuffer(m_RaycastEntity, isReadOnly: true, out DynamicBuffer<AssignedPalette> paletteBuffer) &&
                    paletteBuffer.Length > 0)
                {
                    m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(Apply)} paletteBuffer length = {paletteBuffer.Length}.");
                    Entity[] newPalettePrefabEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
                    for (int i = 0; i < paletteBuffer.Length; i++)
                    {
                        m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(Apply)} paletteBuffer[i].m_PaletteInstanceEntity = {paletteBuffer[i].m_PaletteInstanceEntity.Index}:{paletteBuffer[i].m_PaletteInstanceEntity.Version}.");
                        if (paletteBuffer[i].m_PaletteInstanceEntity != Entity.Null &&
                            EntityManager.TryGetComponent(paletteBuffer[i].m_PaletteInstanceEntity, out PrefabRef palettePrefabEntity) &&
                            EntityManager.TryGetBuffer(palettePrefabEntity.m_Prefab, isReadOnly: true, out DynamicBuffer<SwatchData> swatches) &&
                            swatches.Length >= 2)
                        {
                            m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(Apply)} paletteBuffer[i].m_Channel = {paletteBuffer[i].m_Channel}.");
                            newPalettePrefabEntities[Math.Clamp(paletteBuffer[i].m_Channel, 0, 2)] = palettePrefabEntity.m_Prefab;
                            m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(Apply)} newPalettePrefabEntities[Math.Clamp(paletteBuffer[i].m_Channel, 0, 2)] = {newPalettePrefabEntities[Math.Clamp(paletteBuffer[i].m_Channel, 0, 2)].Index}:{newPalettePrefabEntities[Math.Clamp(paletteBuffer[i].m_Channel, 0, 2)].Version}.");
                        }
                    }

                    m_ColorPainterUISystem.SelectedPaletteEntities = newPalettePrefabEntities;
                    m_ColorPainterUISystem.ToolMode = ColorPainterUISystem.PainterToolMode.Paint;
                    m_Log.Debug($"{nameof(ColorPainterToolSystem)}.{nameof(Apply)} set mode back to paint.");
                    return inputDeps;
                }

                if (EntityManager.TryGetBuffer(m_RaycastEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) &&
                    meshColorBuffer.Length > 0)
                {
                    m_ColorPainterUISystem.ColorSet = meshColorBuffer[0].m_ColorSet;
                    m_ColorPainterUISystem.ToolMode = ColorPainterUISystem.PainterToolMode.Paint;
                    return inputDeps;
                }
            }
            else if (m_State == State.Reseting &&
                     m_ColorPainterUISystem.ToolMode == ColorPainterUISystem.PainterToolMode.Paint)
            {
                m_TimeLastReset = UnityEngine.Time.time;
            }

            if (m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Radius)
            {
                m_TimeLastApplied = UnityEngine.Time.time;
            }

            if (m_State == State.Painting &&
                m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single &&
               !m_SelectedInfoPanelColorFieldsSystem.SingleInstance &&
                m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(m_RaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet _))
            {
                ChangeColorVariation(m_ColorPainterUISystem.RecolorSet, ref buffer , m_RaycastEntity, assetSeasonIdentifier);
                GenerateOrUpdateCustomColorVariationEntity(m_RaycastEntity, ref buffer, assetSeasonIdentifier);
            }

            if (m_State == State.Reseting &&
                m_ColorPainterUISystem.ColorPainterSelectionType == ColorPainterUISystem.SelectionType.Single &&
               !m_SelectedInfoPanelColorFieldsSystem.SingleInstance &&
                m_SelectedInfoPanelColorFieldsSystem.TryGetAssetSeasonIdentifier(m_RaycastEntity, out AssetSeasonIdentifier assetSeasonIdentifier1, out ColorSet _) &&
                m_SelectedInfoPanelColorFieldsSystem.TryGetVanillaColorSet(assetSeasonIdentifier1, out ColorSet vanillaColorSet))
            {
                ChangeColorVariation(new RecolorSet(vanillaColorSet), ref buffer, m_RaycastEntity, assetSeasonIdentifier1);
                DeleteCustomColorVariationEntity(m_RaycastEntity, ref buffer, assetSeasonIdentifier1);
            }

            return inputDeps;
        }

        private bool MatchingCategory()
        {
            if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Building &&
                EntityManager.HasComponent<Building>(m_RaycastEntity))
            {
                return true;
            }

            if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Vehicles &&
                EntityManager.HasComponent<Vehicle>(m_RaycastEntity))
            {
                return true;
            }

            if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.NetLanes &&
                EntityManager.HasComponent<Game.Net.Curve>(m_RaycastEntity))
            {
                return true;
            }

            if (m_ColorPainterUISystem.ColorPainterFilterType == ColorPainterUISystem.FilterType.Props &&
                EntityManager.HasComponent<Game.Objects.Object>(m_RaycastEntity) &&
                EntityManager.HasComponent<Game.Objects.Static>(m_RaycastEntity) &&
               !EntityManager.HasComponent<Building>(m_RaycastEntity))
            {
                return true;
            }

            return false;
        }

        private bool MatchingFilter()
        {
            if (m_ColorPainterUISystem.PaletteFilterType == PaletteFilterTypeData.PaletteFilterType.None)
            {
                return true;
            }

            if (!EntityManager.TryGetComponent(m_RaycastEntity, out PrefabRef prefabRef))
            {
                return false;
            }

            if (m_ColorPainterUISystem.PaletteFilterType == PaletteFilterTypeData.PaletteFilterType.Theme &&
                EntityManager.TryGetComponent(prefabRef.m_Prefab, out SpawnableBuildingData spawnableBuildingData) &&
                m_PrefabSystem.TryGetPrefab(spawnableBuildingData.m_ZonePrefab, out PrefabBase zonePrefabBase) &&
                zonePrefabBase is ZonePrefab)
            {
                ZonePrefab zonePrefab = zonePrefabBase as ZonePrefab;
                if (!zonePrefab.TryGet(out ThemeObject themeObject) ||
                     themeObject == null ||
                    !m_PrefabSystem.TryGetEntity(themeObject.m_Theme, out Entity themeEntity))
                {
                    return false;
                }

                if (m_ColorPainterUISystem.PaletteFilterEntity == themeEntity)
                {
                    return true;
                }
            }
            else if (m_ColorPainterUISystem.PaletteFilterType == PaletteFilterTypeData.PaletteFilterType.ZoningType &&
                     EntityManager.TryGetComponent(prefabRef.m_Prefab, out SpawnableBuildingData spawnableBuildingData2))
            {
                if (m_ColorPainterUISystem.PaletteFilterEntity == spawnableBuildingData2.m_ZonePrefab)
                {
                    return true;
                }
            }
            else if (m_ColorPainterUISystem.PaletteFilterType == PaletteFilterTypeData.PaletteFilterType.Pack &&
                     EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<AssetPackElement> assetPackElements))
        {
                for (int j = 0; j < assetPackElements.Length; j++)
                {
                    if (assetPackElements[j].m_Pack == m_ColorPainterUISystem.PaletteFilterEntity)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
