![License](https://img.shields.io/github/license/joaoborks/unity-gamelift-mirror-sample)
![Last Commit](https://img.shields.io/github/last-commit/joaoborks/unity-gamelift-mirror-sample)

Unity GameLift/Mirror Sample
===

Features a working multiplayer sample using [AWS GameLift](https://aws.amazon.com/gamelift/) and [Mirror](https://mirror-networking.com/) on Unity. Supports
[IL2CPP](https://docs.unity3d.com/Manual/IL2CPP.html) and mobile devices

Table of Contents
---

- [Requirements](#requirements)
- [Understanding the Structure](#understanding-the-structure)
    - [TLDR Version](#tldr-version)
- [Working with the sample](#working-with-the-sample)
    - [Setup](#setup)
        - [Local](#local)
        - [Remote](#remote)
    - [Building](#building)
        - [Local](#local-1)
        - [Remote](#remote-1)
    - [Creating a Game Session](#creating-a-game-session)
        - [Local](#local-2)
        - [Remote](#remote-2)
    - [Creating a Player Session](#creating-a-player-session)
- [GameLift Authentication](#gamelift-authentication)
- [IL2CPP Considerations](#il2cpp-considerations)
- [What now?](#what-now)
- [References](#references)
    

Requirements
---

- [Unity 2019.4.17f1](https://unity3d.com/get-unity/download/archive) or later
- [AWS CLI](https://aws.amazon.com/cli/)
- [Java](https://www.java.com/en/download/) [Local testing only]

Understanding the Structure
---

First, let's dive into the components included in the Unity Project, what they do and how they work together:
- AWS GameLift Server SDK [Server-Only]
    - Communicates with the AWS GameLift service to register 
    [Game Session](https://docs.aws.amazon.com/gamelift/latest/developerguide/integration-server-sdk-csharp-ref-actions.html#integration-server-sdk-csharp-ref-processready) 
    callbacks
    - Dependencies:
        - Google.Protobuf
        - log4net
        - Newtonsoft.Json
        - System.Buffers
        - System.Collections.Immutable
        - System.Memory
        - websocket-sharp
        - System.Runtime.CompilerServices.Unsafe
- AWS GameLift SDK [Client-only]
    - Communicates with the AWS GameLift service to create a 
    [Player Session](https://docs.aws.amazon.com/gamelift/latest/apireference/API_PlayerSession.html) and be able to join the game session
    - Dependencies:
        - AWS Core
        - Microsoft.Bcl.AsyncInterfaces
        - System.Threading.Tasks.Extensions
        - System.Runtime.CompilerServices.Unsafe
- Mirror [Both]
    - Handles game client/server communication, sync and connections. The gameplay is created using Mirror.
    
Since we have libraries we're only ever using on the server or in the client, we need to be able to include/exclude them from the build. So,
for everything that is only required on the server, we only compile and include when the scripting define `SERVER` is specified. For
the client, the same goes for the `CLIENT` define. 

Mirror always has to be included, since the gameplay needs to exist both in the server and in the client. 
If you have any code that is worthless for the server or for the client, you can use these defines to restrict the code compilation.

### TLDR Version:
1. AWS GameLift manages server instantiation and auto-scaling
2. You need a custom AWS Game Server build
3. After handling the AWS GameLift communication and connections, Mirrors comes in
4. You create your gameplay using Mirror
5. You connect to the Game Server via Mirror, using the given IP Address from GameLift
6. Multiplayer Game runs

Working with the sample
---

I highly recommend that you start by [testing locally](https://docs.aws.amazon.com/gamelift/latest/developerguide/integration-testing-local.html),
specially if you still don't have an AWS account.

### Setup

#### Local
- Make sure to download the [GameLift Managed Servers SDK](https://aws.amazon.com/gamelift/getting-started/)
- Open the GameLift Local folder and run:
    ```
    java -jar GameLiftLocal.jar -p 7778
    ```
- Set the port in `GameLiftClient.cs` line 43 to the port you started the GameLift local service:
    ```csharp
                config.ServiceURL = "http://localhost:7778";
    ```

#### Remote
- Setup your AWS credentials in `GameLiftClient.cs` line 46:
```csharp
        client = new AmazonGameLiftClient("access-key", "secret-key", config);
```

Locally, these values do not matter. You shouldn't commit these values in your repository, or if you do, make sure they have restrictive AWS roles.

### Building

Now, to begin testing, you need to create a server build. In order to do that, make sure you are in the correct environment. Check `Environment/Server`.
If the option is greyed out, then you're already in the `SERVER` environment. Now you can make a Standalone Build for your desired platform. If you're
testing locally, choose your own OS. Otherwise I recommend you to use linux to upload the build to AWS.

:warning: _Make sure you check `Server Build` when building the game server!_

#### Local
Here, you should start your local GameLift server for testing and then executing your server build. 

#### Remote
You can now upload your build to the AWS GameLift service by using the following command:
```
aws gamelift upload-build --name <your build name> --build-version <your build number> --build-root <local build path> --operating-system AMAZON_LINUX --region <your region>
```

With the remote build setup, you can now create a fleet that will control the game server creation. Make sure to input the correct build,
providing the correct executable path and extension. Also, it's helpful if you provide an additional argument to output logs to a file: `-logfile logs/server.log`.
This way, when the Game Session gets terminated, you can download the logs via the AWS GameLift console.

Also remember to open the ports necessary for the client to communicate with the server. For this sample, I simply opened ports 7770-7780 on TCP and UDP.
The fleet initialization should take some time. After its status is set to `ACTIVE` you can create a Game Session.

### Creating a Game Session

Either locally or remotely, you will have to **manually** create your game sessions via the AWS CLI:

#### Local
- Set the `--endpoint-url` to your local ip and port used to start the GameLift local service
- The `--fleet-id` value does not really matter locally as long as it starts with `fleet-`
```
aws gamelift create-game-session --endpoint-url http://localhost:7778 --maximum-player-session-count 10 --fleet-id fleet-123
```

#### Remote:
- Make sure you have configured your AWS CLI via `aws config` with your credentials and region
- You can get your fleet id on the AWS GameLift Console
```
aws gamelift create-game-session --maximum-player-session-count 10 --fleet-id <your fleet id>
```

The Game Session should take a few seconds to start, and then it's ready to receive player sessions.

### Creating a Player Session

Make sure your environment is set to the `CLIENT` in `Environment/Client`. You can now make a client build if you want.
To test locally, you must check the `Local` toggle in the `Game Server` object on the `NetworkScene`.

Whenever you hit play in the Unity Editor, or simply run a client build, it will do the following steps:

1. Initialize the GameLift SDK
2. Connect to the AWS GameLift
3. Generate a random PlayerId
4. Get active fleets [Remote-only]
5. Get game sessions
6. Attempt to create a Player Session for the first active game session
7. Connect to the server via Mirror

At this point, you have reached the game server and you should only worry about gameplay.

GameLift Authentication
---

I had to create a simple Authentication layer with Mirror to pass the GameLift `Player Session Id` to the server.
That way, it can call `GameLiftServerAPI.AcceptPlayerSession()` and preserve the connection, otherwise the player would just get disconnected after 1 minute.

IL2CPP Considerations
---

In order to work with IL2CPP, all client binaries are **Net Standard 2.0** and the following **link.xml** is provided:
```xml
<linker>
    <assembly fullname="AWSSDK.Core" preserve="all"/>
    <assembly fullname="AWSSDK.GameLift" preserve="all"/>
</linker>
```
I didn't have to recompile any binaries, just simply downloaded the official **Net Standard 2.0** versions (and their dependencies) from [NuGet](https://www.nuget.org/).
The server does not have any requirement to be IL2CPP since it's a desktop build, so I only built it with Mono.

What now?
---

Now you should be able to use this sample as a reference to your own game and focus on creating the gameplay, instead of wasting any additional time digging
service documentations and tutorials. If there's anything else I could do to help you speed up this process, please do let me know.

References
---

- [AWS GameLift API Reference](https://docs.aws.amazon.com/gamelift/latest/apireference/Welcome.html)
- [AWS GameLift Server API Reference](https://docs.aws.amazon.com/gamelift/latest/developerguide/integration-server-sdk-csharp-ref.html)
- [Add GameLift to your game server](https://docs.aws.amazon.com/gamelift/latest/developerguide/gamelift-sdk-server-api.html)
- [Add Amazon GameLift to Your Game Client](https://docs.aws.amazon.com/gamelift/latest/developerguide/gamelift-sdk-client-api.html)
- [Testing Your Integration](https://docs.aws.amazon.com/gamelift/latest/developerguide/integration-testing-local.html)
- [Amazon GameLift Game Server/Client Interactions Diagram](https://docs.aws.amazon.com/gamelift/latest/developerguide/images/combined_api_interactions_vsd.png)
- [Official AWS GameLift Unity Sample](https://github.com/aws-samples/amazon-gamelift-unity)
- [BatteryAcid's Unity Custom GameLift Server Tutorial](https://github.com/BatteryAcid/unity-custom-gamelift-server)
- [BatteryAcid's Unity Custom GameLift Client Tutorial](https://github.com/BatteryAcid/unity-custom-gamelift-client)

---

Don't hesitate to create [issues](https://github.com/joaoborks/unity-gamelift-mirror-sample/issues) for suggestions and bugs. Have fun!
