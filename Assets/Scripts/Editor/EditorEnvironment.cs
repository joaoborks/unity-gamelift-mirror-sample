/**
 * EditorEnvironment.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2/23/2021 (en-US)
 */

using System.Linq;
using UnityEditor;

public class EditorEnvironment
{
    const string ClientDefinition = "CLIENT";
    const string ServerDefinition = "SERVER";

    [MenuItem("Environment/Client")]
    public static void SetClientEnvironment() => SetEnvironment(ClientDefinition);

    [MenuItem("Environment/Client", true)]
    public static bool SetClientEnvironmentValidate() => !HasScriptingDefine(ClientDefinition);

    [MenuItem("Environment/Server")]
    public static void SetServerEnvironment() => SetEnvironment(ServerDefinition);

    [MenuItem("Environment/Server", true)]
    public static bool SetServerEnvironmentValidate() => !HasScriptingDefine(ServerDefinition);

    static void SetEnvironment(string environment)
    {
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();
        defines.RemoveAll(d => d == ClientDefinition || d == ServerDefinition);
        defines.Add(environment);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", defines));
    }

    static bool HasScriptingDefine(string define) => PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Contains(define);
}