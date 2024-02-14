# Contributing
## Setup your workstation

### Prerequisites

Before you will be able to make a mod, you need the following:

1. Beginner knowledge of C#, such that you are comfortable with your current level of skill.
2. If you understand the concept of object oriented programming as well as main C# elements such as methods, fields, properties, events, and delegates, you should be set to go.
3. Unity knowledge is optional, but useful for advanced modding. Unlike other BepInEx-modded games, Rain World's ties into Unity are sparse due to the use of the Futile engine wrapper, thus the knowledge is not required.
4. Entry-level skills in reverse engineering, using tools like dnSpy (or any other .NET decompiler of your choosing).
5. A .NET programming environment. You can view feasible options here: https://dotnet.microsoft.com/en-us/platform/tools
6. Windows users will want Visual Studio (not Visual Studio Code, they are similar but not the same!)
6. NET Framework 4.8 Developer Pack (not runtime!) which you can download here: https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48

> Remove the plugins file in "Mod/" and replace with a folder named 'plugins' for the build output to work correctly

### Assemblies
You will need to add the following references at minimum to your VS project for Rain World:

* BepInEx/core/BepInEx.dll
* BepInEx/plugins/HOOKS-Assembly-CSharp.dll
* Assembly-stripped.dll -- Discord link: https://discord.com/channels/291184728944410624/431534164932689921/1089696571005874206
* RainWorld_Data/Managed/UnityEngine.dll
* RainWorld_Data/Managed/UnityEngine.CoreModule.dll
* RainWorld_Data/Managed/Assembly.csharp.firstpass

> Note that certain mods may need more functionality from Unity. In the RainWorld_Data/Managed folder you will find DLL files prefixed with UnityEngine. - these are different parts of the Unity engine that allow doing different things, such as UnityEngine.AssetBundleModule for loading asset bundles built in the Unity editor. 

## Guidelines
###### Advice for development

### 1.  Be sympathetic to the game's design -- Use Hooks
Hooks are incredibly useful for injecting your custom code into the game. Use those entrypoints. However, keep in mind that you may encounter trade-offs -- Hooking might remove a higher degree of flexibility that you needed for your code. If you want to make a custom menu, do you hook into where slugcat image are added or does your process manager reroute to a new instance of a menu that you've customized? Is your customized menu truly re-usable or are you copy-pasting code? These are questions whose answers need to be articulated by the time the PR is made.

### 2. Where possible, use primitives
Keeping your method or function's parameters set to primitives can greatly boost its re-usability across the project space. It may be more difficult to do so given some instructions you want to run, but consider this a reminder to stay vigilant in developing simplicity. 

### 3. We cannot teach you programming
Some developers are more advanced than others and this is expected. However, pinging developers for questions that you can research yourself is disrespectful to their time and to your intelligence. The expectation is that by the time you've asked a question, you have exhausted your extracurricular resources, reviewed the game code and need context on Meadow's design itself.

### 4. It's easier to modify than to write
Everyone wants a perfect Pull Request. But if you're going to make a PR, please put in the work to test your  changes, attempt debugging, and write an executive summary in your PR's description rather than relying on the devs to contextualize themselves with the code you've written. 

### 5. Don't go rogue
Rain Meadow is a cool mod. We want to keep it that way. Conform to the vision the team has for its game modes, not use them as an excuse to push your own content. This is Rain World, not Doom 64. We love the creativity of the fan base, and the best way to harness that creativity for this mod is to tackle existing GitHub issues or reach out to the team on Discord for what needs to be worked on next if no issues are remaining.  

### 6. Who do I assign for a review?
* Meadow: @henpemaz
* Story: @Chrometurtle
