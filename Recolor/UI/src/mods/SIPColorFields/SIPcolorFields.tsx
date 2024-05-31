import { getModule } from "cs2/modding";
import { Theme, FocusKey, UniqueFocusKey, Color } from "cs2/bindings";
import { bindValue, trigger, useValue } from "cs2/api";
import { ColorSet } from "mods/Domain/ColorSet";
import { useLocalization } from "cs2/l10n";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import locale from "../lang/en-US.json";

interface InfoSectionComponent {
	group: string;
	tooltipKeys: Array<string>;
	tooltipTags: Array<string>;
}

const uilStandard =                          "coui://uil/Standard/";
const saveSrc =                     uilStandard +  "DiskSave.svg";
const resetSrc =                     uilStandard + "Reset.svg";

const CurrentColorSet$ = bindValue<ColorSet>(mod.id, "CurrentColorSet");
const MatchesSavedData$ = bindValue<boolean>(mod.id, "MatchesSavedColorSet")

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

function changeColor(channel : number, newColor : Color) {
    // This triggers an event on C# side and C# designates the method to implement.
    trigger(mod.id, "ChangeColor", channel, newColor);
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
	componentList["Recolor.Systems.SelectedInfoPanelColorFieldsSystem"] = (e: InfoSectionComponent) => {
        // These get the value of the bindings.
        const CurrentColorSet = useValue(CurrentColorSet$);
        const MatchesSavedColorSet  = useValue(MatchesSavedData$);
        
        // translation handling. Translates using locale keys that are defined in C# or fallback string from en-US.json.
        const { translate } = useLocalization();

        return 	<InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true} className={InfoSectionTheme.infoSection}>
                        <InfoRow 
                            left={translate("Tree_Controller.SECTION_TITLE[InfoRowTitle]",locale["Tree_Controller.SECTION_TITLE[InfoRowTitle]"])}
                            right={translate("Tree_Controller.SECTION_TITLE[InfoRowSubTitle]" ,locale["Tree_Controller.SECTION_TITLE[InfoRowSubTitle]"])}
                            tooltip={translate("Tree_Controller.TOOLTIP_DESCRIPTION[InfoRowTooltip]" ,locale["Tree_Controller.TOOLTIP_DESCRIPTION[InfoRowTooltip]"])}
                            uppercase={true}
                            disableFocus={true}
                            subRow={false}
                            className={InfoRowTheme.infoRow}
                        ></InfoRow>
                        <InfoRow 
                            left={translate("Tree_Controller.SECTION_TITLE[Channel0]" , locale["Tree_Controller.SECTION_TITLE[Channel0]"])}
                            right=
                            {
                                <VanillaComponentResolver.instance.ColorField value={CurrentColorSet.Channel0} onChange={(e) => {changeColor(0, e);}}/>
                            }
                            uppercase={false}
                            disableFocus={true}
                            subRow={true}
                            className={InfoRowTheme.infoRow}
                        ></InfoRow>
                        <InfoRow 
                            left={translate("Tree_Controller.SECTION_TITLE[Channel1]" ,locale["Tree_Controller.SECTION_TITLE[Channel1]"])}
                            right=
                            {
                                <VanillaComponentResolver.instance.ColorField value={CurrentColorSet.Channel1} onChange={(e) => {changeColor(1, e);}}/>
                            }
                            uppercase={false}
                            disableFocus={true}
                            subRow={true}
                            className={InfoRowTheme.infoRow}
                        ></InfoRow>
                        <InfoRow 
                            left={translate("Tree_Controller.SECTION_TITLE[Channel2]",locale["Tree_Controller.SECTION_TITLE[Channel2]"])}
                            right=
                            {
                                <VanillaComponentResolver.instance.ColorField value={CurrentColorSet.Channel2} onChange={(e) => {changeColor(2, e);}}/>
                            }
                            uppercase={false}
                            disableFocus={true}
                            subRow={true}
                            className={InfoRowTheme.infoRow}
                        ></InfoRow>
                        <InfoRow 
                            left={translate("Tree_Controller.SECTION_TITLE[ResetAndSave]" ,locale["Tree_Controller.SECTION_TITLE[ResetAndSave]"])}
                            right=
                            {
                                <>                                
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={resetSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        multiSelect = {false}   // I haven't tested any other value here
                                        disabled = {false}      // I haven't tested any other value here
                                        tooltip = {DescriptionTooltip(translate("Tree_Controller.TOOLTIP_TITLE[Reset]" ,locale["Tree_Controller.TOOLTIP_TITLE[Reset]"]), translate("Tree_Controller.TOOLTIP_DESCRIPTION[Reset]",locale["Tree_Controller.TOOLTIP_DESCRIPTION[Reset]"]))}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => handleClick("ResetColorSet")}
                                    />
                                    <VanillaComponentResolver.instance.ToolButton
                                        src={saveSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        selected = {MatchesSavedColorSet}
                                        multiSelect = {false}   // I haven't tested any other value here
                                        disabled = {false}      // I haven't tested any other value here
                                        tooltip = {DescriptionTooltip( translate ("Tree_Controller.TOOLTIP_TITLE[Save]" ,locale["Tree_Controller.TOOLTIP_TITLE[Save]"]), translate("Tree_Controller.TOOLTIP_DESCRIPTION[Save]" ,locale["Tree_Controller.TOOLTIP_DESCRIPTION[Save]"]))}
                                        className = {VanillaComponentResolver.instance.toolButtonTheme.button}
                                        onSelect={() => handleClick("SaveColorSet")}
                                    />
                                </>
                            }
                            uppercase={false}
                            disableFocus={true}
                            subRow={true}
                            className={InfoRowTheme.infoRow}
                        ></InfoRow>
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