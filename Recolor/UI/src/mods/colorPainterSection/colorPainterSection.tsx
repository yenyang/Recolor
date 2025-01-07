import { useLocalization } from "cs2/l10n";
import {ModuleRegistryExtend, getModule} from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { Color, tool } from "cs2/bindings";
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

const ColorPainterSelectionType$ = bindValue<number>(mod.id, "ColorPainterSelectionType");
const SingleInstance$ = bindValue<ButtonState>(mod.id, 'SingleInstance');
const Matching$ = bindValue<ButtonState>(mod.id, 'Matching');
const CanPasteColorSet$ = bindValue<boolean>(mod.id, "CanPasteColorSet");
const Radius$ = bindValue<number>(mod.id, "Radius");
const Filter$ = bindValue<number>(mod.id, "Filter");
const ToolMode$ = bindValue<PainterToolMode>(mod.id, "PainterToolMode");
const ShowHexaDecimals$ = bindValue<boolean>(mod.id, "ShowHexaDecimals");
const PainterColorSet$ = bindValue<RecolorSet>(mod.id, "PainterColorSet");

const arrowDownSrc =         uilStandard +  "ArrowDownThickStroke.svg";
const arrowUpSrc =           uilStandard +  "ArrowUpThickStroke.svg";

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


export const ColorPainterSectionComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {
        // These get the value of the bindings.
        const toolActive = useValue(tool.activeTool$).id == "ColorPainterTool";        
        const ColorPainterSelectionType = useValue(ColorPainterSelectionType$);
        const SingleInstance = useValue(SingleInstance$);   
        const Matching = useValue(Matching$);
        const CanPasteColorSet = useValue(CanPasteColorSet$);
        const Radius = useValue(Radius$);
        const Filter = useValue(Filter$);        
        const PainterColorSet = useValue(PainterColorSet$);    
        const ToolMode = useValue(ToolMode$);
        const ShowHexaDecimals = useValue(ShowHexaDecimals$);
        // translation handling. Translates using locale keys that are defined in C# or fallback string here.
        const { translate } = useLocalization();

        // This defines aspects of the components.
        const {children, ...otherProps} = props || {};

        // This gets the original component that we may alter and return.
        var result : JSX.Element = Component();
        // It is important that we coordinate how to handle the tool options panel because it is possibile to create a mod that works for your mod but prevents others from doing the same thing.
        // If show icon add new section with title, and one button. 
        if (toolActive) {
            result.props.children?.unshift(
                <>
                    <VanillaComponentResolver.instance.Section title={translate("Toolbar.TOOL_MODE_TITLE", "Tool Mode")}> 
                        <>
                            <VanillaComponentResolver.instance.ToolButton
                                src={colorPaletteSrc}
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
                    {ColorPainterSelectionType == 1 && ToolMode != PainterToolMode.Picker &&(
                        <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[Filter]", locale["Recolor.SECTION_TITLE[Filter]"])}>
                            <VanillaComponentResolver.instance.ToolButton
                                src={buildingSrc}
                                selected={Filter == 0}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                disabled = {false}      
                                tooltip = {DescriptionTooltip( translate("Recolor.TOOLTIP_TITLE[BuildingFilter]",locale["Recolor.TOOLTIP_TITLE[BuildingFilter]"]), translate("Recolor.TOOLTIP_DESCRIPTION[BuildingFilter]", locale["Recolor.TOOLTIP_DESCRIPTION[BuildingFilter]"]))}
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
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { ToolMode == PainterToolMode.Paint && (
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
                    )}
                </>
            );
        }

        return result;
    };
}