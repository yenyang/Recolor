import { bindValue, trigger, useValue } from "cs2/api";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownItem, DropdownToggle, Tooltip } from "cs2/ui";
import mod from "../../../mod.json";
import { PaletteBoxComponent } from "mods/PaletteBoxComponent/PaletteBoxComponent";
import { PaletteChooserUIData } from "mods/Domain/PaletteAndSwatches/PaletteChooserUIData";
import classNames from "classnames";
import styles from "../Domain/ColorFields.module.scss";
import boxStyles from "../PaletteBoxComponent/PaletteBoxStyles.module.scss";
import { ColorFieldTheme } from "mods/SIPColorComponent/SIPColorComponent";
import { Entity, tool } from "cs2/bindings";
import { useState } from "react";
import { entityEquals } from "cs2/utils";
import { FocusDisabled } from "cs2/input";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { useLocalization } from "cs2/l10n";
import locale from "../lang/en-US.json";
import { DescriptionTooltip } from "mods/RecolorMainPanel/RecolorMainPanel";

const uilStandard =                          "coui://uil/Standard/";
const editSrc =                        uilStandard + "PencilPaper.svg";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";

const CopiedPalette$ = bindValue<Entity>(mod.id, "CopiedPalette");
const basicDropDownTheme = getModule('game-ui/common/input/dropdown/dropdown.module.scss', 'classes');
const EditingPrefabEntity$ = bindValue<Entity>(mod.id, "EditingPrefabEntity");
const ShowPaletteEditorPanel$ = bindValue<boolean>(mod.id, "ShowPaletteEditorMenu");

export function assignPalette(channel : number, entity : Entity, eventSuffix?: string) {
    if (eventSuffix == undefined) eventSuffix = "";
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "AssignPalette" + eventSuffix, channel, entity);
}

export function removePalette(channel: number, eventSuffix?: string) {
    if (eventSuffix == undefined) eventSuffix = "";
    trigger(mod.id, "RemovePalette" + eventSuffix, channel);
}

export const PaletteChooserComponent = (props: {channel : number, PaletteChooserData: PaletteChooserUIData, eventSuffix?:string}) => {
    const CopiedPalette = useValue(CopiedPalette$);
    const CanPastePalette : boolean = CopiedPalette.index != 0;
    const EditingPrefabEntity = useValue(EditingPrefabEntity$);    
    const ShowPaletteEditorPanel = useValue(ShowPaletteEditorPanel$);

    const { translate } = useLocalization();

    function GetCurrentSwatches() : JSX.Element {
        for (let i=0; i<props.PaletteChooserData.DropdownItems[props.channel].length; i++) 
        {
            for (let j=0; j<props.PaletteChooserData.DropdownItems[props.channel][i].Palettes.length; j++) 
            {
                if (entityEquals(props.PaletteChooserData.DropdownItems[props.channel][i].Palettes[j].PrefabEntity, props.PaletteChooserData.SelectedPaletteEntities[props.channel])) 
                {
                    return <PaletteBoxComponent Swatches={props.PaletteChooserData.DropdownItems[props.channel][i].Palettes[j].Swatches} totalWidth={80}></PaletteBoxComponent>
                }
            }
        }

        return <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.largeDropdownText)}>None</div>;
    } 


    return (
        <>
            {props.PaletteChooserData.DropdownItems[props.channel] && (
                <div className={styles.columnGroup}>
                    <div className={styles.rowGroup}>
                        <Dropdown 
                            theme = {basicDropDownTheme}
                            content={
                                <FocusDisabled>
                                    <DropdownItem value={translate("Recolor.SECTION_TITLE[None]" , locale["Recolor.SECTION_TITLE[None]"])} className={basicDropDownTheme.dropdownItem} onChange={() => removePalette(props.channel, props.eventSuffix)}>
                                        <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.largeDropdownText)}>{translate("Recolor.SECTION_TITLE[None]", locale["Recolor.SECTION_TITLE[None]"])}</div>
                                    </DropdownItem>
                                    {
                                    props.PaletteChooserData.DropdownItems[props.channel].map((Subcategories) => (
                                        <>
                                            <DropdownItem value={Subcategories} className={basicDropDownTheme.dropdownItem} closeOnSelect={false} >
                                                <Tooltip tooltip={translate("Recolor.Subcategory.DESCRIPTION["+Subcategories.Subcategory+"]")}>
                                                    <div className={classNames(ColorFieldTheme.colorField, boxStyles.subcategory, boxStyles.centered, styles.dropdownText)}>{translate("Recolor.Subcategory.NAME["+Subcategories.Subcategory+"]" ,Subcategories.Subcategory)}</div>
                                                </Tooltip>
                                            </DropdownItem>
                                            {Subcategories.Palettes.map((Palette) => (
                                                <DropdownItem value={Palette} className={basicDropDownTheme.dropdownItem} selected={entityEquals(props.PaletteChooserData.SelectedPaletteEntities[props.channel],Palette.PrefabEntity)} onChange={() => {assignPalette(props.channel, Palette.PrefabEntity, props.eventSuffix);}}>
                                                    <PaletteBoxComponent Swatches={Palette.Swatches} totalWidth={80} tooltip={DescriptionTooltip(translate(Palette.NameKey, Palette.Name), translate(Palette.DescriptionKey))}></PaletteBoxComponent>
                                                </DropdownItem>
                                            ))}
                                        </>
                                    ))}
                                </FocusDisabled>
                            }
                        >
                            <DropdownToggle disabled={false}>
                                {GetCurrentSwatches()}
                            </DropdownToggle>
                        </Dropdown>
                    </div>
                    <div className={styles.rowGroup}>
                        { props.PaletteChooserData.SelectedPaletteEntities[props.channel].index != 0  && (
                            <VanillaComponentResolver.instance.ToolButton
                                src={editSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[EditPalette]", locale["Recolor.TOOLTIP_DESCRIPTION[EditPalette]"])}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                selected = {entityEquals(props.PaletteChooserData.SelectedPaletteEntities[props.channel], EditingPrefabEntity) && EditingPrefabEntity.index != 0 && ShowPaletteEditorPanel}
                                onSelect={() => (entityEquals(props.PaletteChooserData.SelectedPaletteEntities[props.channel], EditingPrefabEntity) && EditingPrefabEntity.index != 0)?
                                    trigger(mod.id, "TogglePaletteEditorMenu") :
                                    trigger(mod.id, "EditPalette", props.PaletteChooserData.SelectedPaletteEntities[props.channel])}
                            />
                        )}
                        { props.PaletteChooserData.SelectedPaletteEntities[props.channel].index != 0 && (
                            <VanillaComponentResolver.instance.ToolButton
                                src={copySrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyPalette]" , locale["Recolor.TOOLTIP_DESCRIPTION[CopyPalette]"])}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => trigger(mod.id, "CopyPalette", props.PaletteChooserData.SelectedPaletteEntities[props.channel])}
                            />
                        )}
                        { CanPastePalette && (
                            <VanillaComponentResolver.instance.ToolButton
                                src={pasteSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PastePalette]" , locale["Recolor.TOOLTIP_DESCRIPTION[PastePalette]"])}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => trigger(mod.id, "AssignPalette", props.channel, CopiedPalette)}
                            />
                        )}
                        { (props.PaletteChooserData.SelectedPaletteEntities[0].index != 0 || props.PaletteChooserData.SelectedPaletteEntities[1].index != 0 || props.PaletteChooserData.SelectedPaletteEntities[2].index != 0) &&
                          props.PaletteChooserData.SelectedPaletteEntities[props.channel].index == 0 && !CanPastePalette && (
                            <span className={styles.belowPaletteArea}></span>
                        )}
                    </div>
                </div>
                
            )}
        </>
    );
}