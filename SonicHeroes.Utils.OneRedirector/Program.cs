﻿using System;
using csharp_prs_interfaces;
using Heroes.SDK;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using SonicHeroes.Utils.OneRedirector.Configuration;
using SonicHeroes.Utils.OneRedirector.Structs;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace SonicHeroes.Utils.OneRedirector;

public unsafe class Program : IMod
{
    public static ILogger Logger { get; private set; }

    private IModLoader _modLoader;
    private OneHook _oneHook;

    public void StartEx(IModLoaderV1 loader, IModConfigV1 config)
    {
        _modLoader = (IModLoader)loader;
        _modLoader.GetController<IReloadedHooks>().TryGetTarget(out var hooks);
        _modLoader.GetController<IPrsInstance>().TryGetTarget(out var prsInstance);
        Logger = (ILogger)_modLoader.GetLogger();
        SDK.Init(hooks, prsInstance);

        /* Your mod code starts here. */
        var modFolder    = _modLoader.GetDirectoryForModId(config.ModId);
        var configFolder = _modLoader.GetModConfigDirectory(config.ModId);
        var configurator = new Configurator(configFolder);
        configurator.Migrate(modFolder, configFolder);

        var modConfig = configurator.GetConfiguration<Config>(0);
        _oneHook = new OneHook(Logger, modConfig, NativeFunctions.GetInstance(hooks));
        _modLoader.ModLoading += OnModLoading;
        _modLoader.ModUnloading += OnModUnloading;
    }

    private void OnModUnloading(IModV1 modInstance, IModConfigV1 modConfig) => _oneHook.OnModUnloading(_modLoader.GetDirectoryForModId(modConfig.ModId));
    private void OnModLoading(IModV1 modInstance, IModConfigV1 modConfig) => _oneHook.OnModLoading(_modLoader.GetDirectoryForModId(modConfig.ModId));

    /* Mod loader actions. */
    public void Suspend() { }
    public void Resume() { }
    public void Unload() { }
    public bool CanUnload()  => false;
    public bool CanSuspend() => false;

    /* Automatically called by the mod loader when the mod is about to be unloaded. */
    public Action Disposing { get; }

    public static void Main(string[] args) { }
}