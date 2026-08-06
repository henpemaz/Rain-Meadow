# this stupid directory

You know the sayings "there's a method to the madness", "it's not stupid if it works", and "there's beauty in simplicity"?
Well this directory has no method to its madness, it is absolutely stupid, and there's no beauty or simplicity at all.
There is only conflicting formatting, questionable design choices, obsolete implementations, pain, and suffering.

To rectify that, I've decided I'm at the very least going to lay out some plans and something resembling a standard that people hopefully conform to.
I intend to eventually review and refactor every single file in this directory eventually (and force conformity mwa ha ha ha), and I'm aware that's quite ambitious, but I think it'll be good to lay things out at the very least.

The specifications are not necessarily followed by the lobby select overhaul(2) PR, but that is **intentional**.
As I am writing the overhaul, I am discovering the current layout and uses of the current classes in this directory and using that information to update this doc and try my best to create a better layout + style standard.
Once I am happy with a layout and the specifications, I will apply changes accordingly.

## Intended Directory Structure

```
 UI/
 ├── Components/
 │   └── Base/
 ├── Dialogs/
 │   └── DialogBoxes/
 ├── Interfaces/
 └── Menus/
     ├── Pages/
     └── Panels/
```

Namespaces follow the directory layout.

The goal is eventually to rename the `Menu/` directory to `UI/` to set the namespace in stone once everything has been moved and refactored. The reason the `Menu` namespace is a no-go is because it conflicts with the base `Menu` namespace.
That is also the reason for using plurals in some other namespaces, such as `Dialogs/` and `Menus/`. Everything else is plural to keep things consistent.

## Namespace Purposes

`RainMeadow.UI.<namespace>` will be shortened to `UI.<namespace>` for simplicity.
"Directory" and "namespace" are used interchangeably here since they're supposed to mirror one another.

### `UI`

This is the base directory, and really shouldn't have that much in it. Probably just `RainMeadow.RainMeadow.MenuHooks`, `UI.UIUtils`, and potentially other extension/helper classes.

### `UI.Components`

`UI.Components` has classes mainly inheriting from `Menu.MenuObject` or other classes in `UI.Components.Base`. These are more complex UI components designed for a specific menu, or are not meant to be inherited from.
Examples include `UI.Components.SlugIcon` and `UI.Components.LobbyCardSelector`.

### `UI.Components.Base`

`UI.Components.Base` also has classes inheriting from `Menu.MenuObject`, but should be much simpler in nature. They should normally be wrappers/rewrites of Rain World UI elements fixing bugs or making them eventful.
These classes should be able to be used on their own or inherited to create more complex components.
Current candidates include classes currently prefixed with "Simpler", as well as more complex classes such as `UI.Components.ButtonScroller` which is used as a base for many other components.

### `UI.Dialogs`

This directory should have classes inheriting from `Menu.Dialog`.
For those who don't know, these classes are loaded by `ProcessManager.ShowDialog` and function as a full screen inhibitor, displaying whatever menu elements you want on top of the prior screen.
`Menu.Dialog` itself inherits from `Menu.Menu`, allowing for basically any many to be placed here.
Asides from some niche uses such as `ArenaPostGameStatsDialog`, these typically load a dialog box from `UI.Dialogs.DialogBoxes` and just show that.
(Potentially not, still working on the best way to handle dialogs right now but this is directory is what triggered this whole write up to begin with)

### `UI.Dialogs.DialogBoxes`

Has classes inheriting from `Menu.MenuDialogBox`.

### `UI.Interfaces`

Proper C# interface declarations, such as `RainMeadow.IHaveADescription`.
Not to be mixed up with the components I added "Interface" to as a suffix (that's my bad), which are going to be renamed and placed in `UI.Menus.Panels`.

### `UI.Menus`

This is where full menus will be placed, all with the suffix "Menu".
This includes `RainMeadow.SmartMenu`, which will most likely be renamed to `RMMenu` to conform to `UI.Components.Base` standards and reasoning.

### `UI.Menus.Pages`

Menus broken up into multiple pages such as the arena lobby menu and the new lobby select, the page classes are to be placed here.
These classes inherit from `Menu.Page` and are basically like whole menu declarations on their own.
This is done for organizational reasons, so I'm debating if an additional namespace to indicate the corresponding menu or if that namespace should replace `.Pages` entirely is a good idea.

### `UI.Menus.Panels`

As mentioned before, there quite a few classes with the suffix "Interface" which are not C# interfaces, but are a collection of labels and selectable components which are usually placed in a `UI.Components.TabContainer.Tab`.
This is a naming blunder which I think I may have caused, and to rectify that I'm choosing to replace their suffixes with "Panel" and move them into this namespace.
These are essentially the exact same as `UI.Menus.Pages` (save for the inheriting requirement), just on a smaller scale.

## Class Specifics

### `UI.UIUtils`

This is a helper class meant for constants and simple repeated calculations, such as the positioning of items relative to the edges of the screen.
This is intended to be different from `RainMeadow.MenuHelpers`, as that class features many complex methods involving string manipulation and extension methods.
Some of the things in `MenuHelpers` is going to have to go, but the string manipulation methods are quite helpful and will most likely be refactored and placed into `UIUtils` at some point.

## Design Philosophies

### Singal

Singal is dumb. Singal is bad. Why RW decided to use this system is beyond me.
Event systems are much better ergonomically and clarity-wise.
Avoid using singal as much as possible, and prefer the use of event hooks.
`RainMeadow.UI.Components.Base` components should never expose singal, and should opt for exposing event hooks instead.

## Class Style Guide

Very much an opinionated section, but I think that even if you don't necessarily parts of an overall format, conflicting formatting all over the place is much worse.

As a heads up, I use the default settings on the `csharpier` formatter so the style guide here is at least partially influenced by that.
I am open to changing the style guide and using different `csharpier` settings or a different formatter as I'm aware the way it formats code, especially large constructor/method calls, is vastly different from the rest of the codebase.

If you think some things here are oddly specific or just nitpicking, you're right.
I am literally writing the exact process I use to lay out code when I write it, although this features some changes I have yet to implement myself.
Like I said, I'm open to changes, I just think it's best to be as specific as possible.

### Layout and Spacing

Namespaces should be FILE SCOPED. Seriously. Don't wrap everything in a pointless indent just cause it's the default.

Use a primary constructor if that's all the class needs.

#### Member layout

1. constants and static fields, such as keys or mapping dictionaries, has a blank line after
1. UI element fields
1. UI element collection/generic fields
1. Other non-primitive fields
1. non-primitive collection/generic fields, has a blank line after
1. event hooks. Has a blank line after
1. primitive fields, in order of width (`int` at the top, `string` at the bottom), has a blank line after
1. Properties, in the same order and spacing as above
1. overridden fields/properties, in the same ordering and spacing as above
1. constructor(s)
1. class methods, all methods should have empty lines above and below them (including constructor and the methods below) unless it's the last item in the class
1. hook classes, such as those used to subscribe to `MatchmakingManager` hooks
1. interface implementation methods
1. overridden classes, ending with `Update()`, `GrafUpdate()`, and `ShutDownProcess()`.
1. enums, surrounded by empty lines
1. Any nested classes, following the exact same ordering.

#### Field/Property Conventions

Generally speaking you shouldn't need properties, so prefer fields in camelCase for class data.
Multiline initialization is always preferred when possible.
Also, mark things as nullable if they can be null, such as event hooks. Annotate, annotate, annotate.
Seriously. Don't have random logic assuming that a field may be null if it's not marked as such.

Properties should be in CamelCase, and are used to either highlight data intended to be used as public API (such as adding something to a mapping dictionary), override/implement a base property, or to return a simple computation involving field data.
An example for the last case could be something like a "currently selected" property which just takes an internal index and returns the element at that index from an internal collection.

#### Method Conventions

Usual C# method conventions, PascalCase, clear and descriptive names, yada yada yada.
For hooks, unless you need the hook again for unsubscribing the action or for use on multiple elements, just make it a lambda function.
Make the parameters clear in their names as well, either named after their type or at the very least what the object is (such as `btn`), just not some stupid generic crap like `obj`.
No need for underscores to deliminate unused parameters in delegates dotnet recognizes them as necessary.

#### Example

Here's an example which is probably more useful than whatever the heck the above sections were.

```csharp
using System;
using System.Collections.Generic;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Example;

public class ExampleUIClass : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
{
    public const string SNAKE_CASE_ALL_CAPS = "key";
    public static Dictionary<int, float> StaticMappingDictionary { get; set; } = new()
    {
        { 0, 0f },
        { 1, 1f },
        { 2, 2f },
        { 3, 3f },
    };

    public ProperlyAlignedMenuLabel uiComponent,
        preferMultiLineInitializations;
    public List<SimplerButton> buttons = [];
    public Vector2 notAUiField;
    public Vector2[] vectorList = [];

    public event Action? Hook;

    public int selectedIndex = -1;
    public bool flag;
    public float num2;
    public string str = "string";

    public Vector2? SelectedVector
    {
        get
        {
            if (selectedIndex < 0)
                return null;
            return vectorList[selectedIndex];
        }
    }

    public string PropertiesForIntendedPublicAPI =>
        "these should follow the same ordering as above";

    public override bool Selected => false;

    public ExampleUIClass(Menu.Menu menu, MenuObject owner, Vector2 pos)
        : base(menu, owner, pos)
    {
        MethodThatDefinitelyDoesSomething();
    }

    public void MethodThatDefinitelyDoesSomething() { }

    public int GetCurrentlySelectedOfSeries(string series)
    {
        return selectedIndex;
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        selectedIndex = to;
    }

    public override void Update()
    {
        base.Update();
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
    }

    public enum RustEnumsAreSoMuchBetter
    {
        SoStupidThatThese,
        AreTreatedAsNumbers,
        ToThePointThatPatternMatching,
        ConsidersCastsAPotentialVariant,
    }

    // just use primary constructor if that's all that's needed
    public class SubClass(Menu.Menu menu, MenuObject owner, Vector2 pos)
        : PositionedMenuObject(menu, owner, pos);
}
```

## Constructor Conventions

### Object Initialization

Initialize UI elements in a variable, unless it's literally a singular element or something.
If the variable is local, specify its type instead of using `var`, and omit the type from the constructor call.
If the variable is a field, specify the object's type in the constructor.

Use field initializors if possible instead.
Modifying fields of fields can be done after the field initializors, which can then be followed by event assignment.

### Adding subObjects

After the last element declaration, add all objects to `subObjects` using `subObjects.AddRange`.
`UIelementWrapper`s should be placed above the `subObjects.AddRange` statement.
If it's just one object (for whatever reason) you can probably just wrap the whole initialization in a `subObjects.Add` and omit the variable assignment if applicable.

```csharp
// Taken from RainMeadow.UI.Components.LobbyCardSelector ctor
        tabWrapper = new MenuTabWrapper(menu, this);

        searchBar = new OpTextBox(new Configurable<string>(""), new Vector2(0, size.y - 10), 500)
        {
            accept = OpTextBox.Accept.StringASCII,
            allowSpace = true,
            description = "Search lobbies by name",
        };
        searchBar.label.text = "Search Lobbies";
        searchBar.OnChange += () =>
        {
            if (filter.lobbyName == searchBar.value)
                return;
            filter.lobbyName = searchBar.value;
            filter.FilterInfos(lobbyInfos);
            UpdateLobbyCards();
        };

        refreshButton = new SimplerSymbolButton(
            menu,
            this,
            "Menu_Symbol_Repeats",
            "",
            new Vector2(534, size.y - 10),
            "Refresh lobbies list"
        );
        refreshButton.OnClick += (btn) => refreshLobbies();

        sortButton = new SimplerSymbolButton(
            menu,
            this,
            "Meadow_Menu_Sort_A-Z",
            "",
            new Vector2(505, size.y - 10),
            "Sort A to Z"
        );
        sortButton.OnClick += CycleSortingOrder;
        sortingOrder = SortingOrder.AtoZ;

        new PatchedUIelementWrapper(tabWrapper, searchBar);
        subObjects.AddRange([tabWrapper, refreshButton, sortButton]);
```
