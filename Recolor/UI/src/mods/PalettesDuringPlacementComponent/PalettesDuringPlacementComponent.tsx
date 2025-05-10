import { bindValue, useValue } from "cs2/api";
import { Entity, tool } from "cs2/bindings";
import { FocusDisabled } from "cs2/input";
import { useLocalization } from "cs2/l10n";
import { assignPalette, PaletteChooserComponent, removePalette } from "mods/PaletteChooserComponent/PaletteChooserComponent";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import styles from "../Domain/ColorFields.module.scss";
import { PaletteChooserUIData } from "mods/Domain/PaletteAndSwatches/PaletteChooserUIData";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";

const uilStandard =                          "coui://uil/Standard/";
const swapSrc =                         uilStandard + "ArrowsMoveLeftRight.svg";

const PaletteChooserDuringPlacementData$ = bindValue<PaletteChooserUIData>(mod.id, "PaletteChoicesDuringPlacement");

const CopiedPaletteSet$ = bindValue<Entity[]>(mod.id, "CopiedPaletteSet");
const CopiedPalette$ = bindValue<Entity>(mod.id, "CopiedPalette");
const EventSuffix = "DuringPlacement";

export const PalettesDuringPlacementComponent = () => 
{
    const toolActive = useValue(tool.activeTool$).id == tool.OBJECT_TOOL;     
    const PaletteChooserDuringPlacmeentData = useValue(PaletteChooserDuringPlacementData$);
    const CopiedPalette = useValue(CopiedPalette$);
    const CopiedPaletteSet = useValue(CopiedPaletteSet$);
    
    const { translate } = useLocalization();
            
    return (
        <>
            { toolActive && (
                <>
                    <VanillaComponentResolver.instance.Section title={"Palette"}><></></VanillaComponentResolver.instance.Section>
                    <VanillaComponentResolver.instance.Section >
                            <FocusDisabled>
                            <PaletteChooserComponent channel={0} PaletteChooserData={PaletteChooserDuringPlacmeentData} eventSuffix={EventSuffix}></PaletteChooserComponent>
                            <div className={styles.columnGroup}>
                                <VanillaComponentResolver.instance.ToolButton
                                    src={swapSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]"])}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => 
                                    {
                                        let entity0 : Entity = PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[0];
                                        PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[1].index != 0 ? assignPalette(0, PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[1], EventSuffix) : removePalette(0, EventSuffix);
                                        entity0.index != 0 ? assignPalette(1, entity0, EventSuffix) : removePalette(1, EventSuffix);
                                    }}
                                />
                                { (PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[0].index != 0 || PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[1].index != 0 || PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[2].index != 0 || CopiedPalette.index != 0)   ?
                                    <span className={styles.belowSwapButton}></span> : <span className={styles.belowSwapButtonSmall}></span>
                                }
                            </div>
                            <PaletteChooserComponent channel={1} PaletteChooserData={PaletteChooserDuringPlacmeentData} eventSuffix="DuringPlacement"></PaletteChooserComponent>
                            <div className={styles.columnGroup}>
                                <VanillaComponentResolver.instance.ToolButton
                                    src={swapSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]"])}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => 
                                    {
                                        let entity1 : Entity = PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[1];
                                        PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[2].index != 0 ? assignPalette(1, PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[2], EventSuffix) : removePalette(1, EventSuffix);
                                        entity1.index != 0 ? assignPalette(2, entity1, EventSuffix) : removePalette(2, EventSuffix);
                                    }}
                                />
                                { (PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[0].index != 0 || PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[1].index != 0 || PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[2].index != 0 || CopiedPalette.index != 0)   ?
                                    <span className={styles.belowSwapButton}></span> : <span className={styles.belowSwapButtonSmall}></span>
                                }
                            </div>
                            <PaletteChooserComponent channel={2} PaletteChooserData={PaletteChooserDuringPlacmeentData} eventSuffix="DuringPlacement"></PaletteChooserComponent>
                        </FocusDisabled>
                    </VanillaComponentResolver.instance.Section>
                </>
            )}
        </>
    );
}