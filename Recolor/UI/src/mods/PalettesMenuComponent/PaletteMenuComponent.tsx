
import { bindValue, trigger, useValue } from "cs2/api";
import panelStyles from "./PaletteMenuStyles.module.scss";
import styles from "../Domain/ColorFields.module.scss";
import { Color, game, selectedInfo, tool } from "cs2/bindings";
import { Button, Dropdown, DropdownItem, Panel, Portal } from "cs2/ui";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { useLocalization } from "cs2/l10n";
import { InfoSection, roundButtonHighlightStyle } from "mods/RecolorMainPanel/RecolorMainPanel";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { StringInputField, StringInputFieldStyle } from "mods/SIPColorComponent/SIPColorComponent";
import { useState } from "react";
import classNames from "classnames";
import { PaletteLocalizationSet } from "mods/Domain/PaletteAndSwatches/PaletteLocalizationSet";
import { SwatchComponent } from "mods/SwatchComponent/SwatchComponent";
import { SwatchUIData } from "mods/Domain/PaletteAndSwatches/SwatchUIData";
import { PaletteCategory } from "mods/Domain/PaletteAndSwatches/PaletteCategoryType";
import { PaletteBoxComponent } from "mods/PaletteBoxComponent/PaletteBoxComponent";

const uilStandard =                         "coui://uil/Standard/";
const closeSrc =         uilStandard +  "XClose.svg";

const buildingSrc =                     uilStandard + "House.svg";
const vehiclesSrc =                     uilStandard + "GenericVehicle.svg";
const propsSrc =                        uilStandard + "BenchAndLampProps.svg";
const allSrc =                          uilStandard + "StarAll.svg";
const plusSrc =                         uilStandard + "Plus.svg";
const saveToDiskSrc =                   uilStandard + "DiskSave.svg";

const Swatches$ = bindValue<SwatchUIData[]>(mod.id, "Swatches");
const UniqueName$ = bindValue<string>(mod.id, "UniqueName");
const ShowPaletteEditorPanel$ = bindValue<boolean>(mod.id, "ShowPaletteEditorMenu");
const CurrentPaletteCategory$ = bindValue<PaletteCategory>(mod.id, "PaletteCategory");

function handleClick(event: string) {
    trigger(mod.id, event);
}

function handleCategoryClick(category : PaletteCategory) {
    trigger(mod.id, "ToggleCategory", category as number);
}

export const PaletteMenuComponent = () => {
    const isPhotoMode = useValue(game.activeGamePanel$)?.__Type == game.GamePanelType.PhotoMode;
    const Swatches = useValue(Swatches$);
    const defaultTool = useValue(tool.activeTool$).id == tool.DEFAULT_TOOL;
    const activeSelection = useValue(selectedInfo.activeSelection$);
    const UniqueName = useValue(UniqueName$);
    const ShowPaletteEditorPanel = useValue(ShowPaletteEditorPanel$);
    const CurrentPaletteCategory = useValue(CurrentPaletteCategory$);
    
    const { translate } = useLocalization();

    let [uniqueNameInput, setTextInput] = useState(UniqueName);
    let [validInput, setValidInput] = useState(true);
    let [locales, setLocales] = useState(["en-US"]);

    let FilterTypes : string[] = [
        "Theme",
        "Pack",
        "Zoning Type"
    ];

    function HandleTextInput () {
       setValidInput(true);
       trigger(mod.id, "ChangeUniqueName", uniqueNameInput);
    }

    return (
        <>
            {ShowPaletteEditorPanel && !isPhotoMode && defaultTool && activeSelection && (
                <Portal>
                    <Panel
                        className={panelStyles.panel}
                        header={(
                            <VanillaComponentResolver.instance.Section title={"Palette Editor Menu"}>
                                <Button className={roundButtonHighlightStyle.button} variant="icon" onSelect={() => handleClick("TogglePaletteEditorMenu")} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                    <img src={closeSrc}></img>
                                </Button>
                            </VanillaComponentResolver.instance.Section>
                        )}>
                        <>
                            <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                <VanillaComponentResolver.instance.Section title={"Unique Name"}>
                                    <StringInputField 
                                        value={uniqueNameInput.replace(/[\r\n]+/gm, '')}
                                        disabled ={false}
                                        onChange={ (e : string) => { setTextInput(e); }}
                                        onChangeEnd={HandleTextInput}
                                        className={validInput?  classNames(StringInputFieldStyle.textInput, styles.nameFieldInput) : classNames(StringInputFieldStyle.textInput, styles.nameFieldInput, styles.invalidFieldInput)}
                                        multiline={false}
                                        maxLength={32}
                                    ></StringInputField>
                                </VanillaComponentResolver.instance.Section>
                            </InfoSection>
                            <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                <VanillaComponentResolver.instance.Section title={"Category"}>
                                    <VanillaComponentResolver.instance.ToolButton src={allSrc}          tooltip = {"All Categories"}                                                                                    selected={CurrentPaletteCategory == PaletteCategory.Any}                                                                                      onSelect={() => {handleCategoryClick(PaletteCategory.Any)} }                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                    <VanillaComponentResolver.instance.ToolButton src={buildingSrc}     tooltip = {translate("Recolor.TOOLTIP_TITLE[BuildingFilter]", locale["Recolor.TOOLTIP_TITLE[BuildingFilter]"])} selected={CurrentPaletteCategory == PaletteCategory.Any || (CurrentPaletteCategory & PaletteCategory.Buildings) == PaletteCategory.Buildings} onSelect={() => {handleCategoryClick(PaletteCategory.Buildings)} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                    <VanillaComponentResolver.instance.ToolButton src={vehiclesSrc}     tooltip = {translate("Recolor.TOOLTIP_TITLE[VehicleFilter]", locale["Recolor.TOOLTIP_TITLE[VehicleFilter]"])}   selected={CurrentPaletteCategory == PaletteCategory.Any || (CurrentPaletteCategory & PaletteCategory.Vehicles) == PaletteCategory.Vehicles}   onSelect={() => {handleCategoryClick(PaletteCategory.Vehicles)} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                    <VanillaComponentResolver.instance.ToolButton src={propsSrc}        tooltip = {translate("Recolor.TOOLTIP_TITLE[PropFilter]", locale["Recolor.TOOLTIP_TITLE[PropFilter]"])}         selected={CurrentPaletteCategory == PaletteCategory.Any || (CurrentPaletteCategory & PaletteCategory.Props) == PaletteCategory.Props}         onSelect={() => {handleCategoryClick(PaletteCategory.Props)} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />  
                                </VanillaComponentResolver.instance.Section>
                                <VanillaComponentResolver.instance.Section title={"Subcategory"}>
                                    <Dropdown content={undefined}>
                                        
                                    </Dropdown>
                                </VanillaComponentResolver.instance.Section>
                                <VanillaComponentResolver.instance.Section title={"Filter Type"}>
                                    <Dropdown 
                                        content={FilterTypes.map((type) => (
                                            <DropdownItem value={type}>
                                                <div>{type}</div>
                                            </DropdownItem>
                                        ))}>
                                        
                                    </Dropdown>
                                </VanillaComponentResolver.instance.Section>
                            </InfoSection>
                            <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                { locales.map((currentLocale) => (
                                    <PaletteLocalizationSet localeCode={currentLocale}></PaletteLocalizationSet>
                                ))}
                                <VanillaComponentResolver.instance.Section title={"Add a Locale"}>
                                    <VanillaComponentResolver.instance.ToolButton src={plusSrc}          tooltip = {"Add Locale"}   onSelect={() => {} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                </VanillaComponentResolver.instance.Section>
                            </InfoSection>
                            <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                <div className={classNames(styles.rowGroup, panelStyles.subtitleRow, styles.centered)}>
                                    <div className={classNames(panelStyles.centeredSubTitle, styles.colorFieldWidth)}>Color</div>
                                    <span className={panelStyles.sliderSpacerLeft}></span>
                                    <div className={classNames(panelStyles.probabilityWeightWidth, panelStyles.centeredSubTitle)}>Probability Weight</div>
                                </div>
                                { Swatches.map((currentSwatch) => (
                                    <SwatchComponent info={currentSwatch}></SwatchComponent>
                                ))}
                                <VanillaComponentResolver.instance.Section title={"Add a Swatch"}>
                                    <VanillaComponentResolver.instance.ToolButton src={plusSrc}          tooltip = {"Add Swatch"}   onSelect={() => {handleClick("AddASwatch")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                </VanillaComponentResolver.instance.Section>
                            </InfoSection>
                            <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                <VanillaComponentResolver.instance.Section title={"Save Palette"}>
                                    <PaletteBoxComponent Swatches={Swatches} totalWidth={80}></PaletteBoxComponent>
                                    <span className={panelStyles.savePaletteSpacer}></span>
                                    <VanillaComponentResolver.instance.ToolButton src={saveToDiskSrc}          tooltip = {"Save Palette"}   onSelect={() => {handleClick("TrySavePalette")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                </VanillaComponentResolver.instance.Section>
                            </InfoSection>
                        </>
                    </Panel>
                </Portal>
            )}
        </>
    );
}