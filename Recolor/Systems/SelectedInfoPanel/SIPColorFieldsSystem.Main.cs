// <copyright file="SIPColorFieldsSystem.Main.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.SelectedInfoPanel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Colossal.UI.Binding;
    using Game;
    using Game.Common;
    using Game.Debug;
    using Game.Input;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
    using Recolor.Systems.Palettes;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;

    /// <summary>
    /// Adds color fields to selected info panel for changing colors of buildings, vehicles, props, etc.
    /// </summary>
    public partial class SIPColorFieldsSystem : ExtendedInfoSectionBase
    {
        /// <inheritdoc/>
        public override void OnWriteProperties(IJsonWriter writer)
        {
        }

        /// <inheritdoc/>
        protected override void OnProcess()
        {
        }

        /// <inheritdoc/>
        protected override void Reset()
        {
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_EditorVisible.Value = false;

            if (mode.IsGameOrEditor())
            {
                GetClimatePrefab();
                Enabled = true;
                m_ReloadInXFrames = 30;

                if (mode.IsEditor() ||
                    Mod.Instance.Settings.AlwaysMinimizedAtGameStart)
                {
                    m_Minimized.Value = true;
                }
                else
                {
                    m_Minimized.Value = Mod.Instance.Settings.Minimized;
                }
            }
            else
            {
                Enabled = false;
                return;
            }

            if (m_VanillaColorSets.Count > 0)
            {
                return;
            }

            NativeList<Entity> colorVariationPrefabEntities = m_SubMeshQuery.ToEntityListAsync(Allocator.Temp, out JobHandle colorVariationPrefabJobHandle);
            NativeList<Entity> prefabsNeedingUpdates = new (Allocator.Temp);
            colorVariationPrefabJobHandle.Complete();

            foreach (Entity e in colorVariationPrefabEntities)
            {
                if (!EntityManager.TryGetBuffer(e, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    continue;
                }

                int length = subMeshBuffer.Length;
                if (EntityManager.HasComponent<Tree>(e))
                {
                    length = Math.Min(4, subMeshBuffer.Length);
                }

                for (int i = 0; i < length; i++)
                {
                    if (!EntityManager.TryGetBuffer(subMeshBuffer[i].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer))
                    {
                        continue;
                    }

                    PrefabBase prefabBase = m_PrefabSystem.GetPrefab<PrefabBase>(subMeshBuffer[i].m_SubMesh);
                    PrefabID prefabID = prefabBase.GetPrefabID();

                    for (int j = 0; j < colorVariationBuffer.Length; j++)
                    {
#if VERBOSE
                    m_Log.Verbose($"{prefabID.GetName()} {(TreeState)(int)Math.Pow(2, i - 1)} {(FoliageUtils.Season)j} {colorVariationBuffer[j].m_ColorSet.m_Channel0} {colorVariationBuffer[j].m_ColorSet.m_Channel2} {colorVariationBuffer[j].m_ColorSet.m_Channel2}");
#endif
                        ColorVariation currentColorVariation = colorVariationBuffer[j];
                        TryGetSeasonFromColorGroupID(currentColorVariation.m_GroupID, out Season season);

                        AssetSeasonIdentifier assetSeasonIdentifier = new ()
                        {
                            m_PrefabID = prefabID,
                            m_Season = season,
                            m_Index = j,
                        };

                        if (!m_VanillaColorSets.ContainsKey(assetSeasonIdentifier))
                        {
                            m_VanillaColorSets.Add(assetSeasonIdentifier, currentColorVariation.m_ColorSet);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (m_ReloadInXFrames > 0)
            {
                m_ReloadInXFrames--;
                if (m_ReloadInXFrames == 0)
                {
                    ReloadSavedColorSetsFromDisk();
                    m_CustomColorVariationSystem.ReloadCustomColorVariations(m_EndFrameBarrier.CreateCommandBuffer());
                }
            }

            if (m_ToolSystem.actionMode.IsEditor())
            {
                m_CurrentEntity = m_ToolSystem.selected;
                if (EntityManager.TryGetComponent(m_CurrentEntity, out PrefabRef selectedPrefabRef))
                {
                    m_CurrentPrefabEntity = selectedPrefabRef.m_Prefab;
                }
            }
            else if (m_ToolSystem.actionMode.IsGame())
            {
                m_CurrentEntity = selectedEntity;
                m_CurrentPrefabEntity = selectedPrefab;
            }

            // This was how I resolved an issue where I wanted to unselect an entity before activating a tool.
            if (m_CurrentEntity == Entity.Null &&
                m_ActivateColorPainter)
            {
                m_ActivateColorPainter = false;
                m_ToolSystem.activeTool = m_ColorPainterTool;
                HandleScopeAndButtonStates();
            }

            if (m_ActivateColorPainterAction.WasPerformedThisFrame())
            {
                m_ActivateColorPainter = true;
                if (m_CurrentEntity != Entity.Null)
                {
                    m_ToolSystem.selected = Entity.Null;
                    if (Mod.Instance.Settings.ColorPainterAutomaticCopyColor)
                    {
                        m_ColorPainterUISystem.ColorSet = m_CurrentColorSet.Value.GetColorSet();
                        m_ColorPainterUISystem.SelectedPaletteEntities = m_PaletteChooserData.Value.m_SelectedPaletteEntities;
                    }
                }

                visible = false;
                m_State = State.NotVisible;

                return;
            }

            // If an editor container is selected and it has a netLane sublane then select the sublane.
            if (EntityManager.HasComponent<Game.Tools.EditorContainer>(selectedEntity)
                && EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> ownerBuffer)
                && ownerBuffer.Length == 1
                && EntityManager.TryGetComponent(ownerBuffer[0].m_SubLane, out PrefabRef prefabRef)
                && m_PrefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase currentPrefabBase)
                && m_PrefabSystem.TryGetEntity(currentPrefabBase, out m_CurrentPrefabEntity))
            {
                m_CurrentEntity = ownerBuffer[0].m_SubLane;
            }

            if (EntityManager.TryGetComponent(selectedEntity, out Game.Vehicles.Controller controller) &&
                EntityManager.TryGetComponent(controller.m_Controller, out Game.Routes.CurrentRoute controllerRoute) &&
                controllerRoute.m_Route != Entity.Null &&
                m_Route.Value == ButtonState.On)
            {
                m_CurrentEntity = controller.m_Controller;
            }

            if (EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Game.Routes.RouteVehicle> routeVehicleBuffer) &&
                routeVehicleBuffer.Length > 0 &&
                EntityManager.TryGetBuffer(routeVehicleBuffer[0].m_Vehicle, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer1) &&
                meshColorBuffer1.Length > 0 &&
                EntityManager.TryGetComponent(routeVehicleBuffer[0].m_Vehicle, out PrefabRef prefabRef1) &&
                m_PrefabSystem.TryGetPrefab(prefabRef1.m_Prefab, out PrefabBase currentPrefabBase1) &&
                m_PrefabSystem.TryGetEntity(currentPrefabBase1, out m_CurrentPrefabEntity))
            {
                m_CurrentEntity = routeVehicleBuffer[0].m_Vehicle;
            }

            if (m_CurrentEntity == Entity.Null &&
                m_PreviousEntity != Entity.Null)
            {
                m_PreviousEntity = Entity.Null;
            }

            if (m_CurrentEntity == Entity.Null)
            {
                m_State = State.NotVisible;
                visible = false;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = false;
                }

                return;
            }

            if (m_PreviousEntity != m_CurrentEntity)
            {
                m_State = State.EntityChanged;
                m_PreviousEntity = m_CurrentEntity;
            }

            bool foundClimatePrefab = true;
            if (m_ClimatePrefab is null)
            {
                foundClimatePrefab = GetClimatePrefab();
            }

            if (!m_PrefabSystem.TryGetPrefab(m_CurrentPrefabEntity, out PrefabBase _))
            {
                visible = false;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = false;
                }

                m_State = State.NotVisible;
                return;
            }

            if (!EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) ||
                meshColorBuffer.Length == 0)
            {
                visible = false;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = false;
                }

                m_State = State.NotVisible;

                return;
            }

            if (m_State == State.Static &&
              ((m_Matching.Value & ButtonState.On) == ButtonState.On ||
                EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity)) &&
               !EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) &&
                m_NeedsColorRefresh == true &&
                UnityEngine.Time.time > m_TimeColorLastChanged + 0.5f)
            {
                ColorRefresh();
            }

            if (m_State == State.Static &&
               (m_CurrentColorSet.Value.Channels[0] != meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet[0] ||
                m_CurrentColorSet.Value.Channels[1] != meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet[1] ||
                m_CurrentColorSet.Value.Channels[2] != meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet[2]))
            {
                m_CurrentColorSet.Value = new RecolorSet(meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet);
                m_State = State.ColorChanged;
            }

            if (m_State == State.Static)
            {
                return;
            }

            if ((m_State & State.ColorChangeScheduled) == State.ColorChangeScheduled)
            {
                m_State &= ~State.ColorChangeScheduled;
                m_State |= State.ColorChanged;
                return;
            }

            if ((m_State & State.UpdateButtonStates) == State.UpdateButtonStates ||
                (m_State & State.EntityChanged) == State.EntityChanged)
            {
                HandleScopeAndButtonStates();
            }

            if (EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer1) &&
                subMeshBuffer1.Length > 0)
            {
               if ((m_State & State.UpdateButtonStates) == State.UpdateButtonStates ||
                   (m_State & State.EntityChanged) == State.EntityChanged)
                {
                    m_SubMeshData.Value.SubMeshIndex = Mathf.Clamp(m_SubMeshData.Value.SubMeshIndex, 0, subMeshBuffer1.Length - 1);
                    m_SubMeshData.Value.SubMeshLength = subMeshBuffer1.Length;
                    m_SubMeshData.Value.SubMeshName = m_PrefabSystem.GetPrefabName(subMeshBuffer1[m_SubMeshData.Value.SubMeshIndex].m_SubMesh);
                    HandleSubMeshScopes();
                }
            }
            else
            {
                visible = false;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = false;
                }

                m_State = State.NotVisible;
            }

            ColorSet originalMeshColor;
            if (EntityManager.TryGetComponent(m_CurrentEntity, out Game.Objects.Tree tree) &&
                 (m_State & State.EntityChanged) == State.EntityChanged)
            {
                if (tree.m_State == Game.Objects.TreeState.Dead || tree.m_State == Game.Objects.TreeState.Collected || tree.m_State == Game.Objects.TreeState.Stump)
                {
                    visible = false;
                    if (m_ToolSystem.actionMode.IsEditor())
                    {
                        m_EditorVisible.Value = false;
                    }

                    m_State = State.NotVisible;
                    return;
                }

                if ((int)tree.m_State > 0)
                {
                    m_SubMeshData.Value.SubMeshIndex = 0;
                    m_SubMeshData.Value.SubMeshLength = 1;
                    m_SubMeshData.Binding.TriggerUpdate();
                    originalMeshColor = meshColorBuffer[(int)Math.Log((int)tree.m_State, 2) + 1].m_ColorSet;
                }
                else
                {
                    originalMeshColor = meshColorBuffer[0].m_ColorSet;
                }

                m_SubMeshIndexes = new List<int>() { 0, 1, 2, 3 };
            }
            else
            {
                originalMeshColor = meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet;
            }

            if ( (m_State & State.EntityChanged) == State.EntityChanged &&
                m_ResidentialBuildingSelected.Value != EntityManager.HasComponent<Game.Buildings.ResidentialProperty>(m_CurrentEntity))
            {
                m_ResidentialBuildingSelected.Value = EntityManager.HasComponent<Game.Buildings.ResidentialProperty>(m_CurrentEntity);
            }

            if ( (m_State & State.EntityChanged) == State.EntityChanged)
            {
                UpdatePalettes();
            }

            // Service Vehicles
            if (( (m_State & State.EntityChanged) == State.EntityChanged ||
                (m_State & State.ColorChanged) == State.ColorChanged ||
                (m_State & State.UpdateButtonStates) == State.UpdateButtonStates) &&
                m_CurrentEntity != Entity.Null &&
                m_CurrentPrefabEntity != Entity.Null &&
                m_ServiceVehicles == ButtonState.On &&
                EntityManager.TryGetComponent(m_CurrentEntity, out Game.Common.Owner owner) &&
                owner.m_Owner != Entity.Null)
            {
                visible = true;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = true;
                }

                m_CanResetOtherSubMeshes.Value = false;
                m_CurrentColorSet.Value = new RecolorSet(meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet);
                if (!EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<ServiceVehicleColor> serviceVehicleColorBuffer) ||
                     serviceVehicleColorBuffer.Length <= m_SubMeshData.Value.SubMeshIndex ||
                     meshColorBuffer.Length <= m_SubMeshData.Value.SubMeshIndex)
                {
                    m_MatchesVanillaColorSet.Value = EntityManager.HasBuffer<ServiceVehicleColor>(owner.m_Owner) ? new bool[] { false, false, false } : new bool[] { true, true, true };
                    m_CanResetSingleChannels.Value = false;
                }
                else if (EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) &&
                        !MatchesEntireVanillaColorSet(meshColorRecordBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet, meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet))
                {
                    m_MatchesVanillaColorSet.Value = MatchesVanillaColorSet(serviceVehicleColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSetRecord, meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet);
                    m_CanResetSingleChannels.Value = true;
                }
                else
                {
                    EntityManager.RemoveComponent<ServiceVehicleColor>(owner.m_Owner);
                    EntityManager.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                    EntityManager.RemoveComponent<MeshColorRecord>(m_CurrentEntity);
                    m_State = State.ColorChanged | State.UpdateButtonStates;
                    return;
                }
            }

            // Routes
            else if (( (m_State & State.EntityChanged) == State.EntityChanged ||
                     (m_State & State.ColorChanged) == State.ColorChanged ||
                     (m_State & State.UpdateButtonStates) == State.UpdateButtonStates) &&
                     m_CurrentEntity != Entity.Null &&
                     m_CurrentPrefabEntity != Entity.Null &&
                     m_Route.Value == ButtonState.On &&
                     EntityManager.TryGetComponent(m_CurrentEntity, out Game.Routes.CurrentRoute currentRoute) &&
                     currentRoute.m_Route != Entity.Null)
            {
                m_CurrentColorSet.Value = new RecolorSet(meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet);
                m_RouteColorChannel = GetRouteColorChannel(m_CurrentPrefabEntity);

                visible = true;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = true;
                }

                m_CanResetOtherSubMeshes.Value = false;
                if (!EntityManager.TryGetBuffer(currentRoute.m_Route, isReadOnly: true, out DynamicBuffer<RouteVehicleColor> routeVehicleColorBuffer) ||
                     routeVehicleColorBuffer.Length <= m_SubMeshData.Value.SubMeshIndex ||
                     meshColorBuffer.Length <= m_SubMeshData.Value.SubMeshIndex)
                {
                    m_MatchesVanillaColorSet.Value = EntityManager.HasBuffer<RouteVehicleColor>(currentRoute.m_Route) ? new bool[] { false, false, false } : new bool[] { true, true, true };
                    m_CanResetSingleChannels.Value = false;
                }
                else if (EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) &&
                        !MatchesEntireVanillaColorSet(meshColorRecordBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet, meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet))
                {
                    m_MatchesVanillaColorSet.Value = MatchesVanillaColorSet(routeVehicleColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSetRecord, meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet);
                    m_CanResetSingleChannels.Value = true;
                }
                else
                {
                    EntityManager.RemoveComponent<RouteVehicleColor>(currentRoute.m_Route);
                    EntityManager.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                    EntityManager.RemoveComponent<MeshColorRecord>(m_CurrentEntity);
                    m_State = State.ColorChanged | State.UpdateButtonStates;
                    return;
                }
            }

            // Colors Variation
            else if (( (m_State & State.EntityChanged) == State.EntityChanged ||
                     (m_State & State.ColorChanged) == State.ColorChanged ||
                     (m_State & State.UpdateButtonStates) == State.UpdateButtonStates) &&
                     m_CurrentEntity != Entity.Null &&
                     m_CurrentPrefabEntity != Entity.Null &&
                     m_Matching.Value == ButtonState.On &&
                    !EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) &&
                     foundClimatePrefab &&
                     EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer) &&
                     EntityManager.TryGetBuffer(subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) &&
                     colorVariationBuffer.Length > 0)
            {
                Season currentSeason = GetSeasonFromSeasonID(m_ClimatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.name);
                ColorSet colorSet = colorVariationBuffer[0].m_ColorSet;
                int index = 0;
                float cummulativeDifference = float.MaxValue;
                Season season = Season.None;
                for (int i = 0; i < colorVariationBuffer.Length; i++)
                {
                    if ((TryGetSeasonFromColorGroupID(colorVariationBuffer[i].m_GroupID, out Season checkSeason) && checkSeason == currentSeason) || checkSeason == Season.None)
                    {
                        float currentCummulativeDifference = CalculateCummulativeDifference(originalMeshColor, colorVariationBuffer[i].m_ColorSet);
                        if (currentCummulativeDifference < cummulativeDifference)
                        {
                            cummulativeDifference = currentCummulativeDifference;
                            index = i;
                            colorSet = colorVariationBuffer[i].m_ColorSet;
                            season = checkSeason;
                        }
                    }
                }

                if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh, out PrefabBase prefabBase))
                {
                    visible = false;
                    if (m_ToolSystem.actionMode.IsEditor())
                    {
                        m_EditorVisible.Value = false;
                    }

                    return;
                }

                visible = true;
                m_CurrentAssetSeasonIdentifier = new AssetSeasonIdentifier()
                {
                    m_Index = index,
                    m_PrefabID = prefabBase.GetPrefabID(),
                    m_Season = season,
                };

                m_CurrentColorSet.Value = new RecolorSet(colorSet);
                m_CanResetSingleChannels.Value = true;

                m_MatchesVanillaColorSet.Value = MatchesVanillaColorSet(colorSet, m_CurrentAssetSeasonIdentifier);

                m_MatchesSavedOnDisk.Value = MatchesSavedOnDiskColorSet(colorSet, m_CurrentAssetSeasonIdentifier);
                m_CanResetOtherSubMeshes.Value = false;
            }

            // Single Instance
            else if (( (m_State & State.EntityChanged) == State.EntityChanged ||
                     (m_State & State.ColorChanged) == State.ColorChanged ||
                     (m_State & State.UpdateButtonStates) == State.UpdateButtonStates) &&
                     m_SingleInstance == ButtonState.On)
            {
                m_CurrentColorSet.Value = new RecolorSet(meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet);
                if (!EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) ||
                    meshColorRecordBuffer.Length <= m_SubMeshData.Value.SubMeshIndex ||
                    meshColorBuffer.Length <= m_SubMeshData.Value.SubMeshIndex)
                {
                    m_MatchesVanillaColorSet.Value = EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) ? new bool[] { false, false, false } : new bool[] { true, true, true };
                    m_CanResetOtherSubMeshes.Value = false;
                    m_CanResetSingleChannels.Value = false;
                }
                else if (EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer))
                {
                    bool removeComponents = true;
                    bool canResetOtherSubMeshes = false;
                    for (int i = 0; i < m_SubMeshData.Value.SubMeshLength; i++)
                    {
                        if (!MatchesEntireVanillaColorSet(meshColorRecordBuffer[i].m_ColorSet, customMeshColorBuffer[i].m_ColorSet))
                        {
                            removeComponents = false;
                            if (i != m_SubMeshData.Value.SubMeshIndex)
                            {
                                canResetOtherSubMeshes = true;
                            }
                        }
                    }

                    if (m_CanResetOtherSubMeshes.Value != canResetOtherSubMeshes)
                    {
                        m_CanResetOtherSubMeshes.Value = canResetOtherSubMeshes;
                    }

                    if (!removeComponents)
                    {
                        m_MatchesVanillaColorSet.Value = MatchesVanillaColorSet(meshColorRecordBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet, meshColorBuffer[m_SubMeshData.Value.SubMeshIndex].m_ColorSet);
                        m_CanResetSingleChannels.Value = true;
                    }
                    else
                    {
                        EntityManager.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                        EntityManager.RemoveComponent<MeshColorRecord>(m_CurrentEntity);
                        m_State = State.ColorChanged | State.UpdateButtonStates;
                        return;
                    }
                }

                visible = true;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = true;
                }
            }

            m_State = State.Static;
        }
    }
}
