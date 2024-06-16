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
const singleSrc =                       uilStandard + "Dot.svg";
const radiusSrc =                        uilStandard + "Circle.svg";

const PainterColorSet$ = bindValue<ColorSet>(mod.id, "PainterColorSet");

const ColorFieldTheme: Theme | any = getModule(
    "game-ui/common/input/color-picker/color-field/color-field.module.scss",
    "classes"
)

function changeColor(channel : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangePainterColor", channel, newColor);
}


function handleClick(eventName : string) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName);
}


export const ColorPainterSectionComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {
        // These get the value of the bindings.
        const toolActive = useValue(tool.activeTool$).id == "ColorPainterTool";
        
        const PainterColorSet = useValue(PainterColorSet$);
        
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
                    { ( false && 
                        <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[ColorSet]", locale["Recolor.SECTION_TITLE[ColorSet]"])}>
                            <VanillaComponentResolver.instance.ToolButton
                                src={singleSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                disabled = {false}      
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColor]",locale["Recolor.TOOLTIP_DESCRIPTION[CopyColor]"])}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleClick("ColorPainterSingleSelection")}
                            />
                        </VanillaComponentResolver.instance.Section>
                    )}
                    <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[ColorSet]", locale["Recolor.SECTION_TITLE[ColorSet]"])}>
                        <div className={styles.columnGroup}>
                            <VanillaComponentResolver.instance.ColorField className={ColorFieldTheme.colorField + " " + styles.rcColorField} value={channel0} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(0, e); changeChannel0(e);}}/>
                        </div>
                        <div className={styles.columnGroup}>
                            <VanillaComponentResolver.instance.ColorField className={ColorFieldTheme.colorField + " " + styles.rcColorField} value={channel1} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(1, e); changeChannel1(e);}}/>
                        </div>
                        <div className={styles.columnGroup}>
                            <VanillaComponentResolver.instance.ColorField className={ColorFieldTheme.colorField + " " + styles.rcColorField} value={channel2} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(2, e); changeChannel2(e);}}/>                                            
                        </div>
                    </VanillaComponentResolver.instance.Section> 
                </>
            );
        }

        return result;
    };
}