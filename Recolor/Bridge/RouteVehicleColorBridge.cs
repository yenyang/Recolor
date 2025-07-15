// <copyright file="RouteVehicleColorBridge.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Bridge
{
    using Colossal.Entities;
    using Game.Rendering;
    using Game.Vehicles;
    using Recolor.Domain;
    using Recolor.Systems.Tools;
    using Unity.Entities;

    /// <summary>
    /// A bridge class for other mods to assign route vehicle colors.
    /// </summary>
    public static class RouteVehicleColorBridge
    {
        /// <summary>
        /// Attempts to assign a route vehicle color.
        /// </summary>
        /// <param name="routeEntity">Instance Entity containing Route Vehicles.</param>
        /// <param name="colorSet">Set of 3 colors packages in a vanilla ColorSet.</param>
        /// <param name="buffer">An entity commnad buffer in the appropriate phase for your system.</param>
        /// <param name="controllingMod">int to be converted to enum for <see cref="Recolor.Domain.RouteVehicleColor.ControlledBy"/> 0 is Recolor | 1 is XTM.</param>
        /// <returns>True if successful, False if couldnot assign route color.</returns>
        public static bool TryAssignRouteVehicleColor(Entity routeEntity, ColorSet colorSet, ref EntityCommandBuffer buffer, int controllingMod)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            ColorPainterToolSystem colorPainterToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ColorPainterToolSystem>();

            if (routeEntity == Entity.Null ||
              !entityManager.TryGetBuffer(routeEntity, isReadOnly: true, out DynamicBuffer<Game.Routes.RouteVehicle> routeVehicleBuffer) ||
               routeVehicleBuffer.Length == 0 ||
              !entityManager.TryGetBuffer(routeVehicleBuffer[0].m_Vehicle, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) ||
               meshColorBuffer.Length == 0)
            {
                return false;
            }

            DynamicBuffer<RouteVehicleColor> newBuffer = buffer.AddBuffer<RouteVehicleColor>(routeEntity);
            for (int i = 0; i < meshColorBuffer.Length; i++)
            {
                newBuffer.Add(new RouteVehicleColor(colorSet, meshColorBuffer[i].m_ColorSet));
            }

            for (int i = 0; i < routeVehicleBuffer.Length; i++)
            {
                if (entityManager.TryGetBuffer(routeVehicleBuffer[i].m_Vehicle, isReadOnly: true, out DynamicBuffer<LayoutElement> layoutElementBuffer) &&
                    layoutElementBuffer.Length > 0)
                {
                    for (int j = 0; j < layoutElementBuffer.Length; j++)
                    {
                       colorPainterToolSystem.ChangeInstanceColorSet(new RecolorSet(colorSet), ref buffer, layoutElementBuffer[j].m_Vehicle);
                    }
                }
                else
                {
                    colorPainterToolSystem.ChangeInstanceColorSet(new RecolorSet(colorSet), ref buffer, routeVehicleBuffer[i].m_Vehicle);
                }
            }

            return true;
        }

    }
}
