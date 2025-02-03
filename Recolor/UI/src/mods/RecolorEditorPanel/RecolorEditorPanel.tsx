import { Panel, Portal } from "cs2/ui";
import editorStyles from "./RecolorEditorPanelStyles.module.scss";
import { RecolorMainPanelComponent } from "mods/RecolorMainPanel/RecolorMainPanel";
import { ColorPainterToolOptionsComponent } from "mods/ColorPainterToolOptionsComponent/ColorPainterToolOptionsComponent";
import { getModule } from "cs2/modding";
import { Theme, tool } from "cs2/bindings";
import { useValue } from "cs2/api";

const ToolOptionsTheme: Theme | any = getModule(
    "game-ui/game/components/tool-options/tool-options-panel.module.scss",
    "classes"
)

export const RecolorEditorPanel = () => {
    const toolActive = useValue(tool.activeTool$).id == "ColorPainterTool"; 

    return (
        <>
            <Portal>
                <Panel className={editorStyles.panel}>
                    <div className={editorStyles.panelSection}>
                        {RecolorMainPanelComponent()}
                    </div>
                    <div className={toolActive? ToolOptionsTheme.toolOptionsPanel : ""}>
                        {ColorPainterToolOptionsComponent()}
                    </div>
                </Panel>
            </Portal>
        </>
    );
}