namespace Recolor.Systems.Tools
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Rendering;
    using Game.Tools;
    using Game.Vehicles;
    using Recolor.Domain;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// A system to apply colors from color painter tool.
    /// </summary>
    public partial class ApplyColorsSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_TempCustomMeshColorQuery;
        private ToolSystem m_ToolSystem;
        private ColorPainterToolSystem m_ColorPainterToolSystem;
        private ColorPainterUISystem m_UISystem;
        private ToolOutputBarrier m_Barrier;

        /// <inheritdoc/>
        protected override void OnCreate ()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_ColorPainterToolSystem = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<ColorPainterUISystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_TempCustomMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAll<Temp, MeshColor>()
                .WithNone<Deleted, Game.Common.Overridden>()
                .Build();

            RequireForUpdate(m_TempCustomMeshColorQuery);
            m_Log.Info($"{nameof(ApplyColorsSystem)}.{nameof(OnCreate)}");
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_Log.Debug($"{nameof(ApplyColorsSystem)}.{nameof(OnUpdate)} m_TempCustomMeshColorQuery.CalculateEntityCount() {m_TempCustomMeshColorQuery.CalculateEntityCount()}");
            ChangeMeshColorJob changeMeshColorJob = new ChangeMeshColorJob()
            {
                m_CustomMeshColorLookup = SystemAPI.GetBufferLookup<CustomMeshColor>(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(),
                m_MeshColorRecordLookup = SystemAPI.GetBufferLookup<MeshColorRecord>(),
                m_SubLaneLookup = SystemAPI.GetBufferLookup<Game.Net.SubLane>(),
                m_SubObjectLookup = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(),
                m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
                buffer = m_Barrier.CreateCommandBuffer(),
            };

            JobHandle jobHandle = changeMeshColorJob.Schedule(m_TempCustomMeshColorQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (tool == m_ColorPainterToolSystem)
            {
                Enabled = true;
                return;
            }

            Enabled = false;
        }
    }
}
