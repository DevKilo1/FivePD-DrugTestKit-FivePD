﻿This plugin may be buggy, I made it years ago.

Installation: Drag and stop the provided DLL into your `fivepd/plugins` folder! **Make sure to add the following line** to the files list of your fxmanifest.lua: `'./config/items.json',`

How to use: The default keybind is "H". Open your trunk and press it!

Setup process: Must modify your items.json to contain the following properties for each item you want the mod to work with (these are optional, so it won't break anything. These values allow the mod to do its thing though.):
- isSuspicious (true/false): Set this to true if you want the item to appear in the test kit upon searching.
- drugType ("PCP", "Fentanyl", "LSD", "Ecstasy/MDMA", "Methamphetamines", "Heroin", "Cocaine", "Marijuana", "None"): These must be an exact match. No spelling errors, might as well copy and paste them. This property determines the outcome of the test results.

# Best use case:
- Items that might appear suspicious on the outside such as "opaque white baggie" should be marked as suspicious, even though they may not contain drugs.
- If you set 'isIllegal' to true as well, FivePD will show you observations like if the ped is nervous. 