/**
 * GameLiftClient.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2/24/2021 (en-US)
 */

#if CLIENT
using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#endif
using UnityEngine;

public class GameLiftClient : MonoBehaviour
{
#if CLIENT
    public string PlayerSessionId => currentPlayerSession.PlayerSessionId;
    public string PlayerId => playerId;

    [SerializeField]
    bool local;

    AmazonGameLiftClient client;
    PlayerSession currentPlayerSession;
    NetworkManager networkManager;
    string playerId;

    void Awake()
    {
        networkManager = FindObjectOfType<NetworkManager>();
    }

    void Start()
    {
        var config = new AmazonGameLiftConfig();
        if (local)
            config.ServiceURL = "http://localhost:7778";
        else
            config.RegionEndpoint = RegionEndpoint.USEast2;
        client = new AmazonGameLiftClient("access-key", "secret-key", config);
        playerId = Guid.NewGuid().ToString();

        Quickplay();
    }

    async void Quickplay()
    {
        var fleets = new List<string>();
        if (!local)
        {
            fleets = await GetFleets();
            Debug.Log($"Found {fleets.Count} active Fleets");
            if (fleets.Count <= 0)
                return;
        }
        var sessions = await GetActiveGameSessionsAsync(local ? "fleet-123" : fleets.First());
        Debug.Log($"Found {sessions.Count} active Game Sessions");
        if (sessions.Count <= 0)
            return;
        var sessionId = sessions.FirstOrDefault(s => s.Status == GameSessionStatus.ACTIVE).GameSessionId;
        currentPlayerSession = await CreatePlayerSessionAsync(sessionId);
        Debug.Log($"Successfully connected to session {currentPlayerSession.GameSessionId} at [{currentPlayerSession.DnsName}] {currentPlayerSession.IpAddress}:{currentPlayerSession.Port}");
        networkManager.networkAddress = currentPlayerSession.IpAddress;
        networkManager.StartClient();
    }

    async Task<List<string>> GetFleets(CancellationToken token = default)
    {
        var response = await client.ListFleetsAsync(new ListFleetsRequest(), token);
        return response.FleetIds;
    }

    async Task<List<GameSession>> GetActiveGameSessionsAsync(string fleetId, CancellationToken token = default)
    {
        var response = await client.DescribeGameSessionsAsync(new DescribeGameSessionsRequest()
        {
            FleetId = fleetId
        }, token);
        return response.GameSessions;
    }

    async Task<PlayerSession> CreatePlayerSessionAsync(string gameSessionId, CancellationToken token = default)
    {
        var response = await client.CreatePlayerSessionAsync(gameSessionId, playerId, token);
        return response.PlayerSession;
    }
#endif
}