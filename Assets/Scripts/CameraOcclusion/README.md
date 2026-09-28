# Indoor wall occlusion

`CameraOcclusionHider` smoothly fades any wall collider between the gameplay camera and the player, then restores its original material as soon as the camera has a clear view.

## Scene setup

1. In **Project Settings > Tags and Layers**, create an `Occluders` layer.
2. Assign that layer to only the indoor walls, roof pieces, and props that may cover the player. They must have a 3D collider (`BoxCollider` is usually right for walls).
3. Add `CameraOcclusionHider` to the active gameplay camera. Assign the Player transform and set **Occluder Layers** to only `Occluders`.
4. Tune **Cast Radius** (start with `0.15`) and **Player Height Offset** (start with `1`) to match the character's size.

The component runs in `LateUpdate`, after the Cinemachine camera has moved. It keeps the blocking collider active, so walls continue to block player movement. At runtime it creates temporary transparent copies of the wall materials, fades them, then releases the copies and reassigns the exact original materials. Set **Fade Duration** to control the transition and **Hidden Opacity** to choose how see-through walls become (0 is fully invisible).

This is designed for URP Lit and URP Unlit materials. Custom shaders must support a transparent surface plus either `_BaseColor` or `_Color` alpha to fade.
