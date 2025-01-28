// <copyright file="SubMeshData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain
{
    using static Recolor.Systems.SelectedInfoPanel.SIPColorFieldsSystem;

    /// <summary>
    /// Handles Data transfer for SubMesh related information.
    /// </summary>
    public class SubMeshData
    {
        /// <summary>
        /// Index for selected submesh.
        /// </summary>
        public int SubMeshIndex;

        /// <summary>
        /// Length of SubMesh Buffer.
        /// </summary>
        public int SubMeshLength;

        /// <summary>
        /// Name of SubMesh.
        /// </summary>
        public string SubMeshName;

        /// <summary>
        /// Selected SubMesh Scope.
        /// </summary>
        public SubMeshScopes SubMeshScope;

        /// <summary>
        /// Button state for single submesh.
        /// </summary>
        public ButtonState SingleSubMesh;

        /// <summary>
        /// Button State for Matching SubMesh.
        /// </summary>
        public ButtonState MatchingSubMeshes;

        /// <summary>
        ///  Button State for All SubMeshes.
        /// </summary>
        public ButtonState AllSubMeshes;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubMeshData"/> class.
        /// </summary>
        /// <param name="subMeshIndex">Index for the SubMesh.</param>
        /// <param name="subMeshLength">Length of SubMesh Buffer.</param>
        /// <param name="subMeshName">Name of the SubMesh.</param>
        /// <param name="subMeshScope">SubMesh Scope.</param>
        /// <param name="singleSubMesh">Button State for Single SubMesh.</param>
        /// <param name="matchingSubMeshes">Button State for Matching SubMesh.</param>
        /// <param name="allSubMeshes">Button State for All SubMeshes.</param>
        public SubMeshData(int subMeshIndex, int subMeshLength, string subMeshName, SubMeshScopes subMeshScope, ButtonState singleSubMesh, ButtonState matchingSubMeshes, ButtonState allSubMeshes)
        {
            SubMeshIndex = subMeshIndex;
            SubMeshLength = subMeshLength;
            SubMeshName = subMeshName;
            SubMeshScope = subMeshScope;
            SingleSubMesh = singleSubMesh;
            MatchingSubMeshes = matchingSubMeshes;
            AllSubMeshes = allSubMeshes;
        }

        /// <summary>
        /// This is a scope for objects with multiple submeshes.
        /// </summary>
        public enum SubMeshScopes
        {
            /// <summary>
            /// Single instance entity.
            /// </summary>
            SingleInstance = 0,

            /// <summary>
            /// All matching meshes.
            /// </summary>
            Matching = 1,

            /// <summary>
            /// All submeshes.
            /// </summary>
            All = 2,
        }


    }
}
