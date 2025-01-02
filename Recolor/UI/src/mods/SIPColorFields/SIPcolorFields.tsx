import { getModule } from "cs2/modding";
import { Theme, FocusKey, UniqueFocusKey, Color } from "cs2/bindings";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";
import { SIPColorComponent } from "mods/SIPColorComponent/SIPColorComponent";
import styles from "../Domain/ColorFields.module.scss";
import classNames from "classnames";

interface InfoSectionComponent {
	group: string;
	tooltipKeys: Array<string>;
	tooltipTags: Array<string>;
}

const uilStandard =                          "coui://uil/Standard/";
const uilColored =                           "coui://uil/Colored/";
const resetSrc =                        uilStandard + "Reset.svg";
const singleSrc =                        uilStandard + "SingleRhombus.svg";
const matchingSrc =                     uilStandard + "SameRhombus.svg";
const copySrc =                         uilStandard + "RectangleCopy.svg";
const pasteSrc =                        uilStandard + "RectanglePaste.svg";
const colorPickerSrc =                  uilStandard + "PickerPipette.svg";
const colorPaleteSrc =                 uilColored + "ColorPalette.svg";
const minimizeSrc =                     uilStandard + "ArrowsMinimize.svg";
const expandSrc =                       uilStandard + "ArrowsExpand.svg";
const saveToDiskSrc =                   uilStandard + "DiskSave.svg";

const SingleInstance$ = bindValue<boolean>(mod.id, 'SingleInstance');
const DisableSingleInstance$ = bindValue<boolean>(mod.id, 'DisableSingleInstance');
const DisableMatching$ = bindValue<boolean>(mod.id, 'DisableMatching');
const MatchesVanillaColorSet$ = bindValue<boolean[]>(mod.id, 'MatchesVanillaColorSet');
const CanPasteColorSet$ = bindValue<boolean>(mod.id, "CanPasteColorSet");
const Minimized$ = bindValue<boolean>(mod.id, "Minimized");
const MatchesSavedtoDisk$ = bindValue<boolean>(mod.id, "MatchesSavedOnDisk");
const ShowHexaDecimals$ = bindValue<boolean>(mod.id, "ShowHexaDecimals");

const InfoSectionTheme: Theme | any = getModule(
	"game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss",
	"classes"
);

const InfoRowTheme: Theme | any = getModule(
	"game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss",
	"classes"
)

const InfoSection: any = getModule( 
    "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx",
    "InfoSection"
)

const InfoRow: any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx",
    "InfoRow"
)

function handleClick(eventName : string) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName);
}

function handleChannelClick(eventName : string, channel : number) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, eventName, channel);
}

const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");
    

// This is working, but it's possible a better solution is possible.
function DescriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null) : JSX.Element {
    return (
        <>
            <div className={descriptionToolTipStyle.title}>{tooltipTitle}</div>
            <div className={descriptionToolTipStyle.content}>{tooltipDescription}</div>
        </>
    );
}

export const SIPcolorFieldsComponent = (componentList: any): any => {
    // I believe you should not put anything here.
	componentList["Recolor.Systems.SelectedInfoPanel.SIPColorFieldsSystem"] = (e: InfoSectionComponent) => {
        // These get the value of the bindings.
        const SingleInstance = useValue(SingleInstance$);
        const DisableSingleInstance = useValue(DisableSingleInstance$);
        const DisableMatching = useValue(DisableMatching$);
        const MatchesVanillaColorSet : boolean[] = useValue(MatchesVanillaColorSet$);
        const CanPasteColorSet = useValue(CanPasteColorSet$);
        const Minimized = useValue(Minimized$);
        const MatchesSavedToDisk = useValue(MatchesSavedtoDisk$);
        const ShowHexaDecimals = useValue(ShowHexaDecimals$);
        
        // translation handling. Translates using locale keys that are defined in C# or fallback string from en-US.json.
        const { translate } = useLocalization();

        return 	<InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} className={InfoSectionTheme.infoSection}>
                        <InfoRow 
                            left={translate("Recolor.SECTION_TITLE[InfoRowTitle]",locale["Recolor.SECTION_TITLE[InfoRowTitle]"])}
                            right=
                            {
                                <>  
                                    {!Minimized && (
                                    <>
                                        {!DisableSingleInstance && (                             
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={singleSrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            selected = {(SingleInstance || DisableMatching) && !DisableSingleInstance}
                                            multiSelect = {false}   // I haven't tested any other value here
                                            disabled = {DisableSingleInstance}      
                                            tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[SingleInstance]",locale["Recolor.TOOLTIP_TITLE[SingleInstance]"]), translate("Recolor.TOOLTIP_DESCRIPTION[SingleInstance]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SingleInstance]"]))}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => handleClick("SingleInstance")}
                                        />)} 
                                        {!DisableMatching && (   
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={matchingSrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            selected = {(!SingleInstance || DisableSingleInstance) && !DisableMatching}
                                            multiSelect = {false}   // I haven't tested any other value here
                                            disabled = {DisableMatching}     
                                            tooltip = {DescriptionTooltip(translate("Recolor.TOOLTIP_TITLE[Matching]",locale["Recolor.TOOLTIP_TITLE[Matching]"]), translate("Recolor.TOOLTIP_DESCRIPTION[Matching]" ,locale["Recolor.TOOLTIP_DESCRIPTION[Matching]"]))}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => handleClick("Matching")}
                                        />)}
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={copySrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            multiSelect = {false}   // I haven't tested any other value here
                                            disabled = {false}      
                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[CopyColorSet]" ,locale["Recolor.TOOLTIP_DESCRIPTION[CopyColorSet]"])}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => handleClick("CopyColorSet")}
                                        />
                                        {CanPasteColorSet && (
                                            <VanillaComponentResolver.instance.ToolButton
                                                src={pasteSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                                multiSelect = {false}   // I haven't tested any other value here
                                                disabled = {false}      
                                                tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[PasteColorSet]", locale["Recolor.TOOLTIP_DESCRIPTION[PasteColorSet]"])}
                                                className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                                onSelect={() => handleClick("PasteColorSet")}
                                            />  
                                        )}
                                        { (!MatchesVanillaColorSet[0] || !MatchesVanillaColorSet[1] || !MatchesVanillaColorSet[2]) && (        
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={resetSrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            multiSelect = {false}   // I haven't tested any other value here
                                            disabled = {false}      
                                            tooltip = {DisableMatching? translate("Recolor.TOOLTIP_DESCRIPTION[ResetInstanceColor]" ,locale["Recolor.TOOLTIP_DESCRIPTION[ResetInstanceColor]"]) : translate("Recolor.TOOLTIP_DESCRIPTION[ResetColorSet]",locale["Recolor.TOOLTIP_DESCRIPTION[ResetColorSet]"])}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => handleClick("ResetColorSet")}
                                        />)}
                                        { (!MatchesVanillaColorSet[0] || !MatchesVanillaColorSet[1] || !MatchesVanillaColorSet[2]) && !DisableMatching && (!SingleInstance || DisableSingleInstance) &&(        
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={saveToDiskSrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            selected={MatchesSavedToDisk}
                                            multiSelect = {false}   // I haven't tested any other value here
                                            disabled = {false}      
                                            tooltip = {MatchesSavedToDisk?  translate("Recolor.TOOLTIP_DESCRIPTION[RemoveFromDisk]" ,locale["Recolor.TOOLTIP_DESCRIPTION[RemoveFromDisk]"]) : translate("Recolor.TOOLTIP_DESCRIPTION[SaveToDisk]" ,locale["Recolor.TOOLTIP_DESCRIPTION[SaveToDisk]"])}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => handleClick(MatchesSavedToDisk? "RemoveFromDisk" : "SaveToDisk")}
                                        />)}
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={colorPickerSrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            multiSelect = {false}   // I haven't tested any other value here
                                            disabled = {false}      
                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ColorPicker]", locale["Recolor.TOOLTIP_DESCRIPTION[ColorPicker]"])}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => handleClick("ActivateColorPicker")}
                                        />
                                        <VanillaComponentResolver.instance.ToolButton
                                            src={colorPaleteSrc}
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            multiSelect = {false}   // I haven't tested any other value here
                                            disabled = {false}      
                                            tooltip = {translate("Recolor.TOOLTIP_DESCRIPTION[ColorPainter]", locale["Recolor.TOOLTIP_DESCRIPTION[ColorPainter]"])}
                                            className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                            onSelect={() => handleClick("ActivateColorPainter")}
                                        />
                                        <VanillaComponentResolver.instance.ToolButton
                                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                            multiSelect = {false}   // I haven't tested any other value here
                                            disabled = {false}     
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
                                        multiSelect = {false}   // I haven't tested any other value here
                                        disabled = {false}      
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
                        { !Minimized && (
                            <InfoRow 
                                left={translate("Recolor.SECTION_TITLE[ColorSet]" ,locale["Recolor.SECTION_TITLE[ColorSet]"])}
                                right=
                                {
                                    <>
                                        <SIPColorComponent channel={0}></SIPColorComponent>
                                        <SIPColorComponent channel={1}></SIPColorComponent>
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
				;
    }

	return componentList as any;
}

/*
      var t = e.icon
              , n = e.left
              , r = e.right
              , i = e.tooltip
              , o = e.link
              , a = e.uppercase
              , s = void 0 !== a && a
              , l = e.subRow
              , c = void 0 !== l && l
              , u = e.disableFocus
              , d = void 0 !== u && u
              , f = e.className;
              /*
/*


                        /*
/*
let VS = {
            "info-row": "info-row_QQ9 item-focused_FuT",
            infoRow: "info-row_QQ9 item-focused_FuT",
            "disable-focus-highlight": "disable-focus-highlight_I85",
            disableFocusHighlight: "disable-focus-highlight_I85",
            link: "link_ICj",
            tooltipRow: "tooltipRow_uIh",
            left: "left_RyE",
            hasIcon: "hasIcon_iZ3",
            right: "right_ZUb",
            icon: "icon_ugE",
            uppercase: "uppercase_f0y",
            subRow: "subRow_NJI"
        };

		 let fS = {
            "info-section": "info-section_I7V",
            infoSection: "info-section_I7V",
            content: "content_Cdk item-focused_FuT",
            column: "column_aPB",
            divider: "divider_rfM",
            "no-margin": "no-margin_K7I",
            noMargin: "no-margin_K7I",
            "disable-focus-highlight": "disable-focus-highlight_ik3",
            disableFocusHighlight: "disable-focus-highlight_ik3",
            "info-wrap-box": "info-wrap-box_Rt4",
            infoWrapBox: "info-wrap-box_Rt4"
        };
		*/