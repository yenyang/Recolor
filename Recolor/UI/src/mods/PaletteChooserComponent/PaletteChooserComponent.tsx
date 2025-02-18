import { bindValue, useValue } from "cs2/api";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";
import { SwatchUIData } from "mods/Domain/PaletteAndSwatches/SwatchUIData";
import mod from "../../../mod.json";
import { PaletteBoxComponent } from "mods/PaletteBoxComponent/PaletteBoxComponent";
import { useState } from "react";


const Palettes$ = bindValue<SwatchUIData[][]>(mod.id, "Palettes");
const basicDropDownTheme = getModule('game-ui/common/input/dropdown/dropdown.module.scss', 'classes');

export const PaletteChooserComponent = () => {

    const Palettes = useValue(Palettes$);
    let [selectedIndex, setSelectedIndex] = useState(0);

    return (
        <>
            {Palettes.length && (
                <Dropdown 
                    theme = {basicDropDownTheme}
                    content={                    
                        Palettes.map((Swatches, index: number) => (
                            <DropdownItem value={Swatches} className={basicDropDownTheme.dropdownItem} selected={selectedIndex == index} onChange={() => setSelectedIndex(index)}>
                                <PaletteBoxComponent Swatches={Swatches} totalWidth={80}></PaletteBoxComponent>
                            </DropdownItem>
                        ))
                    }
                >
                    <DropdownToggle disabled={false}>
                        <PaletteBoxComponent Swatches={Palettes[selectedIndex]} totalWidth={80}></PaletteBoxComponent>
                    </DropdownToggle>
                </Dropdown>
            )}
        </>
    );
}