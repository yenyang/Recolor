import { PaletteLocalizationSet } from "mods/Domain/PaletteAndSwatches/PaletteLocalizationSet";
import { InfoSection } from "mods/RecolorMainPanel/RecolorMainPanel";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { useState } from "react";


const uilStandard =                         "coui://uil/Standard/";
const plusSrc =                         uilStandard + "Plus.svg";

export const LocaleSection = () => {
    let [locales, setLocales] = useState(["en-US"]);

    return (
        <>
            <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                { locales.map((currentLocale) => (
                    <PaletteLocalizationSet localeCode={currentLocale}></PaletteLocalizationSet>
                ))}
                <VanillaComponentResolver.instance.Section title={"Add a Locale"}>
                    <VanillaComponentResolver.instance.ToolButton src={plusSrc}          tooltip = {"Add Locale"}   onSelect={() => {} }     className = {VanillaComponentResolver.instance.toolButtonTheme.button}             focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     />
                </VanillaComponentResolver.instance.Section>
            </InfoSection>
        </>
    );
}