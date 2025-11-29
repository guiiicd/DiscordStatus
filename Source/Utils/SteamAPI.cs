using System.Reflection;
using System.Runtime.InteropServices;

namespace DiscordStatus.Utils;

public class SteamAPI
{
    private IntPtr _gGameServer = IntPtr.Zero;

    // Definições para carregar a biblioteca da Steam
    [DllImport("steam_api")]
    private static extern IntPtr SteamInternal_CreateInterface(string name);

    [DllImport("steam_api", EntryPoint = "SteamGameServer_GetHSteamPipe", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SteamGameServer_GetHSteamPipe();

    [DllImport("steam_api", EntryPoint = "SteamGameServer_GetHSteamUser", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SteamGameServer_GetHSteamUser();

    [DllImport("steam_api", EntryPoint = "SteamAPI_ISteamClient_GetISteamGenericInterface", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ISteamClient_GetISteamGenericInterface(IntPtr instancePtr, IntPtr hSteamUser, IntPtr hSteamPipe, string pchVersion);

    [DllImport("steam_api", EntryPoint = "SteamAPI_ISteamGameServer_GetSteamID", CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong ISteamGameServer_GetSteamID(IntPtr instancePtr);

    public SteamAPI()
    {
        // Resolve a DLL correta (Linux vs Windows)
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
    }

    public void Initialize()
    {
        LoadSteamClient();
    }

    // Método principal que você vai chamar para pegar o IP do SDR
    public string? GetSdrConnectString()
    {
        if (_gGameServer != IntPtr.Zero)
        {
            var steamID64 = ISteamGameServer_GetSteamID(_gGameServer);
            if (steamID64 == 0) return null;

            return ConvertSteamID64ToSteamID(steamID64);
        }
        
        // Tenta recarregar se perdeu a referência
        LoadSteamClient();
        return null;
    }

    private void LoadSteamClient()
    {
        try 
        {
            var steamPipe = SteamGameServer_GetHSteamPipe();
            var steamUser = SteamGameServer_GetHSteamUser();

            if (steamPipe == 0 || steamUser == 0) return;

            var steamClient = SteamInternal_CreateInterface("SteamClient020");
            if (steamClient == IntPtr.Zero) return;

            _gGameServer = ISteamClient_GetISteamGenericInterface(steamClient, steamUser, steamPipe, "SteamGameServer014");
            
            if (_gGameServer != IntPtr.Zero)
            {
                Console.WriteLine("[DiscordStatus] Steam API carregada com sucesso (SDR Ativo).");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DiscordStatus] Erro ao carregar SteamAPI: {ex.Message}");
        }
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == "steam_api")
        {
            // Detecta se é Linux (libsteam_api.so) ou Windows (steam_api64.dll)
            string lib = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "steam_api64" : "libsteam_api";
            return NativeLibrary.Load(lib, assembly, searchPath);
        }
        return IntPtr.Zero;
    }

    private string? ConvertSteamID64ToSteamID(ulong steamID64)
    {
        // Lógica matemática para converter o ID numérico no formato [A:1:...]
        uint accountID = (uint)(steamID64 & 0xFFFFFFFF);
        uint instance = (uint)((steamID64 >> 32) & 0xFFFFF);
        uint accountType = (uint)((steamID64 >> 52) & 0xF);
        uint universe = (uint)((steamID64 >> 56) & 0xFF);

        char accountTypeChar = accountType switch
        {
            0 => 'I', 1 => 'U', 2 => 'M', 3 => 'G', 4 => 'A', 5 => 'P', 6 => 'C', 7 => 'g', 8 => 'T', _ => 'I',
        };

        if (accountID == 0) return null;

        return $"[{accountTypeChar}:{universe}:{accountID}:{instance}]";
    }
}