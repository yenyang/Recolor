import styles from "../Domain/ColorFields.module.scss";
import { Color, Theme } from "cs2/bindings";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { RecolorSet } from "mods/Domain/RecolorSet";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import {getModule} from "cs2/modding";
import classNames from "classnames";

// These contain the coui paths to Unified Icon Library svg assets
const uilStandard =                          "coui://uil/Standard/";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const resetSrc =                     uilStandard + "Reset.svg";

const CurrentColorSet$ = bindValue<RecolorSet>(mod.id, "CurrentColorSet");
const CanPasteColor$ = bindValue<boolean>(mod.id, "CanPasteColor");
const SingleInstance$ = bindValue<boolean>(mod.id, 'SingleInstance');
const DisableSingleInstance$ = bindValue<boolean>(mod.id, 'DisableSingleInstance');
const MatchesVanillaColorSet$ = bindValue<boolean[]>(mod.id, 'MatchesVanillaColorSet');

function copyColor(color : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "CopyColor", color);
}

function handleChannelClick(eventName : string, channel : number) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName, channel);
}

const ColorFieldTheme: Theme | any = getModule(
    "game-ui/common/input/color-picker/color-field/color-field.module.scss",
    "classes"
)

function changeColor(channel : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangeColor", channel, newColor);
}

export const SIPColorComponent = (props : { channel : number }) => {
    const { translate } = useLocalization();

    const CurrentColorSet = useValue(CurrentColorSet$);   
    const CanPasteColor = useValue(CanPasteColor$);    
    const SingleInstance = useValue(SingleInstance$);
    const DisableSingleInstance = useValue(DisableSingleInstance$);
    const MatchesVanillaColorSet : boolean[] = useValue(MatchesVanillaColorSet$);        

    return (
        <div className={styles.columnGroup}>
            <div className={styles.rowGroup}>
                <VanillaComponentResolver.instance.ColorField 
                    className={classNames(ColorFieldTheme.colorField, styles.rcColorField)} 
                    value={CurrentColorSet.Channels[props.channel]} 
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} 
                    onChange={(e) => {changeColor(props.channel, e);}}
                    alpha={1}
                />
            </div>
            <div className={styles.rowGroup}>
                <VanillaComponentResolver.instance.ToolButton
                    src={copySrc}
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                    multiSelect = {false}   // I haven't tested any other value here
                    disabled = {false}      
                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColor]",locale["Recolor.TOOLTIP_DESCRIPTION[CopyColor]"])}
                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                    onSelect={() => copyColor(CurrentColorSet.Channels[props.channel])}
                />
                { CanPasteColor && (
                    <VanillaComponentResolver.instance.ToolButton
                        src={pasteSrc}
                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                        multiSelect = {false}   // I haven't tested any other value here
                        disabled = {false}      
                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColor]",locale["Recolor.TOOLTIP_DESCRIPTION[PasteColor]"])}
                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                        onSelect={() => handleChannelClick("PasteColor", props.channel)}
                    />
                )}
                { !MatchesVanillaColorSet[2] && (!SingleInstance || DisableSingleInstance) && (
                    <VanillaComponentResolver.instance.ToolButton
                        src={resetSrc}
                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                        multiSelect = {false}   // I haven't tested any other value here
                        disabled = {false}      
                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ResetColor]",locale["Recolor.TOOLTIP_DESCRIPTION[ResetColor]"])}
                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                        onSelect={() => handleChannelClick("ResetColor", props.channel)}
                    />
                )}
            </div>
        </div>
    );
}