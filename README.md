# VaM CUA Superglue

Stick your CUAs where they belong! Automatically remove lag from CUAs attached to Person atoms, such as hair and accessories.

![](E:\Large games\VAM\Custom\Scripts\Pineapler\vam-cua-superglue\media\00-showcase.mov "Superglue Showcase")

## Using this plugin

Add the plugin as a **Session plugin**. Scene plugin is also supported but not recommended, as having multiple instances of the plugin loaded will cancel out the effect.

That's it! The rest is automatic.

## Troubleshooting

The effect only works on CustomUnityAsset atoms with Physics disabled. If you think your CUA should be glued but it's not, check that the following settings are set:

 - Physics Options 
   - Physics (False)
![](media/01-settings.png "Physics Options window")
 - Control
   - Link To Atom (as desired)
   - Position, Rotation (Parent Link)
![](media/02-settings.png "Control window")
   
If you run into any further issues, submit an Issue on Github or message me on Discord (Pineapler#3512).

## License

[MIT](LICENSE.md)
