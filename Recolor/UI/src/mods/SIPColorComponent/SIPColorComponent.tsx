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
import { useState } from "react";
import { FocusDisabled } from "cs2/input";
import { Scope } from "mods/Domain/Scope";

// These contain the coui paths to Unified Icon Library svg assets
const uilStandard =                          "coui://uil/Standard/";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const resetSrc =                     uilStandard + "Reset.svg";

const CurrentColorSet$ = bindValue<RecolorSet>(mod.id, "CurrentColorSet");
const CanPasteColor$ = bindValue<boolean>(mod.id, "CanPasteColor");
const CanResetSingleChannels$ = bindValue<boolean>(mod.id, "CanResetSingleChannels");
const MatchesVanillaColorSet$ = bindValue<boolean[]>(mod.id, 'MatchesVanillaColorSet');
const ShowHexaDecimals$ = bindValue<boolean>(mod.id, 'ShowHexaDecimals');

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

export const StringInputField : any = getModule(
    "game-ui/editor/widgets/fields/string-input-field.tsx",
    "StringInputField"
)

export const StringInputFieldStyle : Theme | any = getModule(
    "game-ui/debug/widgets/fields/input-field/input-field.module.scss",
    "classes"
)

function changeColor(channel : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangeColor", channel, newColor);
}

export function convertColorToHexaDecimal(color: Color ) : string {
    const r = Math.round(color.r * 255).toString(16);
    const g = Math.round(color.g * 255).toString(16);
    const b = Math.round(color.b * 255).toString(16);
    const a = Math.round(color.a * 255).toString(16);
    return "#"+r+g+b+a;
}

export function convertHexaDecimalToColor(input: string) : Color 
{
    let r:number = Math.min(Math.max(parseInt(input.slice(1,3), 16) / 255, 0), 1);
    let g:number = Math.min(Math.max(parseInt(input.slice(3,5), 16) / 255, 0), 1);
    let b:number = Math.min(Math.max(parseInt(input.slice(5,7), 16) / 255, 0), 1);
    let a:number = Math.min(Math.max(parseInt(input.slice(7,9), 16) / 255, 0), 1);

    let color : Color = {
        r: r,
        g: g,
        b: b,
        a: a,
    };
    return color;
}

export const SIPColorComponent = (props : { channel : number }) => {
    const { translate } = useLocalization();

    const CurrentColorSet = useValue(CurrentColorSet$);   
    const CanPasteColor = useValue(CanPasteColor$);    
    const MatchesVanillaColorSet : boolean[] = useValue(MatchesVanillaColorSet$);
    const ShowHexaDecimals = useValue(ShowHexaDecimals$);
    const CanResetSingleChannels = useValue(CanResetSingleChannels$);
    
    let [textInput, setTextInput] = useState(convertColorToHexaDecimal(CurrentColorSet.Channels[props.channel]));
    let [validInput, setValidInput] = useState(true);
    let [updateHexaDecimal, setUpdateHexaDecimal] = useState(CurrentColorSet.Channels[props.channel]);

    function HandleTextInput () {
        if (textInput.length == 9 &&  /^#[0-9A-F]{6}[0-9a-f]{0,2}$/i.test(textInput)) 
        { 
            changeColor(props.channel, convertHexaDecimalToColor(textInput));
            setValidInput(true)
        }
        else if (textInput.length == 7 && /^#[0-9A-F]{6}[0-9a-f]{0,2}$/i.test(textInput+"ff")) 
        {
            changeColor(props.channel, convertHexaDecimalToColor(textInput+"ff"));      
            setTextInput(textInput+"ff");      
            setValidInput(true);
        } else 
        {
            setValidInput(false);          
        } 
    }

    if (CurrentColorSet.Channels[props.channel] !== updateHexaDecimal) 
    {
        setTextInput(convertColorToHexaDecimal(CurrentColorSet.Channels[props.channel]));
        setUpdateHexaDecimal(CurrentColorSet.Channels[props.channel]);
        setValidInput(true);
    }

    return (
        <div className={styles.columnGroup}>
            <div className={styles.rowGroup}>
                <VanillaComponentResolver.instance.ColorField 
                    className={classNames(ColorFieldTheme.colorField, styles.rcColorField)} 
                    value={CurrentColorSet.Channels[props.channel]} 
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} 
                    onChange={(e) => {changeColor(props.channel, e); setTextInput(convertColorToHexaDecimal(e))}}
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
                        onSelect={() => {handleChannelClick("PasteColor", props.channel); setUpdateHexaDecimal(CurrentColorSet.Channels[props.channel]);}}
                    />
                )}
                { !MatchesVanillaColorSet[props.channel] && CanResetSingleChannels && (
                    <VanillaComponentResolver.instance.ToolButton
                        src={resetSrc}
                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                        multiSelect = {false}   // I haven't tested any other value here
                        disabled = {false}      
                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ResetColor]",locale["Recolor.TOOLTIP_DESCRIPTION[ResetColor]"])}
                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                        onSelect={() => {handleChannelClick("ResetColor", props.channel); setUpdateHexaDecimal(CurrentColorSet.Channels[props.channel]);}}
                    />
                )}
            </div>
            { ShowHexaDecimals && (
                <FocusDisabled disabled={true}>
                    <div className={styles.rowGroup}>
                        <StringInputField
                            value={textInput.replace(/[\r\n]+/gm, '')}
                            disabled ={false}
                            onChange={ (e : string) => { setTextInput(e); }}
                            onChangeEnd={HandleTextInput}
                            className={validInput?  classNames(StringInputFieldStyle.textInput, styles.rcColorFieldInput) : classNames(StringInputFieldStyle.textInput, styles.rcColorFieldInput, styles.invalidFieldInput)}
                            multiline={false}
                            maxLength={9}
                        />
                    </div>
                </FocusDisabled>
            )}
        </div>
    );
}