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


const PaletteChooserData$ = bindValue<PaletteChooserUIData>(mod.id, "PaletteChooserData");
const basicDropDownTheme = getModule('game-ui/common/input/dropdown/dropdown.module.scss', 'classes');

export function assignPalette(channel : number, entity : Entity) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "AssignPalette", channel, entity);
}

export function removePalette(channel: number) {
    trigger(mod.id, "RemovePalette", channel);
}


export const PaletteChooserComponent = (props: {channel : number}) => {

    const PaletteChooserData = useValue(PaletteChooserData$);

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

        return <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.dropdownText)}>None</div>;
    } 


    return (
        <>
            {PaletteChooserData.DropdownItems[props.channel] && (
                <Dropdown 
                    theme = {basicDropDownTheme}
                    content={
                        <FocusDisabled>
                            <DropdownItem value={"None"} className={basicDropDownTheme.dropdownItem} onChange={() => removePalette(props.channel)}>
                                <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.dropdownText)}>None</div>
                            </DropdownItem>
                            {
                            PaletteChooserData.DropdownItems[props.channel].map((Subcategories) => (
                                <>
                                    <DropdownItem value={Subcategories} className={basicDropDownTheme.dropdownItem} closeOnSelect={false} >
                                        <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.dropdownText)}>{Subcategories.Subcategory}</div>
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
            )}
        </>
    );
}