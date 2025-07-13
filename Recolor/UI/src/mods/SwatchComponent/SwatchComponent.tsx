import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";

import classNames from "classnames";
import { ColorFieldTheme, convertColorToHexaDecimal, convertHexaDecimalToColor, copyColor, StringInputField, StringInputFieldStyle } from "mods/SIPColorComponent/SIPColorComponent";
import { bindValue, trigger, useValue } from "cs2/api";
import mod from "../../../mod.json";
import { Color } from "cs2/bindings";
import styles from "../Domain/ColorFields.module.scss";
import { useState } from "react";
import { useLocalization } from "cs2/l10n";
import locale from "../lang/en-US.json";
import { FocusDisabled } from "cs2/input";
import { getModule } from "cs2/modding";
import { SwatchUIData } from "../Domain/PaletteAndSwatches/SwatchUIData";

/*
import copySrc from "images/uilStandard/RectangleCopy.svg";
import pasteSrc from "images/uilStandard/RectanglePaste.svg";
import minusSrc from "images/uilStandard/Minus.svg";
import randomSrc from "images/uilStandard/Dice.svg";
*/

const uilStandard =                          "coui://uil/Standard/";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const minusSrc =            uilStandard + "Minus.svg";
const randomSrc =           uilStandard + "Dice.svg";


const CanPasteColor$ = bindValue<boolean>(mod.id, "CanPasteColor");
const ShowHexaDecimals$ = bindValue<boolean>(mod.id, 'ShowHexaDecimals');
const Swatches$ = bindValue<SwatchUIData[]>(mod.id, "Swatches");

function changeColor(index : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangeSwatchColor", index, newColor);
}

function handleSwatchClick(eventName : string, index : number) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName, index);
}

function changeValue(event:string, index:number, value : number) {
    trigger(mod.id, event, index, value);
}

const SliderField : any = getModule("game-ui/editor/widgets/fields/number-slider-field.tsx", "FloatSliderField");

export const SwatchComponent = (props: {info: SwatchUIData, index: number}) => {
    const CanPasteColor = useValue(CanPasteColor$);    
    const ShowHexaDecimals = useValue(ShowHexaDecimals$);
    const Swatches = useValue(Swatches$);

    let [textInput, setTextInput] = useState(convertColorToHexaDecimal(props.info.SwatchColor));
    let [validInput, setValidInput] = useState(true);
    let [updateHexaDecimal, setUpdateHexaDecimal] = useState(props.info.SwatchColor);

    function HandleTextInput () {
        if (textInput.length == 9 &&  /^#[0-9A-F]{6}[0-9a-f]{0,2}$/i.test(textInput)) 
        { 
            changeColor(props.index, convertHexaDecimalToColor(textInput));
            setValidInput(true)
        }
        else if (textInput.length == 7 && /^#[0-9A-F]{6}$/i.test(textInput)) 
        {
            changeColor(props.index, convertHexaDecimalToColor(textInput+"ff"));      
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
                    <div className={styles.rowGroup }>
                        { Swatches.length > 2 ? (
                            <VanillaComponentResolver.instance.ToolButton
                                src={minusSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[RemoveSwatch]" , locale["Recolor.TOOLTIP_DESCRIPTION[RemoveSwatch]"])}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => {handleSwatchClick("RemoveSwatch", props.index)}}
                            />
                        ) : 
                            <span className={styles.swapButtonWidth}></span>
                        }
                        <VanillaComponentResolver.instance.ToolButton
                            src={randomSrc}
                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}    
                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[RandomizeSwatch]" ,locale["Recolor.TOOLTIP_DESCRIPTION[RandomizeSwatch]"])}
                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                            onSelect={() => handleSwatchClick("RandomizeSwatch", props.index)}
                        />
                        <VanillaComponentResolver.instance.ToolButton
                            src={copySrc}
                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}    
                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColor]",locale["Recolor.TOOLTIP_DESCRIPTION[CopyColor]"])}
                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                            onSelect={() => copyColor(props.info.SwatchColor)}
                        />
                        { CanPasteColor ? (
                            <VanillaComponentResolver.instance.ToolButton
                                src={pasteSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColor]",locale["Recolor.TOOLTIP_DESCRIPTION[PasteColor]"])}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => {handleSwatchClick("PasteSwatchColor", props.index); setUpdateHexaDecimal(props.info.SwatchColor);}}
                            />
                        ) :      
                        <span className={styles.swapButtonWidth}></span>
                        }
                        <div className={styles.paddingRightAndLeft}> 
                            <VanillaComponentResolver.instance.ColorField 
                                className={classNames(ColorFieldTheme.colorField, styles.rcColorField, styles.marginRight)} 
                                value={props.info.SwatchColor} 
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} 
                                onChange={(e) => {changeColor(props.index, e); setTextInput(convertColorToHexaDecimal(e))}}
                                alpha={1}
                            />
                        </div>
                        { ShowHexaDecimals ? (
                            <FocusDisabled disabled={true}>
                                    <StringInputField
                                        value={textInput.replace(/[\r\n]+/gm, '')}
                                        disabled ={false}
                                        onChange={ (e : string) => { setTextInput(e); }}
                                        onChangeEnd={HandleTextInput}
                                        className={validInput?  classNames(StringInputFieldStyle.textInput, styles.rcColorFieldInput,  styles.marginLeft) : classNames(StringInputFieldStyle.textInput, styles.rcColorFieldInput, styles.invalidFieldInput,  styles.marginLeft)}
                                        multiline={false}
                                        maxLength={9}
                                    />
                            </FocusDisabled>
                        ) : <span className={styles.rcColorFieldInput}></span>
                        }
                    </div>
                </div>
                <div className={styles.columnGroup}>
                    <div className={styles.SliderFieldWidth}>
                        <SliderField value={props.info.ProbabilityWeight} min={1} max={200} fractionDigits={0} onChange={(e: number) => {changeValue("ChangeProbabilityWeight", props.index ,e)}}></SliderField>
                    </div> 
                </div>
            </div>
        </>
    );
} 