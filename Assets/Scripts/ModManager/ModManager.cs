using System.Collections;
using UnityEngine;
using System.IO;
using System;
using System.Reflection;
using Modified.Mono.CSharp;
using System.CodeDom.Compiler;
using System.Text;
using System.Linq;
using UnityEngine.SceneManagement;
using CielaSpike;
using System.Collections.Generic;
using System.Threading;
using VoxaNovus;
public static class Mods
{
    public static List<Type> LoadedMapgens = new List<Type>();
    public static List<string> LoadedMods = new List<string>();
    public static List<string> LoadedMapgens_Code = new List<string>();
    public static List<string> LoadedMapgens_Name = new List<string>();
    public static ModManager Manager;

    public static Type GetMapgen(string ModName)
    {
        int index = LoadedMapgens_Name.BinarySearch(ModName);
        if (index < 0) return null;
        else return LoadedMapgens[index];
    }

    public static void CompileMod(string Mod, string ModName)
    {
        Debug.Log($"Compiling mod: 'Mod_{ModName}'... ");

        bool successful;
        bool skip = false;

        if (!ModManager.ModSafetyChecker.CheckMod(Mod))
            skip = true;

        if (Mod.Contains("WorldGen"))
        {
            LoadedMapgens_Code.Add(Mod);
            LoadedMapgens_Name.Add(ModName);
        }

        if (!skip)
        {
            Assembly assembly = null;
#pragma warning disable 168
            try { assembly = ModManager.Compile(Mod); successful = true; }
            catch (Exception ex) { successful = false; Debug.Log(ex.ToString()); }
#pragma warning restore 168
            if (successful)
            {
                if (ModName.Contains("WorldGen"))
                {
                    Debug.Log($"Mod Manager: MapGen {ModName} loaded.");
                    Mods.LoadedMapgens.Add(assembly.GetType($"Mod_{ModName}"));
                }
                else
                {
                    try
                    {
                        var runtimeType = assembly.GetType($"Mod_{ModName}");
                        var method = runtimeType.GetMethod("ModInitalizer");
                        var del = (Func<GameObject, MonoBehaviour>)
                                        Delegate.CreateDelegate(
                                            typeof(Func<GameObject, MonoBehaviour>),
                                            method
                                    );

                        // We ask the compiled method to add its component to this.gameObject
                        var addedComponent = del.Invoke(Manager.gameObject);
                        Debug.Log($"Mod Manager: Received mod Mod_{ModName} loaded.");
                        Mods.LoadedMods.Add($"Mod_{ModName}");
                    }
                    catch
                    {
                        Debug.Log($"\n<color=red>Networking: Received mod Mod_{ModName} failed to load.</color>");
                    }
                }
            }
        }
    }
}

public class ModManager : MonoBehaviour
{
    [Header("Splash scene")]
    public UnityEngine.UI.Text IngameConsole;
    public UnityEngine.UI.Image BlackOverlay;
    public GameObject MainMenuCanvas;

    private AutoResetEvent autoResetEvent;

    public int LoadedMapgens;
    public int LoadedMods;
    public int LoadedMapgens_Code;
    public int LoadedMapgens_Name;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Mods.Manager = this;
        autoResetEvent = new AutoResetEvent(true);
        this.StartCoroutineAsync(LoadModsCoroutine());
        //StartCoroutine(LoadModsCoroutine());
    }

    void Update()
    {
        LoadedMapgens = Mods.LoadedMapgens.Count;
        LoadedMods = Mods.LoadedMods.Count;
        LoadedMapgens_Code = Mods.LoadedMapgens_Code.Count;
        LoadedMapgens_Name = Mods.LoadedMapgens_Name.Count;
    }

    int LogLines = 0;
    bool UpdateLogLoop = true;
    List<string> consoleLog = new List<string>();
    IEnumerator UpdateLog()
    {
        yield return Ninja.JumpToUnity;
        while (UpdateLogLoop)
        {
            if (consoleLog.Count > 0)
            {
                LogLines++;
                if (LogLines > 25)
                {
                    int index = IngameConsole.text.IndexOf("\n");
                    IngameConsole.text = IngameConsole.text.Substring(index + 1);
                }
                IngameConsole.text += consoleLog[0];
                autoResetEvent.WaitOne();
                consoleLog.RemoveAt(0);
                autoResetEvent.Set();
            }
            yield return null;
        }
    }
    IEnumerator ClearLog()
    {
        yield return Ninja.JumpToUnity;
        IngameConsole.text = "";
    }


    public struct assemblyInfo
    {
        public assemblyInfo(Assembly a, string n)
        {
            modAssembly = a;
            modName = n;
        }
        public Assembly modAssembly;
        public string modName;
    }

    List<assemblyInfo> ModList = new List<assemblyInfo>();

    IEnumerator AttachMods()
    {
        yield return Ninja.JumpToUnity;
        foreach (assemblyInfo asInfo in ModList)
        {
            if (asInfo.modName.Contains("WorldGen"))
            {
                consoleLog.Add($"\nMapGen {asInfo.modName} loaded.");
                Mods.LoadedMapgens.Add(asInfo.modAssembly.GetType($"Mod_{asInfo.modName}"));
                continue;
            }
            try
            {
                var runtimeType = asInfo.modAssembly.GetType($"Mod_{asInfo.modName}");
                var method = runtimeType.GetMethod("ModInitalizer");
                var del = (Func<GameObject, MonoBehaviour>)
                                Delegate.CreateDelegate(
                                    typeof(Func<GameObject, MonoBehaviour>),
                                    method
                            );

                // We ask the compiled method to add its component to this.gameObject
                var addedComponent = del.Invoke(gameObject);
                consoleLog.Add($"\nMod Mod_{asInfo.modName} loaded.");
                Mods.LoadedMods.Add($"Mod_{asInfo.modName}");
            } catch
            {
                consoleLog.Add($"\n<color=red>Mod Mod_{asInfo.modName} failed to load.</color>");
            }
            
            yield return null;
        }
        ModList.Clear();
    }

    public void AsyncAddToList(object Item)
    {
        autoResetEvent.WaitOne();
        if (Item.GetType() == typeof(assemblyInfo))
        {
            ModList.Add((assemblyInfo)Item);
        } else if (Item.GetType() == typeof(string))
        {
            consoleLog.Add((string)Item);
        }
        autoResetEvent.Set();
    }

    IEnumerator LoadModsCoroutine()
    {
        yield return Ninja.JumpToUnity;
        yield return Macros.Coroutine.WaitFor_1_Second;
        consoleLog.Add("Initalizing modloader.");
        StartCoroutine(UpdateLog());
    RetryLoading:
        yield return Ninja.JumpToUnity;
        consoleLog.Clear();
        LogLines = 0;
        StartCoroutine(ClearLog());
#pragma warning disable 168
#if UNITY_EDITOR
        string ModsFolderPath = @"D:\VoxKriegTesting";
        if (!Directory.Exists(ModsFolderPath))
            Directory.CreateDirectory(ModsFolderPath);

        IniFile Mods_INI = new IniFile("Mods/Mods.ini");
        yield return Ninja.JumpBack;
        if (Mods_INI.KeyExists("Mods"))
        {
            int NumberOfMods = int.Parse(Mods_INI.Read("Mods"));
            AsyncAddToList($"\nFound {NumberOfMods} mod{(NumberOfMods > 1 ? "s." : ".")}");
            if (NumberOfMods > 0)
                try
                {
                    System.Threading.Tasks.Parallel.For(0, NumberOfMods, (ModCounter, state) =>
                    {
                        string ModNumberString = ModCounter.ToString();
                        string ModName = Mods_INI.Read("Name", ModNumberString);
                        string ModDirectory = Mods_INI.Read("Directory", ModNumberString);

                        AsyncAddToList($"\nCompiling mod: 'Mod_{ModName}'... ");

                        bool successful = false;
                        StreamReader sr = new StreamReader(Path.Combine(ModsFolderPath, ModDirectory));
                        string Mod = sr.ReadToEnd();
                        sr.Close();
                        bool skip = false;
                        if (!ModSafetyChecker.CheckMod(Mod))
                        {
                            AsyncAddToList($"\n<color=red>Failed to load Mod_{ModName}. Mod contains references to blacklisted assemblies. Check blacklist info in /Mods/ folder.</color>");
                            skip = true;
                        }

                        if (Mod.Contains("WorldGen"))
                        {
                            autoResetEvent.WaitOne();
                            Mods.LoadedMapgens_Code.Add(Mod);
                            Mods.LoadedMapgens_Name.Add(ModName);
                            autoResetEvent.Set();
                        }

                        if (!skip)
                        {
                            Assembly assembly = null;

                            try { assembly = Compile(Mod); successful = true; }
                            catch (Exception ex) { successful = false; }

                            if (successful) AsyncAddToList(new assemblyInfo(assembly, ModName));
                        }
                        skip = false;
                    });
                } catch (Exception ex) {
                    AsyncAddToList($"\n<color=red>Failed to load mods Check Mods/ModLoader.log</color>");
                    goto RetryLoading;
                }
            yield return Ninja.JumpToUnity;
            StartCoroutine(AttachMods());
        }
#else
        string ModsFolderPath = Application.dataPath + "/Mods";
        if (!Directory.Exists(ModsFolderPath))
            Directory.CreateDirectory(ModsFolderPath);

        StreamWriter sw = new StreamWriter(Path.Combine(ModsFolderPath, "ModLoader.log"));
        sw.WriteLine("Initalizing modloader.");
        IniFile Mods_INI = new IniFile("Mods/Mods.ini");
        yield return Ninja.JumpBack;
        if (Mods_INI.KeyExists("Mods"))
        {
            int NumberOfMods = int.Parse(Mods_INI.Read("Mods"));
            sw.WriteLine($"Found {NumberOfMods} mods.");
            consoleLog.Add($"\nFound {NumberOfMods} mod{(NumberOfMods > 1 ? "s." : ".")}");
            if (NumberOfMods > 0)
            {
                try
                {
                    System.Threading.Tasks.Parallel.For(0, NumberOfMods - 1, (ModCounter, state) =>
                    {
                        string ModNumberString = ModCounter.ToString();
                        string ModName = Mods_INI.Read("Name", ModNumberString);
                        string ModDirectory = Mods_INI.Read("Directory", ModNumberString);

                        AsyncAddToList($"\nCompiling mod: 'Mod_{ModName}'... ");

                        bool successful = false;
                        StreamReader sr = new StreamReader(Path.Combine(ModsFolderPath, ModDirectory));
                        string Mod = sr.ReadToEnd();
                        sr.Close();
                        bool skip = false;
                        if (!ModSafetyChecker.CheckMod(Mod))
                        {

                            AsyncAddToList($"\n<color=red>Failed to load Mod_{ModName}. Mod contains references to blacklisted assemblies. Check blacklist info in /Mods/ folder.</color>");
                            sw.WriteLine($"'Mod_{ModName}' error: contains references to blacklisted assemblies. Check blacklist info in /Mods/ folder.");
                            skip = true;
                        }
                        
                        if (Mod.Contains("WorldGen")) {
                            autoResetEvent.WaitOne();
                            Mods.LoadedMapgens_Code.Add(Mod);
                            autoResetEvent.Set();
                        }
                        
                        if (!skip)
                        {
                            Assembly assembly = null;
                            sw.WriteLine($"Compiling mod: 'Mod_{ModName}'");

                            try { assembly = Compile(Mod, sw); successful = true; sw.WriteLine($"'Mod_{ModName}' compiled successfully."); }
                            catch (Exception ex) { successful = false; sw.WriteLine($"######################\n'Mod_{ModName}' failed to compile:\n{ex.ToString()}\n######################"); }

                            if (successful) 
                            {
                                AsyncAddToList(new assemblyInfo(assembly, ModName));
                                sw.WriteLine($"'Mod_{ModName}' Attaching to Mods gameobject...");
                            }
                            
                        }
                    });
                } catch (Exception ex) {
                    sw.WriteLine($"Modloader error: {ex.ToString()}");
                    AsyncAddToList($"\n<color=red>Failed to load mods Check Mods/ModLoader.log</color>");
                    goto RetryLoading;
                }
                yield return Ninja.JumpToUnity;
                StartCoroutine(AttachMods());
            }
            else
                sw.WriteLine("No mods were found.");
        }
        sw.Close();
#endif
#pragma warning restore 168
        while (ModList.Count > 0) { yield return null; };
        UpdateLogLoop = false;
        yield return Ninja.JumpToUnity;

        World world_ = GameObject.Find("World").GetComponent<World>();
        GameObject modLoaderCanvas = GameObject.Find("ModLoaderCanvas");
        UnityEngine.UI.Text LText = GameObject.Find("LoadingText").GetComponent<UnityEngine.UI.Text>();
        LText.text = "Loading...";

        //while (!world_.MainMenu_WorldReady) { yield return new WaitForEndOfFrame(); }

        while (BlackOverlay.color.a < 1f)
        {
            Color col = BlackOverlay.color;
            col.a += 1f * Time.deltaTime;
            BlackOverlay.color = col;
            yield return Macros.Coroutine.WaitFor_EndOfFrame;
        }

        MainMenuCanvas.SetActive(true);

        GameObject.Destroy(modLoaderCanvas);
    }

    public static Assembly Compile(string source, StreamWriter logOutput = null)
    {
        // Replace this Compiler.CSharpCodeProvider wth aeroson's version
        // if you're targeting non-Windows platforms:
        var provider = new CSharpCodeCompiler();

        // Add ALL of the assembly references
        /*foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            param.ReferencedAssemblies.Add(assembly.Location);
        }*/

        if (logOutput != null) logOutput.WriteLine("Parsing Assembly refferences...");

        string[] Assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location)
            .ToArray();

        if (logOutput != null)
        {
            logOutput.WriteLine($"Assemblies referenced:");
            foreach (string str in Assemblies)
                logOutput.WriteLine($"> {str}");
        }

        var param = new CompilerParameters(Assemblies);

        if (logOutput != null) logOutput.WriteLine("Assembly refferences parsed correctly.");

        // Or, uncomment just the assemblies you need...

        // System namespace for common types like collections.
        //param.ReferencedAssemblies.Add("System.dll");

        // This contains methods from the Unity namespaces:
        // param.ReferencedAssemblies.Add("UnityEngine.dll");

        // This assembly contains runtime C# code from your Assets folders:
        // (If you're using editor scripts, they may be in another assembly)
        //param.ReferencedAssemblies.Add("CSharp.dll");


        // Generate a dll in memory
        param.GenerateExecutable = false;
        param.GenerateInMemory = true;
        param.CompilerOptions = "/optimize";
        

        /*
        var unit = new CodeCompileUnit();
        var attr = new CodeTypeReference(typeof(AssemblyVersionAttribute));
        var decl = new CodeAttributeDeclaration(attr, new CodeAttributeArgument(new CodePrimitiveExpression("1.0.2.42")));
        unit.AssemblyCustomAttributes.Add(decl);
        var prov = new CSharpCodeProvider();
        var assemblyInfo = new StringWriter();
        prov.GenerateCodeFromCompileUnit(unit, assemblyInfo, new CodeGeneratorOptions());

        var result = prov.CompileAssemblyFromSource(param, source);
        */
        // Compile the source
        
        var result = provider.CompileAssemblyFromSource(param, source);

        if (result.Errors.Count > 0)
        {
            var msg = new StringBuilder();
            foreach (CompilerError error in result.Errors)
            {
                msg.AppendFormat("Error ({0}): {1}\n",
                    error.ErrorNumber, error.ErrorText);
            }

            if (logOutput != null && msg.Length > 0) logOutput.WriteLine($"Compilation error!\n{msg.ToString()}");
            throw new Exception(msg.ToString());
        }
        // Return the assembly
        return result.CompiledAssembly;
    }
    public static class ModSafetyChecker
    {
        public static bool CheckMod(string Code)
        {
            Code = Code.ToLower();
            if (Code.Contains("system.codedom")) return false;
            if (Code.Contains("system.componentmodel")) return false;
            if (Code.Contains("system.configuration")) return false;
            if (Code.Contains("system.data")) return false;
            if (Code.Contains("system.deployment")) return false;
            //if (Code.Contains("system.diagnostics")) return false;
            if (Code.Contains("system.globalization")) return false;
            if (Code.Contains("system.io")) return false;
            if (Code.Contains("system.management")) return false;
            if (Code.Contains("system.media")) return false;
            if (Code.Contains("system.net")) return false;
            if (Code.Contains("system.resources")) return false;
            if (Code.Contains("system.runtime")) return false;
            if (Code.Contains("system.security")) return false;
            if (Code.Contains("system.threading")) return false;
            if (Code.Contains("system.web")) return false;
            if (Code.Contains("system.windows")) return false;
            if (Code.Contains("system.xml")) return false;
            if (Code.Contains("webclient")) return false;
            if (Code.Contains("url")) return false;
            if (Code.Contains("uri")) return false;
            if (Code.Contains("http")) return false;
            if (Code.Contains("https")) return false;
            if (Code.Contains("ftp")) return false;
            return true;
        }
    }
}
