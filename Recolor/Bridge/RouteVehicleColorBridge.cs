// <copyright file="RouteVehicleColorBridge.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Bridge
{
    using Game.Rendering;
    using Game.Routes;
    using Game.Vehicles;
    using Recolor.Domain;
    using Recolor.Systems.Vehicles;
    using Unity.Entities;
    using static Recolor.Systems.SelectedInfoPanel.SIPColorFieldsSystem;

    /// <summary>
    /// A bridge class for other mods to assign route vehicle colors.
    /// </summary>
    public static class RouteVehicleColorBridge
    {



        /// <summary>
        /// Attempts to assign a route vehicle color.
        /// </summary>
        /// <returns>True if successful, False if couldnot assign route color.</returns>
        public static bool TryAssignRouteVehicleColor(Entity routeEntity, ColorSet colorSet)
        {
            if (
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
                    m_State = State.ColorChanged | State.UpdateButtonStates;
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
        }

    }
}
