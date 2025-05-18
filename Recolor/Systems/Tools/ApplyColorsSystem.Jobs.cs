namespace Recolor.Systems.Tools
{
    using Game;
    using Game.Common;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Jobs for Apply Color systems.
    /// </summary>
    public partial class ApplyColorsSystem : GameSystemBase
    {
#if BURST
        [BurstCompile]
#endif
        private struct ChangeMeshColorJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Temp> m_TempType;
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
            public EntityCommandBuffer buffer;

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
                NativeArray<Temp> tempNativeArray = chunk.GetNativeArray(ref m_TempType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity tempEntity = entityNativeArray[i];
                    Entity originalEntity = tempNativeArray[i].m_Original;

                    if (!m_MeshColorLookup.TryGetBuffer(originalEntity, out DynamicBuffer<MeshColor> originalMeshColors) ||
                        !m_MeshColorLookup.TryGetBuffer(tempEntity, out DynamicBuffer<MeshColor> tempMeshColors) ||
                        tempMeshColors.Length == 0 ||
                        originalMeshColors.Length == 0)
                    {
                        continue;
                    }

                    ColorSet defaultColorSet = tempMeshColors[0].m_ColorSet;

                    if (!m_CustomMeshColorLookup.HasBuffer(tempEntity))
                    {
                        buffer.RemoveComponent<CustomMeshColor>(originalEntity);
                        buffer.RemoveComponent<MeshColorRecord>(originalEntity);
                        buffer.AddComponent<BatchesUpdated>(originalEntity);
                    }
                    else
                    {
                        if (!m_MeshColorRecordLookup.HasBuffer(originalEntity))
                        {
                            DynamicBuffer<MeshColorRecord> meshColorRecords = buffer.AddBuffer<MeshColorRecord>(originalEntity);
                            for (int j = 0; j < originalMeshColors.Length; j++)
                            {
                                meshColorRecords.Add(new MeshColorRecord() { m_ColorSet = originalMeshColors[j].m_ColorSet });
                            }
                        }

                        DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(originalEntity);
                        DynamicBuffer<CustomMeshColor> customMeshColors = buffer.AddBuffer<CustomMeshColor>(originalEntity);
                        for (int j = 0; j < originalMeshColors.Length; j++)
                        {
                            if (tempMeshColors.Length > j)
                            {
                                meshColorBuffer.Add(new MeshColor() { m_ColorSet = tempMeshColors[j].m_ColorSet });
                                customMeshColors.Add(new CustomMeshColor() { m_ColorSet = tempMeshColors[j].m_ColorSet });
                            }
                            else
                            {
                                meshColorBuffer.Add(new MeshColor() { m_ColorSet = defaultColorSet });
                                customMeshColors.Add(new CustomMeshColor { m_ColorSet = defaultColorSet });
                            }
                        }

                        buffer.AddComponent<BatchesUpdated>(originalEntity);
                    }

                    // Add batches updated to subobjects.
                    if (m_SubObjectLookup.TryGetBuffer(originalEntity, out DynamicBuffer<Game.Objects.SubObject> subObjectBuffer))
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
                    if (m_SubLaneLookup.TryGetBuffer(originalEntity, out DynamicBuffer<Game.Net.SubLane> subLaneBuffer))
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

            private void ProcessSubObject(Game.Objects.SubObject subObject)
            {
                if (m_MeshColorLookup.HasBuffer(subObject.m_SubObject) && !m_CustomMeshColorLookup.HasBuffer(subObject.m_SubObject))
                {
                    buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                }
            }
        }

    }
}
