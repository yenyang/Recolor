import { roundButtonHighlightStyle } from "mods/RecolorMainPanel/RecolorMainPanel";
import styles from "../Domain//ColorFields.module.scss";
import panelStyles from "./PaletteMenuStyles.module.scss";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { Button } from "cs2/ui";
import { getModule } from "cs2/modding";
import { trigger } from "cs2/api";
import mod from "../../../mod.json";


const uilStandard =                         "coui://uil/Standard/";
const closeSrc =         uilStandard +  "XClose.svg";

const panelTheme = getModule("game-ui/common/panel/panel.module.scss", "classes");

export const HeaderSection = (props: {title: string, icon: string, onCloseEventName: string}) => {
    return (
        <div className={styles.rowGroup}>
            <div className={panelTheme.icon}>
                <img src={props.icon}></img>
            </div>
            <div className={panelStyles.headerWidth}>
                {props.title}
            </div>
            <Button className={roundButtonHighlightStyle.button} variant="icon" onSelect={() => trigger(mod.id, props.onCloseEventName)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                    <img src={closeSrc}></img>
            </Button>
        </div>
    );
}