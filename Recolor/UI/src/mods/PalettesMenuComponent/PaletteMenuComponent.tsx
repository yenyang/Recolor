
import { bindValue, trigger, useValue } from "cs2/api";
import panelStyles from "./PaletteMenuStyles.module.scss";
import styles from "../Domain/ColorFields.module.scss";
import {  game, selectedInfo, tool } from "cs2/bindings";
import {  Dropdown, DropdownItem, DropdownToggle, Panel, Portal } from "cs2/ui";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { useLocalization } from "cs2/l10n";
import { InfoSection } from "mods/RecolorMainPanel/RecolorMainPanel";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { useState } from "react";
import classNames from "classnames";
import { SwatchComponent } from "mods/SwatchComponent/SwatchComponent";
import { SwatchUIData } from "mods/Domain/PaletteAndSwatches/SwatchUIData";
import { PaletteCategory } from "mods/Domain/PaletteAndSwatches/PaletteCategoryType";
import { PaletteBoxComponent } from "mods/PaletteBoxComponent/PaletteBoxComponent";
import { getModule } from "cs2/modding";
import { PaletteFilterType } from "mods/Domain/PaletteAndSwatches/PaletteFilterType";
import { ButtonState } from "mods/Domain/ButtonState";
import { SubcategoryEditorMenuComponent } from "./SubcategoryEditorMenuComponent";
import { HeaderSection } from "./HeaderSection";
import { MenuType } from "mods/Domain/MenuType";
import { UniqueNameSectionComponent } from "./UniqueNameSection";
import { CategorySection } from "./CategorySection";
import { LocaleSection } from "./LocaleSection";

/*
import closeSrc from "images/uilStandard/XClose.svg";
import buildingSrc from "images/uilStandard/House.svg";
import vehiclesSrc from "images/uilStandard/GenericVehicle.svg";
import propsSrc from "images/uilStandard/BenchAndLampProps.svg";
import allSrc from "images/uilStandard/StarAll.svg";
import plusSrc from "images/uilStandard/Plus.svg";
import saveToDiskSrc from "images/uilStandard/DiskSave.svg";
*/

const uilStandard =                         "coui://uil/Standard/";
const uilColored =                           "coui://uil/Colored/";
const plusSrc =                         uilStandard + "Plus.svg";
const saveToDiskSrc =                   uilStandard + "DiskSave.svg";
const trashSrc =                        uilStandard + "Trash.svg";
const colorPaletteSrc =                  uilColored + "ColorPalette.svg";
const editSrc =                        uilStandard + "PencilPaper.svg";

const Swatches$ = bindValue<SwatchUIData[]>(mod.id, "Swatches");
const UniqueNames$ = bindValue<string[]>(mod.id, "UniqueNames");
const ShowPaletteEditorPanel$ = bindValue<boolean>(mod.id, "ShowPaletteEditorMenu");
const PaletteCategories$ = bindValue<PaletteCategory[]>(mod.id, "PaletteCategories");
const ShowPaletteChoices$ = bindValue<ButtonState>(mod.id,"ShowPaletteChoices");
const ResidentialBuildingSelected$ = bindValue<boolean>(mod.id, "ResidentialBuildingSelected");
const Subcategories$ = bindValue<string[]>(mod.id, "Subcategories");
const SelectedSubcategory$ = bindValue<string>(mod.id, "SelectedSubcategory");
const ShowSubcategoryEditorMenu$ = bindValue<boolean>(mod.id, "ShowSubcategoryEditorPanel");

function handleClick(event: string) {
    trigger(mod.id, event);
}


const dropDownThemes = getModule('game-ui/editor/themes/editor-dropdown.module.scss', 'classes');


export const PaletteMenuComponent = () => {
    const isPhotoMode = useValue(game.activeGamePanel$)?.__Type == game.GamePanelType.PhotoMode;
    const Swatches = useValue(Swatches$);
    const defaultTool = useValue(tool.activeTool$).id == tool.DEFAULT_TOOL;
    const activeSelection = useValue(selectedInfo.activeSelection$);
    const UniqueNames = useValue(UniqueNames$);
    const ShowPaletteEditorPanel = useValue(ShowPaletteEditorPanel$);
    const PaletteCategories = useValue(PaletteCategories$);    
    const ShowPaletteChoices = useValue(ShowPaletteChoices$);    
    const ResidentialBuildingSelected = useValue(ResidentialBuildingSelected$);
    const Subcategories = useValue(Subcategories$);
    const SelectedSubcategory = useValue(SelectedSubcategory$);
    const ShowSubcategoryEditorMenu = useValue(ShowSubcategoryEditorMenu$);
    
    const { translate } = useLocalization();

    
    let [currentFilter, setFilter] = useState(PaletteFilterType.Theme)

    let FilterTypes : string[] = [
        "Theme",
        "Pack",
        "Zoning Type"
    ];

    return (
        <>
            {ShowPaletteEditorPanel && !isPhotoMode && defaultTool && activeSelection && ShowPaletteChoices == ButtonState.On && (
                <>
                    <Portal>
                        <div className={classNames(panelStyles.panelRowGroup, panelStyles.panel, ResidentialBuildingSelected? panelStyles.residenialBuildingPosition : "")}>
                        <Panel 
                            header={(
                                <HeaderSection title="Palette Editor Menu" icon={colorPaletteSrc} onCloseEventName={"TogglePaletteEditorMenu"}></HeaderSection>
                            )}
                            footer = {(
                                <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                    <VanillaComponentResolver.instance.Section title={"Palette"}>
                                        <PaletteBoxComponent Swatches={Swatches} totalWidth={80}></PaletteBoxComponent>
                                        <span className={panelStyles.smallSpacer}></span>
                                        <VanillaComponentResolver.instance.ToolButton src={saveToDiskSrc}          tooltip = {"Save Palette"}   onSelect={() => {handleClick("TrySavePalette")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                        <span className={panelStyles.wideSpacer}></span>
                                        <VanillaComponentResolver.instance.ToolButton src={trashSrc}     tooltip = {"Delete Palette"}   onSelect={() => {handleClick("DeletePalette")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                    </VanillaComponentResolver.instance.Section>
                                </InfoSection>
                            )}>
                            <>
                                <UniqueNameSectionComponent uniqueName={UniqueNames[MenuType.Palette]} uniqueNameType={MenuType.Palette}></UniqueNameSectionComponent>
                                <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                    <CategorySection category={PaletteCategories[MenuType.Palette]} menu={MenuType.Palette}></CategorySection>
                                    <VanillaComponentResolver.instance.Section title={"Subcategory"}>
                                        <>
                                            { (SelectedSubcategory != Subcategories[0]) && (
                                                <VanillaComponentResolver.instance.ToolButton
                                                    src={editSrc}
                                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                    tooltip = {"Edit Subcategory"}
                                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                    selected = {UniqueNames[MenuType.Subcategory] == SelectedSubcategory && ShowSubcategoryEditorMenu}
                                                    onSelect={() => {
                                                            if (!ShowSubcategoryEditorMenu) { trigger(mod.id, "ShowSubcategoryEditorPanel"); trigger(mod.id, "EditSubcategory", SelectedSubcategory)}
                                                            else if (UniqueNames[MenuType.Subcategory] == SelectedSubcategory) { trigger(mod.id, "ShowSubcategoryEditorPanel"); }
                                                            else {trigger(mod.id, "EditSubcategory", SelectedSubcategory); console.log("UN: " + UniqueNames[MenuType.Subcategory]); console.log(SelectedSubcategory)}}}
                                                />
                                            )}
                                            <span className={panelStyles.smallSpacer}></span>
                                        
                                            <Dropdown 
                                                theme = {dropDownThemes}
                                                content={                    
                                                    Subcategories.map((subcategory) => (
                                                        <DropdownItem value={subcategory} className={dropDownThemes.dropdownItem} selected={subcategory==SelectedSubcategory} onChange={() =>  trigger(mod.id, "ChangeSubcategory", subcategory)}>
                                                            <div className={panelStyles.subcategoryDropwdownWidth}>{subcategory}</div>
                                                        </DropdownItem>
                                                    ))
                                                }
                                            >
                                                <DropdownToggle disabled={false}>
                                                    <div className={panelStyles.subcategoryDropwdownWidth}>{SelectedSubcategory}</div>
                                                </DropdownToggle>
                                            </Dropdown>
                                            <span className={panelStyles.smallSpacer}></span>
                                            <VanillaComponentResolver.instance.ToolButton src={plusSrc}  selected={UniqueNames[MenuType.Subcategory] != SelectedSubcategory && ShowSubcategoryEditorMenu}        tooltip = {"Add Subcategory"}        className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}    
                                                onSelect={() => {
                                                    if (!ShowSubcategoryEditorMenu) { trigger(mod.id, "ShowSubcategoryEditorPanel")} 
                                                    else if (UniqueNames[MenuType.Subcategory] == SelectedSubcategory) { trigger(mod.id, "GenerateNewSubcategory")}
                                                    else {trigger(mod.id, "ShowSubcategoryEditorPanel")}}} />
                                        </>
                                    </VanillaComponentResolver.instance.Section>
                                    <VanillaComponentResolver.instance.Section title={"Filter Type"}>
                                        <Dropdown 
                                            theme = {dropDownThemes}
                                            content={                    
                                                FilterTypes.map((type, index: number) => (
                                                    <DropdownItem value={type} className={dropDownThemes.dropdownItem} selected={currentFilter==index} onChange={() => setFilter(index)}>
                                                        <div className={panelStyles.filterTypeWidth}>{type}</div>
                                                    </DropdownItem>
                                                ))
                                            }
                                        >
                                            <DropdownToggle disabled={false}>
                                                <div className={panelStyles.filterTypeWidth}>{FilterTypes[currentFilter]}</div>
                                            </DropdownToggle>
                                        </Dropdown>
                                    </VanillaComponentResolver.instance.Section>
                                </InfoSection>
                                <LocaleSection></LocaleSection>
                                <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                    { Swatches.length < 8 && (
                                        <VanillaComponentResolver.instance.Section title={"Add a Swatch"}>
                                            <VanillaComponentResolver.instance.ToolButton src={plusSrc}          tooltip = {"Add Swatch"}   onSelect={() => {handleClick("AddASwatch")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                        </VanillaComponentResolver.instance.Section>
                                    )}
                                    <div className={classNames(styles.rowGroup, panelStyles.subtitleRow, styles.centered)}>
                                        <div className={classNames(panelStyles.centeredSubTitle, styles.colorFieldWidth)}>Color</div>
                                        <span className={panelStyles.sliderSpacerLeft}></span>
                                        <div className={classNames(panelStyles.probabilityWeightWidth, panelStyles.centeredSubTitle)}>Probability Weight</div>
                                    </div>
                                    { Swatches.map((currentSwatch, index:number) => (
                                        <SwatchComponent info={currentSwatch} index={index}></SwatchComponent>
                                    ))}
                                    { Swatches.length >= 8 && (
                                        <VanillaComponentResolver.instance.Section title={"Add a Swatch"}>
                                            <VanillaComponentResolver.instance.ToolButton src={plusSrc}          tooltip = {"Add Swatch"}   onSelect={() => {handleClick("AddASwatch")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                        </VanillaComponentResolver.instance.Section>
                                    )}
                                </InfoSection>
                            </>
                        </Panel>
                        <span className={panelStyles.smallSpacer}></span>
                        <SubcategoryEditorMenuComponent></SubcategoryEditorMenuComponent>
                        </div>
                            
                    </Portal>
                </>
                
            )}
        </>
    );
}