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

const uilStandard =                          "coui://uil/Standard/";
const uilColored =                           "coui://uil/Colored/";
const resetSrc =                        uilStandard + "Reset.svg";
const singleSrc =                        uilStandard + "SingleRhombus.svg";
const matchingSrc =                     uilStandard + "SameRhombus.svg";
const allSrc =                          uilStandard + "StarAll.svg";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const colorPickerSrc =                  uilStandard + "PickerPipette.svg";
const colorPaleteSrc =                 uilColored + "ColorPalette.svg";
const minimizeSrc =                     uilStandard + "ArrowsMinimize.svg";
const expandSrc =                       uilStandard + "ArrowsExpand.svg";
const saveToDiskSrc =                   uilStandard + "DiskSave.svg";
const swapSrc =                         uilStandard + "ArrowsMoveLeftRight.svg";
const serviceVehiclesSrc =              uilStandard + "ServiceVehicles.svg";
const routeSrc =                        uilStandard + "BusShelter.svg";
const arrowLeftSrc =           uilStandard +  "ArrowLeftThickStroke.svg";
const arrowRightSrc =           uilStandard +  "ArrowRightThickStroke.svg";

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

function handleClick(eventName : string) {
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
function DescriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null) : JSX.Element {
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
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={colorPaleteSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        tooltip = {"Toggle Palette Options"}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => handleClick("TogglePaletteOptions")}
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
                                    <VanillaComponentResolver.instance.ToolButton
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        selected={ShowHexaDecimals}
                                        children={<div className={styles.buttonWithText}>#</div>} 
                                        tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ShowHexaDecimals]", locale["Recolor.TOOLTIP_DESCRIPTION[ShowHexaDecimals]"])}
                                        className = {classNames(VanillaComponentResolver.instance.toolButtonTheme.button)}
                                        onSelect={() => handleClick("ToggleShowHexaDecimals")}
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
                        <InfoRow 
                            left={translate("Recolor.SECTION_TITLE[ColorSet]" ,locale["Recolor.SECTION_TITLE[ColorSet]"])}
                            right=
                            {
                                <>
                                    <SIPColorComponent channel={0}></SIPColorComponent>
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
                                    </div>
                                    <SIPColorComponent channel={1}></SIPColorComponent>
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
                                    </div>
                                    <SIPColorComponent channel={2}></SIPColorComponent>
                                </>
                            }
                            uppercase={false}
                            disableFocus={true}
                            subRow={true}
                            className={InfoRowTheme.infoRow}
                        ></InfoRow>
                    )}
                </InfoSection>
            )}
        </>
    );
}