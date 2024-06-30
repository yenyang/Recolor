import { useLocalization } from "cs2/l10n";
import {ModuleRegistryExtend, getModule} from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { Color, Theme, tool } from "cs2/bindings";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { ColorSet } from "mods/Domain/ColorSet";
import styles from "../Domain/ColorFields.module.scss";
import { useState } from "react";

// These contain the coui paths to Unified Icon Library svg assets
const uilStandard =                          "coui://uil/Standard/";
const singleSelectionSrc =                       uilStandard + "Dot.svg";
const radiusSelectionSrc =                        uilStandard + "Circle.svg";
const singleSrc =                        uilStandard + "SingleRhombus.svg";
const matchingSrc =                     uilStandard + "SameRhombus.svg";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const buildingSrc =                     uilStandard + "House.svg";
const vehiclesSrc =                     uilStandard + "GenericVehicle.svg";
const propsSrc =                        uilStandard + "BenchAndLampProps.svg";

const PainterColorSet$ = bindValue<ColorSet>(mod.id, "PainterColorSet");
const ColorPainterSelectionType$ = bindValue<number>(mod.id, "ColorPainterSelectionType");
const SingleInstance$ = bindValue<boolean>(mod.id, 'SingleInstance');
const CanPasteColor$ = bindValue<boolean>(mod.id, "CanPasteColor");
const CanPasteColorSet$ = bindValue<boolean>(mod.id, "CanPasteColorSet");
const CopiedColorSet$ = bindValue<ColorSet>(mod.id, "CopiedColorSet");
const CopiedColor$ = bindValue<Color>(mod.id, "CopiedColor");
const Radius$ = bindValue<Number>(mod.id, "Radius");
const Filter$ = bindValue<Number>(mod.id, "Filter");

const arrowDownSrc =         uilStandard +  "ArrowDownThickStroke.svg";
const arrowUpSrc =           uilStandard +  "ArrowUpThickStroke.svg";

const ColorFieldTheme: Theme | any = getModule(
    "game-ui/common/input/color-picker/color-field/color-field.module.scss",
    "classes"
)

function changeColor(channel : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangePainterColor", channel, newColor);
}

function copyColor(color : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "CopyColor", color);
}


function handleClick(eventName : string) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName);
}

function handleChannelClick(eventName : string, channel : number) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName, channel);
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
        
        const PainterColorSet = useValue(PainterColorSet$);
        const ColorPainterSelectionType = useValue(ColorPainterSelectionType$);
        const SingleInstance = useValue(SingleInstance$);     
        const CanPasteColor = useValue(CanPasteColor$);
        const CanPasteColorSet = useValue(CanPasteColorSet$);
        const CopiedColorSet = useValue(CopiedColorSet$);
        const CopiedColor = useValue(CopiedColor$);
        const Radius = useValue(Radius$);
        const Filter = useValue(Filter$);
        
        // translation handling. Translates using locale keys that are defined in C# or fallback string here.
        const { translate } = useLocalization();

        let [channel0, changeChannel0] = useState<Color>({r: PainterColorSet.Channel0.r, g: PainterColorSet.Channel0.g, b: PainterColorSet.Channel0.b, a: PainterColorSet.Channel0.a});
        let [channel1, changeChannel1] = useState<Color>({r: PainterColorSet.Channel1.r, g: PainterColorSet.Channel1.g, b: PainterColorSet.Channel1.b, a: PainterColorSet.Channel1.a});
        let [channel2, changeChannel2] = useState<Color>({r: PainterColorSet.Channel2.r, g: PainterColorSet.Channel2.g, b: PainterColorSet.Channel2.b, a: PainterColorSet.Channel2.a});

        // This defines aspects of the components.
        const {children, ...otherProps} = props || {};

        // This gets the original component that we may alter and return.
        var result : JSX.Element = Component();
        // It is important that we coordinate how to handle the tool options panel because it is possibile to create a mod that works for your mod but prevents others from doing the same thing.
        // If show icon add new section with title, and one button. 
        if (toolActive) {
            result.props.children?.unshift(
                <>
                    <VanillaComponentResolver.instance.Section title={translate( "Recolor.SECTION_TITLE[InfoRowTitle]",locale["Recolor.SECTION_TITLE[InfoRowTitle]"])}> 
                        <>
                            <VanillaComponentResolver.instance.ToolButton
                                src={singleSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                selected = {SingleInstance || ColorPainterSelectionType == 1}
                                multiSelect = {false}   // I haven't tested any other value here 
                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[SingleInstance]",locale["Recolor.TOOLTIP_TITLE[SingleInstance]"]), translate("Recolor.TOOLTIP_DESCRIPTION[SingleInstance]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SingleInstance]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("SingleInstance")}
                            />
                            { ColorPainterSelectionType == 0 && (
                            <VanillaComponentResolver.instance.ToolButton
                                src={matchingSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                selected = {!SingleInstance}
                                multiSelect = {false}   // I haven't tested any other value here
                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[Matching]",locale["Recolor.TOOLTIP_TITLE[Matching]"]), translate("Recolor.TOOLTIP_DESCRIPTION[Matching]" ,locale["Recolor.TOOLTIP_DESCRIPTION[Matching]"]))}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("Matching")}
                            />)}
                            <VanillaComponentResolver.instance.ToolButton
                                src={copySrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColorSet]" ,locale["Recolor.TOOLTIP_DESCRIPTION[CopyColorSet]"])}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("ColorPainterCopyColorSet")}
                            />
                            { CanPasteColorSet && (
                                <VanillaComponentResolver.instance.ToolButton
                                    src={pasteSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    multiSelect = {false}   // I haven't tested any other value here
                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColorSet]", locale["Recolor.TOOLTIP_DESCRIPTION[PasteColorSet]"])}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => { handleClick("ColorPainterPasteColorSet"); changeChannel0(CopiedColorSet.Channel0); changeChannel1(CopiedColorSet.Channel1); changeChannel2(CopiedColorSet.Channel2); }}
                                />
                            )}
                        </>
                        </VanillaComponentResolver.instance.Section>
                    { ColorPainterSelectionType == 1 && (
                        <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[Radius]",locale["Recolor.SECTION_TITLE[Radius]"])}>
                            <VanillaComponentResolver.instance.ToolButton tooltip={translate("Recolor.TOOLTIP_DESCRIPTION[DecreaseRadius]" ,locale["Recolor.TOOLTIP_DESCRIPTION[DecreaseRadius]"])} onSelect={() => handleClick("DecreaseRadius")} src={arrowDownSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton}></VanillaComponentResolver.instance.ToolButton>
                            <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{ Radius + " m"}</div>
                            <VanillaComponentResolver.instance.ToolButton tooltip={translate("Recolor.TOOLTIP_DESCRIPTION[IncreaseRadius]" ,locale["Recolor.TOOLTIP_DESCRIPTION[IncreaseRadius]"])} onSelect={() => handleClick("IncreaseRadius")} src={arrowUpSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton} ></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    
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
                    {ColorPainterSelectionType == 1 && (
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
                    <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[ColorSet]", locale["Recolor.SECTION_TITLE[ColorSet]"])}>
                        <div className={styles.columnGroup}>
                            <div className={styles.rowGroup}>
                                <VanillaComponentResolver.instance.ColorField className={ColorFieldTheme.colorField + " " + styles.rcColorField} value={channel0} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(0, e); changeChannel0(e); }}/>
                            </div>
                            <div className={styles.rowGroup}>
                                <VanillaComponentResolver.instance.ToolButton
                                    src={copySrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    multiSelect = {false}   // I haven't tested any other value here
                                    disabled = {false}      
                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColor]",locale["Recolor.TOOLTIP_DESCRIPTION[CopyColor]"])}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => copyColor(PainterColorSet.Channel0)}
                                />
                                {CanPasteColor && (
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={pasteSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        multiSelect = {false}   // I haven't tested any other value here
                                        disabled = {false}      
                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColor]",locale["Recolor.TOOLTIP_DESCRIPTION[PasteColor]"])}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => { handleChannelClick("ColorPainterPasteColor", 0); changeChannel0(CopiedColor);} }
                                    />
                                )}
                            </div>
                        </div>
                        <div className={styles.columnGroup}>
                            <div className={styles.rowGroup}>
                                <VanillaComponentResolver.instance.ColorField className={ColorFieldTheme.colorField + " " + styles.rcColorField} value={channel1} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(1, e); changeChannel1(e);}}/>
                            </div>
                            <div className={styles.rowGroup}>
                                <VanillaComponentResolver.instance.ToolButton
                                    src={copySrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    multiSelect = {false}   // I haven't tested any other value here
                                    disabled = {false}      
                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColor]",locale["Recolor.TOOLTIP_DESCRIPTION[CopyColor]"])}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => copyColor(PainterColorSet.Channel1)}
                                />
                                {CanPasteColor && (
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={pasteSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        multiSelect = {false}   // I haven't tested any other value here
                                        disabled = {false}      
                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColor]",locale["Recolor.TOOLTIP_DESCRIPTION[PasteColor]"])}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => { handleChannelClick("ColorPainterPasteColor", 1); changeChannel1(CopiedColor);}}
                                    />
                                )}
                            </div>
                        </div>
                        <div className={styles.columnGroup}>
                            <div className={styles.rowGroup}>
                                <VanillaComponentResolver.instance.ColorField className={ColorFieldTheme.colorField + " " + styles.rcColorField} value={channel2} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(2, e); changeChannel2(e); }}/>                                            
                            </div>
                            <div className={styles.rowGroup}>
                                <VanillaComponentResolver.instance.ToolButton
                                    src={copySrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    multiSelect = {false}   // I haven't tested any other value here
                                    disabled = {false}      
                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColor]",locale["Recolor.TOOLTIP_DESCRIPTION[CopyColor]"])}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => copyColor(PainterColorSet.Channel2)}
                                />
                                {CanPasteColor && (
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={pasteSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        multiSelect = {false}   // I haven't tested any other value here
                                        disabled = {false}      
                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColor]",locale["Recolor.TOOLTIP_DESCRIPTION[PasteColor]"])}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => { handleChannelClick("ColorPainterPasteColor", 2); changeChannel2(CopiedColor);}}
                                    />
                                )}
                            </div>
                        </div>
                    </VanillaComponentResolver.instance.Section> 
                </>
            );
        }

        return result;
    };
}