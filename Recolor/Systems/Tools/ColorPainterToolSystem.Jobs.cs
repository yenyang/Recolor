// <copyright file="ColorPainterToolSystem.Jobs.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.Tools
{
    using System;
    using System.Xml;
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
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;
            [ReadOnly]
            public BufferLookup<Game.Net.SubLane> m_SubLaneLookup;
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;
            [ReadOnly]
            public BufferLookup<CustomMeshColor> m_CustomMeshColorLookup;
            [ReadOnly]
            public BufferLookup<MeshColorRecord> m_MeshColorRecordLookup;
            public bool m_ResettingColorsToRecord;
            public ColorSet m_ApplyColorSet;
            public bool3 m_ChannelToggles;
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
                        ColorSet currentColorSet = m_ApplyColorSet;
                        bool completeReset = false;

                        if (!m_MeshColorLookup.TryGetBuffer(currentEntity, out DynamicBuffer<MeshColor> originalMeshColors))
                        {
                            continue;
                        }

                        if (m_ResettingColorsToRecord &&
                           !m_MeshColorRecordLookup.HasBuffer(currentEntity))
                        {
                            buffer.RemoveComponent<CustomMeshColor>(currentEntity);
                            buffer.AddComponent<BatchesUpdated>(currentEntity);
                            completeReset = true;
                        }
                        else if (m_ResettingColorsToRecord &&
                                 m_MeshColorRecordLookup.TryGetBuffer(currentEntity, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer))
                        {
                            bool matchesVanillaColorSet = true;
                            for (int j = 0; j < 3; j++)
                            {
                                if (m_ChannelToggles[j])
                                {
                                    currentColorSet[j] = meshColorRecordBuffer[0].m_ColorSet[j];
                                }

                                if (currentColorSet[j] != meshColorRecordBuffer[0].m_ColorSet[j])
                                {
                                    matchesVanillaColorSet = false;
                                }
                            }

                            if (matchesVanillaColorSet)
                            {
                                buffer.RemoveComponent<CustomMeshColor>(currentEntity);
                                buffer.RemoveComponent<MeshColorRecord>(currentEntity);
                                buffer.AddComponent<BatchesUpdated>(currentEntity);
                                completeReset = true;
                            }
                        }
                        else if (m_ResettingColorsToRecord)
                        {
                            continue;
                        }

                        if (!completeReset)
                        {
                            if (!m_MeshColorRecordLookup.HasBuffer(currentEntity))
                            {
                                DynamicBuffer<MeshColorRecord> meshColorRecords = buffer.AddBuffer<MeshColorRecord>(currentEntity);
                                for (int j = 0; j < originalMeshColors.Length; j++)
                                {
                                    meshColorRecords.Add(new MeshColorRecord() { m_ColorSet = originalMeshColors[j].m_ColorSet });
                                }
                            }

                            DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(currentEntity);
                            DynamicBuffer<CustomMeshColor> customMeshColors = buffer.AddBuffer<CustomMeshColor>(currentEntity);
                            for (int j = 0; j < originalMeshColors.Length; j++)
                            {
                                meshColorBuffer.Add(new MeshColor() { m_ColorSet = CompileColorSet(currentColorSet, m_ChannelToggles, originalMeshColors[j].m_ColorSet) });
                                customMeshColors.Add(new CustomMeshColor() { m_ColorSet = CompileColorSet(currentColorSet, m_ChannelToggles, originalMeshColors[j].m_ColorSet) });
                            }
                        }

                        buffer.AddComponent<BatchesUpdated>(currentEntity);

                        // Add batches updated to subobjects.
                        if (m_SubObjectLookup.TryGetBuffer(currentEntity, out DynamicBuffer<Game.Objects.SubObject> subObjectBuffer))
                        {
                            foreach (Game.Objects.SubObject subObject in subObjectBuffer)
                            {
                                ProcessSubObject(subObject);

                                if (!m_SubObjectLookup.TryGetBuffer(subObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer))
                                {
                                    continue;
                                }

                                foreach (Game.Objects.SubObject deepSubObject in deepSubObjectBuffer)
                                {
                                    ProcessSubObject(deepSubObject);

                                    if (!m_SubObjectLookup.TryGetBuffer(deepSubObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer2))
                                    {
                                        continue;
                                    }

                                    foreach (Game.Objects.SubObject deepSubObject2 in deepSubObjectBuffer2)
                                    {
                                        ProcessSubObject(deepSubObject2);

                                        if (!m_SubObjectLookup.TryGetBuffer(deepSubObject2.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer3))
                                        {
                                            continue;
                                        }

                                        foreach (Game.Objects.SubObject deepSubObject3 in deepSubObjectBuffer3)
                                        {
                                            ProcessSubObject(deepSubObject3);

                                            if (!m_SubObjectLookup.TryGetBuffer(deepSubObject3.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer4))
                                            {
                                                continue;
                                            }

                                            foreach (Game.Objects.SubObject deepSubObject4 in deepSubObjectBuffer4)
                                            {
                                                ProcessSubObject(deepSubObject4);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Add batches updated to sublanes.
                        if (m_SubLaneLookup.TryGetBuffer(currentEntity, out DynamicBuffer<Game.Net.SubLane> subLaneBuffer))
                        {
                            foreach (Game.Net.SubLane subLane in subLaneBuffer)
                            {
                                if (m_MeshColorLookup.HasBuffer(subLane.m_SubLane) && !m_CustomMeshColorLookup.HasBuffer(subLane.m_SubLane))
                                {
                                    buffer.AddComponent<BatchesUpdated>(subLane.m_SubLane);
                                }
                            }
                        }
                    }
                }
            }

            private void ProcessSubObject(Game.Objects.SubObject subObject)
            {
                if (m_MeshColorLookup.HasBuffer(subObject.m_SubObject) && !m_CustomMeshColorLookup.HasBuffer(subObject.m_SubObject))
                {
                    buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                }
            }

            private ColorSet CompileColorSet(ColorSet applyColorSet, bool3 channelToggles, ColorSet originalColors)
            {
                ColorSet colorSet = originalColors;
                if (channelToggles[0])
                {
                    colorSet.m_Channel0 = applyColorSet.m_Channel0;
                }

                if (channelToggles[1])
                {
                    colorSet.m_Channel1 = applyColorSet.m_Channel1;
                }

                if (channelToggles[2])
                {
                    colorSet.m_Channel2 = applyColorSet.m_Channel2;
                }

                return colorSet;
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private readonly bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
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
            public bool3 m_ChannelToggles;
            public EntityCommandBuffer buffer;
            public float m_Radius;
            public float3 m_Position;
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;
            [ReadOnly]
            public BufferLookup<CustomMeshColor> m_CustomMeshColorLookup;
            [ReadOnly]
            public BufferLookup<MeshColorRecord> m_MeshColorRecordLookup;
            public bool m_ResettingColorsToRecord;

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

                        ColorSet currentColorSet = m_ApplyColorSet;
                        bool completeReset = false;

                        if (!m_MeshColorLookup.TryGetBuffer(currentEntity, out DynamicBuffer<MeshColor> originalMeshColors))
                        {
                            continue;
                        }

                        if (m_ResettingColorsToRecord &&
                           !m_MeshColorRecordLookup.HasBuffer(currentEntity))
                        {
                            buffer.RemoveComponent<CustomMeshColor>(currentEntity);
                            buffer.AddComponent<BatchesUpdated>(currentEntity);
                            completeReset = true;
                        }
                        else if (m_ResettingColorsToRecord &&
                                 m_MeshColorRecordLookup.TryGetBuffer(currentEntity, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer))
                        {
                            bool matchesVanillaColorSet = true;
                            for (int j = 0; j < 3; j++)
                            {
                                if (m_ChannelToggles[j])
                                {
                                    currentColorSet[j] = meshColorRecordBuffer[0].m_ColorSet[j];
                                }

                                if (currentColorSet[j] != meshColorRecordBuffer[0].m_ColorSet[j])
                                {
                                    matchesVanillaColorSet = false;
                                }
                            }

                            if (matchesVanillaColorSet)
                            {
                                buffer.RemoveComponent<CustomMeshColor>(currentEntity);
                                buffer.RemoveComponent<MeshColorRecord>(currentEntity);
                                buffer.AddComponent<BatchesUpdated>(currentEntity);
                                completeReset = true;
                            }
                        }
                        else if (m_ResettingColorsToRecord)
                        {
                            continue;
                        }

                        if (!completeReset)
                        {
                            if (!m_MeshColorRecordLookup.HasBuffer(currentEntity))
                            {
                                DynamicBuffer<MeshColorRecord> meshColorRecords = buffer.AddBuffer<MeshColorRecord>(currentEntity);
                                for (int j = 0; j < originalMeshColors.Length; j++)
                                {
                                    meshColorRecords.Add(new MeshColorRecord() { m_ColorSet = originalMeshColors[j].m_ColorSet });
                                }
                            }

                            DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(currentEntity);
                            DynamicBuffer<CustomMeshColor> customMeshColors = buffer.AddBuffer<CustomMeshColor>(currentEntity);
                            for (int j = 0; j < originalMeshColors.Length; j++)
                            {
                                meshColorBuffer.Add(new MeshColor() { m_ColorSet = CompileColorSet(currentColorSet, m_ChannelToggles, originalMeshColors[j].m_ColorSet) });
                                customMeshColors.Add(new CustomMeshColor() { m_ColorSet = CompileColorSet(currentColorSet, m_ChannelToggles, originalMeshColors[j].m_ColorSet) });
                            }
                        }

                        buffer.AddComponent<BatchesUpdated>(currentEntity);

                        // Add batches updated to subobjects.
                        if (m_SubObjectLookup.TryGetBuffer(currentEntity, out DynamicBuffer<Game.Objects.SubObject> subObjectBuffer))
                        {
                            foreach (Game.Objects.SubObject subObject in subObjectBuffer)
                            {
                                ProcessSubObject(subObject);

                                if (!m_SubObjectLookup.TryGetBuffer(subObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer))
                                {
                                    continue;
                                }

                                foreach (Game.Objects.SubObject deepSubObject in deepSubObjectBuffer)
                                {
                                    ProcessSubObject(deepSubObject);

                                    if (!m_SubObjectLookup.TryGetBuffer(deepSubObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer2))
                                    {
                                        continue;
                                    }

                                    foreach (Game.Objects.SubObject deepSubObject2 in deepSubObjectBuffer2)
                                    {
                                        ProcessSubObject(deepSubObject2);

                                        if (!m_SubObjectLookup.TryGetBuffer(deepSubObject2.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer3))
                                        {
                                            continue;
                                        }

                                        foreach (Game.Objects.SubObject deepSubObject3 in deepSubObjectBuffer3)
                                        {
                                            ProcessSubObject(deepSubObject3);

                                            if (!m_SubObjectLookup.TryGetBuffer(deepSubObject3.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer4))
                                            {
                                                continue;
                                            }

                                            foreach (Game.Objects.SubObject deepSubObject4 in deepSubObjectBuffer4)
                                            {
                                                ProcessSubObject(deepSubObject4);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private void ProcessSubObject(Game.Objects.SubObject subObject)
            {
                if (m_MeshColorLookup.HasBuffer(subObject.m_SubObject) && !m_CustomMeshColorLookup.HasBuffer(subObject.m_SubObject))
                {
                    buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                }
            }

            private ColorSet CompileColorSet(ColorSet applyColorSet, bool3 channelToggles, ColorSet originalColors)
            {
                ColorSet colorSet = originalColors;
                if (channelToggles[0])
                {
                    colorSet.m_Channel0 = applyColorSet.m_Channel0;
                }

                if (channelToggles[1])
                {
                    colorSet.m_Channel1 = applyColorSet.m_Channel1;
                }

                if (channelToggles[2])
                {
                    colorSet.m_Channel2 = applyColorSet.m_Channel2;
                }

                return colorSet;
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private readonly bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
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
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;
            [ReadOnly]
            public BufferLookup<Game.Net.SubLane> m_SubLaneLookup;
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;
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

                        // Add batches updated to subobjects.
                        if (m_SubObjectLookup.TryGetBuffer(currentEntity, out DynamicBuffer<Game.Objects.SubObject> subObjectBuffer))
                        {
                            foreach (Game.Objects.SubObject subObject in subObjectBuffer)
                            {
                                ProcessSubObject(subObject);

                                if (!m_SubObjectLookup.TryGetBuffer(subObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer))
                                {
                                    continue;
                                }

                                foreach (Game.Objects.SubObject deepSubObject in deepSubObjectBuffer)
                                {
                                    ProcessSubObject(deepSubObject);

                                    if (!m_SubObjectLookup.TryGetBuffer(deepSubObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer2))
                                    {
                                        continue;
                                    }

                                    foreach (Game.Objects.SubObject deepSubObject2 in deepSubObjectBuffer2)
                                    {
                                        ProcessSubObject(deepSubObject2);

                                        if (!m_SubObjectLookup.TryGetBuffer(deepSubObject2.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer3))
                                        {
                                            continue;
                                        }

                                        foreach (Game.Objects.SubObject deepSubObject3 in deepSubObjectBuffer3)
                                        {
                                            ProcessSubObject(deepSubObject3);

                                            if (!m_SubObjectLookup.TryGetBuffer(deepSubObject3.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer4))
                                            {
                                                continue;
                                            }

                                            foreach (Game.Objects.SubObject deepSubObject4 in deepSubObjectBuffer4)
                                            {
                                                ProcessSubObject(deepSubObject4);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Add batches updated to sublanes.
                        if (m_SubLaneLookup.TryGetBuffer(currentEntity, out DynamicBuffer<Game.Net.SubLane> subLaneBuffer))
                        {
                            foreach (Game.Net.SubLane subLane in subLaneBuffer)
                            {
                                if (m_MeshColorLookup.HasBuffer(subLane.m_SubLane))
                                {
                                    buffer.AddComponent<BatchesUpdated>(subLane.m_SubLane);
                                }
                            }
                        }
                    }
                }
            }


            private void ProcessSubObject(Game.Objects.SubObject subObject)
            {
                if (m_MeshColorLookup.HasBuffer(subObject.m_SubObject))
                {
                    buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private readonly bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
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
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;

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


                        // Add batches updated to subobjects.
                        if (m_SubObjectLookup.TryGetBuffer(currentEntity, out DynamicBuffer<Game.Objects.SubObject> subObjectBuffer))
                        {
                            foreach (Game.Objects.SubObject subObject in subObjectBuffer)
                            {
                                ProcessSubObject(subObject);

                                if (!m_SubObjectLookup.TryGetBuffer(subObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer))
                                {
                                    continue;
                                }

                                foreach (Game.Objects.SubObject deepSubObject in deepSubObjectBuffer)
                                {
                                    ProcessSubObject(deepSubObject);

                                    if (!m_SubObjectLookup.TryGetBuffer(deepSubObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer2))
                                    {
                                        continue;
                                    }

                                    foreach (Game.Objects.SubObject deepSubObject2 in deepSubObjectBuffer2)
                                    {
                                        ProcessSubObject(deepSubObject2);

                                        if (!m_SubObjectLookup.TryGetBuffer(deepSubObject2.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer3))
                                        {
                                            continue;
                                        }

                                        foreach (Game.Objects.SubObject deepSubObject3 in deepSubObjectBuffer3)
                                        {
                                            ProcessSubObject(deepSubObject3);

                                            if (!m_SubObjectLookup.TryGetBuffer(deepSubObject3.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer4))
                                            {
                                                continue;
                                            }

                                            foreach (Game.Objects.SubObject deepSubObject4 in deepSubObjectBuffer4)
                                            {
                                                ProcessSubObject(deepSubObject4);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private void ProcessSubObject(Game.Objects.SubObject subObject)
            {
                if (m_MeshColorLookup.HasBuffer(subObject.m_SubObject))
                {
                    buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private readonly bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
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

        private struct CreateDefinitionJob: IJob
        {
            [ReadOnly]
            public Entity m_InstanceEntity;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformData;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            public EntityCommandBuffer m_CommandBuffer;

            public void Execute()
            {
                Entity e = m_CommandBuffer.CreateEntity();
                CreationDefinition creationDefinition = new ()
                {
                    m_Original = m_InstanceEntity,
                    m_Flags = CreationFlags.Select,
                };
                if (m_PrefabRefLookup.HasComponent(m_InstanceEntity))
                {
                    creationDefinition.m_Prefab = m_PrefabRefLookup[m_InstanceEntity];
                }

                m_CommandBuffer.AddComponent(e, default(Updated));
                if (m_TransformData.HasComponent(m_InstanceEntity))
                {
                    Game.Objects.Transform transform = m_TransformData[m_InstanceEntity];
                    ObjectDefinition objectDefinition = new ()
                    {
                        m_Position = transform.m_Position,
                        m_Rotation = transform.m_Rotation,
                        m_ParentMesh = -1,
                        m_Probability = 100,
                        m_PrefabSubIndex = -1,
                    };
                    m_CommandBuffer.AddComponent(e, objectDefinition);
                }

                m_CommandBuffer.AddComponent(e, creationDefinition);
            }
        }

    }
}
