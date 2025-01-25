// <copyright file="AssignedRouteVehicleCustomColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Vehicles
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Routes;
    using Game.Vehicles;
    using Recolor.Domain;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Adds custom mesh color components to created Route vehicles who's owners have Route vehicle color buffers.
    /// </summary>
    public partial class AssignedRouteVehicleCustomColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_AssignedRouteVehicleQuery;
        private EndFrameBarrier m_Barrier;
        private ColorPainterToolSystem m_ColorPainterToolSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(AssignedRouteVehicleCustomColorSystem)}.{nameof(OnCreate)}");

            m_ColorPainterToolSystem = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            m_AssignedRouteVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<MeshColor>(),
                    ComponentType.ReadOnly<Game.Routes.CurrentRoute>(),
                    ComponentType.ReadOnly<Game.Vehicles.Vehicle>(),
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Created>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                    ComponentType.ReadOnly<Domain.CustomMeshColor>(),
                },
            });

            RequireForUpdate(m_AssignedRouteVehicleQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_AssignedRouteVehicleQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out Game.Routes.CurrentRoute currentRoute) ||
                     currentRoute.m_Route == Entity.Null ||
                    !EntityManager.TryGetBuffer(currentRoute.m_Route, isReadOnly: true, out DynamicBuffer<RouteVehicleColor> routeVehicleBuffer) ||
                     routeVehicleBuffer.Length == 0 ||
                    !EntityManager.HasBuffer<RouteVehicle>(currentRoute.m_Route))
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<LayoutElement> layoutElementBuffer))
                {
                    m_ColorPainterToolSystem.ChangeInstanceColorSet(new RecolorSet(routeVehicleBuffer[0].m_ColorSet), ref buffer, entity);
                }
                else
                {
                    foreach (LayoutElement layoutElement in layoutElementBuffer)
                    {
                        m_ColorPainterToolSystem.ChangeInstanceColorSet(new RecolorSet(routeVehicleBuffer[0].m_ColorSet), ref buffer, layoutElement.m_Vehicle);
                    }
                }
            }
        }
    }
}
