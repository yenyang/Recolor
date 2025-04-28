
import { bindValue, trigger, useValue } from "cs2/api";
import panelStyles from "./PaletteMenuStyles.module.scss";
import styles from "../Domain/ColorFields.module.scss";
import {  Entity, game, selectedInfo, tool } from "cs2/bindings";
import {  Dropdown, DropdownItem, DropdownToggle, Panel, Portal } from "cs2/ui";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { useLocalization } from "cs2/l10n";
import { InfoSection } from "mods/RecolorMainPanel/RecolorMainPanel";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { useState, version } from "react";
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
import { PaletteFilterEntityUIData } from "mods/Domain/PaletteAndSwatches/PaletteFilterEntityUIData";
import { entityEquals } from "cs2/utils";
import { FocusDisabled } from "cs2/input";
import { LocalizationUIData } from "mods/Domain/PaletteAndSwatches/LocalizationUIData";

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
const minusSrc =                         uilStandard + "Minus.svg";
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
const SelectedFilterType$ = bindValue<PaletteFilterType>(mod.id, "SelectedFilterType");
const FilterEntities$ = bindValue<PaletteFilterEntityUIData[]>(mod.id, "FilterEntities");
const SelectedFilterPrefabEntities$ = bindValue<Entity[]>(mod.id, "SelectedFilterPrefabEntities");
const LocalizationUIDatas$ = bindValue<LocalizationUIData[][]>(mod.id, "LocalizationDatas");

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
    const SelectedFilterType = useValue(SelectedFilterType$);
    const FilterEntities = useValue(FilterEntities$);
    const SelectedFilterPrefabEntities = useValue(SelectedFilterPrefabEntities$);
    const LocalizationUIDatas = useValue(LocalizationUIDatas$);
    
    const { translate } = useLocalization();

    function GetFilterUIData(prefabEntity : Entity) 
    {
        for (let i=0; i<FilterEntities.length; i++) 
        {
            if (entityEquals(FilterEntities[i].FilterPrefabEntity, prefabEntity)) 
            {
                return FilterEntities[i];
            }
        }

        if (FilterEntities.length > 0) 
        {
            return FilterEntities[0];
        }

        const nullEntity : Entity = {
            index: 0,
            version: 0,
        }

        const defaultData : PaletteFilterEntityUIData = {
            FilterPrefabEntity:  nullEntity,
            Src: "",
            LocaleKey: "",
        };

        return defaultData;
    }

    function IsSelected(prefabEntity: Entity) 
    {
        for (let i=0; i<SelectedFilterPrefabEntities.length; i++) 
        {
            if (entityEquals(prefabEntity, SelectedFilterPrefabEntities[i])) 
            {
                return true;
            }
        }

        return false;
    }
    

    let FilterTypes : (string | null) [] = [
        translate("Recolor.SECTION_TITLE[None]", locale["Recolor.SECTION_TITLE[None]"]),
        translate("Toolbar.THEME_PANEL_TITLE", "Theme"),
        translate("Toolbar.ASSET_PACKS_PANEL_TITLE", "Pack"),
        translate("Tutorials.TITLE[ZoningTutorialZoneTypes]", "Zone Types"),
    ];

    return (
        <>
            {ShowPaletteEditorPanel && !isPhotoMode && defaultTool && activeSelection && ShowPaletteChoices == ButtonState.On && (
                <>
                    <Portal>
                        <div className={classNames(panelStyles.panelRowGroup, panelStyles.panel, ResidentialBuildingSelected? panelStyles.residenialBuildingPosition : "")}>
                        <Panel 
                            header={(
                                <HeaderSection title={translate("Recolor.SECTION_TITLE[PaletteEditorMenu]" ,locale["Recolor.SECTION_TITLE[PaletteEditorMenu]"])} icon={colorPaletteSrc} onCloseEventName={"TogglePaletteEditorMenu"}></HeaderSection>
                            )}
                            footer = {(
                                <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                    <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[Palette]" ,locale["Recolor.SECTION_TITLE[Palette]"])}>
                                        <PaletteBoxComponent Swatches={Swatches} totalWidth={80}></PaletteBoxComponent>
                                        { (Swatches.length <= 3 && LocalizationUIDatas[MenuType.Palette].length <= 1) ?
                                            <span className={panelStyles.spacer15}></span> : 
                                            <span className={panelStyles.spacer25}></span>
                                        }
                                        <VanillaComponentResolver.instance.ToolButton src={saveToDiskSrc}          tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SavePalette]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SavePalette]"])}   onSelect={() => {handleClick("TrySavePalette")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                        <span className={panelStyles.wideSpacer}></span>
                                        <VanillaComponentResolver.instance.ToolButton src={trashSrc}     tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[DeletePalette]" ,locale["Recolor.TOOLTIP_DESCRIPTION[DeletePalette]"])}   onSelect={() => {handleClick("DeletePalette")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                    </VanillaComponentResolver.instance.Section>
                                </InfoSection>
                            )}>
                            <>
                                <UniqueNameSectionComponent uniqueName={UniqueNames[MenuType.Palette]} uniqueNameType={MenuType.Palette}></UniqueNameSectionComponent>
                                <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                    <CategorySection category={PaletteCategories[MenuType.Palette]} menu={MenuType.Palette}></CategorySection>
                                    <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[Subcategory]" ,locale["Recolor.SECTION_TITLE[Subcategory]"])}>
                                        <>
                                            { (SelectedSubcategory != Subcategories[0]) && (
                                                <VanillaComponentResolver.instance.ToolButton
                                                    src={editSrc}
                                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[EditSubcategory]" ,locale["Recolor.TOOLTIP_DESCRIPTION[EditSubcategory]"])}
                                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                    selected = {UniqueNames[MenuType.Subcategory] == SelectedSubcategory && ShowSubcategoryEditorMenu}
                                                    onSelect={() => {
                                                            if (!ShowSubcategoryEditorMenu) { trigger(mod.id, "ShowSubcategoryEditorPanel"); trigger(mod.id, "EditSubcategory", SelectedSubcategory)}
                                                            else if (UniqueNames[MenuType.Subcategory] == SelectedSubcategory) { trigger(mod.id, "ShowSubcategoryEditorPanel"); }
                                                            else {trigger(mod.id, "EditSubcategory", SelectedSubcategory); }}}
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
                                            <VanillaComponentResolver.instance.ToolButton src={plusSrc}  selected={UniqueNames[MenuType.Subcategory] != SelectedSubcategory && ShowSubcategoryEditorMenu}        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[AddSubcategory]" ,locale["Recolor.TOOLTIP_DESCRIPTION[AddSubcategory]"])}        className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}    
                                                onSelect={() => {
                                                    if (!ShowSubcategoryEditorMenu) { trigger(mod.id, "ShowSubcategoryEditorPanel")} 
                                                    else if (UniqueNames[MenuType.Subcategory] == SelectedSubcategory) { trigger(mod.id, "GenerateNewSubcategory")}
                                                    else {trigger(mod.id, "ShowSubcategoryEditorPanel")}}} />
                                        </>
                                    </VanillaComponentResolver.instance.Section>
                                    <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[FilterType]", locale["Recolor.SECTION_TITLE[FilterType]"])}>
                                            <Dropdown 
                                                theme = {dropDownThemes}
                                                content={                    
                                                    FilterTypes.map((type, index: number) => (
                                                        <DropdownItem value={type} className={dropDownThemes.dropdownItem} selected={SelectedFilterType==index} onChange={() => trigger(mod.id, "SetFilter", index)}>
                                                            <div className={panelStyles.filterTypeWidth}>{type}</div>
                                                        </DropdownItem>
                                                    ))
                                                }
                                            >
                                                <DropdownToggle disabled={false}>
                                                    <div className={panelStyles.filterTypeWidth}>{FilterTypes[SelectedFilterType]}</div>
                                                </DropdownToggle>
                                            </Dropdown>
                                    </VanillaComponentResolver.instance.Section>
                                    {FilterEntities.length > 0 && SelectedFilterPrefabEntities.length > 0 && (
                                        <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[FilterChoices]", locale["Recolor.SECTION_TITLE[FilterChoices]"])}>
                                            <FocusDisabled disabled={true}>
                                                <div className={styles.columnGroup}>
                                                    {SelectedFilterPrefabEntities.map((selectedEntity: Entity, index: number) => (
                                                        <div className={styles.rowGroup}>
                                                            <Dropdown 
                                                                theme = {dropDownThemes}
                                                                content={         
                                                                    FilterEntities.map((entityData: PaletteFilterEntityUIData) => (
                                                                        <>
                                                                        {(IsSelected(entityData.FilterPrefabEntity) == false || entityEquals(SelectedFilterPrefabEntities[index], entityData.FilterPrefabEntity)) && (
                                                                            <DropdownItem value={entityData} className={dropDownThemes.dropdownItem} onChange={() => trigger(mod.id, "SetFilterChoice", index, entityData.FilterPrefabEntity)}>
                                                                                <div className={classNames(panelStyles.filterChoicesDropdown, panelStyles.filterRowGroup)}>
                                                                                    <img src={entityData.Src} className={panelStyles.filterChoicesIcon}></img>
                                                                                    <span className={panelStyles.smallSpacer}></span>
                                                                                    <div>{translate(entityData.LocaleKey)}</div>
                                                                                </div>
                                                                            </DropdownItem>
                                                                        )}
                                                                        </>
                                                                    ))
                                                                }
                                                                >
                                                                <DropdownToggle disabled={false}>
                                                                    <div className={classNames(panelStyles.filterChoicesDropdown, panelStyles.filterRowGroup)}>
                                                                        <img src={GetFilterUIData(selectedEntity).Src} className={panelStyles.filterChoicesIcon}></img>
                                                                        <span className={panelStyles.smallSpacer}></span>
                                                                        <div>{translate(GetFilterUIData(selectedEntity).LocaleKey)}</div>
                                                                    </div>
                                                                </DropdownToggle>
                                                            </Dropdown>
                                                            <span className={panelStyles.smallSpacer}></span>
                                                            { SelectedFilterPrefabEntities.length < FilterEntities.length - 1 && index == SelectedFilterPrefabEntities.length - 1 && (
                                                                <VanillaComponentResolver.instance.ToolButton src={plusSrc}  tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[AddFilter]" ,locale["Recolor.TOOLTIP_DESCRIPTION[AddFilter]"])}        className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}    
                                                                    onSelect={() => trigger(mod.id, "AddFilterChoice")}
                                                                />
                                                            )}
                                                            { FilterEntities.length > 2 && index != SelectedFilterPrefabEntities.length - 1 && (
                                                                <VanillaComponentResolver.instance.ToolButton src={minusSrc}  tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[RemoveFilter]", locale["Recolor.TOOLTIP_DESCRIPTION[RemoveFilter]"])}        className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}    
                                                                    onSelect={() => trigger(mod.id, "RemoveFilterChoice", index)}
                                                                />
                                                            )}
                                                            { (FilterEntities.length <= 2 || (SelectedFilterPrefabEntities.length == FilterEntities.length - 1 && index == SelectedFilterPrefabEntities.length - 1)) && (
                                                                <span className={styles.ButtonWidth}></span>
                                                            )}
                                                        </div>
                                                    ))}    
                                                </div>        
                                            </FocusDisabled>                                        
                                        </VanillaComponentResolver.instance.Section>
                                    )}
                                </InfoSection>
                                <LocaleSection menu={MenuType.Palette} localizations={LocalizationUIDatas[MenuType.Palette]}></LocaleSection>
                                <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                                    { Swatches.length < 8 && (
                                        <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[AddSwatch]" ,locale["Recolor.SECTION_TITLE[AddSwatch]"])}>
                                            <VanillaComponentResolver.instance.ToolButton src={plusSrc}          tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[AddSwatch]" , locale["Recolor.TOOLTIP_DESCRIPTION[AddSwatch]"])}   onSelect={() => {handleClick("AddASwatch")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                                        </VanillaComponentResolver.instance.Section>
                                    )}
                                    <div className={classNames(styles.rowGroup, panelStyles.subtitleRow, styles.centered)}>
                                        <span className={panelStyles.colorSpacerLeft}></span>
                                        <div className={classNames(panelStyles.centeredSubTitle, styles.colorFieldWidth)}>{translate("PhotoMode.PROPERTY_TITLE[Vignette.color]")}</div>
                                        <span className={panelStyles.sliderSpacerLeft}></span>
                                        <div className={classNames(panelStyles.probabilityWeightWidth, panelStyles.centeredSubTitle)}>{translate("Recolor.TOOLTIP_DESCRIPTION[ProbabilityWeight]" , locale["Recolor.TOOLTIP_DESCRIPTION[ProbabilityWeight]"])}</div>
                                    </div>
                                    { Swatches.map((currentSwatch, index:number) => (
                                        <SwatchComponent info={currentSwatch} index={index}></SwatchComponent>
                                    ))}
                                    { Swatches.length >= 8 && (
                                        <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[AddSwatch]" ,locale["Recolor.SECTION_TITLE[AddSwatch]"])}>
                                            <VanillaComponentResolver.instance.ToolButton src={plusSrc}          tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[AddSwatch]" , locale["Recolor.TOOLTIP_DESCRIPTION[AddSwatch]"])}   onSelect={() => {handleClick("AddASwatch")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
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