using Newtonsoft.Json;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

public class ServerStartUp : MonoBehaviour
{
    private const string _internalServerIP = "0.0.0.0";
    private string _externalServerIP = "0.0.0.0";
    private ushort _internalServerPort = 7777;

    private bool _serverMode = false;

    private string ExternalConnectionString => $"{_externalServerIP}:{_internalServerPort}";

    private IMultiplayService _multiplayService;
    private const int _multiplayerServiceTimeout = 20000;

    private string _allocationId;
    private MultiplayEventCallbacks _serverCallbacks;
    private IServerEvents _serverEvents;

    private BackfillTicket _localBackfillTicket;
    private CreateBackfillTicketOptions _createBackfillOptions;
    private const int _ticketCheckMs = 1000;

    private MatchmakingResults _matchmakingPayload;

    private async void Start()
    {
        var args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-dedicatedServer")
            {
                _serverMode = true;

                Logging.Log("Server is starting in dedicated mode.");
                return;
            }

            if (args[i] == "-port" && (i + 1 < args.Length))
            {
                if (ushort.TryParse(args[i + 1], out ushort port))
                {
                    _internalServerPort = port;
                }
            }

            if (args[i] == "-ip" && (i + 1 < args.Length))
            {
                _externalServerIP = args[i + 1];
            }
        }

        if (_serverMode)
        {
            StartServer();
            await StartServerServices();
        }
    }

    private void StartServer()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(_internalServerIP, _internalServerPort);
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
    }

    private async Task StartServerServices()
    {
        await UnityServices.InitializeAsync();

        try
        {
            _multiplayService = MultiplayService.Instance;
            await _multiplayService.StartServerQueryHandlerAsync((ushort)NetworkApprovalHandler._maxPlayer, "n/a", "n/a", "0", "n/a");
        }
        catch (System.Exception e)
        {
            Logging.LogWarning($"Failed to start the query handler: {e.Message}");
        }

        try
        {
            _matchmakingPayload = await GetMatchmakerPayload(_multiplayerServiceTimeout);
            if (_matchmakingPayload != null)
            {
                Logging.Log($"Matchmaker Payload: {_matchmakingPayload}");
                await StartBackfill(_matchmakingPayload);
            }
            else
            {
                Logging.LogWarning("Failed to get the matchmaker payload.");
            }
        }
        catch (System.Exception e)
        {
            Logging.LogWarning($"Failed to get the matchmaker payload: {e.Message}");
        }
    }

    private async Task<MatchmakingResults> GetMatchmakerPayload(int _timeout)
    {
        var _matchmakerPayloadTask = SubscribeAndAwaitMatchmakerAllocation();

        if (await Task.WhenAny(_matchmakerPayloadTask, Task.Delay(_timeout)) == _matchmakerPayloadTask)
            return _matchmakerPayloadTask.Result;

        return null;
    }

    private async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        if (_multiplayService == null) return null;

        _allocationId = null;

        _serverCallbacks = new MultiplayEventCallbacks();
        _serverCallbacks.Allocate += OnMultiplayAllocated;

        _serverEvents = await _multiplayService.SubscribeToServerEventsAsync(_serverCallbacks);

        _allocationId = await AwaitAllocationId();
        var _mmPayload = await GetMatchmakingAllocationPayloadAsync();

        return _mmPayload;
    }

    private void OnMultiplayAllocated(MultiplayAllocation _multiplayAllocation)
    {
        Logging.Log($"Allocation ID: {_multiplayAllocation.AllocationId}");

        if (string.IsNullOrEmpty(_multiplayAllocation.AllocationId)) return;

        _allocationId = _multiplayAllocation.AllocationId;
    }

    private async Task<string> AwaitAllocationId()
    {
        var _config = _multiplayService.ServerConfig;

        Logging.Log(
            $"Awaiting Aloocation. Server Config is:\n" +
            $"-ServerID: {_config.ServerId}\n" +
            $"-AllocationID: {_config.AllocationId}\n" +
            $"-Port: {_config.Port}\n" +
            $"-QPort: {_config.QueryPort}\n" +
            $"-log: {_config.ServerLogDirectory}");

        while (string.IsNullOrEmpty(_allocationId))
        {
            var _configId = _config.AllocationId;

            if (!string.IsNullOrEmpty(_configId) && string.IsNullOrEmpty(_allocationId))
            {
                _allocationId = _configId;
                break;
            }

            await Task.Delay(100);
        }

        return _allocationId;
    }

    private async Task<MatchmakingResults> GetMatchmakingAllocationPayloadAsync()
    {
        try
        {
            var _payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

            var _modelAsJson = JsonConvert.SerializeObject(_payloadAllocation, Formatting.Indented);

            Logging.Log($"{nameof(GetMatchmakingAllocationPayloadAsync)}:\n{_modelAsJson}");

            return _payloadAllocation;
        }
        catch (System.Exception e)
        {
            Logging.LogWarning($"Failed to get the matchmaking payload: {e.Message}");
        }

        return null;
    }

    private async Task StartBackfill(MatchmakingResults _matchmakerPayload)
    {
        var _backfillProperties = new BackfillTicketProperties(_matchmakerPayload.MatchProperties);
        _localBackfillTicket = new BackfillTicket { Id = _matchmakerPayload.MatchProperties.BackfillTicketId, Properties = _backfillProperties };

        await BeginBackfilling(_matchmakerPayload);
    }

    private async Task BeginBackfilling(MatchmakingResults _matchmakerPayload)
    {
        var _matchProperties = _matchmakerPayload.MatchProperties;

        if (string.IsNullOrEmpty(_localBackfillTicket.Id))
        {
            _createBackfillOptions = new CreateBackfillTicketOptions
            {
                Connection = ExternalConnectionString,
                QueueName = _matchmakerPayload.QueueName,
                Properties = new BackfillTicketProperties(_matchProperties),
            };

            _localBackfillTicket.Id = await MatchmakerService.Instance.CreateBackfillTicketAsync(_createBackfillOptions);
        }

        BackfillLoop();
    }

    private async void BackfillLoop()
    {
        while (NeedsPlayers())
        {
            _localBackfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(_localBackfillTicket.Id);

            if (!NeedsPlayers())
            {
                await MatchmakerService.Instance.DeleteBackfillTicketAsync(_localBackfillTicket.Id);
                _localBackfillTicket.Id = null;
                return;
            }

            await Task.Delay(_ticketCheckMs);
        }
    }

    private void ClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count > 0 && NeedsPlayers())
        {
            _ = BeginBackfilling(_matchmakingPayload);
        }
    }

    private bool NeedsPlayers() => NetworkManager.Singleton.ConnectedClients.Count < NetworkApprovalHandler._maxPlayer;

    private void Dispose()
    {
        _serverCallbacks.Allocate -= OnMultiplayAllocated;
        _serverEvents?.UnsubscribeAsync();
    }
}