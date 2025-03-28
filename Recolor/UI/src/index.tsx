import { ModRegistrar } from "cs2/modding";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import mod from "../mod.json";
import { RecolorSelectedInfoPanelComponent } from "mods/RecolorSelectedInfoPanel/SIPcolorFields";
import { ToolOptionsVisibility } from "mods/ToolOptionsVisible/toolOptionsVisible";
import { ColorPainterSectionComponent } from "mods/colorPainterSection/colorPainterSection";
import { RecolorEditorPanel } from "mods/RecolorEditorPanel/RecolorEditorPanel";

const register: ModRegistrar = (moduleRegistry) => {
      // console.log('mr', moduleRegistry);

      // The vanilla component resolver is a singleton that helps extrant and maintain components from game that were not specifically exposed.
      VanillaComponentResolver.setRegistry(moduleRegistry);

      
     // This extends mouse tooltip options with Anarchy section and toggle. It may or may not work with gamepads.
     moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', ColorPainterSectionComponent);
     //
     
     moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', RecolorSelectedInfoPanelComponent);

     moduleRegistry.extend("game-ui/game/components/tool-options/tool-options-panel.tsx", 'useToolOptionsVisible', ToolOptionsVisibility);

     // This appends the editor ui to include a recolor panel.
     moduleRegistry.append('Editor', RecolorEditorPanel);
     
     // This is just to verify using UI console that all the component registriations was completed.
     console.log(mod.id + " UI module registrations completed.");
}

export default register;