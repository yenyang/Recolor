import { useLocalization } from "cs2/l10n";
import {ModuleRegistryExtend, getModule} from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { Color, Theme, tool } from "cs2/bindings";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { ColorSet } from "mods/Domain/ColorSet";
import styles from "../Domain/ColorFields.module.scss";

// These contain the coui paths to Unified Icon Library svg assets
const uilStandard =                          "coui://uil/Standard/";


const PainterColorSet$ = bindValue<ColorSet>(mod.id, "PainterColorSet");

const ColorFieldTheme: Theme | any = getModule(
    "game-ui/common/input/color-picker/color-field/color-field.module.scss",
    "classes"
)

function changeColor(channel : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangePainterColor", channel, newColor);
}

export const ColorPainterSectionComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {
        // These get the value of the bindings.
        const toolActive = useValue(tool.activeTool$).id == "ColorPainterTool";
        
        const PainterColorSet = useValue(PainterColorSet$);
        
        // translation handling. Translates using locale keys that are defined in C# or fallback string here.
        const { translate } = useLocalization();
       
        // This defines aspects of the components.
        const {children, ...otherProps} = props || {};

        // This gets the original component that we may alter and return.
        var result : JSX.Element = Component();
        // It is important that we coordinate how to handle the tool options panel because it is possibile to create a mod that works for your mod but prevents others from doing the same thing.
        // If show icon add new section with title, and one button. 
        result.props.children?.unshift(
            /* 
            Add a new section before other tool options sections with translated title based of this localization key. Localization key defined in C#.
            Add a new Tool button into that section. Selected is based on Anarchy Enabled binding. 
            Tooltip is translated based on localization key. OnSelect run callback fucntion here to trigger event. 
            Anarchy specific image source changes bases on Anarchy Enabled binding. 
            */
            <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[ColorSet]", locale["Recolor.SECTION_TITLE[ColorSet]"])}>
                <VanillaComponentResolver.instance.ColorField className={ColorFieldTheme.colorField + " " + styles.rcColorField} value={PainterColorSet.Channel0} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(0, e);}}/>
                <VanillaComponentResolver.instance.ColorField className={ColorFieldTheme.colorField + " " + styles.rcColorField} value={PainterColorSet.Channel1} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(1, e);}}/>
                <VanillaComponentResolver.instance.ColorField className={ColorFieldTheme.colorField + " " + styles.rcColorField} value={PainterColorSet.Channel2} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(2, e);}}/>                                            
            </VanillaComponentResolver.instance.Section> 
        );

        return result;
    };
}