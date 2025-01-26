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
    using Game.Input;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
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

                if (mode.IsEditor())
                {
                    m_Minimized.Value = true;
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
                    }
                }

                visible = false;
                m_PreviouslySelectedEntity = Entity.Null;

                return;
            }

            if (m_CurrentEntity == Entity.Null &&
                m_PreviouslySelectedEntity != Entity.Null)
            {
                m_PreviouslySelectedEntity = Entity.Null;
            }

            if (m_CurrentEntity == Entity.Null)
            {
                visible = false;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = false;
                }

                return;
            }

            if (m_PreviouslySelectedEntity == Entity.Null)
            {
                visible = false;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = false;
                }
            }

            if (m_PreviouslySelectedEntity != m_CurrentEntity)
            {
                m_PreviouslySelectedEntity = Entity.Null;
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

            if (!EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                visible = false;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = false;
                }

                return;
            }

            ColorSet originalMeshColor = meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet;
            if (EntityManager.TryGetComponent(m_CurrentEntity, out Game.Objects.Tree tree))
            {
                if (tree.m_State == Game.Objects.TreeState.Dead || tree.m_State == Game.Objects.TreeState.Collected || tree.m_State == Game.Objects.TreeState.Stump)
                {
                    visible = false;
                    if (m_ToolSystem.actionMode.IsEditor())
                    {
                        m_EditorVisible.Value = false;
                    }

                    return;
                }

                if ((int)tree.m_State > 0)
                {
                    m_SubMeshIndex.Value = 0;
                    originalMeshColor = meshColorBuffer[(int)Math.Log((int)tree.m_State, 2) + 1].m_ColorSet;
                }
            }

            HandleScopeAndButtonStates();

            // Service Vehicles
            if (m_PreviouslySelectedEntity != m_CurrentEntity &&
                m_CurrentEntity != Entity.Null &&
                m_CurrentPrefabEntity != Entity.Null &&
                m_ServiceVehicles == ButtonState.On &&
                EntityManager.TryGetComponent(m_CurrentEntity, out Game.Common.Owner owner) &&
                owner.m_Owner != Entity.Null)
            {
                m_CurrentColorSet.Value = new RecolorSet(meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet);
                m_PreviouslySelectedEntity = m_CurrentEntity;
                if (!EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<ServiceVehicleColor> serviceVehicleColorBuffer) ||
                     serviceVehicleColorBuffer.Length <= m_SubMeshIndex.Value ||
                     meshColorBuffer.Length <= m_SubMeshIndex.Value)
                {
                    m_MatchesVanillaColorSet.Value = EntityManager.HasBuffer<ServiceVehicleColor>(owner.m_Owner) ? new bool[] { false, false, false } : new bool[] { true, true, true };
                    m_CanResetSingleChannels.Value = false;
                }
                else if (EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) &&
                        !MatchesEntireVanillaColorSet(meshColorRecordBuffer[m_SubMeshIndex.Value].m_ColorSet, meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet))
                {
                    m_MatchesVanillaColorSet.Value = MatchesVanillaColorSet(serviceVehicleColorBuffer[m_SubMeshIndex.Value].m_ColorSetRecord, meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet);
                    m_CanResetSingleChannels.Value = true;
                }
                else
                {
                    EntityManager.RemoveComponent<ServiceVehicleColor>(owner.m_Owner);
                    EntityManager.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                    EntityManager.RemoveComponent<MeshColorRecord>(m_CurrentEntity);
                    m_PreviouslySelectedEntity = Entity.Null;
                }

                visible = true;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = true;
                }
            }

            // Routes
            else if (m_PreviouslySelectedEntity != m_CurrentEntity &&
                     m_CurrentEntity != Entity.Null &&
                     m_CurrentPrefabEntity != Entity.Null &&
                     m_Route.Value == ButtonState.On &&
                     EntityManager.TryGetComponent(m_CurrentEntity, out Game.Routes.CurrentRoute currentRoute) &&
                     currentRoute.m_Route != Entity.Null)
            {
                m_CurrentColorSet.Value = new RecolorSet(meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet);
                m_PreviouslySelectedEntity = m_CurrentEntity;

                if (!EntityManager.TryGetBuffer(currentRoute.m_Route, isReadOnly: true, out DynamicBuffer<RouteVehicleColor> routeVehicleColorBuffer) ||
                     routeVehicleColorBuffer.Length <= m_SubMeshIndex.Value ||
                     meshColorBuffer.Length <= m_SubMeshIndex.Value)
                {
                    m_MatchesVanillaColorSet.Value = EntityManager.HasBuffer<RouteVehicleColor>(currentRoute.m_Route) ? new bool[] { false, false, false } : new bool[] { true, true, true };
                    m_CanResetSingleChannels.Value = false;
                }
                else if (EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) &&
                        !MatchesEntireVanillaColorSet(meshColorRecordBuffer[m_SubMeshIndex.Value].m_ColorSet, meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet))
                {
                    m_MatchesVanillaColorSet.Value = MatchesVanillaColorSet(routeVehicleColorBuffer[m_SubMeshIndex.Value].m_ColorSetRecord, meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet);
                    m_CanResetSingleChannels.Value = true;
                }
                else
                {
                    EntityManager.RemoveComponent<RouteVehicleColor>(currentRoute.m_Route);
                    EntityManager.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                    EntityManager.RemoveComponent<MeshColorRecord>(m_CurrentEntity);
                    m_PreviouslySelectedEntity = Entity.Null;
                }

                m_RouteColorChannel = GetRouteColorChannel(m_CurrentPrefabEntity);

                visible = true;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = true;
                }
            }

            // Colors Variation
            else if (m_PreviouslySelectedEntity != m_CurrentEntity &&
                     m_CurrentEntity != Entity.Null &&
                     m_CurrentPrefabEntity != Entity.Null &&
                     m_Matching.Value == ButtonState.On &&
                    !EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) &&
                     foundClimatePrefab &&
                     EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer) &&
                     EntityManager.TryGetBuffer(subMeshBuffer[m_SubMeshIndex.Value].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) &&
                     colorVariationBuffer.Length > 0)
            {
                Season currentSeason = GetSeasonFromSeasonID(m_ClimatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.m_NameID);
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

                if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[m_SubMeshIndex.Value].m_SubMesh, out PrefabBase prefabBase))
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

                m_PreviouslySelectedEntity = m_CurrentEntity;

                m_MatchesVanillaColorSet.Value = MatchesVanillaColorSet(colorSet, m_CurrentAssetSeasonIdentifier);
                m_MatchesSavedOnDisk.Value = MatchesSavedOnDiskColorSet(colorSet, m_CurrentAssetSeasonIdentifier);
            }

            // Single Instance
            else if (m_PreviouslySelectedEntity != m_CurrentEntity &&
                     m_SingleInstance == ButtonState.On)
            {
                m_CurrentColorSet.Value = new RecolorSet(meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet);
                m_PreviouslySelectedEntity = m_CurrentEntity;
                if (!EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) ||
                    meshColorRecordBuffer.Length <= m_SubMeshIndex.Value ||
                    meshColorBuffer.Length <= m_SubMeshIndex.Value)
                {
                    m_MatchesVanillaColorSet.Value = EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) ? new bool[] { false, false, false } : new bool[] { true, true, true };
                    m_CanResetSingleChannels.Value = false;
                }
                else if (!MatchesEntireVanillaColorSet(meshColorRecordBuffer[m_SubMeshIndex.Value].m_ColorSet, meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet))
                {
                    m_MatchesVanillaColorSet.Value = MatchesVanillaColorSet(meshColorRecordBuffer[m_SubMeshIndex.Value].m_ColorSet, meshColorBuffer[m_SubMeshIndex.Value].m_ColorSet);
                    m_CanResetSingleChannels.Value = true;
                }
                else
                {
                    EntityManager.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                    EntityManager.RemoveComponent<MeshColorRecord>(m_CurrentEntity);
                    m_PreviouslySelectedEntity = Entity.Null;
                }

                visible = true;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = true;
                }
            }

            if (m_PreviouslySelectedEntity == m_CurrentEntity &&
              ((m_Matching.Value & ButtonState.On) == ButtonState.On ||
                EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity)) &&
               !EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) &&
                m_NeedsColorRefresh == true &&
                UnityEngine.Time.time > m_TimeColorLastChanged + 0.5f)
            {
                ColorRefresh();
            }
        }
    }
}
