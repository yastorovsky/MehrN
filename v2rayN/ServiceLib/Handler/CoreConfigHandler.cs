namespace ServiceLib.Handler;

/// <summary>
/// Core configuration file processing class
/// </summary>
public static class CoreConfigHandler
{
    private static readonly string _tag = "CoreConfigHandler";

    public static async Task<RetResult> GenerateClientConfig(CoreConfigContext context, string? fileName)
    {
        var config = AppManager.Instance.Config;
        var result = new RetResult();
        var node = context.Node;

        if (node.ConfigType == EConfigType.Custom)
        {
            result = node.CoreType switch
            {
                ECoreType.mihomo => await new CoreConfigClashService(config, context.IsTunEnabled).GenerateClientCustomConfig(node, fileName),
                ECoreType.aether => await GenerateClientAetherConfig(node, fileName),
                ECoreType.psiphon => await GenerateClientPsiphonConfig(node, fileName),
                _ => await GenerateClientCustomConfig(node, fileName)
            };
        }
        else if (context.RunCoreType == ECoreType.sing_box)
        {
            result = new CoreConfigSingboxService(context).GenerateClientConfigContent();
        }
        else
        {
            result = new CoreConfigV2rayService(context).GenerateClientConfigContent();
        }
        if (result.Success != true)
        {
            return result;
        }
        if (fileName.IsNotEmpty() && result.Data != null)
        {
            await File.WriteAllTextAsync(fileName, result.Data.ToString());
        }

        return result;
    }

    private static async Task<RetResult> GenerateClientCustomConfig(ProfileItem node, string? fileName)
    {
        var ret = new RetResult();
        try
        {
            if (node == null || fileName is null)
            {
                ret.Msg = ResUI.CheckServerSettings;
                return ret;
            }

            if (File.Exists(fileName))
            {
                File.SetAttributes(fileName, FileAttributes.Normal); //If the file has a read-only attribute, direct deletion will fail
                File.Delete(fileName);
            }

            var addressFileName = node.Address;
            if (!File.Exists(addressFileName))
            {
                addressFileName = Utils.GetConfigPath(addressFileName);
            }
            if (!File.Exists(addressFileName))
            {
                ret.Msg = ResUI.FailedGenDefaultConfiguration;
                return ret;
            }
            File.Copy(addressFileName, fileName);
            File.SetAttributes(fileName, FileAttributes.Normal); //Copy will keep the attributes of addressFileName, so we need to add write permissions to fileName just in case of addressFileName is a read-only file.

            //check again
            if (!File.Exists(fileName))
            {
                ret.Msg = ResUI.FailedGenDefaultConfiguration;
                return ret;
            }

            ret.Msg = string.Format(ResUI.SuccessfulConfiguration, "");
            ret.Success = true;
            return await Task.FromResult(ret);
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            ret.Msg = ResUI.FailedGenDefaultConfiguration;
            return ret;
        }
    }

    public static async Task<RetResult> GenerateClientAetherConfig(ProfileItem node, string? fileName)
    {
        var ret = new RetResult();
        try
        {
            if (node == null || fileName is null)
            {
                ret.Msg = ResUI.CheckServerSettings;
                return ret;
            }

            if (File.Exists(fileName))
            {
                File.SetAttributes(fileName, FileAttributes.Normal);
                File.Delete(fileName);
            }

            var addressFileName = node.Address;
            if (addressFileName.IsNotEmpty() && !File.Exists(addressFileName))
            {
                addressFileName = Utils.GetConfigPath(addressFileName);
            }

            if (File.Exists(addressFileName))
            {
                File.Copy(addressFileName, fileName);
                File.SetAttributes(fileName, FileAttributes.Normal);
            }
            else
            {
                // Write a base identity stub so Aether initializes seamlessly
                await File.WriteAllTextAsync(fileName, "# Aether Configuration\n");
            }

            ret.Msg = string.Format(ResUI.SuccessfulConfiguration, "");
            ret.Success = true;
            return ret;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            ret.Msg = ResUI.FailedGenDefaultConfiguration;
            return ret;
        }
    }

    public static async Task<RetResult> GenerateClientPsiphonConfig(ProfileItem node, string? fileName, string? upstreamProxyUrl = null)
    {
        var ret = new RetResult();
        try
        {
            if (node == null || fileName is null)
            {
                ret.Msg = ResUI.CheckServerSettings;
                return ret;
            }

            if (File.Exists(fileName))
            {
                File.SetAttributes(fileName, FileAttributes.Normal);
                File.Delete(fileName);
            }

            var extra = node.GetProtocolExtra();
            var globalPsiphon = AppManager.Instance.Config.PsiphonItem;

            // Build Psiphon JSON config (shirokhorshid fork compatible)
            var socksPort = node.PreSocksPort is > 0 and <= 65535 ? node.PreSocksPort.Value : 1080;
            var httpPort = socksPort + 1;
            var egressRegion = extra?.PsiphonEgressRegion.IsNotEmpty() == true
                ? extra.PsiphonEgressRegion
                : (globalPsiphon?.DefaultEgressRegion ?? "");
            var poolSize = (extra?.PsiphonTunnelPoolSize is > 0)
                ? extra.PsiphonTunnelPoolSize.Value
                : (globalPsiphon?.DefaultTunnelPoolSize is > 0 ? globalPsiphon.DefaultTunnelPoolSize : 1);
            var cdnFronting = extra?.PsiphonCdnFronting ?? globalPsiphon?.CdnFrontingEnabled ?? false;
            var cdnEdges = extra?.PsiphonCdnFrontingEdges.IsNotEmpty() == true
                ? extra.PsiphonCdnFrontingEdges
                : (globalPsiphon?.CdnFrontingEdges ?? "");

            var dirSuffix = fileName.IsNotEmpty() && Path.GetFileNameWithoutExtension(fileName) != "config"
                ? $"_{Path.GetFileNameWithoutExtension(fileName)}"
                : "";
            var dataDir = Path.Combine(Utils.GetBinConfigPath(), $"psiphon_data{dirSuffix}");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            EnsurePsiphonServerList(dataDir);

            var psiphonConfig = new Dictionary<string, object?>
            {
                ["LocalSocksProxyPort"] = socksPort,
                ["LocalHttpProxyPort"] = httpPort,
                ["DataRootDirectory"] = dataDir,
                ["EgressRegion"] = egressRegion,
                ["TunnelPoolSize"] = poolSize,
                ["EmitDiagnosticNotices"] = true,
                ["EmitServerAlerts"] = true,
                ["PropagationChannelId"] = "2C595532AC17D0E6",
                ["SponsorId"] = "1BC527D3D09985CF",
                ["ClientPlatform"] = Utils.IsWindows() ? "Windows_10.0.26200_11" : "Linux",
                ["ClientVersion"] = "158",
                ["UseIndistinguishableTLS"] = true,
                ["DisableLocalSocksProxy"] = false,
                ["DisableLocalHTTPProxy"] = false,
                ["RemoteServerListSignaturePublicKey"] = "MIICIDANBgkqhkiG9w0BAQEFAAOCAg0AMIICCAKCAgEAt7Ls+/39r+T6zNW7GiVpJfzq/xvL9SBH5rIFnk0RXYEYavax3WS6HOD35eTAqn8AniOwiH+DOkvgSKF2caqk/y1dfq47Pdymtwzp9ikpB1C5OfAysXzBiwVJlCdajBKvBZDerV1cMvRzCKvKwRmvDmHgphQQ7WfXIGbRbmmk6opMBh3roE42KcotLFtqp0RRwLtcBRNtCdsrVsjiI1Lqz/lH+T61sGjSjQ3CHMuZYSQJZo/KrvzgQXpkaCTdbObxHqb6/+i1qaVOfEsvjoiyzTxJADvSytVtcTjijhPEV6XskJVHE1Zgl+7rATr/pDQkw6DPCNBS1+Y6fy7GstZALQXwEDN/qhQI9kWkHijT8ns+i1vGg00Mk/6J75arLhqcodWsdeG/M/moWgqQAnlZAGVtJI1OgeF5fsPpXu4kctOfuZlGjVZXQNW34aOzm8r8S0eVZitPlbhcPiR4gT/aSMz/wd8lZlzZYsje/Jr8u/YtlwjjreZrGRmG8KMOzukV3lLmMppXFMvl4bxv6YFEmIuTsOhbLTwFgh7KYNjodLj/LsqRVfwz31PgWQFTEPICV7GCvgVlPRxnofqKSjgTWI4mxDhBpVcATvaoBl1L/6WLbFvBsoAUBItWwctO2xalKxF5szhGm8lccoc5MZr8kfE0uxMgsxz4er68iCID+rsCAQM=",
                ["ServerEntrySignaturePublicKey"] = "sHuUVTWaRyh5pZwy4UguSgkwmBe0EHtJJkoF5WrxmvA=",
                ["RemoteServerListURLs"] = new object[]
                {
                    new Dictionary<string, object> { ["OnlyAfterAttempts"] = 0, ["SkipVerify"] = false, ["URL"] = "aHR0cHM6Ly9zMy5hbWF6b25hd3MuY29tL3BzaXBob24vd2ViL2p2bmstMmN5ZC1tcDRoL3NlcnZlcl9saXN0X2NvbXByZXNzZWQ=" },
                    new Dictionary<string, object> { ["OnlyAfterAttempts"] = 2, ["SkipVerify"] = true, ["URL"] = "aHR0cHM6Ly93d3cud2hlZWxyc3NrbWluc2lkZS5jb20vd2ViL2p2bmstMmN5ZC1tcDRoL3NlcnZlcl9saXN0X2NvbXByZXNzZWQ=" },
                    new Dictionary<string, object> { ["OnlyAfterAttempts"] = 2, ["SkipVerify"] = true, ["URL"] = "aHR0cHM6Ly93d3cuYnJhbmRpbmd1c2FnYW1lcmVwLmNvbS93ZWIvanZuay0yY3lkLW1wNGgvc2VydmVyX2xpc3RfY29tcHJlc3NlZA==" },
                    new Dictionary<string, object> { ["OnlyAfterAttempts"] = 2, ["SkipVerify"] = true, ["URL"] = "aHR0cHM6Ly93d3cuZ3BhbGx0aGluZ3NudW1iZXJ3ZWF0aGVyLmNvbS93ZWIvanZuay0yY3lkLW1wNGgvc2VydmVyX2xpc3RfY29tcHJlc3NlZA==" }
                },
                ["ObfuscatedServerListRootURLs"] = new object[]
                {
                    new Dictionary<string, object> { ["OnlyAfterAttempts"] = 0, ["SkipVerify"] = false, ["URL"] = "aHR0cHM6Ly9zMy5hbWF6b25hd3MuY29tL3BzaXBob24vd2ViL2p2bmstMmN5ZC1tcDRoL29zbA==" },
                    new Dictionary<string, object> { ["OnlyAfterAttempts"] = 2, ["SkipVerify"] = true, ["URL"] = "aHR0cHM6Ly93d3cud2hlZWxyc3NrbWluc2lkZS5jb20vd2ViL2p2bmstMmN5ZC1tcDRoL29zbA==" },
                    new Dictionary<string, object> { ["OnlyAfterAttempts"] = 2, ["SkipVerify"] = true, ["URL"] = "aHR0cHM6Ly93d3cuYnJhbmRpbmd1c2FnYW1lcmVwLmNvbS93ZWIvanZuay0yY3lkLW1wNGgvb3Ns" },
                    new Dictionary<string, object> { ["OnlyAfterAttempts"] = 2, ["SkipVerify"] = true, ["URL"] = "aHR0cHM6Ly93d3cuZ3BhbGx0aGluZ3NudW1iZXJ3ZWF0aGVyLmNvbS93ZWIvanZuay0yY3lkLW1wNGgvb3Ns" }
                },
            };

            if (upstreamProxyUrl.IsNotEmpty())
            {
                psiphonConfig["UpstreamProxyURL"] = upstreamProxyUrl;
                psiphonConfig["UpstreamProxyUrl"] = upstreamProxyUrl;
            }

            // CDN fronting fields (shirokhorshid fork feature)
            if (cdnFronting && cdnEdges.IsNotEmpty())
            {
                // Parse user-supplied edge IPs/CIDRs/SNI; comma- or newline-separated
                var edges = cdnEdges
                    .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                psiphonConfig["CdnFrontingSpecs"] = edges.Select(e => new Dictionary<string, string>
                {
                    ["Address"] = e
                }).ToList<object>();
            }

            var json = JsonUtils.Serialize(psiphonConfig);
            await File.WriteAllTextAsync(fileName, json);

            ret.Msg = string.Format(ResUI.SuccessfulConfiguration, "");
            ret.Success = true;
            return ret;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            ret.Msg = ResUI.FailedGenDefaultConfiguration;
            return ret;
        }
    }

    public static string EnsurePsiphonServerList(string dataDir)
    {
        try
        {
            var targetPath = Path.Combine(dataDir, "server_list.dat");
            if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 0)
            {
                return targetPath;
            }

            var binPath = Path.Combine(Utils.GetBinPath(), "psiphon", "server_list.dat");
            if (File.Exists(binPath) && new FileInfo(binPath).Length > 0)
            {
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }
                File.Copy(binPath, targetPath, true);
                return targetPath;
            }

            var appDataPsiphon = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Psiphon3", "server_list.dat");
            if (File.Exists(appDataPsiphon) && new FileInfo(appDataPsiphon).Length > 0)
            {
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }
                File.Copy(appDataPsiphon, targetPath, true);
                return targetPath;
            }

            var localAppDataPsiphon = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Psiphon3", "server_list.dat");
            if (File.Exists(localAppDataPsiphon) && new FileInfo(localAppDataPsiphon).Length > 0)
            {
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }
                File.Copy(localAppDataPsiphon, targetPath, true);
                return targetPath;
            }

            var embedContent = EmbedUtils.GetEmbedText(Global.PsiphonServerList);
            if (embedContent.IsNotEmpty())
            {
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }
                File.WriteAllText(targetPath, embedContent);
                return targetPath;
            }

            if (File.Exists(binPath))
            {
                return binPath;
            }

            return targetPath;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            return string.Empty;
        }
    }

    public static async Task<RetResult> GenerateClientSpeedtestConfig(Config config, string fileName, List<ServerTestItem> selecteds, ECoreType coreType)
    {
        var result = new RetResult();
        var dummyNode = new ProfileItem
        {
            CoreType = coreType
        };
        var builderResult = await CoreConfigContextBuilder.Build(config, dummyNode);
        var context = builderResult.Context;
        foreach (var testItem in selecteds)
        {
            var node = testItem.Profile;
            var (actNode, _) = await CoreConfigContextBuilder.ResolveNodeAsync(context, node, true);
            if (node.IndexId == actNode.IndexId)
            {
                continue;
            }
            context.ServerTestItemMap[node.IndexId] = actNode.IndexId;
        }
        if (coreType == ECoreType.sing_box)
        {
            result = new CoreConfigSingboxService(context).GenerateClientSpeedtestConfig(selecteds);
        }
        else if (coreType == ECoreType.Xray)
        {
            result = new CoreConfigV2rayService(context).GenerateClientSpeedtestConfig(selecteds);
        }
        if (result.Success != true)
        {
            return result;
        }
        await File.WriteAllTextAsync(fileName, result.Data.ToString());
        return result;
    }

    public static async Task<RetResult> GenerateClientSpeedtestConfig(Config config, CoreConfigContext context, ServerTestItem testItem, string fileName)
    {
        var result = new RetResult();
        var initPort = AppManager.Instance.GetLocalPort(EInboundProtocol.speedtest);
        var port = Utils.GetFreePort(initPort + testItem.QueueNum);
        testItem.Port = port;

        if (context.RunCoreType == ECoreType.sing_box)
        {
            result = new CoreConfigSingboxService(context).GenerateClientSpeedtestConfig(port);
        }
        else
        {
            result = new CoreConfigV2rayService(context).GenerateClientSpeedtestConfig(port);
        }
        if (result.Success != true)
        {
            return result;
        }

        await File.WriteAllTextAsync(fileName, result.Data.ToString());
        return result;
    }
}
