import { getModule } from "cs2/modding";
import { Theme, Color } from "cs2/bindings";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { SIPColorComponent } from "mods/SIPColorComponent/SIPColorComponent";
import styles from "../Domain/ColorFields.module.scss";
import classNames from "classnames";
import { RecolorSet } from "mods/Domain/RecolorSet";
import { ButtonState } from "mods/Domain/ButtonState";
import { Scope } from "mods/Domain/Scope";
import { tool } from "cs2/bindings";
import { Button } from "cs2/ui";
import { SubMeshData, SubMeshScopes } from "mods/Domain/SubMeshData";
import paintSrc from "images/format_painter.svg";
import { assignPalette, PaletteChooserComponent, removePalette } from "mods/PaletteChooserComponent/PaletteChooserComponent";
import { PaletteChooserUIData } from "mods/Domain/PaletteAndSwatches/PaletteChooserUIData";
import { Entity } from "cs2/utils";
import { FocusDisabled } from "cs2/input";
import { convertToBackGroundColor } from "mods/PaletteBoxComponent/PaletteBoxComponent";

/*
import resetSrc from "images/uilStandard/Reset.svg";
import singleSrc from "images/uilStandard/SingleRhombus.svg";
import matchingSrc from "images/uilStandard/SameRhombus.svg";
import allSrc from "images/uilStandard/StarAll.svg";
import copySrc from "images/uilStandard/RectangleCopy.svg";
import pasteSrc from "images/uilStandard/RectanglePaste.svg";
import colorPickerSrc from "images/uilStandard/PickerPipette.svg";
import colorPaletteSrc from "images/uilColored/ColorPalette.svg";
import minimizeSrc from "images/uilStandard/ArrowsMinimize.svg";
import expandSrc from "images/uilStandard/ArrowsExpand.svg";
import saveToDiskSrc from "images/uilStandard/DiskSave.svg";
import swapSrc from "images/uilStandard/ArrowsMoveLeftRight.svg";
import serviceVehiclesSrc from "images/uilStandard/ServiceVehicles.svg";
import routeSrc from "images/uilStandard/BusShelter.svg";
import arrowLeftSrc from "images/uilStandard/ArrowLeftThickStroke.svg";
import arrowRightSrc from "images/uilStandard/ArrowRightThickStroke.svg";
import plusSrc from "images/uilStandard/Plus.svg";
*/

const uilStandard =                          "coui://uil/Standard/";
const uilColored =                           "coui://uil/Colored/";
const resetSrc =                        uilStandard + "Reset.svg";
const singleSrc =                       uilStandard + "SingleRhombus.svg";
const matchingSrc =                     uilStandard + "SameRhombus.svg";
const allSrc =                          uilStandard + "StarAll.svg";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const colorPickerSrc =                  uilStandard + "PickerPipette.svg";
const colorPaletteSrc =                  uilColored + "ColorPalette.svg";
const minimizeSrc =                     uilStandard + "ArrowsMinimize.svg";
const expandSrc =                       uilStandard + "ArrowsExpand.svg";
const saveToDiskSrc =                   uilStandard + "DiskSave.svg";
const swapSrc =                         uilStandard + "ArrowsMoveLeftRight.svg";
const serviceVehiclesSrc =              uilStandard + "ServiceVehicles.svg";
const routeSrc =                        uilStandard + "BusShelter.svg";
const arrowLeftSrc =                    uilStandard +  "ArrowLeftThickStroke.svg";
const arrowRightSrc =                   uilStandard +  "ArrowRightThickStroke.svg";
const plusSrc =                         uilStandard + "Plus.svg";

const SingleInstance$ = bindValue<ButtonState>(mod.id, 'SingleInstance');
const Matching$ = bindValue<ButtonState>(mod.id, 'Matching');
const ServiceVehicles$ = bindValue<ButtonState>(mod.id, 'ServiceVehicles');
const MatchesVanillaColorSet$ = bindValue<boolean[]>(mod.id, 'MatchesVanillaColorSet');
const CanPasteColorSet$ = bindValue<boolean>(mod.id, "CanPasteColorSet");
const Minimized$ = bindValue<boolean>(mod.id, "Minimized");
const MatchesSavedtoDisk$ = bindValue<boolean>(mod.id, "MatchesSavedOnDisk");
const ShowHexaDecimals$ = bindValue<boolean>(mod.id, "ShowHexaDecimals");
const CurrentColorSet$ = bindValue<RecolorSet>(mod.id, "CurrentColorSet");
const Route$ = bindValue<ButtonState>(mod.id, 'Route');
const EditorVisible$ = bindValue<boolean>(mod.id, "EditorVisible");
const SubMeshData$ = bindValue<SubMeshData>(mod.id, "SubMeshData");
const CanResetOtherSubMeshes$ = bindValue<boolean>(mod.id, "CanResetOtherSubMeshes");
const ShowPaletteChoices$ = bindValue<ButtonState>(mod.id,"ShowPaletteChoices");
const PaletteChooserData$ = bindValue<PaletteChooserUIData>(mod.id, "PaletteChooserData");
const EditingPrefabEntity$ = bindValue<Entity>(mod.id, "EditingPrefabEntity");
const ShowPaletteEditorPanel$ = bindValue<boolean>(mod.id, "ShowPaletteEditorMenu");
const CopiedPaletteSet$ = bindValue<Entity[]>(mod.id, "CopiedPaletteSet");
const CopiedPalette$ = bindValue<Entity>(mod.id, "CopiedPalette");
const PaletteLibraryVersion$ = bindValue<number>(mod.id, "PaletteLibraryVersion");
const SubcategoryLibraryVersion$ = bindValue<number>(mod.id, "SubcategoryLibraryVersion");

export const InfoRowTheme: Theme | any = getModule(
	"game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss",
	"classes"
)

export const InfoSection: any = getModule( 
    "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx",
    "InfoSection"
)

export const InfoRow: any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx",
    "InfoRow"
)

function changeScope(newScope : Scope) {
    trigger(mod.id, "ChangeScope", newScope);
}

function changeSubMeshScope(newSubMeshScope : SubMeshScopes) {
    trigger(mod.id, "ChangeSubMeshScope", newSubMeshScope);
}

export function handleClick(eventName : string) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName);
}

function changeColor(channel : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangeColor", channel, newColor);
}


export const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");
    
export const roundButtonHighlightStyle = getModule("game-ui/common/input/button/themes/round-highlight-button.module.scss", "classes");

// This is working, but it's possible a better solution is possible.
export function DescriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null) : JSX.Element {
    return (
        <>
            <div className={descriptionToolTipStyle.title}>{tooltipTitle}</div>
            <div className={descriptionToolTipStyle.content}>{tooltipDescription}</div>
        </>
    );
}

export const RecolorMainPanelComponent = () => {
    const CurrentColorSet = useValue(CurrentColorSet$);   
    const SingleInstance = useValue(SingleInstance$);
    const Matching = useValue(Matching$);
    const ServiceVehicles = useValue(ServiceVehicles$);
    const MatchesVanillaColorSet : boolean[] = useValue(MatchesVanillaColorSet$);
    const CanPasteColorSet = useValue(CanPasteColorSet$);
    const Minimized = useValue(Minimized$);
    const MatchesSavedToDisk = useValue(MatchesSavedtoDisk$);
    const ShowHexaDecimals = useValue(ShowHexaDecimals$);
    const Route = useValue(Route$);
    const IsEditor = useValue(tool.isEditor$);
    const EditorVisible = useValue(EditorVisible$);
    const SubMeshData = useValue(SubMeshData$);
    const CanResetOtherSubMeshes = useValue(CanResetOtherSubMeshes$);
    const ShowPaletteChoices = useValue(ShowPaletteChoices$);    
    const PaletteChooserData = useValue(PaletteChooserData$);
    const EditingPrefabEntity = useValue(EditingPrefabEntity$);
    const ShowPaletteEditorPanel = useValue(ShowPaletteEditorPanel$);
    const CopiedPaletteSet = useValue(CopiedPaletteSet$);
    const CopiedPalette = useValue(CopiedPalette$);    
    const PaletteLibraryVersion = useValue(PaletteLibraryVersion$);
    const SubcategoryLibraryVersion = useValue(SubcategoryLibraryVersion$);
    
    // translation handling. Translates using locale keys that are defined in C# or fallback string from en-US.json.
    const { translate } = useLocalization();

    return (
        <>
            {(!IsEditor || EditorVisible) && (
                <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} >
                    <InfoRow 
                        left={translate("Recolor.SECTION_TITLE[InfoRowTitle]",locale["Recolor.SECTION_TITLE[InfoRowTitle]"])}
                        right=
                        {
                            <>
                                {!Minimized && (
                                <>
                                    {(ShowPaletteChoices & ButtonState.Hidden) != ButtonState.Hidden && (
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={colorPaletteSrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[TogglePaletteOptions]", locale["Recolor.TOOLTIP_DESCRIPTION[TogglePaletteOptions]"])}
                                            selected={ShowPaletteChoices == ButtonState.On}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => handleClick("ToggleShowPaletteChoices")}
                                        />
                                    )}
                                    <VanillaComponentResolver.instance.ToolButton
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        selected={ShowHexaDecimals}
                                        children={<div className={styles.buttonWithText}>#</div>} 
                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ShowHexaDecimals]", locale["Recolor.TOOLTIP_DESCRIPTION[ShowHexaDecimals]"])}
                                        className = {classNames(VanillaComponentResolver.instance.toolButtonTheme.button)}
                                        onSelect={() => handleClick("ToggleShowHexaDecimals")}
                                    />
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={colorPickerSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} 
                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ColorPicker]", locale["Recolor.TOOLTIP_DESCRIPTION[ColorPicker]"])}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => handleClick("ActivateColorPicker")}
                                    />
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={paintSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ColorPainter]", locale["Recolor.TOOLTIP_DESCRIPTION[ColorPainter]"])}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => handleClick("ActivateColorPainter")}
                                    />
                                </>
                                )}
                                <VanillaComponentResolver.instance.ToolButton
                                    src={Minimized? expandSrc : minimizeSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    tooltip = {Minimized? translate("Recolor.TOOLTIP_DESCRIPTION[Expand]" ,locale["Recolor.TOOLTIP_DESCRIPTION[Expand]"]) : translate("Recolor.TOOLTIP_DESCRIPTION[Minimize]", locale["Recolor.TOOLTIP_DESCRIPTION[Minimize]"])}
                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                    onSelect={() => handleClick("Minimize")}
                                />
                            </>
                        }
                        tooltip={translate("Recolor.TOOLTIP_DESCRIPTION[InfoRowTooltip]" ,locale["Recolor.TOOLTIP_DESCRIPTION[InfoRowTooltip]"])}
                        uppercase={true}
                        disableFocus={true}
                        subRow={false}
                        className={InfoRowTheme.infoRow}
                    ></InfoRow>
                    { SubMeshData.SubMeshLength > 1 && !Minimized && (
                        <InfoRow
                            left={translate("Recolor.SECTION_TITLE[SubMeshes]" ,locale["Recolor.SECTION_TITLE[SubMeshes]"])}
                            right= 
                            {
                                <div className={styles.columnGroup}>
                                    <>
                                        {SubMeshData.AllSubMeshes != ButtonState.On? 
                                            <div className={styles.rowGroup}>
                                                <Button className={roundButtonHighlightStyle.button} variant="icon" onSelect={() => {handleClick("ReduceSubMeshIndex");} } focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                                    <img src={arrowLeftSrc}></img>
                                                </Button>
                                                { SubMeshData.MatchingSubMeshes == ButtonState.On? 
                                                    <div className={styles.subMeshText}>{SubMeshData.SubMeshName}</div> :
                                                    <div className={styles.subMeshText}>{SubMeshData.SubMeshName} : {SubMeshData.SubMeshIndex+1} /{SubMeshData.SubMeshLength}</div>
                                                }
                                                <Button className={roundButtonHighlightStyle.button} variant="icon" onSelect={() => {handleClick("IncreaseSubMeshIndex");} } focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                                    <img src={arrowRightSrc}></img>
                                                </Button>
                                            </div>
                                            : <div className={styles.rowGroup}>
                                                    <span className={styles.subMeshButtonWidth}></span>
                                                    <span className={styles.subMeshText}></span>
                                                    <span className={styles.subMeshButtonWidth}></span>
                                              </div>
                                        }
                                        <div className={styles.rowGroup}>
                                            <>
                                                {(SubMeshData.SingleSubMesh & ButtonState.Hidden) != ButtonState.Hidden  && (
                                                    <VanillaComponentResolver.instance.ToolButton
                                                        src={singleSrc}
                                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                        selected = {SubMeshData.SingleSubMesh == ButtonState.On}
                                                        tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[SingleSubMesh]", locale["Recolor.TOOLTIP_TITLE[SingleSubMesh]"]), translate("Recolor.TOOLTIP_DESCRIPTION[SingleSubMesh]", locale["Recolor.TOOLTIP_DESCRIPTION[SingleSubMesh]"]))}
                                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                        onSelect={() => { changeSubMeshScope(SubMeshScopes.SingleInstance)}}
                                                    />
                                                )}
                                                {(SubMeshData.MatchingSubMeshes & ButtonState.Hidden) != ButtonState.Hidden  && (
                                                    <VanillaComponentResolver.instance.ToolButton
                                                        src={matchingSrc}
                                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                        selected = {SubMeshData.MatchingSubMeshes == ButtonState.On}
                                                        tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[MatchingSubmeshes]", locale["Recolor.TOOLTIP_TITLE[MatchingSubmeshes]"]), translate("Recolor.TOOLTIP_DESCRIPTION[MatchingSubmeshes]", locale["Recolor.TOOLTIP_DESCRIPTION[MatchingSubmeshes]"]))}
                                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                        onSelect={() => { changeSubMeshScope(SubMeshScopes.Matching)}}
                                                    />
                                                )}
                                                {(SubMeshData.AllSubMeshes & ButtonState.Hidden) != ButtonState.Hidden  && (
                                                    <VanillaComponentResolver.instance.ToolButton
                                                        src={allSrc}
                                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                        selected = {SubMeshData.AllSubMeshes == ButtonState.On}
                                                        tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[AllSubmeshes]", locale["Recolor.TOOLTIP_TITLE[AllSubmeshes]"]), translate("Recolor.TOOLTIP_DESCRIPTION[AllSubmeshes]", locale["Recolor.TOOLTIP_DESCRIPTION[AllSubmeshes]"]))}
                                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                        onSelect={() => { changeSubMeshScope(SubMeshScopes.All)}}
                                                    />
                                                )}
                                            </>
                                        </div>
                                    </>
                                </div>
                            }
                            uppercase={false}
                            disableFocus={true}
                            subRow={true}
                            className={InfoRowTheme.infoRow}
                        ></InfoRow>
                    )}

                    { !Minimized && (
                        <>
                            <InfoRow
                                left={translate("Recolor.SECTION_TITLE[ColorSet]" ,locale["Recolor.SECTION_TITLE[ColorSet]"])}          
                                right={
                                    <>
                                        {(SingleInstance & ButtonState.Hidden) != ButtonState.Hidden && (                             
                                            <VanillaComponentResolver.instance.ToolButton
                                                src={singleSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                selected = {SingleInstance == ButtonState.On}   
                                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[SingleInstance]",locale["Recolor.TOOLTIP_TITLE[SingleInstance]"]), translate("Recolor.TOOLTIP_DESCRIPTION[SingleInstance]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SingleInstance]"]))}
                                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                onSelect={() => changeScope(Scope.SingleInstance)}
                                            />
                                        )} 
                                        {(Matching & ButtonState.Hidden) != ButtonState.Hidden && (   
                                            <VanillaComponentResolver.instance.ToolButton
                                                src={matchingSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                selected = {Matching == ButtonState.On} 
                                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[Matching]",locale["Recolor.TOOLTIP_TITLE[Matching]"]), translate("Recolor.TOOLTIP_DESCRIPTION[Matching]" ,locale["Recolor.TOOLTIP_DESCRIPTION[Matching]"]))}
                                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                onSelect={() => changeScope(Scope.Matching)}
                                            />
                                        )}
                                        {(ServiceVehicles & ButtonState.Hidden) != ButtonState.Hidden && (   
                                            <VanillaComponentResolver.instance.ToolButton
                                                src={serviceVehiclesSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                selected = {ServiceVehicles == ButtonState.On} 
                                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[ServiceVehicles]" ,locale["Recolor.TOOLTIP_TITLE[ServiceVehicles]"]), translate("Recolor.TOOLTIP_DESCRIPTION[ServiceVehicles]" , locale["Recolor.TOOLTIP_DESCRIPTION[ServiceVehicles]"]))}
                                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                onSelect={() => changeScope(Scope.ServiceVehicles)}
                                            />
                                        )}
                                        {(Route & ButtonState.Hidden) != ButtonState.Hidden && (   
                                            <VanillaComponentResolver.instance.ToolButton
                                                src={routeSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                selected = {Route == ButtonState.On} 
                                                tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[RouteVehicles]" ,locale["Recolor.TOOLTIP_TITLE[RouteVehicles]"]), translate("Recolor.TOOLTIP_DESCRIPTION[RouteVehicles]", locale["Recolor.TOOLTIP_DESCRIPTION[RouteVehicles]"]))}
                                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                onSelect={() => changeScope(Scope.Route)}
                                            />
                                        )}

                                        <VanillaComponentResolver.instance.ToolButton
                                            src={copySrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} 
                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColorSet]" ,locale["Recolor.TOOLTIP_DESCRIPTION[CopyColorSet]"])}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => handleClick("CopyColorSet")}
                                        />
                                        {CanPasteColorSet && (
                                            <VanillaComponentResolver.instance.ToolButton
                                                src={pasteSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColorSet]", locale["Recolor.TOOLTIP_DESCRIPTION[PasteColorSet]"])}
                                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                onSelect={() => handleClick("PasteColorSet")}
                                            />  
                                        )}
                                        { (!MatchesVanillaColorSet[0] || !MatchesVanillaColorSet[1] || !MatchesVanillaColorSet[2] || CanResetOtherSubMeshes) && (        
                                            <VanillaComponentResolver.instance.ToolButton
                                                src={resetSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                tooltip = {((Matching & ButtonState.Hidden) == ButtonState.Hidden)? translate("Recolor.TOOLTIP_DESCRIPTION[ResetInstanceColor]" ,locale["Recolor.TOOLTIP_DESCRIPTION[ResetInstanceColor]"]) : translate("Recolor.TOOLTIP_DESCRIPTION[ResetColorSet]",locale["Recolor.TOOLTIP_DESCRIPTION[ResetColorSet]"])}
                                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                onSelect={() => handleClick("ResetColorSet")}
                                            />
                                        )}
                                        { (!MatchesVanillaColorSet[0] || !MatchesVanillaColorSet[1] || !MatchesVanillaColorSet[2]) && Matching == ButtonState.On &&(        
                                            <VanillaComponentResolver.instance.ToolButton
                                                src={saveToDiskSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                selected={MatchesSavedToDisk}
                                                tooltip = {MatchesSavedToDisk?  translate("Recolor.TOOLTIP_DESCRIPTION[RemoveFromDisk]" ,locale["Recolor.TOOLTIP_DESCRIPTION[RemoveFromDisk]"]) : translate("Recolor.TOOLTIP_DESCRIPTION[SaveToDisk]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SaveToDisk]"])}
                                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                onSelect={() => handleClick(MatchesSavedToDisk? "RemoveFromDisk" : "SaveToDisk")}
                                            />
                                            
                                        )}
                                    </>
                                }                      
                                uppercase={false}
                                disableFocus={true}
                                subRow={true}
                                className={InfoRowTheme.infoRow}
                            >
                            </InfoRow>
                            <InfoRow 
                                right=
                                {
                                    <>
                                        <SIPColorComponent channel={0}></SIPColorComponent>
                                        { (PaletteChooserData.SelectedPaletteEntities[0].index == 0 && PaletteChooserData.SelectedPaletteEntities[1].index == 0) ?  
                                            <div className={styles.columnGroup}>
                                                <VanillaComponentResolver.instance.ToolButton
                                                    src={swapSrc}
                                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapColors]", locale["Recolor.TOOLTIP_DESCRIPTION[SwapColors]"])}
                                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                    onSelect={() => 
                                                    {
                                                        let channel0 : Color = CurrentColorSet.Channels[0];
                                                        changeColor(0, CurrentColorSet.Channels[1]);
                                                        changeColor(1, channel0);
                                                    }}
                                                />
                                                <span className={styles.belowSwapButton}></span>  
                                                {ShowHexaDecimals && (
                                                    <span className={styles.inputHeight}></span>
                                                )}
                                            </div> : 
                                            <div className={styles.columnGroup}>
                                                <span className={styles.ButtonWidth}></span>  
                                                <span className={styles.belowSwapButton}></span>  
                                                {ShowHexaDecimals && (
                                                    <span className={styles.inputHeight}></span>
                                                )}
                                            </div>
                                        }
                                        <SIPColorComponent channel={1}></SIPColorComponent>
                                        { (PaletteChooserData.SelectedPaletteEntities[1].index == 0 && PaletteChooserData.SelectedPaletteEntities[2].index == 0) ?  
                                            <div className={styles.columnGroup}>
                                                <VanillaComponentResolver.instance.ToolButton
                                                    src={swapSrc}
                                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                    tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapColors]", locale["Recolor.TOOLTIP_DESCRIPTION[SwapColors]"])}
                                                    className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                    onSelect={() => 
                                                    {
                                                        let channel1 : Color = CurrentColorSet.Channels[1];
                                                        changeColor(1, CurrentColorSet.Channels[2]);
                                                        changeColor(2, channel1);
                                                    }}
                                                />
                                                <span className={styles.belowSwapButton}></span>  
                                                {ShowHexaDecimals && (
                                                    <span className={styles.inputHeight}></span>
                                                )}
                                            </div> : 
                                            <div className={styles.columnGroup}>
                                                <span className={styles.ButtonWidth}></span>  
                                                <span className={styles.belowSwapButton}></span>  
                                                {ShowHexaDecimals && (
                                                    <span className={styles.inputHeight}></span>
                                                )}
                                            </div>
                                        }
                                        <SIPColorComponent channel={2}></SIPColorComponent>
                                    </>
                                }
                                subRow={true}
                                className={InfoRowTheme.infoRow}
                            ></InfoRow>
                        </>
                    )}
                    { !Minimized && ShowPaletteChoices == ButtonState.On && (
                        <>
                        <InfoRow
                            left={translate("Recolor.SECTION_TITLE[Palette]", locale["Recolor.SECTION_TITLE[Palette]"])}
                            right={
                                <div className={styles.rowGroup}>
                                    { (PaletteChooserData.SelectedPaletteEntities[0].index != 0 || PaletteChooserData.SelectedPaletteEntities[1].index != 0 || PaletteChooserData.SelectedPaletteEntities[2].index != 0) && (
                                    <VanillaComponentResolver.instance.ToolButton src={copySrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                    tooltip = {translate ( "Recolor.TOOLTIP_DESCRIPTION[CopyPaletteSet]",locale["Recolor.TOOLTIP_DESCRIPTION[CopyPaletteSet]"])}
                                                                                    onSelect={() => { trigger(mod.id, "CopyPaletteSet", PaletteChooserData.SelectedPaletteEntities)}}
                                    />
                                    )} 
                                    { (CopiedPaletteSet[0].index != 0 ||  CopiedPaletteSet[1].index != 0 || CopiedPaletteSet[2].index != 0) && (
                                    <VanillaComponentResolver.instance.ToolButton src={pasteSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                    tooltip = {translate ("Recolor.TOOLTIP_DESCRIPTION[PastePaletteSet]" , locale["Recolor.TOOLTIP_DESCRIPTION[PastePaletteSet]"])}
                                                                                    onSelect={() => { trigger(mod.id, "AssignPalette", 0, CopiedPaletteSet[0]); trigger(mod.id, "AssignPalette", 1, CopiedPaletteSet[1]); trigger(mod.id, "AssignPalette", 2, CopiedPaletteSet[2]);}}
                                    />
                                    )} 
                                    <VanillaComponentResolver.instance.ToolButton src={plusSrc}  className = {VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                                                    tooltip = {(ShowPaletteEditorPanel && EditingPrefabEntity.index == 0) ? translate("Recolor.TOOLTIP_DESCRIPTION[CloseEditorPanel]" ,locale["Recolor.TOOLTIP_DESCRIPTION[CloseEditorPanel]"])  : translate("Recolor.TOOLTIP_DESCRIPTION[GenerateNewPalette]" ,locale["Recolor.TOOLTIP_DESCRIPTION[GenerateNewPalette]"])} 
                                                                                    selected={ShowPaletteEditorPanel && EditingPrefabEntity.index == 0} 
                                                                                    onSelect={() => { if (!ShowPaletteEditorPanel || (ShowPaletteEditorPanel && EditingPrefabEntity.index == 0))  {handleClick("TogglePaletteEditorMenu"); }
                                                                                                    if (EditingPrefabEntity.index != 0) {handleClick("GenerateNewPalette");}}}
                                    />
                                </div>
                            }
                            uppercase={false}
                            disableFocus={true}
                            subRow={true}
                            className={InfoRowTheme.infoRow}
                        ></InfoRow>
                        <InfoRow
                            right={
                                <FocusDisabled>
                                    <PaletteChooserComponent channel={0} PaletteChooserData={PaletteChooserData}></PaletteChooserComponent>
                                    <div className={styles.columnGroup}>
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={swapSrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]"])}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => 
                                            {
                                                let entity0 : Entity = PaletteChooserData.SelectedPaletteEntities[0];
                                                PaletteChooserData.SelectedPaletteEntities[1].index != 0 ? assignPalette(0, PaletteChooserData.SelectedPaletteEntities[1]) : removePalette(0);
                                                entity0.index != 0 ? assignPalette(1, entity0) : removePalette(1);
                                            }}
                                        />
                                        { (PaletteChooserData.SelectedPaletteEntities[0].index != 0 || PaletteChooserData.SelectedPaletteEntities[1].index != 0 || PaletteChooserData.SelectedPaletteEntities[2].index != 0 || CopiedPalette.index != 0)   ?
                                            <span className={styles.belowSwapButton}></span> : <span className={styles.belowSwapButtonSmall}></span>
                                        }
                                    </div>
                                    <PaletteChooserComponent channel={1}  PaletteChooserData={PaletteChooserData}></PaletteChooserComponent>
                                    <div className={styles.columnGroup}>
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={swapSrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SwapPalettes]"])}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => 
                                            {
                                                let entity1 : Entity = PaletteChooserData.SelectedPaletteEntities[1];
                                                PaletteChooserData.SelectedPaletteEntities[2].index != 0 ? assignPalette(1, PaletteChooserData.SelectedPaletteEntities[2]) : removePalette(1);
                                                entity1.index != 0 ? assignPalette(2, entity1) : removePalette(2);
                                            }}
                                        />
                                        { (PaletteChooserData.SelectedPaletteEntities[0].index != 0 || PaletteChooserData.SelectedPaletteEntities[1].index != 0 || PaletteChooserData.SelectedPaletteEntities[2].index != 0 || CopiedPalette.index != 0)   ?
                                            <span className={styles.belowSwapButton}></span> : <span className={styles.belowSwapButtonSmall}></span>
                                        }
                                    </div>
                                    <PaletteChooserComponent channel={2}  PaletteChooserData={PaletteChooserData}></PaletteChooserComponent>
                                </FocusDisabled>
                            }
                            uppercase={false}
                            disableFocus={true}
                            subRow={true}
                            className={InfoRowTheme.infoRow}
                        ></InfoRow>
                        </>
                    )}
                </InfoSection>
            )}
        </>
    );
}