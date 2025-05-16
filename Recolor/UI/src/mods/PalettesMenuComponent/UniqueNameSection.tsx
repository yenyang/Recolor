import { trigger} from "cs2/api";
import { useState } from "react";
import mod from "../../../mod.json";
import { InfoSection } from "mods/RecolorMainPanel/RecolorMainPanel";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { StringInputField, StringInputFieldStyle } from "mods/SIPColorComponent/SIPColorComponent";
import classNames from "classnames";
import styles from "../Domain/ColorFields.module.scss";
import { MenuType } from "mods/Domain/MenuType";
import { useLocalization } from "cs2/l10n";
import locale from "../lang/en-US.json";

const invalidNames = ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LTP4", "LTP5", "LTP6", "LTP7", "LTP8", "LTP9"];
const invalidCharacters = ['/', '<', '>', ':', '"', "\\", "|", "?", "*", "." ]

export const UniqueNameSectionComponent = (props: {uniqueName: string, uniqueNameType: MenuType}) => {
    let [uniqueNameInput, setTextInput] = useState(props.uniqueName);
    let [validInput, setValidInput] = useState(true);
    let [updateText, setUpdateText] = useState(props.uniqueName);

    const { translate } = useLocalization();
    
    function HandleTextInput () {
            for (let i=0; i<invalidCharacters.length; i++) 
            {
                if (uniqueNameInput.includes(invalidCharacters[i])) 
                {
                    setValidInput(false);
                    return;
                }
            }

            for (let i=0; i<invalidNames.length; i++) 
            {
                if (uniqueNameInput.trim().toUpperCase() == invalidNames[i]) 
                {
                    setValidInput(false);
                    return;
                }
            }

            setValidInput(true);
            trigger(mod.id, "ChangeUniqueName", uniqueNameInput.trim(), props.uniqueNameType);
    }
        
    if (props.uniqueName !== updateText) 
    {
        setTextInput(props.uniqueName);
        setUpdateText(props.uniqueName);
    }

    return (
        <>
            <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[UniqueName]", locale["Recolor.SECTION_TITLE[UniqueName]"])}>
                    <StringInputField 
                        value={uniqueNameInput.replace(/[\r\n]+/gm, '')}
                        disabled ={false}
                        onChange={ (e : string) => { setTextInput(e); }}
                        onChangeEnd={HandleTextInput}
                        className={validInput?  classNames(StringInputFieldStyle.textInput, styles.nameFieldInput) : classNames(StringInputFieldStyle.textInput, styles.nameFieldInput, styles.invalidFieldInput)}
                        multiline={false}
                        maxLength={32}
                    ></StringInputField>
                </VanillaComponentResolver.instance.Section>
            </InfoSection>
        </>
    );
}