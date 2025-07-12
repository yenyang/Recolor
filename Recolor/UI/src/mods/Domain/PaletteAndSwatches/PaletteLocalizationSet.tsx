import classNames from "classnames";
import { StringInputField, StringInputFieldStyle } from "mods/SIPColorComponent/SIPColorComponent";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { useState } from "react";
import styles from ".././ColorFields.module.scss";
import { bindValue, trigger, useValue } from "cs2/api";
import mod from "mod.json";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";
import { useLocalization } from "cs2/l10n";
import locale from "../../lang/en-US.json";
import { LocalizationUIData } from "./LocalizationUIData";
import { MenuType } from "../MenuType";
import panelStyles from "../../PalettesMenuComponent/PaletteMenuStyles.module.scss";


const SupportedLocaleCodes$ = bindValue<string[]>(mod.id, "SupportedLocaleCodes");
const LocalizationUIDatas$ = bindValue<LocalizationUIData[][]>(mod.id, "LocalizationDatas");

const uilStandard =                         "coui://uil/Standard/";
const minusSrc =                         uilStandard + "Minus.svg";

const dropDownThemes = getModule('game-ui/editor/themes/editor-dropdown.module.scss', 'classes');

export const PaletteLocalizationSet = (props : { localizationData : LocalizationUIData, menu : MenuType, index: number}) => {

    const SupportedLocaleCodes = useValue(SupportedLocaleCodes$);    
    const LocalizationUIDatas = useValue(LocalizationUIDatas$);

    let [localizedNameInput, setLocalizedNameInput] = useState(props.localizationData.LocalizedName);    
    let [localizedDescriptionInput, setLocalizedDescriptionInput] = useState(props.localizationData.LocalizedDescription);
    let [updateName, setUpdateName] = useState(props.localizationData.LocalizedName);
    let [updateDescription, setUpdateDescription] = useState(props.localizationData.LocalizedDescription);

    function IsLocaleCodeAlreadySelected(localeCode: string) 
    {
        for (let i=0; i<LocalizationUIDatas[props.menu].length; i++) 
        {
            if (localeCode == LocalizationUIDatas[props.menu][i].LocaleCode) 
            {
                return true;
            }
        }

        return false;
    }

    if (updateName !== props.localizationData.LocalizedName) 
    {
        setLocalizedNameInput(props.localizationData.LocalizedName);
        setUpdateName(props.localizationData.LocalizedName);
    }

    if (updateDescription !== props.localizationData.LocalizedDescription) 
    {
        setLocalizedDescriptionInput(props.localizationData.LocalizedDescription);
        setUpdateDescription(props.localizationData.LocalizedDescription);
    }
    
    const { translate } = useLocalization();

    return (
        <>
            <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[LocaleCode]" ,locale["Recolor.SECTION_TITLE[LocaleCode]"])}>
                <Dropdown 
                    theme = {dropDownThemes}
                    content={                    
                        SupportedLocaleCodes.map((localeCode) => (
                            <>
                                { (IsLocaleCodeAlreadySelected(localeCode) == false || LocalizationUIDatas[props.menu][props.index].LocaleCode == localeCode) && 
                                    <DropdownItem value={localeCode} className={dropDownThemes.dropdownItem} selected={localeCode==props.localizationData.LocaleCode} onChange={(value: string) =>  trigger(mod.id, "ChangeLocaleCode", value, props.index)}>
                                        <div className={styles.localeCodeWidth}>{localeCode}</div>
                                    </DropdownItem>
                                }
                            </>
                        ))
                    }>
                    <DropdownToggle disabled={false}>
                        <div className={styles.localeCodeWidth}>{props.localizationData.LocaleCode}</div>
                    </DropdownToggle>
                </Dropdown>
                <span className={panelStyles.smallSpacer}></span>
                { LocalizationUIDatas[props.menu].length > 1 ?
                    <VanillaComponentResolver.instance.ToolButton src={minusSrc}  tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[RemoveALocale]" ,locale["Recolor.TOOLTIP_DESCRIPTION[RemoveALocale]"])}        className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}    
                            onSelect={() => { trigger(mod.id, "RemoveLocale", LocalizationUIDatas[props.menu][props.index].LocaleCode);}}
                    /> : 
                    <span className={styles.ButtonWidth}></span>
                }
            </VanillaComponentResolver.instance.Section>
            <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[LocalizedName]" ,locale["Recolor.SECTION_TITLE[LocalizedName]"])}>
                <StringInputField 
                    value={localizedNameInput?.replace(/[\r\n]+/gm, '')}
                    disabled ={false}
                    onChange={ (e : string) => { setLocalizedNameInput(e); }}
                    onChangeEnd={trigger(mod.id, "ChangeLocalizedName", props.menu, props.index, localizedNameInput)}
                    className={ classNames(StringInputFieldStyle.textInput, styles.nameFieldInput)}
                    multiline={false}
                    maxLength={props.menu == MenuType.Palette? 32 : 15}
                ></StringInputField>
            </VanillaComponentResolver.instance.Section>
            <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[LocalizedDescription]", locale["Recolor.SECTION_TITLE[LocalizedDescription]"])}>
                <StringInputField 
                    value={localizedDescriptionInput}
                    disabled ={false}
                    onChange={ (e : string) => { setLocalizedDescriptionInput(e); }}
                    onChangeEnd={trigger(mod.id, "ChangeLocalizedDescription", props.menu, props.index, localizedDescriptionInput)}
                    className={classNames(StringInputFieldStyle.textInput, styles.descriptionFieldInput)}
                    multiline={true}
                    maxLength={128}
                ></StringInputField>
            </VanillaComponentResolver.instance.Section>
        </>
    );
}