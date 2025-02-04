import { SwatchUIData } from "mods/Domain/PaletteAndSwatches/SwatchUIData";
import styles from "../Domain/ColorFields.module.scss";
import { Color } from "cs2/bindings";
import { ColorFieldTheme } from "mods/SIPColorComponent/SIPColorComponent";
import classNames from "classnames";
import boxStyles from "./PaletteBoxStyles.module.scss";

interface inLinedStyle {
    backgroundColor : string;
    width : string;
}

function calculateWidths (Swatches: SwatchUIData[], totalWidth: number) : number[] {
    let totalProbabilityWeight = 0;
    for (let i=0; i<Swatches.length; i++) 
    {
        totalProbabilityWeight += Swatches[i].ProbabilityWeight;
    }

    let widths : number[] = new Array(Swatches.length);
    for (let i=0; i<Swatches.length; i++) 
    {
        widths[i] = Swatches[i].ProbabilityWeight / totalProbabilityWeight * totalWidth;
    }

    return widths;
}

function convertToBackGroundColor(color : Color) : string {
    const r = Math.round(color.r * 255);
    const g = Math.round(color.g * 255);
    const b = Math.round(color.b * 255);
    return 'rgba('+r+','+g+','+b+','+color.a+')';
}

function generateStyles (Swatches: SwatchUIData[], widths : number[]) : inLinedStyle[] {
    let inLinedStyles : inLinedStyle[] = new Array(Swatches.length);

    for (let i=0; i<Swatches.length; i++) 
    {
        inLinedStyles[i] = {
            backgroundColor : convertToBackGroundColor(Swatches[i].SwatchColor),
            width: widths[i]+'rem',
        }
    }

    return inLinedStyles;
}

export const PaletteBoxComponent = (props : {Swatches : SwatchUIData[], totalWidth: number}) => {

    let widths : number[] = calculateWidths(props.Swatches, props.totalWidth);
    let inLinedStyles : inLinedStyle[] = generateStyles(props.Swatches, widths);

    return (
        <>
            <div className={classNames(ColorFieldTheme.colorField, styles.rcColorField, boxStyles.centered)}> 
                {inLinedStyles.map((swatch) => (
                    <div className={styles.inputHeight} style={swatch}></div>
                ))}
            </div>
        </>
    );
}