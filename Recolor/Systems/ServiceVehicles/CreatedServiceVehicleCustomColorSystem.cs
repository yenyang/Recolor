// <copyright file="CreatedServiceVehicleCustomColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.ServiceVehicles
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Vehicles;
    using Recolor.Domain;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Adds custom mesh color components to created service vehicles who's owners have service vehicle color buffers.
    /// </summary>
    public partial class CreatedServiceVehicleCustomColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_CreatedServiceVehicleQuery;
        private EndFrameBarrier m_Barrier;
        private ColorPainterToolSystem m_ColorPainterToolSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(CreatedServiceVehicleCustomColorSystem)}.{nameof(OnCreate)}");

            m_ColorPainterToolSystem = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            m_CreatedServiceVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<MeshColor>(),
                    ComponentType.ReadOnly<Game.Common.Owner>(),
                    ComponentType.ReadOnly<Created>(),
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<Game.Vehicles.Ambulance>(),
                    ComponentType.ReadOnly<Game.Vehicles.FireEngine>(),
                    ComponentType.ReadOnly<Game.Vehicles.PoliceCar>(),
                    ComponentType.ReadOnly<Game.Vehicles.GarbageTruck>(),
                    ComponentType.ReadOnly<Game.Vehicles.Hearse>(),
                    ComponentType.ReadOnly<Game.Vehicles.MaintenanceVehicle>(),
                    ComponentType.ReadOnly<Game.Vehicles.PostVan>(),
                    ComponentType.ReadOnly<Game.Vehicles.RoadMaintenanceVehicle>(),
                    ComponentType.ReadOnly<Game.Vehicles.Taxi>(),
                    ComponentType.ReadOnly<Game.Vehicles.ParkMaintenanceVehicle>(),
                    ComponentType.ReadOnly<Game.Vehicles.PostVan>(),
                    ComponentType.ReadOnly<Game.Vehicles.WorkVehicle>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });

            RequireForUpdate(m_CreatedServiceVehicleQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_CreatedServiceVehicleQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out Owner owner) ||
                    owner.m_Owner == Entity.Null ||
                   !EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<ServiceVehicleColor> serviceVehicleBuffer) ||
                    serviceVehicleBuffer.Length == 0 ||
                   !EntityManager.HasBuffer<OwnedVehicle>(owner.m_Owner))
                {
                    continue;
                }

                m_ColorPainterToolSystem.ChangeInstanceColorSet(new RecolorSet(serviceVehicleBuffer[0].m_ColorSet), ref buffer, entity);
            }
        }
    }
}
