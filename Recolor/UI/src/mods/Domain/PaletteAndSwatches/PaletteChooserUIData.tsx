import { Entity } from "cs2/utils";
import { PaletteSubcategoryUIData } from "./PaletteSubCategoryUIData";

export interface PaletteChooserUIData {
    DropdownItems : PaletteSubcategoryUIData[][],
    SelectedPaletteEntities : Entity[],
}