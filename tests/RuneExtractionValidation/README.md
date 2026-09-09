# Rune extraction game test

This temporary BepInEx plugin runs eight checks against EpicLoot's actual
`BuildEnchantedRune` method after its prefabs load. It creates temporary item
data without loading a character, changing an inventory, or joining a server.
It automatically quits the game after the checks finish or the wait times out.

Use an isolated profile with EpicLoot 0.14.0, its dependencies, and the
PraetorisClient build under test. Do not install this plugin in a normal profile.

1. Set the game paths in `Environment.props` or pass MSBuild property overrides.
2. Build with `dotnet build tests/RuneExtractionValidation/RuneExtractionValidation.csproj -c Release`.
3. Copy `bin/Release/net481/RuneExtractionValidation.dll` from this test directory
   into the isolated profile's `plugins/RuneExtractionValidation` directory.
4. Start that profile and wait for the game to exit.
5. Check `BepInEx/LogOutput.log` for `RUNE_TEST ALL PASS (8 cases)` and confirm
   there is no `BuildEnchantedRune` patch exception.
6. Restore the original active profile.

The checks cover extraction above the effect definition's maximum, ordinary
extraction, boolean effects, two-decimal rounding, zero/NaN/999/negative power
modifiers, and a NaN source value. Successful cases also check effect identity,
rarity, and that the source value is unchanged.

The original PraetorisClient 0.1.61 build fails the first check with EpicLoot
0.14.0: the old transpiler throws during startup and the extracted value remains
capped at the effect definition's maximum times the power modifier.
