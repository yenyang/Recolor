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
    using Unity.Entities.UniversalDelegates;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;
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

        private struct CreateDefinitionJob : IJob
        {
            [ReadOnly]
            public Entity m_InstanceEntity;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformData;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            public EntityCommandBuffer buffer;
            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerLookup;

            public void Execute()
            {
                Entity e = buffer.CreateEntity();
                CreationDefinition creationDefinition = new ()
                {
                    m_Original = m_InstanceEntity,
                    m_Flags = CreationFlags.Select,
                };
                if (m_PrefabRefLookup.HasComponent(m_InstanceEntity))
                {
                    creationDefinition.m_Prefab = m_PrefabRefLookup[m_InstanceEntity];
                }

                if (m_OwnerLookup.TryGetComponent(m_InstanceEntity, out Owner owner))
                {
                    creationDefinition.m_Owner = owner.m_Owner;
                }


                buffer.AddComponent(e, default(Updated));
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

                    if (owner.m_Owner != Entity.Null &&
                        m_TransformData.TryGetComponent(m_InstanceEntity, out Game.Objects.Transform subobjectTransform) &&
                        m_TransformData.TryGetComponent(owner.m_Owner, out Game.Objects.Transform ownerTransform))
                    {
                        Game.Objects.Transform inverseParentTransform = ObjectUtils.InverseTransform(ownerTransform);
                        Game.Objects.Transform localTransform = ObjectUtils.WorldToLocal(inverseParentTransform, subobjectTransform);

                        objectDefinition.m_LocalRotation = localTransform.m_Rotation;
                        objectDefinition.m_LocalPosition = localTransform.m_Position;
                    }

                    buffer.AddComponent(e, objectDefinition);
                }

                buffer.AddComponent(e, creationDefinition);
            }
        }

        private struct CreateDefinitionsWithRadiusOfTransform : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public EntityCommandBuffer buffer;
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;
            public float m_Radius;
            public float3 m_Position;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            public NativeList<Entity> m_SelectedEntities;
            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (CheckForWithinRadius(m_Position, transformNativeArray[i].m_Position, m_Radius) &&
                       !m_SelectedEntities.Contains(entityNativeArray[i]))
                    {
                        Entity e = buffer.CreateEntity();
                        CreationDefinition creationDefinition = new()
                        {
                            m_Original = entityNativeArray[i],
                            m_Flags = CreationFlags.Select,
                        };
                        if (m_PrefabRefLookup.HasComponent(entityNativeArray[i]))
                        {
                            creationDefinition.m_Prefab = m_PrefabRefLookup[entityNativeArray[i]];
                        }

                        if (m_OwnerLookup.TryGetComponent(entityNativeArray[i], out Owner owner))
                        {
                            creationDefinition.m_Owner = owner.m_Owner;
                        }

                        buffer.AddComponent(e, default(Updated));
                        ObjectDefinition objectDefinition = new()
                        {
                            m_Position = transformNativeArray[i].m_Position,
                            m_Rotation = transformNativeArray[i].m_Rotation,
                            m_ParentMesh = -1,
                            m_Probability = 100,
                            m_PrefabSubIndex = -1,
                        };

                        if (owner.m_Owner != Entity.Null &&
                            m_TransformLookup.TryGetComponent(owner.m_Owner, out Game.Objects.Transform ownerTransform))
                        {
                            Game.Objects.Transform inverseParentTransform = ObjectUtils.InverseTransform(ownerTransform);
                            Game.Objects.Transform localTransform = ObjectUtils.WorldToLocal(inverseParentTransform, transformNativeArray[i]);

                            objectDefinition.m_LocalRotation = localTransform.m_Rotation;
                            objectDefinition.m_LocalPosition = localTransform.m_Position;
                        }

                        buffer.AddComponent(e, objectDefinition);

                        buffer.AddComponent(e, creationDefinition);
                        m_SelectedEntities.Add(entityNativeArray[i]);
                    }
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

        private struct CreateDefinitionsWithRadiusOfInterpolatedTransform : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;
            public EntityCommandBuffer buffer;
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;
            public float m_Radius;
            public float3 m_Position;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            public NativeList<Entity> m_SelectedEntities;
            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<InterpolatedTransform> interpolatedTransformNativeArray = chunk.GetNativeArray(ref m_InterpolatedTransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (CheckForWithinRadius(m_Position, interpolatedTransformNativeArray[i].m_Position, m_Radius) &&
                       !m_SelectedEntities.Contains(entityNativeArray[i]))
                    {
                        Entity e = buffer.CreateEntity();
                        CreationDefinition creationDefinition = new()
                        {
                            m_Original = entityNativeArray[i],
                            m_Flags = CreationFlags.Select,
                        };
                        if (m_PrefabRefLookup.HasComponent(entityNativeArray[i]))
                        {
                            creationDefinition.m_Prefab = m_PrefabRefLookup[entityNativeArray[i]];
                        }

                        if (m_OwnerLookup.TryGetComponent(entityNativeArray[i], out Owner owner))
                        {
                            creationDefinition.m_Owner = owner.m_Owner;
                        }

                        buffer.AddComponent(e, default(Updated));
                        ObjectDefinition objectDefinition = new()
                        {
                            m_Position = interpolatedTransformNativeArray[i].m_Position,
                            m_Rotation = interpolatedTransformNativeArray[i].m_Rotation,
                            m_ParentMesh = -1,
                            m_Probability = 100,
                            m_PrefabSubIndex = -1,
                        };

                        buffer.AddComponent(e, objectDefinition);

                        buffer.AddComponent(e, creationDefinition);
                        m_SelectedEntities.Add(entityNativeArray[i]);
                    }
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

    }
}
