import { bindValue, useValue } from "cs2/api";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";
import { SwatchUIData } from "mods/Domain/PaletteAndSwatches/SwatchUIData";
import mod from "../../../mod.json";
import { PaletteBoxComponent } from "mods/PaletteBoxComponent/PaletteBoxComponent";
import { useState } from "react";
import { PaletteChooserUIData } from "mods/Domain/PaletteAndSwatches/PaletteChooserUIData";
import { chirper } from "cs2/bindings";


const PaletteChooserData$ = bindValue<PaletteChooserUIData>(mod.id, "PaletteChooserData");
const basicDropDownTheme = getModule('game-ui/common/input/dropdown/dropdown.module.scss', 'classes');

export const PaletteChooserComponent = (props: {channel : number}) => {

    const PaletteChooserData = useValue(PaletteChooserData$);
    let [selectedIndex, setSelectedIndex] = useState(0);
    let [selectedSubcategory, setSelectedSubcategory] = useState(0);

    return (
        <>
            {PaletteChooserData.DropdownItems[props.channel] && (
                <Dropdown 
                    theme = {basicDropDownTheme}
                    content={
                        PaletteChooserData.DropdownItems[props.channel].map((Subcategories, subcategoryIndex: number) => (
                            <>
                                <DropdownItem value={Subcategories} className={basicDropDownTheme.dropdownItem}>
                                    <div>{Subcategories.Subcategory}</div>
                                </DropdownItem>
                                {Subcategories.Palettes.map((Swatches, index: number) => (
                                    <DropdownItem value={Swatches} className={basicDropDownTheme.dropdownItem} selected={selectedIndex == index} onChange={() => {setSelectedIndex(index); setSelectedSubcategory(subcategoryIndex)}}>
                                        <PaletteBoxComponent Swatches={Swatches} totalWidth={80}></PaletteBoxComponent>
                                    </DropdownItem>
                                ))}
                            </>
                        ))
                    }
                >
                    <DropdownToggle disabled={false}>
                        <PaletteBoxComponent Swatches={PaletteChooserData.DropdownItems[props.channel][selectedSubcategory].Palettes[selectedIndex]} totalWidth={80}></PaletteBoxComponent>
                    </DropdownToggle>
                </Dropdown>
            )}
        </>
    );
}