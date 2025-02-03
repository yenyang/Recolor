import classNames from "classnames";
import { StringInputField, StringInputFieldStyle } from "mods/SIPColorComponent/SIPColorComponent";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { useState } from "react";
import styles from ".././ColorFields.module.scss";

export const PaletteLocalizationSet = (props : { localeCode : string}) => {

    let [localeKey, setLocaleKey] = useState(props.localeCode);
    let [textInput, setTextInput] = useState("");
    let [validInput, setValidInput] = useState(true);

    function HandleTextInput () {
       setValidInput(true);
    }

    return (
        <>
            <VanillaComponentResolver.instance.Section title={"Locale Code"}>
                <StringInputField 
                    value={localeKey.replace(/[\r\n]+/gm, '')}
                    disabled ={false}
                    onChange={ (e : string) => { setLocaleKey(e); }}
                    onChangeEnd={HandleTextInput}
                    className={validInput?  classNames(StringInputFieldStyle.textInput, styles.nameFieldInput) : classNames(StringInputFieldStyle.textInput, styles.nameFieldInput, styles.invalidFieldInput)}
                    multiline={false}
                    maxLength={7}
                ></StringInputField>
            </VanillaComponentResolver.instance.Section>
            <VanillaComponentResolver.instance.Section title={"Localized Name"}>
                <StringInputField 
                    value={textInput.replace(/[\r\n]+/gm, '')}
                    disabled ={false}
                    onChange={ (e : string) => { setTextInput(e); }}
                    onChangeEnd={HandleTextInput}
                    className={validInput?  classNames(StringInputFieldStyle.textInput, styles.nameFieldInput) : classNames(StringInputFieldStyle.textInput, styles.nameFieldInput, styles.invalidFieldInput)}
                    multiline={false}
                    maxLength={32}
                ></StringInputField>
            </VanillaComponentResolver.instance.Section>
            <VanillaComponentResolver.instance.Section title={"Localized Description"}>
                <StringInputField 
                    value={textInput}
                    disabled ={false}
                    onChange={ (e : string) => { setTextInput(e); }}
                    onChangeEnd={HandleTextInput}
                    className={validInput?  classNames(StringInputFieldStyle.textInput, styles.nameFieldInput) : classNames(StringInputFieldStyle.textInput, styles.nameFieldInput, styles.invalidFieldInput)}
                    multiline={true}
                    maxLength={32}
                ></StringInputField>
            </VanillaComponentResolver.instance.Section>
        </>
    );
}