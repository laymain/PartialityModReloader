## PartialityModReloader

This [Partiality](https://github.com/PartialityModding/Partiality) mod enables hot reloading your mods while the game is running.

### Why

Modding has an ungrateful _die and retry_ part which consist on
1. Compiling a mod
2. Loading mod with Partiality
3. Launching the game
4. Going to the point you need to test your mod (ie. loading save game, open inventory, ...)
5. Discovering that your mod is not working because of...
6. Exiting the game
7. Updating your code
8. Returning to step 1 crossing your fingers...

### How it works

1. The mod watches the `Mods` folder for any file change
2. Each updated `.dll` file is analysed for reloadable methods
3. New methods are injected at runtime by re-pointing method implementations

### How to install

1. Install [PartialityLauncher](https://github.com/PartialityModding/PartialityLauncher) to enable your game modding
2. Because current Partiality version locks `.dll` files which prevent us to overwrite loaded mods,
you have to download this patched [Partiality.dll](https://github.com/laymain/Partiality/releases/download/0.2.0/Partiality.dll)
_(from [laymain/Partiality](https://github.com/laymain/Partiality))_ and copy it into `PartialityLauncher\bin` folder.
3. Copy the [latest version](https://github.com/laymain/PartialityModReloader/releases/latest) of `PartialityModReloader` into your game `Mods` subfolder

### How to use

* Mark your reloadable methods by using the following attribute `[MethodImpl(MethodImplOptions.NoInlining)]`
* Enable `PartialityModeReloader` and your mod using `PartialityLauncher`
* Your mod marked methods will be reloadable at runtime when you update your `.dll` file

### Remarks

* This mod is heavily inspired by Andreas Pardeike's (creator of [Harmony](https://harmony.pardeike.net/))
[RimWorld Reloader](https://github.com/pardeike/Reloader) mod.
* Why the choice of using [MethodImplAttribute](https://docs.microsoft.com/fr-fr/dotnet/api/system.runtime.compilerservices.methodimplattribute?view=netframework-3.5) and not a custom Attribute?
  * re-pointing method implementations at runtime does not work for inlined methods
  * it is a framework attribute so you don't have to reference this library in your project

### Limitations

* Only non-generic methods can be patched at runtime for now (including getters and setters)
* This mod does not reload your mod, it only update method implementations which means that
all initialization code and hooks will not be called again (such as `OnEnable` method).
* Loaded assembly cannot be unloaded so each reload will increase the memory usage,
enable this mod only when your are developing your mod to avoid memory leaks.
Only AppDomain can be unloaded but communication between AppDomains implies that classes
are Serializable which leads to an unnecessary complexity that you don't want to mess with.
* .Net runtime will not reload an assembly with the same version, you must increment your mod
version. The CSharp Compiler can generate for you a random revision number which will change the
[AssemblyVersion](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assemblyversionattribute?redirectedfrom=MSDN&view=netframework-3.5) on each build by setting a wildcard for the fourth part of the version:
```c#
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
[assembly: AssemblyVersion("1.0.0.*")]
```

### Example

```C#
using System;
using Partiality.Modloader;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DummyMod
{
    public class Mod : PartialityMod
    {
        public Mod()
        {
            ModID = nameof(DummyMod);
            Version = typeof(Mod).Assembly.GetName().Version.ToString();
            author = "Laymain";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            On.MenuManager.ToggleMap += OnToggleMap;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnToggleMap(On.MenuManager.orig_ToggleMap orig, MenuManager self, CharacterUI _owner)
        {
            orig(self, _owner);
            Debug.Log($"[{nameof(DummyMod)}] Map toggled!");
        }

    }
}
```
