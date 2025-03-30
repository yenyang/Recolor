import { useLocalization } from "cs2/l10n";
import { MenuType } from "mods/Domain/MenuType";
import { PaletteCategory } from "mods/Domain/PaletteAndSwatches/PaletteCategoryType";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import locale from "../lang/en-US.json";
import { trigger } from "cs2/api";
import mod from "../../../mod.json";


const uilStandard =                         "coui://uil/Standard/";
const buildingSrc =                     uilStandard + "House.svg";
const vehiclesSrc =                     uilStandard + "GenericVehicle.svg";
const propsSrc =                        uilStandard + "BenchAndLampProps.svg";
const allSrc =                          uilStandard + "StarAll.svg";


function handleCategoryClick(category : PaletteCategory, menu: MenuType) {
    trigger(mod.id, "ToggleCategory", category as number, menu as number);
}

export const CategorySection = (props: {category: PaletteCategory, menu : MenuType}) => {
    
    const { translate } = useLocalization();
    
    return (
        <>
            <VanillaComponentResolver.instance.Section title={"Category"}>
                <VanillaComponentResolver.instance.ToolButton src={allSrc}          tooltip = {"All Categories"}                                                                                    selected={props.category == PaletteCategory.Any}                                                                                      onSelect={() => {handleCategoryClick(PaletteCategory.Any, props.menu)} }                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                <VanillaComponentResolver.instance.ToolButton src={buildingSrc}     tooltip = {translate("Recolor.TOOLTIP_TITLE[BuildingFilter]", locale["Recolor.TOOLTIP_TITLE[BuildingFilter]"])} selected={props.category == PaletteCategory.Any || (props.category & PaletteCategory.Buildings) == PaletteCategory.Buildings} onSelect={() => {handleCategoryClick(PaletteCategory.Buildings, props.menu)} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                <VanillaComponentResolver.instance.ToolButton src={vehiclesSrc}     tooltip = {translate("Recolor.TOOLTIP_TITLE[VehicleFilter]", locale["Recolor.TOOLTIP_TITLE[VehicleFilter]"])}   selected={props.category == PaletteCategory.Any || (props.category & PaletteCategory.Vehicles) == PaletteCategory.Vehicles}   onSelect={() => {handleCategoryClick(PaletteCategory.Vehicles, props.menu)} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                <VanillaComponentResolver.instance.ToolButton src={propsSrc}        tooltip = {translate("Recolor.TOOLTIP_TITLE[PropFilter]", locale["Recolor.TOOLTIP_TITLE[PropFilter]"])}         selected={props.category == PaletteCategory.Any || (props.category & PaletteCategory.Props) == PaletteCategory.Props}         onSelect={() => {handleCategoryClick(PaletteCategory.Props, props.menu)} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />  
            </VanillaComponentResolver.instance.Section>
        </>
    );
}