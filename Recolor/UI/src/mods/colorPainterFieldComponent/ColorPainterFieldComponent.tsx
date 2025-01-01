
import styles from "../Domain/ColorFields.module.scss";
import classNames from "classnames";
import {getModule} from "cs2/modding";
import { useLocalization } from "cs2/l10n";
import { Color, Theme } from "cs2/bindings";
import { bindValue, trigger, useValue } from "cs2/api";
import { RecolorSet } from "mods/Domain/RecolorSet";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";

const uilStandard =                          "coui://uil/Standard/";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const offSrc =                          uilStandard + "Off.svg";
const onSrc =                           uilStandard + "On.svg";

const PainterColorSet$ = bindValue<RecolorSet>(mod.id, "PainterColorSet");
const CanPasteColor$ = bindValue<boolean>(mod.id, "CanPasteColor");

const toggleChannelEvent =              "ToggleChannel";

function copyColor(color : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "CopyColor", color);
}

const ColorFieldTheme: Theme | any = getModule(
    "game-ui/common/input/color-picker/color-field/color-field.module.scss",
    "classes"
)

function changeColor(channel : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangePainterColor", channel, newColor);
}

function handleChannelClick(eventName : string, channel : number) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName, channel);
}

export const ColorPainterFieldComponent = (props : { channel : number }) => {
    

    const PainterColorSet = useValue(PainterColorSet$);    
    const CanPasteColor = useValue(CanPasteColor$);    
        
    const ChannelToggleValue : number = (PainterColorSet.States[0]? 1:0) + (PainterColorSet.States[1]? 1:0) + (PainterColorSet.States[2]? 1:0)


    // translation handling. Translates using locale keys that are defined in C# or fallback string here.
    const { translate } = useLocalization();

    return (
        <>
            <div className={styles.columnGroup}>
                <div className={styles.rowGroup}>
                    {PainterColorSet.States[props.channel]? (
                        <VanillaComponentResolver.instance.ColorField className={classNames(ColorFieldTheme.colorField,styles.rcColorField)} value={PainterColorSet.Channels[props.channel]} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} onChange={(e) => {changeColor(props.channel, e); }}/>
                    ):(
                        <span className={styles.rcColorField}></span>
                    )
                    }       
                </div>
                <div className={styles.rowGroup}>
                    { (ChannelToggleValue > 1 || PainterColorSet.States[props.channel] == false) && (
                        <VanillaComponentResolver.instance.ToolButton
                            src={PainterColorSet.States[props.channel]? onSrc : offSrc}
                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                            multiSelect={false}
                            selected={PainterColorSet.States[props.channel]}
                            disabled={false}
                            tooltip={translate("Recolor.TOOLTIP_DESCRIPTION[ToggleChannel]", locale["Recolor.TOOLTIP_DESCRIPTION[ToggleChannel]"])}
                            className={VanillaComponentResolver.instance.toolButtonTheme.button}
                            onSelect={() => { handleChannelClick(toggleChannelEvent, props.channel)}}
                        />
                    )}
                    {PainterColorSet.States[props.channel] && (
                        <>                                        
                            <VanillaComponentResolver.instance.ToolButton
                                src={copySrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                multiSelect = {false}   // I haven't tested any other value here
                                disabled = {false}      
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColor]",locale["Recolor.TOOLTIP_DESCRIPTION[CopyColor]"])}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => copyColor(PainterColorSet.Channels[props.channel])}
                            />
                            {CanPasteColor && (
                                <VanillaComponentResolver.instance.ToolButton
                                    src={pasteSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    multiSelect = {false}   // I haven't tested any other value here
                                    disabled = {false}      
                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColor]",locale["Recolor.TOOLTIP_DESCRIPTION[PasteColor]"])}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => { handleChannelClick("ColorPainterPasteColor", props.channel);} }
                                />
                            )}
                        </>
                    )}
                </div>
            </div>
        </>
    );
}