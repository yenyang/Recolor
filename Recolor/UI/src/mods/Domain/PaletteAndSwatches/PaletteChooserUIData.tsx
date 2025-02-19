import { PaletteSubcategoryUIData } from "./PaletteSubCategoryUIData";

export interface PaletteChooserUIData {
    DropdownItems : PaletteSubcategoryUIData[][],
    SelectedIndexes : number[],
    SelectedSubcategories: number[],
}