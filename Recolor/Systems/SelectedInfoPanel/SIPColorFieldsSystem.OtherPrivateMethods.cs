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

            if (m_ToolSystem.activeTool == m_DefaultToolSystem)
            {
                if (EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity))
                {
                    matching |= ButtonState.Hidden;
                }

                if (EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity))
                {
                    singleInstance |= ButtonState.Hidden;
                }

                if (!EntityManager.TryGetComponent(m_CurrentEntity, out Game.Common.Owner owner) ||
                    owner.m_Owner == Entity.Null ||
                    ((EntityManager.HasComponent<Game.Vehicles.Ambulance>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Vehicles.FireEngine>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Vehicles.PoliceCar>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Vehicles.GarbageTruck>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Vehicles.Hearse>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Vehicles.MaintenanceVehicle>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Vehicles.PostVan>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Vehicles.RoadMaintenanceVehicle>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Vehicles.Taxi>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Vehicles.ParkMaintenanceVehicle>(m_CurrentEntity)) != true))
                {
                    serviceVehicles |= ButtonState.Hidden;
                }
            }
            else if (m_ToolSystem.activeTool == m_ColorPainterTool)
            {
                serviceVehicles |= ButtonState.Hidden;
            }

            if ((singleInstance & ButtonState.Hidden) != ButtonState.Hidden &&
                (m_PreferredScope == Scope.SingleInstance ||
                (m_PreferredScope == Scope.ServiceVehicles &&
                (serviceVehicles & ButtonState.Hidden) == ButtonState.Hidden) ||
                (m_PreferredScope == Scope.Matching &&
                (matching & ButtonState.Hidden) == ButtonState.Hidden)))
            {
                singleInstance = ButtonState.On;
            }
            else if ((matching & ButtonState.Hidden) != ButtonState.Hidden &&
                     (m_PreferredScope == Scope.Matching ||
                     (m_PreferredScope == Scope.SingleInstance &&
                     (singleInstance & ButtonState.Hidden) == ButtonState.Hidden) ||
                     (m_PreferredScope == Scope.ServiceVehicles &&
                     (serviceVehicles & ButtonState.Hidden) == ButtonState.Hidden)))
            {
                matching = ButtonState.On;
            }
            else if ((serviceVehicles & ButtonState.Hidden) != ButtonState.Hidden &&
                       (m_PreferredScope == Scope.ServiceVehicles ||
                       (m_PreferredScope == Scope.SingleInstance &&
                       (singleInstance & ButtonState.Hidden) == ButtonState.Hidden) ||
                       (m_PreferredScope == Scope.Matching &&
                       (matching & ButtonState.Hidden) == ButtonState.Hidden)))
            {
                serviceVehicles = ButtonState.On;
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
        }

        private ColorSet ChangeColor(int channel, UnityEngine.Color color)
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
            if ((m_SingleInstance.Value & ButtonState.On) == ButtonState.On &&
                !EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity) &&
                EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                if (!EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity))
                {
                    DynamicBuffer<CustomMeshColor> newBuffer = EntityManager.AddBuffer<CustomMeshColor>(m_CurrentEntity);
                    foreach (MeshColor meshColor in meshColorBuffer)
                    {
                        newBuffer.Add(new CustomMeshColor(meshColor));
                    }

                    if (!EntityManager.HasBuffer<MeshColorRecord>(m_CurrentEntity))
                    {
                        DynamicBuffer<MeshColorRecord> meshColorRecordBuffer = EntityManager.AddBuffer<MeshColorRecord>(m_CurrentEntity);
                        foreach (MeshColor meshColor in meshColorBuffer)
                        {
                            meshColorRecordBuffer.Add(new MeshColorRecord(meshColor));
                        }
                    }
                }

                if (!EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: false, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer))
                {
                    return default;
                }

                ColorSet colorSet = default;
                int length = meshColorBuffer.Length;
                if (EntityManager.HasComponent<Tree>(m_CurrentEntity))
                {
                    length = Math.Min(4, meshColorBuffer.Length);
                }

                for (int i = 0; i < length; i++)
                {
                    CustomMeshColor customMeshColor = customMeshColorBuffer[i];
                    if (channel >= 0 && channel < 3)
                    {
                        customMeshColor.m_ColorSet[channel] = color;
                    }

                    customMeshColorBuffer[i] = customMeshColor;
                    m_TimeColorLastChanged = UnityEngine.Time.time;
                    m_PreviouslySelectedEntity = Entity.Null;
                    colorSet = customMeshColor.m_ColorSet;
                }

                buffer.AddComponent<BatchesUpdated>(m_CurrentEntity);
                AddBatchesUpdatedToSubElements(m_CurrentEntity, buffer);

                return colorSet;
            }
            else if ((m_Matching.Value & ButtonState.On) == ButtonState.On &&
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

            return default;
        }

        private void ResetColorSet()
        {
            ResetColor(0);
            ResetColor(1);
            ResetColor(2);
        }

        private bool[] MatchesVanillaColorSet(MeshColorRecord meshColorRecord, ColorSet colorSet)
        {
            bool[] matches = new bool[3] { false, false, false };
            for (int i = 0; i < 3; i++)
            {
                if (meshColorRecord.m_ColorSet[i] == colorSet[i])
                {
                    matches[i] = true;
                }
            }

            return matches;
        }

        private bool MatchesEntireVanillaColorSet(MeshColorRecord meshColorRecord, ColorSet meshColor)
        {
            bool[] matches = MatchesVanillaColorSet(meshColorRecord, meshColor);
            if (matches[0] == false || matches[1] == false || matches[2] == false)
            {
                return false;
            }

            return true;
        }

        private void ResetColor(int channel)
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();

            if (EntityManager.HasComponent<CustomMeshColor>(m_CurrentEntity))
            {
                if (EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) &&
                    EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: false, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer) &&
                    meshColorRecordBuffer.Length > m_SubMeshIndex.Value &&
                    customMeshColorBuffer.Length > m_SubMeshIndex.Value &&
                    channel >= 0 && channel <= 2)
                {
                    CustomMeshColor customMeshColor = customMeshColorBuffer[m_SubMeshIndex.Value];
                    customMeshColor.m_ColorSet[channel] = meshColorRecordBuffer[m_SubMeshIndex.Value].m_ColorSet[channel];
                    customMeshColorBuffer[m_SubMeshIndex.Value] = customMeshColor;

                    if (MatchesEntireVanillaColorSet(meshColorRecordBuffer[m_SubMeshIndex.Value], customMeshColor.m_ColorSet))
                    {
                        buffer.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                        buffer.RemoveComponent<MeshColorRecord>(m_CurrentEntity);
                    }
                }
                else
                {
                    buffer.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                    buffer.RemoveComponent<MeshColorRecord>(m_CurrentEntity);
                }

                buffer.AddComponent<BatchesUpdated>(m_CurrentEntity);

                AddBatchesUpdatedToSubElements(m_CurrentEntity, buffer);

                m_PreviouslySelectedEntity = Entity.Null;
                return;
            }

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
    }
}
