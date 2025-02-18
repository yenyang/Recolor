import { bindValue, useValue } from "cs2/api";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";
import { SwatchUIData } from "mods/Domain/PaletteAndSwatches/SwatchUIData";
import mod from "../../../mod.json";
import { PaletteBoxComponent } from "mods/PaletteBoxComponent/PaletteBoxComponent";


const Swatches$ = bindValue<SwatchUIData[]>(mod.id, "Swatches");
const basicDropDownTheme = getModule('game-ui/common/input/dropdown/dropdown.module.scss', 'classes');

export const PaletteChooserComponent = () => {

    const Swatches = useValue(Swatches$);

    return (
        <>
            <Dropdown 
                theme = {basicDropDownTheme}
                content={                    
                        <DropdownItem value={Swatches} className={basicDropDownTheme.dropdownItem}>
                            <PaletteBoxComponent Swatches={Swatches} totalWidth={80}></PaletteBoxComponent>
                        </DropdownItem>
                }
            >
                <DropdownToggle disabled={false}>
                    <PaletteBoxComponent Swatches={Swatches} totalWidth={80}></PaletteBoxComponent>
                </DropdownToggle>
            </Dropdown>
        </>
    );
}