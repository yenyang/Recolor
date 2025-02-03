import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";

import classNames from "classnames";
import { ColorFieldTheme, convertColorToHexaDecimal, convertHexaDecimalToColor, copyColor, StringInputField, StringInputFieldStyle } from "mods/SIPColorComponent/SIPColorComponent";
import { bindValue, trigger, useValue } from "cs2/api";
import mod from "../../../../mod.json";
import { Color } from "cs2/bindings";
import styles from ".././ColorFields.module.scss";
import { useState } from "react";
import { useLocalization } from "cs2/l10n";
import locale from "../../lang/en-US.json";
import { FocusDisabled } from "cs2/input";
import { getModule } from "cs2/modding";
import { SwatchUIData } from "./SwatchUIData";

const uilStandard =                          "coui://uil/Standard/";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const minusSrc =            uilStandard + "Minus.svg";

const CanPasteColor$ = bindValue<boolean>(mod.id, "CanPasteColor");
const ShowHexaDecimals$ = bindValue<boolean>(mod.id, 'ShowHexaDecimals');
const PaletteCreationMenuData$ = bindValue<SwatchUIData[]>(mod.id, "PaletteCreationMenuData");

function changeColor(index : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangePaletteColor", index, newColor);
}

function handleChannelClick(eventName : string, index : number) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName, index);
}

function changeValue(event:string, index:number, value : number) {
    trigger(mod.id, event, index, value);
}

const SliderField : any = getModule("game-ui/editor/widgets/fields/number-slider-field.tsx", "FloatSliderField");

export const SwatchComponent = (props: {info: SwatchUIData}) => {
    const CanPasteColor = useValue(CanPasteColor$);    
    const ShowHexaDecimals = useValue(ShowHexaDecimals$);
    const PaletteCreationMenuData = useValue(PaletteCreationMenuData$);

    let [textInput, setTextInput] = useState(convertColorToHexaDecimal(props.info.SwatchColor));
    let [validInput, setValidInput] = useState(true);
    let [updateHexaDecimal, setUpdateHexaDecimal] = useState(props.info.SwatchColor);

    function HandleTextInput () {
        if (textInput.length == 9 &&  /^#[0-9A-F]{6}[0-9a-f]{0,2}$/i.test(textInput)) 
        { 
            changeColor(props.info.Index, convertHexaDecimalToColor(textInput));
            setValidInput(true)
        }
        else if (textInput.length == 7 && /^#[0-9A-F]{6}[0-9a-f]{0,2}$/i.test(textInput+"ff")) 
        {
            changeColor(props.info.Index, convertHexaDecimalToColor(textInput+"ff"));      
            setTextInput(textInput+"ff");      
            setValidInput(true);
        } else 
        {
            setValidInput(false);          
        } 
    }

    if (props.info.SwatchColor !== updateHexaDecimal) 
    {
        setTextInput(convertColorToHexaDecimal(props.info.SwatchColor));
        setUpdateHexaDecimal(props.info.SwatchColor);
        setValidInput(true);
    }
    
    const { translate } = useLocalization();

    return (
        <>
            <div className={classNames(styles.rowGroup, styles.centered)}>
                <div className={styles.columnGroup}>
                    <div className={styles.rowGroup}>
                        <VanillaComponentResolver.instance.ColorField 
                            className={classNames(ColorFieldTheme.colorField, styles.rcColorField)} 
                            value={props.info.SwatchColor} 
                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} 
                            onChange={(e) => {changeColor(props.info.Index, e); setTextInput(convertColorToHexaDecimal(e))}}
                            alpha={1}
                        />
                    </div>
                    <div className={styles.rowGroup}>
                        { PaletteCreationMenuData.length > 1 && (
                            <VanillaComponentResolver.instance.ToolButton
                                src={minusSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                tooltip = {"Remove"}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => {}}
                            />
                        )}
                        <VanillaComponentResolver.instance.ToolButton
                            src={copySrc}
                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}    
                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColor]",locale["Recolor.TOOLTIP_DESCRIPTION[CopyColor]"])}
                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                            onSelect={() => copyColor(props.info.SwatchColor)}
                        />
                        { CanPasteColor && (
                            <VanillaComponentResolver.instance.ToolButton
                                src={pasteSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColor]",locale["Recolor.TOOLTIP_DESCRIPTION[PasteColor]"])}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => {handleChannelClick("PasteSwatchColor", props.info.Index); setUpdateHexaDecimal(props.info.SwatchColor);}}
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
                <div className={styles.columnGroup}>
                    <div className={styles.SliderFieldWidth}>
                        <SliderField value={props.info.ProbabilityWeight} min={1} max={200} fractionDigits={0} onChange={(e: number) => {changeValue("SetProbabilityWeight", props.info.Index ,e)}}></SliderField>
                    </div> 
                    <span className={styles.belowSwapButton}></span>
                </div>
            </div>
        </>
    );
} 