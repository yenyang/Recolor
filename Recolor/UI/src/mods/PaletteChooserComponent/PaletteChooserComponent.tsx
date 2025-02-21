import { bindValue, trigger, useValue } from "cs2/api";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";
import mod from "../../../mod.json";
import { PaletteBoxComponent } from "mods/PaletteBoxComponent/PaletteBoxComponent";
import { PaletteChooserUIData } from "mods/Domain/PaletteAndSwatches/PaletteChooserUIData";
import classNames from "classnames";
import styles from "../Domain/ColorFields.module.scss";
import boxStyles from "../PaletteBoxComponent/PaletteBoxStyles.module.scss";
import { ColorFieldTheme } from "mods/SIPColorComponent/SIPColorComponent";
import { Entity } from "cs2/bindings";
import { useState } from "react";
import { entityEquals } from "cs2/utils";
import { FocusDisabled } from "cs2/input";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";

const uilStandard =                          "coui://uil/Standard/";
const editSrc =                        uilStandard + "PencilPaper.svg";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";

const PaletteChooserData$ = bindValue<PaletteChooserUIData>(mod.id, "PaletteChooserData");
const basicDropDownTheme = getModule('game-ui/common/input/dropdown/dropdown.module.scss', 'classes');

export function assignPalette(channel : number, entity : Entity) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "AssignPalette", channel, entity);
}

export function removePalette(channel: number) {
    trigger(mod.id, "RemovePalette", channel);
}

function handleChannelClick(eventName : string, channel : number) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName, channel);
}


export const PaletteChooserComponent = (props: {channel : number}) => {

    const PaletteChooserData = useValue(PaletteChooserData$);
    const CanPastePalette = false;

    function GetCurrentSwatches() : JSX.Element {
        for (let i=0; i<PaletteChooserData.DropdownItems[props.channel].length; i++) 
        {
            for (let j=0; j<PaletteChooserData.DropdownItems[props.channel][i].Palettes.length; j++) 
            {
                if (entityEquals(PaletteChooserData.DropdownItems[props.channel][i].Palettes[j].PrefabEntity, PaletteChooserData.SelectedPaletteEntities[props.channel])) 
                {
                    return <PaletteBoxComponent Swatches={PaletteChooserData.DropdownItems[props.channel][i].Palettes[j].Swatches} totalWidth={80}></PaletteBoxComponent>
                }
            }
        }

        return <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.largeDropdownText)}>None</div>;
    } 


    return (
        <>
            {PaletteChooserData.DropdownItems[props.channel] && (
                <div className={styles.columnGroup}>
                    <div className={styles.rowGroup}>
                        <Dropdown 
                            theme = {basicDropDownTheme}
                            content={
                                <FocusDisabled>
                                    <DropdownItem value={"None"} className={basicDropDownTheme.dropdownItem} onChange={() => removePalette(props.channel)}>
                                        <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.largeDropdownText)}>None</div>
                                    </DropdownItem>
                                    {
                                    PaletteChooserData.DropdownItems[props.channel].map((Subcategories) => (
                                        <>
                                            <DropdownItem value={Subcategories} className={basicDropDownTheme.dropdownItem} closeOnSelect={false} >
                                                <div className={classNames(ColorFieldTheme.colorField, boxStyles.subcategory, boxStyles.centered, styles.dropdownText)}>{Subcategories.Subcategory}</div>
                                            </DropdownItem>
                                            {Subcategories.Palettes.map((Palette) => (
                                                <DropdownItem value={Palette} className={basicDropDownTheme.dropdownItem} selected={entityEquals(PaletteChooserData.SelectedPaletteEntities[props.channel],Palette.PrefabEntity)} onChange={() => {assignPalette(props.channel, Palette.PrefabEntity)}}>
                                                    <PaletteBoxComponent Swatches={Palette.Swatches} totalWidth={80}></PaletteBoxComponent>
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
                        <VanillaComponentResolver.instance.ToolButton
                            src={editSrc}
                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                            tooltip = {"Edit Palette"}
                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                            onSelect={() => handleChannelClick("EditPalette", props.channel)}
                        />
                        <VanillaComponentResolver.instance.ToolButton
                            src={copySrc}
                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                            tooltip = {"Copy Palette"}
                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                            onSelect={() => handleChannelClick("CopyPalette", props.channel)}
                        />
                        { CanPastePalette && PaletteChooserData.SelectedPaletteEntities[props.channel].index == 0 && (
                            <VanillaComponentResolver.instance.ToolButton
                                src={pasteSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                tooltip = {"Paste Palette"}
                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                onSelect={() => handleChannelClick("PastePalette", props.channel)}
                            />
                        )}
                        
                    </div>
                </div>
                
            )}
        </>
    );
}