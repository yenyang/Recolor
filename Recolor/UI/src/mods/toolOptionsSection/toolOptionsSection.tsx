import {ModuleRegistryExtend} from "cs2/modding";
import { ColorPainterToolOptionsComponent } from "mods/ColorPainterToolOptionsComponent/ColorPainterToolOptionsComponent";
import { PalettesDuringPlacementComponent } from "mods/PalettesDuringPlacementComponent/PalettesDuringPlacementComponent";

export const ToolOptionsSectionComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {       
        // This defines aspects of the components.
        const {children, ...otherProps} = props || {};

        // This gets the original component that we may alter and return.
        var result : JSX.Element = Component();
        // It is important that we coordinate how to handle the tool options panel because it is possibile to create a mod that works for your mod but prevents others from doing the same thing.
        result.props.children?.unshift(ColorPainterToolOptionsComponent());
        result.props.children?.unshift(PalettesDuringPlacementComponent());

        return result;
    };
}