// <copyright file="SIPColorFieldsSystem.OtherPrivateMethods.cs" company="Yenyang's Mods. MIT License">
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
    using Game.Routes;
    using Game.Simulation;
    using Game.Tools;
    using Game.Vehicles;
    using Recolor.Domain;
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;
    using static Game.Rendering.OverlayRenderSystem;

    /// <summary>
    ///  Adds color fields to selected info panel for changing colors of buildings, vehicles, props, etc.
    /// </summary>
    public partial class SIPColorFieldsSystem : ExtendedInfoSectionBase
    {
        private bool GetClimatePrefab()
        {
            Entity currentClimate = m_ClimateSystem.currentClimate;
            if (currentClimate == Entity.Null)
            {
                m_Log.Warn($"{nameof(SIPColorFieldsSystem)}.{nameof(GetClimatePrefab)} couldn't find climate entity.");
                return false;
            }

            if (!m_PrefabSystem.TryGetPrefab(m_ClimateSystem.currentClimate, out m_ClimatePrefab))
            {
                m_Log.Warn($"{nameof(SIPColorFieldsSystem)}.{nameof(GetClimatePrefab)} couldn't find climate prefab.");
                return false;
            }

            return true;
        }

        private void CopyColor(UnityEngine.Color color)
        {
            m_CanPasteColor.Value = true;
            m_CopiedColor = color;
        }

        private void CopyColorSet()
        {
            m_CanPasteColorSet.Value = true;
            m_CopiedColorSet = m_CurrentColorSet.Value.GetColorSet();
        }

        private void PasteColor(int channel)
        {
            ChangeColor(channel, m_CopiedColor);
        }

        private void PasteColorSet()
        {
            ChangeColor(0, m_CopiedColorSet.m_Channel0);
            ChangeColor(1, m_CopiedColorSet.m_Channel1);
            ChangeColor(2, m_CopiedColorSet.m_Channel2);
        }

        private void ChangeColorAction(int channel, UnityEngine.Color color)
        {
            ChangeColor(channel, color);
        }

        private void HandleScopeAndButtonStates()
        {
            ButtonState singleInstance = ButtonState.Off;
            ButtonState matching = ButtonState.Off;
            ButtonState serviceVehicles = ButtonState.Off;
            ButtonState route = ButtonState.Off;

            if (m_ToolSystem.activeTool == m_DefaultToolSystem)
            {
                if (EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity))
                {
                    matching |= ButtonState.Hidden;
                }

                if (EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity) ||
                   (EntityManager.TryGetComponent(m_CurrentEntity, out Game.Common.Owner owner) &&
                    owner.m_Owner != Entity.Null &&
                    EntityManager.HasBuffer<ServiceVehicleColor>(owner.m_Owner)) ||
                   (EntityManager.TryGetComponent(m_CurrentEntity, out Game.Routes.CurrentRoute currentRoute) &&
                    currentRoute.m_Route != Entity.Null &&
                    EntityManager.HasBuffer<RouteVehicleColor>(currentRoute.m_Route)))
                {
                    singleInstance |= ButtonState.Hidden;
                }

                if (!EntityManager.TryGetComponent(m_CurrentEntity, out Game.Common.Owner owner1) ||
                          owner1.m_Owner == Entity.Null ||
                        ((EntityManager.HasComponent<Game.Vehicles.Ambulance>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.FireEngine>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.PoliceCar>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.GarbageTruck>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.Hearse>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.MaintenanceVehicle>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.PostVan>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.RoadMaintenanceVehicle>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.Taxi>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.ParkMaintenanceVehicle>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.WorkVehicle>(m_CurrentEntity) ||
                          EntityManager.HasComponent<Game.Vehicles.PostVan>(m_CurrentEntity)) != true))
                {
                    serviceVehicles |= ButtonState.Hidden;
                }

                if (EntityManager.TryGetComponent(m_CurrentEntity, out Game.Vehicles.Controller controller) &&
                    EntityManager.TryGetComponent(controller.m_Controller, out Game.Routes.CurrentRoute controllerRoute) &&
                    controllerRoute.m_Route != Entity.Null &&
                    EntityManager.HasBuffer<RouteVehicleColor>(controllerRoute.m_Route))
                {
                    singleInstance |= ButtonState.Hidden;
                }
                else if ((!EntityManager.TryGetComponent(m_CurrentEntity, out Game.Routes.CurrentRoute currentRoute1) ||
                           currentRoute1.m_Route == Entity.Null) &&
                         (!EntityManager.TryGetComponent(m_CurrentEntity, out Game.Vehicles.Controller controller1) ||
                          !EntityManager.TryGetComponent(controller.m_Controller, out Game.Routes.CurrentRoute controllerRoute1) ||
                           controllerRoute1.m_Route == Entity.Null))
                {
                    route |= ButtonState.Hidden;
                }
            }
            else if (m_ToolSystem.activeTool == m_ColorPainterTool)
            {
                serviceVehicles |= ButtonState.Hidden;
                route |= ButtonState.Hidden;
            }

            if ((singleInstance & ButtonState.Hidden) != ButtonState.Hidden &&
                (m_PreferredScope == Scope.SingleInstance ||
                IsPreferredScopeHidden()))
            {
                singleInstance = ButtonState.On;
            }
            else if ((matching & ButtonState.Hidden) != ButtonState.Hidden &&
                     (m_PreferredScope == Scope.Matching ||
                     IsPreferredScopeHidden()))
            {
                matching = ButtonState.On;
            }
            else if ((serviceVehicles & ButtonState.Hidden) != ButtonState.Hidden &&
                     (m_PreferredScope == Scope.ServiceVehicles ||
                     IsPreferredScopeHidden()))
            {
                serviceVehicles = ButtonState.On;
            }
            else if ((route & ButtonState.Hidden) != ButtonState.Hidden &&
                      (m_PreferredScope == Scope.Route ||
                      IsPreferredScopeHidden()))
            {
                route = ButtonState.On;
            }
            else
            {
                m_Log.Info($"{nameof(SIPColorFieldsSystem)}.{nameof(HandleScopeAndButtonStates)} No valid scope.");
            }

            if (m_SingleInstance.Value != singleInstance)
            {
                m_SingleInstance.Value = singleInstance;
            }

            if (m_Matching.Value != matching)
            {
                m_Matching.Value = matching;
            }

            if (m_ServiceVehicles.Value != serviceVehicles)
            {
                m_ServiceVehicles.Value = serviceVehicles;
            }

            if (m_Route.Value != route)
            {
                m_Route.Value = route;
            }
        }

        private bool IsPreferredScopeHidden()
        {
            if (m_PreferredScope == Scope.SingleInstance &&
                (m_SingleInstance.Value & ButtonState.Hidden) == ButtonState.Hidden)
            {
                return true;
            }

            if (m_PreferredScope == Scope.Matching &&
                (m_Matching.Value & ButtonState.Hidden) == ButtonState.Hidden)
            {
                return true;
            }

            if (m_PreferredScope == Scope.ServiceVehicles &&
                (m_ServiceVehicles.Value & ButtonState.Hidden) == ButtonState.Hidden)
            {
                return true;
            }

            if (m_PreferredScope == Scope.Route &&
                (m_Route.Value & ButtonState.Hidden) == ButtonState.Hidden)
            {
                return true;
            }

            return false;
        }

        private int GetRouteColorChannel(Entity prefabEntity)
        {
            if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer) ||
                subMeshBuffer.Length == 0 ||
               !m_PrefabSystem.TryGetPrefab(subMeshBuffer[0].m_SubMesh, out PrefabBase prefabBase) ||
                prefabBase is null ||
               !prefabBase.TryGet(out Game.Prefabs.ColorProperties colorProperties))
            {
                return -1;
            }

            for (int i = 0; i < colorProperties.m_ChannelsBinding.Count; i++)
            {
                if (colorProperties.m_ChannelsBinding[i].m_CanBeModifiedByExternal)
                {
                    return i;
                }
            }

            return -1;
        }

        private ColorSet ChangeColor(int channel, UnityEngine.Color color)
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();

            // Single Instance
            if (m_SingleInstance.Value == ButtonState.On &&
               !EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity) &&
                EntityManager.HasBuffer<MeshColor>(m_CurrentEntity))
            {
                return ChangeSingleInstanceColorChannel(channel, color, m_CurrentEntity, buffer);
            }

            // Color Variations
            else if (m_Matching.Value == ButtonState.On &&
                    !EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity))
            {
                if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    return default;
                }

                ColorVariation colorVariation = default;
                int length = subMeshBuffer.Length;
                if (EntityManager.HasComponent<Tree>(m_CurrentEntity))
                {
                    length = Math.Min(4, subMeshBuffer.Length);
                }

                for (int i = 0; i < length; i++)
                {
                    if (!EntityManager.TryGetBuffer(subMeshBuffer[i].m_SubMesh, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer) ||
                         colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
                    {
                        continue;
                    }

                    colorVariation = colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index];
                    if (channel >= 0 && channel < 3)
                    {
                        colorVariation.m_ColorSet[channel] = color;
                    }

                    colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index] = colorVariation;
                    m_TimeColorLastChanged = UnityEngine.Time.time;
                    m_PreviouslySelectedEntity = Entity.Null;
                }

                buffer.AddComponent<BatchesUpdated>(m_CurrentEntity);
                AddBatchesUpdatedToSubElements(m_CurrentEntity, buffer);

                GenerateOrUpdateCustomColorVariationEntity();
                m_NeedsColorRefresh = true;

                return colorVariation.m_ColorSet;
            }

            // Service Vehicles
            else if (m_ServiceVehicles.Value == ButtonState.On &&
                     EntityManager.TryGetComponent(m_CurrentEntity, out Owner owner) &&
                     owner.m_Owner != Entity.Null &&
                    !EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity) &&
                     EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer1) &&
                     EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<OwnedVehicle> ownedVehicleBuffer))
            {
                if (!EntityManager.HasBuffer<ServiceVehicleColor>(owner.m_Owner))
                {
                    DynamicBuffer<ServiceVehicleColor> newBuffer = EntityManager.AddBuffer<ServiceVehicleColor>(owner.m_Owner);
                    foreach (MeshColor meshColor in meshColorBuffer1)
                    {
                        newBuffer.Add(new ServiceVehicleColor(meshColor.m_ColorSet, meshColor.m_ColorSet));
                    }
                }

                if (!EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: false, out DynamicBuffer<ServiceVehicleColor> serviceVehicleColorBuffer))
                {
                    return default;
                }

                ColorSet colorSet = default;
                int length = meshColorBuffer1.Length;

                for (int i = 0; i < length; i++)
                {
                    ServiceVehicleColor serviceVehicleColor = serviceVehicleColorBuffer[i];
                    if (channel >= 0 && channel < 3)
                    {
                        serviceVehicleColor.m_ColorSet[channel] = color;
                    }

                    serviceVehicleColorBuffer[i] = serviceVehicleColor;
                    m_PreviouslySelectedEntity = Entity.Null;
                    colorSet = serviceVehicleColor.m_ColorSet;
                }

                foreach (OwnedVehicle ownedVehicle in ownedVehicleBuffer)
                {
                    colorSet = ChangeSingleInstanceColorChannel(channel, color, ownedVehicle.m_Vehicle, buffer);
                }

                return colorSet;
            }

            // Routes
            else if (m_Route.Value == ButtonState.On &&
                     EntityManager.TryGetComponent(m_CurrentEntity, out Game.Routes.CurrentRoute currentRoute) &&
                     currentRoute.m_Route != Entity.Null &&
                    !EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity) &&
                     EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColor> routeMeshColorBuffer) &&
                     EntityManager.TryGetBuffer(currentRoute.m_Route, isReadOnly: true, out DynamicBuffer<Game.Routes.RouteVehicle> routeVehicleBuffer) &&
                     EntityManager.TryGetComponent(currentRoute.m_Route, out Game.Routes.Color routeColor))
            {
                if (!EntityManager.HasBuffer<RouteVehicleColor>(currentRoute.m_Route))
                {
                    DynamicBuffer<RouteVehicleColor> newBuffer = EntityManager.AddBuffer<RouteVehicleColor>(currentRoute.m_Route);
                    foreach (MeshColor meshColor in routeMeshColorBuffer)
                    {
                        newBuffer.Add(new RouteVehicleColor(meshColor.m_ColorSet, meshColor.m_ColorSet));
                    }
                }

                if (!EntityManager.TryGetBuffer(currentRoute.m_Route, isReadOnly: false, out DynamicBuffer<RouteVehicleColor> routeVehicleColorBuffer))
                {
                    return default;
                }

                ColorSet colorSet = default;
                int length = routeMeshColorBuffer.Length;

                for (int i = 0; i < length; i++)
                {
                    RouteVehicleColor routeVehicleColor = routeVehicleColorBuffer[i];
                    if (channel >= 0 && channel < 3)
                    {
                        routeVehicleColor.m_ColorSet[channel] = color;
                    }

                    if (m_RouteColorChannel == channel)
                    {
                        routeVehicleColor.m_ColorSetRecord[channel] = color;
                    }

                    routeVehicleColorBuffer[i] = routeVehicleColor;
                    m_PreviouslySelectedEntity = Entity.Null;
                    colorSet = routeVehicleColor.m_ColorSet;
                }

                foreach (RouteVehicle routeVehicle in routeVehicleBuffer)
                {
                    if (EntityManager.TryGetBuffer(routeVehicle.m_Vehicle, isReadOnly: true, out DynamicBuffer<LayoutElement> layoutElementBuffer) &&
                        layoutElementBuffer.Length > 0)
                    {
                        foreach (Game.Vehicles.LayoutElement layoutElement in layoutElementBuffer)
                        {
                            ChangeSingleInstanceColorChannel(channel, color, layoutElement.m_Vehicle, buffer);
                        }
                    }
                    else
                    {
                        ChangeSingleInstanceColorChannel(channel, color, routeVehicle.m_Vehicle, buffer);
                    }
                }

                if (m_RouteColorChannel == channel)
                {
                    routeColor.m_Color = color;
                    EntityManager.SetComponentData(currentRoute.m_Route, routeColor);
                    EntityManager.AddComponent<Game.Routes.ColorUpdated>(currentRoute.m_Route);
                }

                return colorSet;
            }

            return default;
        }

        private ColorSet ChangeSingleInstanceColorChannel(int channel, UnityEngine.Color color, Entity entity, EntityCommandBuffer buffer)
        {
            if (!EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                return default;
            }

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
                return default;
            }

            ColorSet colorSet = default;

            for (int i = 0; i < meshColorBuffer.Length; i++)
            {
                CustomMeshColor customMeshColor = customMeshColorBuffer[i];
                if (channel >= 0 && channel < 3)
                {
                    customMeshColor.m_ColorSet[channel] = color;
                }

                customMeshColorBuffer[i] = customMeshColor;
                m_PreviouslySelectedEntity = Entity.Null;
                colorSet = customMeshColor.m_ColorSet;
            }

            buffer.AddComponent<BatchesUpdated>(entity);
            AddBatchesUpdatedToSubElements(entity, buffer);
            return colorSet;
        }

        private void ResetColorSet()
        {
            ResetColor(0);
            ResetColor(1);
            ResetColor(2);
        }

        private bool[] MatchesVanillaColorSet(ColorSet record, ColorSet colorSet)
        {
            bool[] matches = new bool[3] { false, false, false };
            for (int i = 0; i < 3; i++)
            {
                if (record[i] == colorSet[i])
                {
                    matches[i] = true;
                }
            }

            return matches;
        }

        private bool MatchesEntireVanillaColorSet(ColorSet record, ColorSet meshColor)
        {
            bool[] matches = MatchesVanillaColorSet(record, meshColor);
            if (matches[0] == false || matches[1] == false || matches[2] == false)
            {
                return false;
            }

            return true;
        }

        private void ResetColor(int channel)
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();

            // Service Vehicles
            if (EntityManager.TryGetComponent(m_CurrentEntity, out Owner owner) &&
                owner.m_Owner != Entity.Null &&
                EntityManager.HasBuffer<ServiceVehicleColor>(owner.m_Owner) &&
                EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<OwnedVehicle> ownedVehicleBuffer))
            {
                foreach (OwnedVehicle ownedVehicle in ownedVehicleBuffer)
                {
                    ResetSingleInstanceByChannel(channel, ownedVehicle.m_Vehicle, buffer);
                }

                m_PreviouslySelectedEntity = Entity.Null;
                return;
            }

            // Route vehicles
            if (EntityManager.TryGetComponent(m_CurrentEntity, out Game.Routes.CurrentRoute currentRoute) &&
                currentRoute.m_Route != Entity.Null &&
                EntityManager.HasBuffer<Domain.RouteVehicleColor>(currentRoute.m_Route) &&
                EntityManager.TryGetBuffer(currentRoute.m_Route, isReadOnly: true, out DynamicBuffer<Game.Routes.RouteVehicle> routeVehicleBuffer))
            {
                foreach (RouteVehicle routeVehicle in routeVehicleBuffer)
                {
                    if (!EntityManager.TryGetBuffer(routeVehicle.m_Vehicle, isReadOnly: true, out DynamicBuffer<LayoutElement> layoutElementBuffer))
                    {
                        ResetSingleInstanceByChannel(channel, routeVehicle.m_Vehicle, buffer);
                    }
                    else
                    {
                        foreach (LayoutElement layoutElement in layoutElementBuffer)
                        {
                            ResetSingleInstanceByChannel(channel, layoutElement.m_Vehicle, buffer);
                        }
                    }
                }

                m_PreviouslySelectedEntity = Entity.Null;
                return;
            }

            // Single Instance
            if (EntityManager.HasComponent<CustomMeshColor>(m_CurrentEntity))
            {
                ResetSingleInstanceByChannel(channel, m_CurrentEntity, buffer);
                m_PreviouslySelectedEntity = Entity.Null;
                return;
            }

            // Color Variations
            if (!TryGetVanillaColorSet(m_CurrentAssetSeasonIdentifier, out ColorSet vanillaColorSet))
            {
                m_Log.Info($"{nameof(SIPColorFieldsSystem)}.{nameof(ResetColor)} Could not find vanilla color set for {m_CurrentAssetSeasonIdentifier.m_PrefabID} {m_CurrentAssetSeasonIdentifier.m_Season} {m_CurrentAssetSeasonIdentifier.m_Index}");
                return;
            }

            ColorSet colorSet = default;
            if (channel >= 0 && channel <= 2)
            {
                colorSet = ChangeColor(channel, vanillaColorSet[channel]);
            }

            bool[] matches = MatchesVanillaColorSet(colorSet, m_CurrentAssetSeasonIdentifier);

            m_PreviouslySelectedEntity = Entity.Null;

            if (matches.Length >= 3 && matches[0] && matches[1] && matches[2])
            {
                ColorRefresh();
            }
            else
            {
                GenerateOrUpdateCustomColorVariationEntity();
            }
        }

        private void ResetSingleInstanceByChannel(int channel, Entity entity, EntityCommandBuffer buffer)
        {
            if (EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) &&
                    EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer) &&
                    meshColorRecordBuffer.Length > m_SubMeshIndex.Value &&
                    customMeshColorBuffer.Length > m_SubMeshIndex.Value &&
                    channel >= 0 && channel <= 2)
            {
                CustomMeshColor customMeshColor = customMeshColorBuffer[m_SubMeshIndex.Value];
                customMeshColor.m_ColorSet[channel] = meshColorRecordBuffer[m_SubMeshIndex.Value].m_ColorSet[channel];
                customMeshColorBuffer[m_SubMeshIndex.Value] = customMeshColor;

                if (MatchesEntireVanillaColorSet(meshColorRecordBuffer[m_SubMeshIndex.Value].m_ColorSet, customMeshColor.m_ColorSet))
                {
                    buffer.RemoveComponent<CustomMeshColor>(entity);
                    buffer.RemoveComponent<MeshColorRecord>(entity);
                }
            }
            else
            {
                buffer.RemoveComponent<CustomMeshColor>(entity);
                buffer.RemoveComponent<MeshColorRecord>(entity);
            }

            buffer.AddComponent<BatchesUpdated>(entity);

            AddBatchesUpdatedToSubElements(entity, buffer);
        }
    }
}
