import { Button, Panel, Tooltip } from "cs2/ui"
import { InfoSection, roundButtonHighlightStyle } from "mods/RecolorMainPanel/RecolorMainPanel";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import panelStyles from "./PaletteMenuStyles.module.scss";
import { ColorFieldTheme, StringInputField, StringInputFieldStyle } from "mods/SIPColorComponent/SIPColorComponent";
import { useState } from "react";
import mod from "../../../mod.json";
import { bindValue, trigger, useValue } from "cs2/api";
import { PaletteCategory } from "mods/Domain/PaletteAndSwatches/PaletteCategoryType";
import classNames from "classnames";
import styles from "../Domain/ColorFields.module.scss";
import { useLocalization } from "cs2/l10n";
import { UniqueNameSectionComponent } from "./UniqueNameSection";
import { MenuType } from "mods/Domain/MenuType";
import { HeaderSection } from "./HeaderSection";
import { CategorySection } from "./CategorySection";
import { LocaleSection } from "./LocaleSection";
import { getModule } from "cs2/modding";
import boxStyles from "../PaletteBoxComponent/PaletteBoxStyles.module.scss";
import locale from "../lang/en-US.json";
import { LocalizationUIData } from "mods/Domain/PaletteAndSwatches/LocalizationUIData";

const uilStandard =                         "coui://uil/Standard/";
const closeSrc =                        uilStandard +  "XClose.svg";
const buildingSrc =                     uilStandard + "House.svg";
const vehiclesSrc =                     uilStandard + "GenericVehicle.svg";
const propsSrc =                        uilStandard + "BenchAndLampProps.svg";
const allSrc =                          uilStandard + "StarAll.svg";
const plusSrc =                         uilStandard + "Plus.svg";
const saveToDiskSrc =                   uilStandard + "DiskSave.svg";
const trashSrc =                        uilStandard + "Trash.svg";
const subcategoryIcon =                 uilStandard+"GearOnText.svg";


const UniqueNames$ = bindValue<string[]>(mod.id, "UniqueNames");
const PaletteCategories$ = bindValue<PaletteCategory[]>(mod.id, "PaletteCategories");
const ShowSubcategoryEditorMenu$ = bindValue<boolean>(mod.id, "ShowSubcategoryEditorPanel");
const LocalizationUIDatas$ = bindValue<LocalizationUIData[][]>(mod.id, "LocalizationDatas");

function handleClick(event: string) {
    trigger(mod.id, event);
}

function handleCategoryClick(category : PaletteCategory) {
    trigger(mod.id, "ToggleCategory", category as number);
}


const basicDropDownTheme = getModule('game-ui/common/input/dropdown/dropdown.module.scss', 'classes');

export const SubcategoryEditorMenuComponent = () => {
    const UniqueNames = useValue(UniqueNames$);
    const PaletteCategories = useValue(PaletteCategories$);
    const ShowSubcategoryEditorMenu = useValue(ShowSubcategoryEditorMenu$);
    const LocalizationUIDatas = useValue(LocalizationUIDatas$);

    const { translate } = useLocalization();

    return (
        <>
            { ShowSubcategoryEditorMenu && (
                <Panel 
                    className={panelStyles.subcategoryPanel}
                    header={(
                        <HeaderSection title="Subcategory Editor Menu" icon={subcategoryIcon} onCloseEventName={"ShowSubcategoryEditorPanel"}></HeaderSection>
                    )}
                    footer={(
                        <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[Subcategory]" , locale["Recolor.SECTION_TITLE[Subcategory]"])}>
                                    <Tooltip tooltip={LocalizationUIDatas[MenuType.Subcategory][0].LocalizedDescription}>
                                        <div className={classNames(ColorFieldTheme.colorField, boxStyles.subcategory, boxStyles.centered, styles.dropdownText, basicDropDownTheme.dropdownItem)}>{LocalizationUIDatas[MenuType.Subcategory][0].LocalizedName}</div>
                                    </Tooltip>
                                    <span className={panelStyles.smallSpacer}></span>
                                    <VanillaComponentResolver.instance.ToolButton src={saveToDiskSrc}          tooltip = { translate("Recolor.TOOLTIP_DESCRIPTION[SaveSubcategory]" , locale["Recolor.TOOLTIP_DESCRIPTION[SaveSubcategory]"])}   onSelect={() => {handleClick("TrySaveSubcategory")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                    <span className={panelStyles.wideSpacer}></span>
                                    <VanillaComponentResolver.instance.ToolButton src={trashSrc}     tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[DeleteSubcategory]" , locale["Recolor.TOOLTIP_DESCRIPTION[DeleteSubcategory]"])}   onSelect={() => {handleClick("DeleteSubcategory")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                </VanillaComponentResolver.instance.Section>
                        </InfoSection>
                    )}
                >
                    <UniqueNameSectionComponent uniqueName={UniqueNames[MenuType.Subcategory]} uniqueNameType={MenuType.Subcategory}></UniqueNameSectionComponent>
                    <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                        <CategorySection category={PaletteCategories[MenuType.Subcategory]} menu={MenuType.Subcategory}></CategorySection>
                    </InfoSection>
                    <LocaleSection menu={MenuType.Subcategory} localizations={LocalizationUIDatas[MenuType.Subcategory]}></LocaleSection>
                </Panel>
            )}
        </>
    );
}