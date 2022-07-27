#if DEBUG
using MelonLoader;
using MerinoClient;
using Main = MerinoClient.Main;

[assembly: MelonInfo(typeof(Main), ModInfo.Name, ModInfo.Version, ModInfo.Author)]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: MelonProcess("VRChat.exe")]
[assembly: MelonOptionalDependencies("BouncyCastle.Crypto")]
[assembly: VerifyLoaderVersion(0, 5, 4, true)]
#endif