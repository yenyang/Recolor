 /// <summary>
 /// This is a scope for objects with multiple submeshes.

import { ButtonState } from "./ButtonState";

 /// </summary>
 export enum SubMeshScopes
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

export interface SubMeshData {
    SubMeshIndex : number;
    SubMeshLength : number;
    SubMeshName : string;
    SubMeshScope : SubMeshScopes;
    SingleSubMesh : ButtonState;
    MatchingSubMeshes : ButtonState;
    AllSubMeshes : ButtonState;
}