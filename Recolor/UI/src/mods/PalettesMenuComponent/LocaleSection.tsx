import { useLocalization } from "cs2/l10n";
import { PaletteLocalizationSet } from "mods/Domain/PaletteAndSwatches/PaletteLocalizationSet";
import { InfoSection } from "mods/RecolorMainPanel/RecolorMainPanel";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { useState } from "react";
import locale from "../lang/en-US.json";
import { MenuType } from "mods/Domain/MenuType";
import { LocalizationUIData } from "mods/Domain/PaletteAndSwatches/LocalizationUIData";
import { trigger } from "cs2/api";
import mod from "mod.json";

const uilStandard =                         "coui://uil/Standard/";
const plusSrc =                         uilStandard + "Plus.svg";

export const LocaleSection = (props: {menu : MenuType, localizations : LocalizationUIData[]}) => {
    const { translate } = useLocalization();

    return (
        <>
            <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                { props.localizations.map((currentLocalization, index) => (
                    <PaletteLocalizationSet localizationData={currentLocalization} menu={props.menu} index={index} ></PaletteLocalizationSet>
                ))}
                <VanillaComponentResolver.instance.Section title={translate("Recolor.SECTION_TITLE[AddALocale]", locale["Recolor.SECTION_TITLE[AddALocale]"])}>
                    <VanillaComponentResolver.instance.ToolButton src={plusSrc}          tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[AddLocale]", locale["Recolor.TOOLTIP_DESCRIPTION[AddLocale]"])}   onSelect={() => {trigger(mod.id, "AddLocale")} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                </VanillaComponentResolver.instance.Section>
            </InfoSection>
        </>
    );
}