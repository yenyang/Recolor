import { useLocalization } from "cs2/l10n";
import { getModule} from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { Color, Entity} from "cs2/bindings";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { PainterToolMode } from "mods/Domain/PainterToolMode";
import { ColorPainterFieldComponent } from "mods/colorPainterFieldComponent/ColorPainterFieldComponent";
import classNames from "classnames";
import styles from "../Domain/ColorFields.module.scss";
import { RecolorSet } from "mods/Domain/RecolorSet";
import { ButtonState } from "mods/Domain/ButtonState";
import { Scope } from "mods/Domain/Scope";
import { tool } from "cs2/bindings";
import paintSrc from "images/format_painter.svg";
import { assignPalette, PaletteChooserComponent, removePalette } from "mods/PaletteChooserComponent/PaletteChooserComponent";
import { PaletteChooserUIData } from "mods/Domain/PaletteAndSwatches/PaletteChooserUIData";
import { FocusDisabled } from "cs2/input";
import { PaletteCategory } from "mods/Domain/PaletteAndSwatches/PaletteCategoryType";
import { MenuType } from "mods/Domain/MenuType";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";
import panelStyles from "../PalettesMenuComponent/PaletteMenuStyles.module.scss";
import { PaletteFilterEntityUIData } from "mods/Domain/PaletteAndSwatches/PaletteFilterEntityUIData";
import { entityEquals } from "cs2/utils";
import { PaletteFilterType } from "mods/Domain/PaletteAndSwatches/PaletteFilterType";
/*
import resetSrc from "images/uilStandard/Reset.svg";
import singleSrc from "images/uilStandard/SingleRhombus.svg";
import matchingSrc from "images/uilStandard/SameRhombus.svg";
import copySrc from "images/uilStandard/RectangleCopy.svg";
import pasteSrc from "images/uilStandard/RectanglePaste.svg";
import colorPickerSrc from "images/uilStandard/PickerPipette.svg";
import colorPaletteSrc from "images/uilColored/ColorPalette.svg";
import swapSrc from "images/uilStandard/ArrowsMoveLeftRight.svg";
import singleSelectionSrc from "images/uilStandard/Dot.svg";
import radiusSelectionSrc from "images/uilStandard/Circle.svg";
import buildingSrc from "images/uilStandard/House.svg";
import vehiclesSrc from "images/uilStandard/GenericVehicle.svg";
import propsSrc from "images/uilStandard/BenchAndLampProps.svg";
import arrowDownSrc from "images/uilStandard/ArrowDownThickStroke.svg";
import arrowUpSrc from "images/uilStandard/ArrowUpThickStroke.svg";
*/
// These contain the coui paths to Unified Icon Library svg assets



// These contain the coui paths to Unified Icon Library svg assets
const uilStandard =                          "coui://uil/Standard/";
const uilColored =                           "coui://uil/Colored/";
const singleSelectionSrc =                       uilStandard + "Dot.svg";
const radiusSelectionSrc =                        uilStandard + "Circle.svg";
const singleSrc =                        uilStandard + "SingleRhombus.svg";
const matchingSrc =                     uilStandard + "SameRhombus.svg";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const buildingSrc =                     uilStandard + "House.svg";
const vehiclesSrc =                     uilStandard + "GenericVehicle.svg";
const propsSrc =                        uilStandard + "BenchAndLampProps.svg";
const swapSrc =                         uilStandard + "ArrowsMoveLeftRight.svg";
const resetSrc =                     uilStandard + "Reset.svg";
const colorPickerSrc =                  uilStandard + "PickerPipette.svg";
const colorPaletteSrc =                 uilColored + "ColorPalette.svg";
const arrowDownSrc =         uilStandard +  "ArrowDownThickStroke.svg";
const arrowUpSrc =           uilStandard +  "ArrowUpThickStroke.svg";
const netLanesSrc =                         uilStandard + "FenceIsometric.svg";
const plusSrc =                         uilStandard + "Plus.svg";

const ColorPainterSelectionType$ = bindValue<number>(mod.id, "ColorPainterSelectionType");
const SingleInstance$ = bindValue<ButtonState>(mod.id, 'SingleInstance');
const Matching$ = bindValue<ButtonState>(mod.id, 'Matching');
const CanPasteColorSet$ = bindValue<boolean>(mod.id, "CanPasteColorSet");
const Radius$ = bindValue<number>(mod.id, "Radius");
const Filter$ = bindValue<number>(mod.id, "Filter");
const ToolMode$ = bindValue<PainterToolMode>(mod.id, "PainterToolMode");
const ShowHexaDecimals$ = bindValue<boolean>(mod.id, "ShowHexaDecimals");
const ShowPaletteChoices$ = bindValue<ButtonState>(mod.id,"ShowPaletteChoices");
const PainterColorSet$ = bindValue<RecolorSet>(mod.id, "PainterColorSet");

const SelectedFilterType$ = bindValue<PaletteFilterType>(mod.id, "ColorPainterPaletteFilterType");
const FilterEntities$ = bindValue<PaletteFilterEntityUIData[]>(mod.id, "ColorPainterPaletteFilterEntities");
const SelectedFilterPrefabEntity$ = bindValue<Entity>(mod.id, "ColorPainterPaletteFilterPrefabEntity");

const PaletteChooserPainterData$ = bindValue<PaletteChooserUIData>(mod.id, "PaletteChoicesPainter");
const EditingPrefabEntity$ = bindValue<Entity>(mod.id, "EditingPrefabEntity");
const ShowPaletteEditorPanel$ = bindValue<boolean>(mod.id, "ShowPaletteEditorMenu");
const CopiedPaletteSet$ = bindValue<Entity[]>(mod.id, "CopiedPaletteSet");
const CopiedPalette$ = bindValue<Entity>(mod.id, "CopiedPalette");
const EventSuffix = "Painter";

 


function handleClick(eventName : string) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName);
}

function changeToolMode(toolMode: PainterToolMode) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangeToolMode", toolMode as number);
}

function changeColor(channel : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangePainterColor", channel, newColor);
}

function changeScope(newScope : Scope) {
    trigger(mod.id, "ChangeScope", newScope);
}

const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");
    
// This is working, but it's possible a better solution is possible.
function DescriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null) : JSX.Element {
    return (
        <>
            <div className={descriptionToolTipStyle.title}>{tooltipTitle}</div>
            <div className={descriptionToolTipStyle.content}>{tooltipDescription}</div>
        </>
    );
}

function handleCategoryClick(category : PaletteCategory) {
    trigger(mod.id, "ToggleCategoryForPainter", category as number);
}



const dropDownThemes = getModule('game-ui/editor/themes/editor-dropdown.module.scss', 'classes');



export const ColorPainterToolOptionsComponent = () => {
    
    // These get the value of the bindings.
    const ColorPainterSelectionType = useValue(ColorPainterSelectionType$);
    const SingleInstance = useValue(SingleInstance$);   
    const Matching = useValue(Matching$);
    const CanPasteColorSet = useValue(CanPasteColorSet$);
    const Radius = useValue(Radius$);
    const Filter = useValue(Filter$);        
    const PainterColorSet = useValue(PainterColorSet$);    
    const ToolMode = useValue(ToolMode$);
    const ShowHexaDecimals = useValue(ShowHexaDecimals$);
    const toolActive = useValue(tool.activeTool$).id == "ColorPainterTool";     
    const ShowPaletteChoices = useValue(ShowPaletteChoices$);    

    const SelectedFilterType = useValue(SelectedFilterType$);
    const FilterEntities = useValue(FilterEntities$);
    const SelectedFilterPrefabEntity = useValue(SelectedFilterPrefabEntity$);
    const PaletteChooserPainterData = useValue(PaletteChooserPainterData$);
    const CopiedPalette = useValue(CopiedPalette$);
    const CopiedPaletteSet = useValue(CopiedPaletteSet$);
    const EditingPrefabEntity = useValue(EditingPrefabEntity$);
    const ShowPaletteEditorPanel = useValue(ShowPaletteEditorPanel$);    

    // translation handling. Translates using locale keys that are defined in C# or fallback string here.
    const { translate } = useLocalization();

    
    function IsSelected(prefabEntity: Entity) 
    {
        if (entityEquals(prefabEntity, SelectedFilterPrefabEntity)) 
        {
            return true;
        }

        return false;
    }
       
    
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


    let FilterTypes : (string | null) [] = [
        translate("Recolor.SECTION_TITLE[None]", locale["Recolor.SECTION_TITLE[None]"]),
        translate("Toolbar.THEME_PANEL_TITLE", "Theme"),
        translate("Toolbar.ASSET_PACKS_PANEL_TITLE", "Pack"),
        translate("Tutorials.TITLE[ZoningTutorialZoneTypes]", "Zone Types"),
    ];

    return (
        <>
            { toolActive && (
                <>
                    <VanillaComponentResolver.instance.Section title={translate("Toolbar.TOOL_MODE_TITLE", "Tool Mode")}> 
                        <>
                            <VanillaComponentResolver.instance.ToolButton
                                src={colorPaletteSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[TogglePaletteOptions]", locale["Recolor.TOOLTIP_DESCRIPTION[TogglePaletteOptions]"]) }
                                selected={ShowPaletteChoices == ButtonState.On}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("ToggleShowPaletteChoices")}
                            />
                            <VanillaComponentResolver.instance.ToolButton
                                src={paintSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                selected = {ToolMode == PainterToolMode.Paint}
                                multiSelect = {false}   // I haven't tested any other value here 
                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[PaintToolMode]" ,locale["Recolor.TOOLTIP_TITLE[PaintToolMode]"]), translate("Recolor.TOOLTIP_DESCRIPTION[PaintToolMode]" ,locale["Recolor.TOOLTIP_DESCRIPTION[PaintToolMode]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => changeToolMode(PainterToolMode.Paint)}
                            />
                            <VanillaComponentResolver.instance.ToolButton
                                    src={resetSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    selected = {ToolMode == PainterToolMode.Reset}
                                    multiSelect = {false}   // I haven't tested any other value here 
                                    tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[ResetToolMode]",locale["Recolor.TOOLTIP_TITLE[ResetToolMode]"]), translate("Recolor.TOOLTIP_DESCRIPTION[ResetToolMode]" ,locale["Recolor.TOOLTIP_DESCRIPTION[ResetToolMode]"]))}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => changeToolMode(PainterToolMode.Reset)}
                            />
                            <VanillaComponentResolver.instance.ToolButton
                                src={colorPickerSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                selected = {ToolMode == PainterToolMode.Picker}
                                multiSelect = {false}   // I haven't tested any other value here 
                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[PickerToolMode]",locale["Recolor.TOOLTIP_TITLE[PickerToolMode]"]), translate("Recolor.TOOLTIP_DESCRIPTION[PickerToolMode]" ,locale["Recolor.TOOLTIP_DESCRIPTION[PickerToolMode]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => changeToolMode(PainterToolMode.Picker)}
                            />
                        </>
                    </VanillaComponentResolver.instance.Section>
                    { ToolMode != PainterToolMode.Picker && (
                        <VanillaComponentResolver.instance.Section title={translate( "Recolor.SECTION_TITLE[InfoRowTitle]",locale["Recolor.SECTION_TITLE[InfoRowTitle]"])}> 
                            <>
                                <VanillaComponentResolver.instance.ToolButton
                                    src={singleSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    selected = {SingleInstance == ButtonState.On || ColorPainterSelectionType == 1}
                                    multiSelect = {false}   // I haven't tested any other value here 
                                    tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[SingleInstance]",locale["Recolor.TOOLTIP_TITLE[SingleInstance]"]), translate("Recolor.TOOLTIP_DESCRIPTION[SingleInstance]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SingleInstance]"]))}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => changeScope(Scope.SingleInstance)}
                                />
                                { ColorPainterSelectionType == 0 && (
                                <VanillaComponentResolver.instance.ToolButton
                                    src={matchingSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    selected = {Matching == ButtonState.On}
                                    multiSelect = {false}   // I haven't tested any other value here
                                    tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[Matching]",locale["Recolor.TOOLTIP_TITLE[Matching]"]), translate("Recolor.TOOLTIP_DESCRIPTION[Matching]" ,locale["Recolor.TOOLTIP_DESCRIPTION[Matching]"]))}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => changeScope(Scope.Matching)}
                                />)}
                                { ToolMode == PainterToolMode.Paint && (
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={copySrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        multiSelect = {false}   // I haven't tested any other value here
                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColorSet]" ,locale["Recolor.TOOLTIP_DESCRIPTION[CopyColorSet]"])}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => handleClick("ColorPainterCopyColorSet")}
                                    />
                                )}
                                { CanPasteColorSet && ToolMode == PainterToolMode.Paint && (
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={pasteSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        multiSelect = {false}   // I haven't tested any other value here
                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColorSet]", locale["Recolor.TOOLTIP_DESCRIPTION[PasteColorSet]"])}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => { handleClick("ColorPainterPasteColorSet");}}
                                    />
                                )}
                                <VanillaComponentResolver.instance.ToolButton
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            multiSelect = {false}   // I haven't tested any other value here
                                            disabled = {false}     
                                            selected={ShowHexaDecimals}
                                            children={<div className={styles.buttonWithText}>#</div>} 
                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ShowHexaDecimals]", locale["Recolor.TOOLTIP_DESCRIPTION[ShowHexaDecimals]"])}
                                            className = {classNames(VanillaComponentResolver.instance.toolButtonTheme.button)}
                                            onSelect={() => handleClick("ToggleShowHexaDecimals")}
                                />
                            </>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { ColorPainterSelectionType == 1 && ToolMode != PainterToolMode.Picker && (
                        <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[Radius]",locale["Recolor.SECTION_TITLE[Radius]"])}>
                            <VanillaComponentResolver.instance.ToolButton tooltip={translate("Recolor.TOOLTIP_DESCRIPTION[DecreaseRadius]" ,locale["Recolor.TOOLTIP_DESCRIPTION[DecreaseRadius]"])} onSelect={() => handleClick("DecreaseRadius")} src={arrowDownSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton}></VanillaComponentResolver.instance.ToolButton>
                            <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{ Radius + " m"}</div>
                            <VanillaComponentResolver.instance.ToolButton tooltip={translate("Recolor.TOOLTIP_DESCRIPTION[IncreaseRadius]" ,locale["Recolor.TOOLTIP_DESCRIPTION[IncreaseRadius]"])} onSelect={() => handleClick("IncreaseRadius")} src={arrowUpSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton} ></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { ToolMode != PainterToolMode.Picker && (
                        <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[Selection]", locale["Recolor.SECTION_TITLE[Selection]"])}>
                            <VanillaComponentResolver.instance.ToolButton
                                src={singleSelectionSrc}
                                selected={ColorPainterSelectionType == 0}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                disabled = {false}      
                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[SingleSelection]",locale["Recolor.TOOLTIP_TITLE[SingleSelection]"]), translate("Recolor.TOOLTIP_DESCRIPTION[SingleSelection]", locale["Recolor.TOOLTIP_DESCRIPTION[SingleSelection]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("ColorPainterSingleSelection")}
                            />
                            <VanillaComponentResolver.instance.ToolButton
                                src={radiusSelectionSrc}
                                selected={ColorPainterSelectionType == 1}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                disabled = {false}      
                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[RadiusSelection]",locale["Recolor.TOOLTIP_TITLE[RadiusSelection]"]), translate("Recolor.TOOLTIP_DESCRIPTION[RadiusSelection]" ,locale["Recolor.TOOLTIP_DESCRIPTION[RadiusSelection]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("ColorPainterRadiusSelection")}
                            />
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { (ColorPainterSelectionType == 1 || (ShowPaletteChoices & ButtonState.On) == ButtonState.On) && ToolMode == PainterToolMode.Paint  && (
                        <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[Category]", locale["Recolor.SECTION_TITLE[Category]"])}>
                            <VanillaComponentResolver.instance.ToolButton
                                src={buildingSrc}
                                selected={Filter == 0}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                disabled = {false}      
                                tooltip = {DescriptionTooltip( translate("Recolor.TOOLTIP_TITLE[BuildingFilter]" ,locale["Recolor.TOOLTIP_TITLE[BuildingFilter]"]), translate("Recolor.TOOLTIP_DESCRIPTION[BuildingFilter]", locale["Recolor.TOOLTIP_DESCRIPTION[BuildingFilter]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("BuildingFilter")}
                            />
                            <VanillaComponentResolver.instance.ToolButton
                                src={propsSrc}
                                selected={Filter == 1}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                disabled = {false}      
                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[PropFilter]",locale["Recolor.TOOLTIP_TITLE[PropFilter]"]), translate("Recolor.TOOLTIP_DESCRIPTION[PropFilter]", locale["Recolor.TOOLTIP_DESCRIPTION[PropFilter]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("PropFilter")}
                            />
                            <VanillaComponentResolver.instance.ToolButton
                                src={vehiclesSrc}
                                selected={Filter == 2}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                disabled = {false}      
                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[VehicleFilter]", locale["Recolor.TOOLTIP_TITLE[VehicleFilter]"]) ,translate("Recolor.TOOLTIP_DESCRIPTION[VehicleFilter]", locale["Recolor.TOOLTIP_DESCRIPTION[VehicleFilter]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("VehicleFilter")}
                            />
                            <VanillaComponentResolver.instance.ToolButton
                                src={netLanesSrc}
                                selected={Filter == 3}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                disabled = {false}      
                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[NetLanePainterCategory]", locale["Recolor.TOOLTIP_TITLE[NetLanePainterCategory]"]) ,translate("Recolor.TOOLTIP_DESCRIPTION[NetLanePainterCategory]", locale["Recolor.TOOLTIP_DESCRIPTION[NetLanePainterCategory]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("NetLanesFilter")}
                            />
                        </VanillaComponentResolver.instance.Section>
                    )}
                    {(ShowPaletteChoices & ButtonState.On) == ButtonState.On && ToolMode == PainterToolMode.Paint && (
                        <>
                            <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[FilterType]", locale["Recolor.SECTION_TITLE[FilterType]"])}>
                                    <Dropdown 
                                        theme = {dropDownThemes}
                                        content={                    
                                            FilterTypes.map((type, index: number) => (
                                                <DropdownItem value={type} className={dropDownThemes.dropdownItem} selected={SelectedFilterType==index} onChange={() => trigger(mod.id, "SetColorPainterPaletteFilter", index)}>
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
                            {FilterEntities.length > 0 && SelectedFilterPrefabEntity.index != 0 && (
                                <VanillaComponentResolver.instance.Section title={""}>
                                    <FocusDisabled disabled={true}>
                                        <div className={styles.columnGroup}>
                                                <div className={styles.rowGroup}>
                                                    <Dropdown 
                                                        theme = {dropDownThemes}
                                                        content={         
                                                            FilterEntities.map((entityData: PaletteFilterEntityUIData) => (
                                                                <DropdownItem value={entityData} className={dropDownThemes.dropdownItem} onChange={() => trigger(mod.id, "SetColorPainterPaletteFilterChoice", entityData.FilterPrefabEntity)}>
                                                                    <div className={classNames(panelStyles.filterChoicesDropdownToolOptions, panelStyles.filterRowGroup)}>
                                                                        <img src={entityData.Src} className={panelStyles.filterChoicesIcon}></img>
                                                                        <span className={panelStyles.smallSpacer}></span>
                                                                        <div>{translate(entityData.LocaleKey)}</div>
                                                                    </div>
                                                                </DropdownItem>
                                                            ))
                                                        }
                                                        >
                                                        <DropdownToggle disabled={false}>
                                                            <div className={classNames(panelStyles.filterChoicesDropdownToolOptions, panelStyles.filterRowGroup)}>
                                                                <img src={GetFilterUIData(SelectedFilterPrefabEntity).Src} className={panelStyles.filterChoicesIcon}></img>
                                                                <span className={panelStyles.smallSpacer}></span>
                                                                <div>{translate(GetFilterUIData(SelectedFilterPrefabEntity).LocaleKey)}</div>
                                                            </div>
                                                        </DropdownToggle>
                                                    </Dropdown>
                                                </div>
                                        </div>        
                                    </FocusDisabled>                                        
                                </VanillaComponentResolver.instance.Section>
                            )}
                        </>
                    )}
                    { ToolMode == PainterToolMode.Paint && (
                        <>
                            {(ShowPaletteChoices & ButtonState.On) == ButtonState.On ? 
                                <>
                                    <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[Palette]",locale["Recolor.SECTION_TITLE[Palette]"])}>
                                    <div className={styles.rowGroup}>
                                                <>
                                                    { (PaletteChooserPainterData.SelectedPaletteEntities[0].index != 0 || PaletteChooserPainterData.SelectedPaletteEntities[1].index != 0 || PaletteChooserPainterData.SelectedPaletteEntities[2].index != 0) && (
                                                    <>
                                                        <VanillaComponentResolver.instance.ToolButton src={resetSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ResetPaletteChoosersToNone]", locale["Recolor.TOOLTIP_DESCRIPTION[ResetPaletteChoosersToNone]"])}
                                                                                                        onSelect={() => { removePalette(0, EventSuffix); removePalette(1, EventSuffix); removePalette(2, EventSuffix);}}
                                                        />
                                                        <VanillaComponentResolver.instance.ToolButton src={copySrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyPaletteSet]" ,locale["Recolor.TOOLTIP_DESCRIPTION[CopyPaletteSet]"])}
                                                                                                        onSelect={() => { trigger(mod.id, "CopyPaletteSet", PaletteChooserPainterData.SelectedPaletteEntities)}}
                                                        />
                                                    </>
                                                    )} 
                                                    { (CopiedPaletteSet[0].index != 0 ||  CopiedPaletteSet[1].index != 0 || CopiedPaletteSet[2].index != 0) && (
                                                    <VanillaComponentResolver.instance.ToolButton src={pasteSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PastePaletteSet]" ,locale["Recolor.TOOLTIP_DESCRIPTION[PastePaletteSet]"])}
                                                                                                    onSelect={() => { assignPalette(0, CopiedPaletteSet[0], EventSuffix); assignPalette(1, CopiedPaletteSet[1], EventSuffix); assignPalette(2, CopiedPaletteSet[2], EventSuffix)}}
                                                    />
                                                    )} 
                                                    <VanillaComponentResolver.instance.ToolButton src={plusSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                                    tooltip = {(ShowPaletteEditorPanel && EditingPrefabEntity.index == 0) ? translate("Recolor.TOOLTIP_DESCRIPTION[CloseEditorPanel]" ,locale["Recolor.TOOLTIP_DESCRIPTION[CloseEditorPanel]"])  : translate("Recolor.TOOLTIP_DESCRIPTION[GenerateNewPalette]" ,locale["Recolor.TOOLTIP_DESCRIPTION[GenerateNewPalette]"])} 
                                                                                                    selected={ShowPaletteEditorPanel && EditingPrefabEntity.index == 0} 
                                                                                                    onSelect={() => { if (!ShowPaletteEditorPanel || (ShowPaletteEditorPanel && EditingPrefabEntity.index == 0))  {handleClick("TogglePaletteEditorMenu"); }
                                                                                                                    if (EditingPrefabEntity.index != 0) {handleClick("GenerateNewPalette");}}}
                                                    />
                                                </>
                                        </div>
                                    </VanillaComponentResolver.instance.Section>
                                    <VanillaComponentResolver.instance.Section>
                                            <FocusDisabled>
                                            <PaletteChooserComponent channel={0} PaletteChooserData={PaletteChooserPainterData} eventSuffix={EventSuffix} noneHasColor={false}></PaletteChooserComponent>
                                            <div className={styles.columnGroup}>
                                                <VanillaComponentResolver.instance.ToolButton
                                                    src={swapSrc}
                                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]"])}
                                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                    onSelect={() => 
                                                    {
                                                        let entity0 : Entity = PaletteChooserPainterData.SelectedPaletteEntities[0];
                                                        PaletteChooserPainterData.SelectedPaletteEntities[1].index != 0 ? assignPalette(0, PaletteChooserPainterData.SelectedPaletteEntities[1], EventSuffix) : removePalette(0, EventSuffix);
                                                        entity0.index != 0 ? assignPalette(1, entity0, EventSuffix) : removePalette(1, EventSuffix);
                                                    }}
                                                />
                                                { (PaletteChooserPainterData.SelectedPaletteEntities[0].index != 0 || PaletteChooserPainterData.SelectedPaletteEntities[1].index != 0 || PaletteChooserPainterData.SelectedPaletteEntities[2].index != 0 || CopiedPalette.index != 0)   ?
                                                    <span className={styles.belowSwapButton}></span> : <span className={styles.belowSwapButtonSmall}></span>
                                                }
                                            </div>
                                            <PaletteChooserComponent channel={1} PaletteChooserData={PaletteChooserPainterData} eventSuffix={EventSuffix} noneHasColor={false}></PaletteChooserComponent>
                                            <div className={styles.columnGroup}>
                                                <VanillaComponentResolver.instance.ToolButton
                                                    src={swapSrc}
                                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]"])}
                                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                    onSelect={() => 
                                                    {
                                                        let entity1 : Entity = PaletteChooserPainterData.SelectedPaletteEntities[1];
                                                        PaletteChooserPainterData.SelectedPaletteEntities[2].index != 0 ? assignPalette(1, PaletteChooserPainterData.SelectedPaletteEntities[2], EventSuffix) : removePalette(1, EventSuffix);
                                                        entity1.index != 0 ? assignPalette(2, entity1, EventSuffix) : removePalette(2, EventSuffix);
                                                    }}
                                                />
                                                { (PaletteChooserPainterData.SelectedPaletteEntities[0].index != 0 || PaletteChooserPainterData.SelectedPaletteEntities[1].index != 0 || PaletteChooserPainterData.SelectedPaletteEntities[2].index != 0 || CopiedPalette.index != 0)   ?
                                                    <span className={styles.belowSwapButton}></span> : <span className={styles.belowSwapButtonSmall}></span>
                                                }
                                            </div>
                                            <PaletteChooserComponent channel={2} PaletteChooserData={PaletteChooserPainterData} eventSuffix={EventSuffix} noneHasColor={true}></PaletteChooserComponent>
                                        </FocusDisabled>
                                    </VanillaComponentResolver.instance.Section>
                                </>
                                :
                                <VanillaComponentResolver.instance.Section title={""/*translate("Recolor.SECTION_TITLE[ColorSet]", locale["Recolor.SECTION_TITLE[ColorSet]"])*/}>
                                    <>
                                        <ColorPainterFieldComponent channel={0}></ColorPainterFieldComponent>
                                        { (PainterColorSet.States[0] && PainterColorSet.States[1])? (
                                            <div className={styles.columnGroup}>
                                                <VanillaComponentResolver.instance.ToolButton
                                                    src={swapSrc}
                                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                    multiSelect = {false}   // I haven't tested any other value here
                                                    disabled = {false}      
                                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapColors]", locale["Recolor.TOOLTIP_DESCRIPTION[SwapColors]"])}
                                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                    onSelect={() => 
                                                    {
                                                        let channel0 : Color = PainterColorSet.Channels[0];
                                                        changeColor(0, PainterColorSet.Channels[1]);
                                                        changeColor(1, channel0);
                                                    }}
                                                />                                              
                                                <span className={styles.belowSwapButton}></span>  
                                                {ShowHexaDecimals && (
                                                    <span className={styles.inputHeight}></span>
                                                )}
                                            </div>
                                        ):(
                                            <span className={styles.swapButtonWidth}></span>
                                        )}
                                        <ColorPainterFieldComponent channel={1}></ColorPainterFieldComponent>
                                        { (PainterColorSet.States[1] && PainterColorSet.States[2])? (
                                            <div className={styles.columnGroup}>
                                                <VanillaComponentResolver.instance.ToolButton
                                                    src={swapSrc}
                                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                    multiSelect = {false}   // I haven't tested any other value here
                                                    disabled = {false}      
                                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapColors]", locale["Recolor.TOOLTIP_DESCRIPTION[SwapColors]"])}
                                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                    onSelect={() => 
                                                    {
                                                        let channel1 : Color = PainterColorSet.Channels[1];
                                                        changeColor(1, PainterColorSet.Channels[2]);
                                                        changeColor(2, channel1);
                                                    }}
                                                />                                              
                                                <span className={styles.belowSwapButton}></span>  
                                                {ShowHexaDecimals && (
                                                    <span className={styles.inputHeight}></span>
                                                )}
                                            </div>
                                        ):(
                                            <span className={styles.swapButtonWidth}></span>
                                        )}
                                        <ColorPainterFieldComponent channel={2}></ColorPainterFieldComponent>
                                    </>
                                </VanillaComponentResolver.instance.Section> 
                            }
                        </>
                    )}
                </>
            )}
        </>
    );
}