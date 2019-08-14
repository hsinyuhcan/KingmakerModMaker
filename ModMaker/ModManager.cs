using Harmony12;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;

namespace ModMaker
{
    public interface IModEventHandler
    {
        void HandleModEnable();

        void HandleModDisable();
    }

    public class ModManager<TCore, TSettings>
        where TCore : class, new()
        where TSettings : UnityModManager.ModSettings, new()
    {
        #region Fields & Properties

        private UnityModManager.ModEntry.ModLogger _logger;
        private Assembly _assembly;

        private List<IModEventHandler> _eventHandler;

        public TCore Core { get; private set; }

        public TSettings Settings { get; private set; }

        public bool Enabled { get; private set; }

        public bool Patched { get; private set; }

        #endregion

        public ModManager(UnityModManager.ModEntry modEntry, Assembly assembly)
        {
            _logger = modEntry.Logger;
            _assembly = assembly;
        }

        public void ResetSettings()
        {
            if (Enabled)
            {
                Debug(MethodBase.GetCurrentMethod());

                Settings = new TSettings();
            }
        }

        #region Toggle

        public void Enable(UnityModManager.ModEntry modEntry)
        {
            if (Enabled)
            {
                Debug("Already enabled.");
                return;
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Debug($"[{stopWatch.Elapsed:ss\\.ff}] Enabling.");
            
            try
            {
                Debug($"[{stopWatch.Elapsed:ss\\.ff}] Loading settings.");
                Settings = UnityModManager.ModSettings.Load<TSettings>(modEntry);
                modEntry.OnSaveGUI += HandleSaveGUI;

                // must before patching, because the patching process may call this
                Core = new TCore();

                if (!Patched)
                {
                    HarmonyInstance harmonyInstance = HarmonyInstance.Create(modEntry.Info.Id);
                    foreach (Type type in _assembly.GetTypes())
                    {
                        List<HarmonyMethod> harmonyMethods = type.GetHarmonyMethods();
                        if (harmonyMethods != null && harmonyMethods.Count() > 0)
                        {
                            Debug($"[{stopWatch.Elapsed:ss\\.ff}] Patching: {type.DeclaringType?.Name}.{type.Name}");
                            HarmonyMethod attributes = HarmonyMethod.Merge(harmonyMethods);
                            PatchProcessor patchProcessor = new PatchProcessor(harmonyInstance, type, attributes);
                            patchProcessor.Patch();
                        }
                    }
                    Patched = true;
                }

                Enabled = true;

                Debug($"[{stopWatch.Elapsed:ss\\.ff}] Registering events.");
                _eventHandler = _assembly.GetTypes().Where(type =>
                    !type.IsInterface && !type.IsAbstract &&
                    type != typeof(TCore) && typeof(IModEventHandler).IsAssignableFrom(type))
                    .Select(type => Activator.CreateInstance(type, true) as IModEventHandler).ToList();

                Debug($"[{stopWatch.Elapsed:ss\\.ff}] Raising events: 'OnEnable'");
                if (Core is IModEventHandler core)
                    core.HandleModEnable();
                foreach (IModEventHandler handler in _eventHandler)
                    handler.HandleModEnable();
            }
            catch (Exception e)
            {
                Error(e);
                Disable(modEntry, true);
                throw e;
            }

            Debug($"[{stopWatch.Elapsed:ss\\.ff}] Enabled.");

            stopWatch.Stop();
        }

        public void Disable(UnityModManager.ModEntry modEntry, bool unpatch = false)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Debug($"[{stopWatch.Elapsed:ss\\.ff}] Disabling.");

            Enabled = false;

            // use try-catch to prevent the progression being disrupt by exceptions
            if (_eventHandler != null)
            {
                Debug($"[{stopWatch.Elapsed:ss\\.ff}] Raising events: 'OnDisable'");
                foreach (IModEventHandler handler in _eventHandler)
                {
                    try { handler.HandleModDisable(); }
                    catch (Exception e) { Error(e); }
                }
                if (Core is IModEventHandler core)
                {
                    try { core.HandleModDisable(); }
                    catch (Exception e) { Error(e); }
                }
                _eventHandler = null;
            }

            if (unpatch)
            {
                HarmonyInstance harmonyInstance = HarmonyInstance.Create(modEntry.Info.Id);
                foreach (MethodBase method in harmonyInstance.GetPatchedMethods().ToList())
                {
                    Patches patchInfo = harmonyInstance.GetPatchInfo(method);
                    IEnumerable<Patch> patches = patchInfo.Transpilers.Concat(patchInfo.Postfixes).Concat(patchInfo.Prefixes)
                        .Where(patch => patch.owner == modEntry.Info.Id);
                    if (patches.Any())
                    {
                        Debug($"[{stopWatch.Elapsed:ss\\.ff}] Unpatching: " +
                            $"{patches.First().patch.DeclaringType.DeclaringType?.Name}.{method.DeclaringType.Name}.{method.Name}");
                        foreach (Patch patch in patches)
                        {
                            try { harmonyInstance.Unpatch(method, patch.patch); }
                            catch (Exception e) { Error(e); }
                        }
                    }
                }
                Patched = false;
            }

            Core = null;

            modEntry.OnSaveGUI -= HandleSaveGUI;
            Settings = null;

            Debug($"[{stopWatch.Elapsed:ss\\.ff}] Disabled.");

            stopWatch.Stop();
        }

        #endregion

        #region Event Handlers

        private void HandleSaveGUI(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save(Settings, modEntry);
        }

        #endregion

        #region Loggers

        public void Critical(string str)
        {
            _logger.Critical(str);
        }

        public void Critical(object obj)
        {
            _logger.Critical(obj?.ToString() ?? "null");
        }

        public void Error(Exception e)
        {
            _logger.Error($"{e.Message}\n{e.StackTrace}");
        }

        public void Error(string str)
        {
            _logger.Error(str);
        }

        public void Error(object obj)
        {
            _logger.Error(obj?.ToString() ?? "null");
        }

        public void Log(string str)
        {
            _logger.Log(str);
        }

        public void Log(object obj)
        {
            _logger.Log(obj?.ToString() ?? "null");
        }

        public void Warning(string str)
        {
            _logger.Warning(str);
        }

        public void Warning(object obj)
        {
            _logger.Warning(obj?.ToString() ?? "null");
        }

        [Conditional("DEBUG")]
        public void Debug(MethodBase method, params object[] parameters)
        {
            _logger.Log($"{method.DeclaringType.Name}.{method.Name}({string.Join(", ", parameters)})");
        }

        [Conditional("DEBUG")]
        public void Debug(string str)
        {
            _logger.Log(str);
        }

        [Conditional("DEBUG")]
        public void Debug(object obj)
        {
            _logger.Log(obj?.ToString() ?? "null");
        }

        #endregion
    }
}
