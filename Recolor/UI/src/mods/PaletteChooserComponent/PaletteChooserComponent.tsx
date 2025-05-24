import { bindValue, trigger, useValue } from "cs2/api";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownItem, DropdownToggle, Tooltip } from "cs2/ui";
import mod from "../../../mod.json";
import { convertToBackGroundColor, PaletteBoxComponent } from "mods/PaletteBoxComponent/PaletteBoxComponent";
import { PaletteChooserUIData } from "mods/Domain/PaletteAndSwatches/PaletteChooserUIData";
import classNames from "classnames";
import styles from "../Domain/ColorFields.module.scss";
import boxStyles from "../PaletteBoxComponent/PaletteBoxStyles.module.scss";
import { ColorFieldTheme } from "mods/SIPColorComponent/SIPColorComponent";
import { Color, Entity, tool } from "cs2/bindings";
import { CSSProperties, useState } from "react";
import { entityEquals } from "cs2/utils";
import { FocusDisabled } from "cs2/input";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { useLocalization } from "cs2/l10n";
import locale from "../lang/en-US.json";
import { DescriptionTooltip } from "mods/RecolorMainPanel/RecolorMainPanel";
import { PaletteUIData } from "mods/Domain/PaletteAndSwatches/PaletteUIData";
import { PaletteSubcategoryUIData } from "mods/Domain/PaletteAndSwatches/PaletteSubCategoryUIData";

const uilStandard =                          "coui://uil/Standard/";
const editSrc =                        uilStandard + "PencilPaper.svg";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";

const CopiedPalette$ = bindValue<Entity>(mod.id, "CopiedPalette");
const basicDropDownTheme = getModule('game-ui/common/input/dropdown/dropdown.module.scss', 'classes');
const EditingPrefabEntity$ = bindValue<Entity>(mod.id, "EditingPrefabEntity");
const ShowPaletteEditorPanel$ = bindValue<boolean>(mod.id, "ShowPaletteEditorMenu");
const NonePaletteColors$ = bindValue<Color[]>(mod.id, "NonePaletteColors");
const PaletteLibrary$ = bindValue<PaletteUIData[]>(mod.id, "PaletteLibrary");
const SubcategoryLibrary$ = bindValue<PaletteSubcategoryUIData[]>(mod.id, "SubcategoryLibrary");
const PaletteLibraryVersion$ = bindValue<number>(mod.id, "PaletteLibraryVersion");
const SubcategoryLibraryVersion$ = bindValue<number>(mod.id, "SubcategoryLibraryVersion");

export function assignPalette(channel : number, entity : Entity, eventSuffix?: string) {
    if (eventSuffix == undefined) eventSuffix = "";
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "AssignPalette" + eventSuffix, channel, entity);
}

export function removePalette(channel: number, eventSuffix?: string) {
    if (eventSuffix == undefined) eventSuffix = "";
    trigger(mod.id, "RemovePalette" + eventSuffix, channel);
}

function updatePaletteLookup(palettes :PaletteUIData[]) 
{
    let lookup = [];
    for (let i=0; i<palettes.length; i++) 
    {
        lookup[palettes[i].PrefabEntity.index] = palettes[i];
    }
    return lookup;
}

function updateSubcategoriesLookup(subcategories :PaletteSubcategoryUIData[]) 
{
    let lookup = [];
    for (let i=0; i<subcategories.length; i++) 
    {
        lookup[subcategories[i].PrefabEntity.index] = subcategories[i];
    }
    return lookup;
}

export const PaletteChooserComponent = (props: {channel : number, PaletteChooserData: PaletteChooserUIData, eventSuffix?:string, noneHasColor?:boolean}) => {
    const CopiedPalette = useValue(CopiedPalette$);
    const CanPastePalette : boolean = CopiedPalette.index != 0;
    const EditingPrefabEntity = useValue(EditingPrefabEntity$);    
    const ShowPaletteEditorPanel = useValue(ShowPaletteEditorPanel$);
    const NonePaletteColors = useValue(NonePaletteColors$);
    const PaletteLibrary = useValue(PaletteLibrary$);
    const SubcategoryLibrary = useValue(SubcategoryLibrary$);
    const PaletteLibraryVersion = useValue(PaletteLibraryVersion$);
    const SubcategoryLibraryVersion = useValue(SubcategoryLibraryVersion$);

    let [PaletteLookup, setPaletteLookup] = useState(updatePaletteLookup(PaletteLibrary));    
    let [SubcategoriesLookup, setSubcategoriesLookup] = useState(updateSubcategoriesLookup(SubcategoryLibrary));
    let [PalettesVersion, setPalettesVersion] = useState(PaletteLibraryVersion);
    let [SubcategoriesVersion, setSubcategoriesVersion] = useState(SubcategoryLibraryVersion);

    if (PalettesVersion !== PaletteLibraryVersion) 
    {
        setPaletteLookup(updatePaletteLookup(PaletteLibrary));
        setPalettesVersion(PaletteLibraryVersion);
    }

    if (SubcategoriesVersion !== SubcategoriesVersion) 
    {
        setSubcategoriesLookup(updateSubcategoriesLookup(SubcategoryLibrary));
        setSubcategoriesVersion(SubcategoryLibraryVersion);
    }

    const { translate } = useLocalization();

    function getStyle() : CSSProperties | undefined
    {
        if (props.noneHasColor == undefined || props.PaletteChooserData.SelectedPaletteEntities[props.channel].index != 0) return {color: "#ffffff"};
        let col = "#ffffff";
        if (NonePaletteColors[props.channel].r *0.299 + NonePaletteColors[props.channel].g * 0.587 + NonePaletteColors[props.channel].b * 0.114 > 186/256) {
            col = "#000000";
        }

        return {color: col, backgroundColor: convertToBackGroundColor(NonePaletteColors[props.channel])}
    }   

    function GetCurrentSwatches() : JSX.Element {
        if (PaletteLookup[props.PaletteChooserData.SelectedPaletteEntities[props.channel].index] != undefined &&
            entityEquals(PaletteLookup[props.PaletteChooserData.SelectedPaletteEntities[props.channel].index].PrefabEntity,  props.PaletteChooserData.SelectedPaletteEntities[props.channel])) 
        {
            return <PaletteBoxComponent Swatches={PaletteLookup[props.PaletteChooserData.SelectedPaletteEntities[props.channel].index].Swatches} totalWidth={80}></PaletteBoxComponent>
        }

        return <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.largeDropdownText)} style={getStyle()}>{translate("Recolor.SECTION_TITLE[None]" , locale["Recolor.SECTION_TITLE[None]"])}</div>;
    } 

    function GetDropDown(prefabEntity: Entity) 
    {
        if (PaletteLookup[prefabEntity.index] != undefined &&
            entityEquals(PaletteLookup[prefabEntity.index].PrefabEntity,  prefabEntity)) 
        {
            let Palette = PaletteLookup[prefabEntity.index];
            return (
                <DropdownItem value={prefabEntity} className={basicDropDownTheme.dropdownItem} selected={entityEquals(props.PaletteChooserData.SelectedPaletteEntities[props.channel],Palette.PrefabEntity)} onChange={() => {assignPalette(props.channel, Palette.PrefabEntity, props.eventSuffix);}}>
                        <PaletteBoxComponent Swatches={Palette.Swatches} totalWidth={80} tooltip={DescriptionTooltip(translate(Palette.NameKey, Palette.Name), translate(Palette.DescriptionKey))}></PaletteBoxComponent>
                </DropdownItem>
            );
        }

        if (SubcategoriesLookup[prefabEntity.index] != undefined &&
            entityEquals(SubcategoriesLookup[prefabEntity.index].PrefabEntity,  prefabEntity)) 
        {
            let Subcategory = SubcategoriesLookup[prefabEntity.index];
            return (
                <DropdownItem value={Subcategory} className={basicDropDownTheme.dropdownItem} closeOnSelect={false} >
                    <Tooltip tooltip={translate("Recolor.Subcategory.DESCRIPTION["+Subcategory.Subcategory+"]")}>
                        <div className={classNames(ColorFieldTheme.colorField, boxStyles.subcategory, boxStyles.centered, styles.dropdownText)}>{translate("Recolor.Subcategory.NAME["+Subcategory.Subcategory+"]" ,Subcategory.Subcategory)}</div>
                    </Tooltip>
                </DropdownItem>
            )
        }


        return <></>
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
                                    <DropdownItem value={translate("Recolor.SECTION_TITLE[None]", locale["Recolor.SECTION_TITLE[None]"])} className={basicDropDownTheme.dropdownItem} onChange={() => removePalette(props.channel, props.eventSuffix)}>
                                        <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered, styles.largeDropdownText)} style={getStyle()}>{translate("Recolor.SECTION_TITLE[None]", locale["Recolor.SECTION_TITLE[None]"])}</div>
                                    </DropdownItem>
                                    {

                                    props.PaletteChooserData.DropdownItems[props.channel].map((prefabEntity) => (
                                        <>
                                            {GetDropDown(prefabEntity)}
                                        </>
                                    ))
                                    }
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
                                onSelect={() => assignPalette(props.channel, CopiedPalette, props.eventSuffix)}
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