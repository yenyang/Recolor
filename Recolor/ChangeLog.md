# Patch V1.1.0
* You can select NetLane fences and hedges placed by EDT or inside buildings using a tool activated by keyboard shortcut.
* Color picker and painter can select Netlane fences and hedges.
* When changing colors, SubObject and SubLanes colors will update immediately.
* Color painter can toggle channels.
* Alpha transparency sliders added. (May not effect every asset the same way.)
* Optional Hexadecimal input fields.
* Individual resets for single selection. (Does not currently work on instances changed with previous versions.)
* Added swap color buttons between channels.
* Added ability to set color set for all service vehicles from a service building instance. This includes any future ones that spawn from the building.
* Added ability to set color set for all vehicles on a route. This includes any futures ones that spawn or are assigned to that route.
* Removed I18N Everywhere dependency.
* Translations for officially supported languages are handled internally with embedded resource files.
* Recolor works in the editor.
* Fixed random colors when hovering over assets with multiple meshes and custom colors.
* While changing Single Instance colors on a prefab with Multiple Meshes, you can limit color changes to individual submeshes, matching submeshes, or all submeshes.
* While changing Matching Color Variations on a prefab with Multiple Meshes, you are limited to only matching submeshes and it will not change all submeshes. You can pick which type of submesh to change. 