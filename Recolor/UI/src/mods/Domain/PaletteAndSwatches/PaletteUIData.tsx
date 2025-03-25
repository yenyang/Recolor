import { Entity } from "cs2/utils";
import { SwatchUIData } from "./SwatchUIData";

export interface PaletteUIData {
    Swatches : SwatchUIData[],
    PrefabEntity : Entity,
}