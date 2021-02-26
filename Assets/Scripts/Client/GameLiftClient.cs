/**
 * GameLiftClient.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2/24/2021 (en-US)
 */

#if CLIENT
using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
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
        var config = new AmazonGameLiftConfig()
        {
            ServiceURL = "http://10.0.1.2:7778"
        };
        Debug.Log(config.DetermineServiceURL());
        client = new AmazonGameLiftClient("key", "key", config);
        playerId = Guid.NewGuid().ToString();

        Quickplay();
    }

    async void Quickplay()
    {
        var sessions = await GetActiveGameSessionsAsync();
        Debug.Log($"Found {sessions.Count} active Game Sessions");
        if (sessions.Count <= 0)
            return;
        var sessionId = sessions.FirstOrDefault(s => s.Status == GameSessionStatus.ACTIVE).GameSessionId;
        Debug.Log($"Attempting to join session {sessionId}");
        currentPlayerSession = await CreatePlayerSessionAsync(sessionId);
        Debug.Log($"Successfully connected to session {currentPlayerSession.GameSessionId} at [{currentPlayerSession.DnsName}] {currentPlayerSession.IpAddress}:{currentPlayerSession.Port}");
        Debug.Log($"Attempting to Mirror Server at [{currentPlayerSession.DnsName}] {currentPlayerSession.IpAddress}:{currentPlayerSession.Port}");
        networkManager.StartClient(new Uri($"tcp4://{currentPlayerSession.DnsName}"));
        Debug.Log($"Successfully connected to Mirror Server at [{currentPlayerSession.DnsName}] {currentPlayerSession.IpAddress}:{currentPlayerSession.Port}");
    }

    async Task<List<GameSession>> GetActiveGameSessionsAsync(CancellationToken token = default)
    {
        var request = new DescribeGameSessionsRequest()
        {
            FleetId = "fleet-123"
        };
        var response = await client.DescribeGameSessionsAsync(request);
        return response.GameSessions;
    }

    async Task<PlayerSession> CreatePlayerSessionAsync(string gameSessionId, CancellationToken token = default)
    {
        var response = await client.CreatePlayerSessionAsync(gameSessionId, playerId);
        return response.PlayerSession;
    }
#endif
}