import { bindValue, trigger, useValue } from "cs2/api";
import { Entity, tool } from "cs2/bindings";
import { FocusDisabled } from "cs2/input";
import { useLocalization } from "cs2/l10n";
import { assignPalette, PaletteChooserComponent, removePalette } from "mods/PaletteChooserComponent/PaletteChooserComponent";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import styles from "../Domain/ColorFields.module.scss";
import { PaletteChooserUIData } from "mods/Domain/PaletteAndSwatches/PaletteChooserUIData";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { handleClick } from "mods/RecolorMainPanel/RecolorMainPanel";

const uilStandard =                          "coui://uil/Standard/";
const swapSrc =                         uilStandard + "ArrowsMoveLeftRight.svg";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const plusSrc =                         uilStandard + "Plus.svg";
const eyeClosedSrc =                    uilStandard + "EyeClosed.svg";
const minimizeSrc =                     uilStandard + "ArrowsMinimize.svg";
const expandSrc =                       uilStandard + "ArrowsExpand.svg";
const resetSrc =                        uilStandard + "Reset.svg";


const PaletteChooserDuringPlacementData$ = bindValue<PaletteChooserUIData>(mod.id, "PaletteChoicesDuringPlacement");
const EditingPrefabEntity$ = bindValue<Entity>(mod.id, "EditingPrefabEntity");
const ShowPaletteEditorPanel$ = bindValue<boolean>(mod.id, "ShowPaletteEditorMenu");
const CopiedPaletteSet$ = bindValue<Entity[]>(mod.id, "CopiedPaletteSet");
const CopiedPalette$ = bindValue<Entity>(mod.id, "CopiedPalette");
const Minimized$ = bindValue<boolean>(mod.id, "MinimizeDuringPlacement");
const ShowPaletteChooserDuringPlacement$ = bindValue<boolean>(mod.id, "ShowPaletteChooserDuringPlacement");
const EventSuffix = "DuringPlacement";

export const PalettesDuringPlacementComponent = () => 
{
    const toolActive = useValue(tool.activeTool$).id == tool.OBJECT_TOOL;     
    const PaletteChooserDuringPlacmeentData = useValue(PaletteChooserDuringPlacementData$);
    const CopiedPalette = useValue(CopiedPalette$);
    const CopiedPaletteSet = useValue(CopiedPaletteSet$);
    const ShowPaletteChooserDuringPlacement = useValue(ShowPaletteChooserDuringPlacement$);    
    const EditingPrefabEntity = useValue(EditingPrefabEntity$);
    const ShowPaletteEditorPanel = useValue(ShowPaletteEditorPanel$);    
    const Minimized = useValue(Minimized$);
    
    const { translate } = useLocalization();
            
    return (
        <>
            { toolActive && ShowPaletteChooserDuringPlacement && (
                <>
                    <VanillaComponentResolver.instance.Section title={"Palette"}>
                        <div className={styles.rowGroup}>
                                    {!Minimized && (
                                        <>
                                            { (PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[0].index != 0 || PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[1].index != 0 || PaletteChooserDuringPlacmeentData.SelectedPaletteEntities[2].index != 0) && (
                                            <>
                                                <VanillaComponentResolver.instance.ToolButton src={resetSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ResetPalettesDuringPlacement]", locale["Recolor.TOOLTIP_DESCRIPTION[ResetPalettesDuringPlacement]"])}
                                                                                                onSelect={() => { removePalette(0, EventSuffix); removePalette(1, EventSuffix); removePalette(2, EventSuffix);}}
                                                />
                                                <VanillaComponentResolver.instance.ToolButton src={copySrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyPaletteSet]" ,locale["Recolor.TOOLTIP_DESCRIPTION[CopyPaletteSet]"])}
                                                                                                onSelect={() => { trigger(mod.id, "CopyPaletteSet", PaletteChooserDuringPlacmeentData.SelectedPaletteEntities)}}
                                                />
                                            </>
                                            )} 
                                            { (CopiedPaletteSet[0].index != 0 ||  CopiedPaletteSet[1].index != 0 || CopiedPaletteSet[2].index != 0) && (
                                            <VanillaComponentResolver.instance.ToolButton src={pasteSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PastePaletteSet]" ,locale["Recolor.TOOLTIP_DESCRIPTION[PastePaletteSet]"])}
                                                                                            onSelect={() => { assignPalette(0, CopiedPaletteSet[0], EventSuffix); assignPalette(1, CopiedPaletteSet[1], EventSuffix); assignPalette(2, CopiedPaletteSet[2], EventSuffix)}}
                                            />
                                            )} 
                                            <VanillaComponentResolver.instance.ToolButton src={plusSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                            tooltip = {(ShowPaletteEditorPanel && EditingPrefabEntity.index == 0) ? translate("Recolor.TOOLTIP_DESCRIPTION[CloseEditorPanel]" ,locale["Recolor.TOOLTIP_DESCRIPTION[CloseEditorPanel]"])  : translate("Recolor.TOOLTIP_DESCRIPTION[GenerateNewPalette]" ,locale["Recolor.TOOLTIP_DESCRIPTION[GenerateNewPalette]"])} 
                                                                                            selected={ShowPaletteEditorPanel && EditingPrefabEntity.index == 0} 
                                                                                            onSelect={() => { if (!ShowPaletteEditorPanel || (ShowPaletteEditorPanel && EditingPrefabEntity.index == 0))  {handleClick("TogglePaletteEditorMenu"); }
                                                                                                            if (EditingPrefabEntity.index != 0) {handleClick("GenerateNewPalette");}}}
                                            />
                                            <VanillaComponentResolver.instance.ToolButton src={eyeClosedSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[HidePalettesDuringPlacement]", locale["Recolor.TOOLTIP_DESCRIPTION[HidePalettesDuringPlacement]"])} 
                                                                                            onSelect={() => { handleClick("HidePalettesDuringPlacement");}}
                                            />
                                        </>
                                    )}
                                    <VanillaComponentResolver.instance.ToolButton src={Minimized ? expandSrc : minimizeSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                    tooltip = {Minimized? translate("Recolor.TOOLTIP_DESCRIPTION[ExpandDuringPlacement]" ,locale["Recolor.TOOLTIP_DESCRIPTION[ExpandDuringPlacement]"]) : translate( "Recolor.TOOLTIP_DESCRIPTION[MinimizeDuringPlacement]",locale["Recolor.TOOLTIP_DESCRIPTION[MinimizeDuringPlacement]"])} 
                                                                                    onSelect={() => { Minimized? handleClick("MaximizePalettesDuringPlacement") : handleClick("MinimizePalettesDuringPlacement");}}
                                    />
                            </div>
                    </VanillaComponentResolver.instance.Section>
                    {!Minimized && (
                        <VanillaComponentResolver.instance.Section>
                                <FocusDisabled>
                                <PaletteChooserComponent channel={0} PaletteChooserData={PaletteChooserDuringPlacmeentData} eventSuffix={EventSuffix} noneHasColor={true}></PaletteChooserComponent>
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
                                <PaletteChooserComponent channel={1} PaletteChooserData={PaletteChooserDuringPlacmeentData} eventSuffix={EventSuffix} noneHasColor={true}></PaletteChooserComponent>
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
                                <PaletteChooserComponent channel={2} PaletteChooserData={PaletteChooserDuringPlacmeentData} eventSuffix={EventSuffix} noneHasColor={true}></PaletteChooserComponent>
                            </FocusDisabled>
                        </VanillaComponentResolver.instance.Section>
                    )}
                </>
            )}
        </>
    );
}