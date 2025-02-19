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


const PaletteChooserData$ = bindValue<PaletteChooserUIData>(mod.id, "PaletteChooserData");
const basicDropDownTheme = getModule('game-ui/common/input/dropdown/dropdown.module.scss', 'classes');

export function setSelectedIndex(channel : number, index : number) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "SetSelectedPaletteIndex", channel, index);
}

export function setSelectedSubcategory(channel : number, subcategoryIndex : number) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "SetSelectedSubcategoryIndex", channel, subcategoryIndex);
}

export const PaletteChooserComponent = (props: {channel : number}) => {

    const PaletteChooserData = useValue(PaletteChooserData$);

    return (
        <>
            {PaletteChooserData.DropdownItems[props.channel] && (
                <Dropdown 
                    theme = {basicDropDownTheme}
                    content={
                        <>
                        <DropdownItem value={"None"} className={basicDropDownTheme.dropdownItem} onChange={() => setSelectedIndex(props.channel, -1)}>
                            <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.dropdownText)}>None</div>
                        </DropdownItem>
                        {
                        PaletteChooserData.DropdownItems[props.channel].map((Subcategories, subcategoryIndex: number) => (
                            <>
                                <DropdownItem value={Subcategories} className={basicDropDownTheme.dropdownItem} closeOnSelect={false} >
                                    <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.dropdownText)}>{Subcategories.Subcategory}</div>
                                </DropdownItem>
                                {Subcategories.Palettes.map((Swatches, index: number) => (
                                    <DropdownItem value={Swatches} className={basicDropDownTheme.dropdownItem} selected={PaletteChooserData.SelectedIndexes[props.channel] == index} onChange={() => {setSelectedIndex(props.channel, index); setSelectedSubcategory(props.channel, subcategoryIndex)}}>
                                        <PaletteBoxComponent Swatches={Swatches} totalWidth={80}></PaletteBoxComponent>
                                    </DropdownItem>
                                ))}
                            </>
                        ))}
                        </>
                    }
                >
                    <DropdownToggle disabled={false}>
                        {PaletteChooserData.SelectedIndexes[props.channel] == -1 ?  
                        <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.dropdownText)}>None</div>
                        :
                        <PaletteBoxComponent Swatches={PaletteChooserData.DropdownItems[props.channel][PaletteChooserData.SelectedSubcategories[props.channel]].Palettes[PaletteChooserData.SelectedIndexes[props.channel]]} totalWidth={80}></PaletteBoxComponent>
                        }
                    </DropdownToggle>
                </Dropdown>
            )}
        </>
    );
}