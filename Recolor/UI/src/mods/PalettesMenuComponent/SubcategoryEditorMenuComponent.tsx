import { Button, Panel } from "cs2/ui"
import { InfoSection, roundButtonHighlightStyle } from "mods/RecolorMainPanel/RecolorMainPanel";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import panelStyles from "./PaletteMenuStyles.module.scss";
import { StringInputField, StringInputFieldStyle } from "mods/SIPColorComponent/SIPColorComponent";
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

function handleClick(event: string) {
    trigger(mod.id, event);
}

function handleCategoryClick(category : PaletteCategory) {
    trigger(mod.id, "ToggleCategory", category as number);
}

export const SubcategoryEditorMenuComponent = () => {
    const UniqueNames = useValue(UniqueNames$);
    const PaletteCategories = useValue(PaletteCategories$);
    const ShowSubcategoryEditorMenu = useValue(ShowSubcategoryEditorMenu$);

    const { translate } = useLocalization();

    return (
        <>
            { ShowSubcategoryEditorMenu && (
                <Panel 
                    className={panelStyles.subcategoryPanel}
                    header={(
                        <HeaderSection title="Subcategory Editor Menu" icon={subcategoryIcon} onCloseEventName={"TogglePaletteEditorMenu"}></HeaderSection>
                    )}
                    footer={(
                        <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                <VanillaComponentResolver.instance.Section title={"Subcategory"}>
                                    <span className={panelStyles.smallSpacer}></span>
                                    <VanillaComponentResolver.instance.ToolButton src={saveToDiskSrc}          tooltip = {"Save Subcategory"}   onSelect={() => {handleClick("TrySaveSubcategory")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                    <span className={panelStyles.wideSpacer}></span>
                                    <VanillaComponentResolver.instance.ToolButton src={trashSrc}     tooltip = {"Delete Subcategory"}   onSelect={() => {handleClick("DeleteSubcategory")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                </VanillaComponentResolver.instance.Section>
                        </InfoSection>
                    )}
                >
                    <UniqueNameSectionComponent uniqueName={UniqueNames[MenuType.Subcategory]} uniqueNameType={MenuType.Subcategory}></UniqueNameSectionComponent>
                    <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                        <CategorySection category={PaletteCategories[MenuType.Subcategory]} menu={MenuType.Subcategory}></CategorySection>
                    </InfoSection>
                    <LocaleSection></LocaleSection>
                </Panel>
            )}
        </>
    );
}