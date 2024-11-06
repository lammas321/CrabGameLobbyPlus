using ChatCommands;
using System.Collections.Generic;
using System.Linq;
using static ChatCommands.CommandArgumentParser;
using static LobbyPlus.LobbyPlus;

namespace LobbyPlus
{
    public class ReadyCommand : BaseCommand
    {
        public ReadyCommand()
        {
            id = "ready";
            description = "Makes given player(s) ready up.";
            args = new([
                new(
                    [typeof(DefaultCommandArgumentParsers.OnlineClientId[]), typeof(DefaultCommandArgumentParsers.OnlineClientId)],
                    "player(s)",
                    true
                )
            ]);
        }

        public override BaseCommandResponse Execute(BaseExecutionMethod executionMethod, object executorDetails, string args, bool ignorePermissions = false)
        {
            if (GameManager.Instance == null || LobbyManager.Instance.gameMode != GameModeManager.Instance.defaultMode)
                return new BasicCommandResponse(["You cannot make players ready up right now."], CommandResponseType.Private);

            if (args.Length == 0)
            {
                if (executionMethod is not ChatExecutionMethod)
                    return new BasicCommandResponse(["A player selector or player is required for the first argument."], CommandResponseType.Private);

                ulong clientId = (ulong)executorDetails;
                if (GameManager.Instance.activePlayers[clientId].waitingReady)
                    return new BasicCommandResponse(["You are already ready."], CommandResponseType.Private);

                GameManager.Instance.activePlayers[clientId].waitingReady = true;
                ServerSend.SendPlayerReady(clientId, true);
                if (LobbyManager.Instance.map.id == 6) // Dorm
                    ServerSend.Interact(clientId, 4);
                return new BasicCommandResponse([], CommandResponseType.Private);
            }

            if (!ignorePermissions && !executionMethod.HasPermission(executorDetails, "command.ready.others"))
                return new BasicCommandResponse(["You don't have sufficient permission to make other players ready up."], CommandResponseType.Private);

            IEnumerable<ulong> clientIds;
            ParsedResult<DefaultCommandArgumentParsers.OnlineClientId[]> playersResult = Api.CommandArgumentParser.Parse<DefaultCommandArgumentParsers.OnlineClientId[]>(args);
            if (playersResult.successful)
                clientIds = playersResult.result.Select(clientId => (ulong)clientId);
            else
            {
                ParsedResult<DefaultCommandArgumentParsers.OnlineClientId> playerResult = Api.CommandArgumentParser.Parse<DefaultCommandArgumentParsers.OnlineClientId>(args);
                if (playerResult.successful)
                    clientIds = [playerResult.result];
                else
                    return new BasicCommandResponse(["You did not select any players."], CommandResponseType.Private);
            }

            foreach (ulong clientId in clientIds)
            {
                if (GameManager.Instance.activePlayers[clientId].waitingReady)
                    continue;

                GameManager.Instance.activePlayers[clientId].waitingReady = true;
                ServerSend.SendPlayerReady(clientId, true);
                if (LobbyManager.Instance.map.id == 6) // Dorm
                    ServerSend.Interact(clientId, 4);
            }
            return new BasicCommandResponse([], CommandResponseType.Private);
        }
    }

    public class UnreadyCommand : BaseCommand
    {
        public UnreadyCommand()
        {
            id = "unready";
            description = "Makes given player(s) unready.";
            args = new([
                new(
                    [typeof(DefaultCommandArgumentParsers.OnlineClientId[]), typeof(DefaultCommandArgumentParsers.OnlineClientId)],
                    "player(s)",
                    true
                )
            ]);
        }

        public override BaseCommandResponse Execute(BaseExecutionMethod executionMethod, object executorDetails, string args, bool ignorePermissions = false)
        {
            if (GameManager.Instance == null || LobbyManager.Instance.gameMode != GameModeManager.Instance.defaultMode)
                return new BasicCommandResponse(["You cannot make players unready right now."], CommandResponseType.Private);

            if (args.Length == 0)
            {
                if (executionMethod is not ChatExecutionMethod)
                    return new BasicCommandResponse(["A player selector or player is required for the first argument."], CommandResponseType.Private);

                ulong clientId = (ulong)executorDetails;
                if (!GameManager.Instance.activePlayers[clientId].waitingReady)
                    return new BasicCommandResponse(["You are already unreadied."], CommandResponseType.Private);

                GameManager.Instance.activePlayers[clientId].waitingReady = false;
                ServerSend.SendPlayerReady(clientId, false);
                if (LobbyManager.Instance.map.id == 6) // Dorm
                    ServerSend.Interact(clientId, 4);
                return new BasicCommandResponse([], CommandResponseType.Private);
            }

            if (!ignorePermissions && !executionMethod.HasPermission(executorDetails, "command.unready.others"))
                return new BasicCommandResponse(["You don't have sufficient permission to make other players unready."], CommandResponseType.Private);

            IEnumerable<ulong> clientIds;
            ParsedResult<DefaultCommandArgumentParsers.OnlineClientId[]> playersResult = Api.CommandArgumentParser.Parse<DefaultCommandArgumentParsers.OnlineClientId[]>(args);
            if (playersResult.successful)
                clientIds = playersResult.result.Select(clientId => (ulong)clientId);
            else
            {
                ParsedResult<DefaultCommandArgumentParsers.OnlineClientId> playerResult = Api.CommandArgumentParser.Parse<DefaultCommandArgumentParsers.OnlineClientId>(args);
                if (playerResult.successful)
                    clientIds = [playerResult.result];
                else
                    return new BasicCommandResponse(["You did not select any players."], CommandResponseType.Private);
            }

            foreach (ulong clientId in clientIds)
            {
                if (!GameManager.Instance.activePlayers[clientId].waitingReady)
                    continue;

                GameManager.Instance.activePlayers[clientId].waitingReady = false;
                ServerSend.SendPlayerReady(clientId, false);
                if (LobbyManager.Instance.map.id == 6) // Dorm
                    ServerSend.Interact(clientId, 4);
            }
            return new BasicCommandResponse([], CommandResponseType.Private);
        }
    }

    public class LoadLobbyCommand : BaseCommand
    {
        public LoadLobbyCommand()
        {
            id = "loadlobby";
            description = "Loads the given Lobby+ lobby config.";
            args = new([
                new(
                    [typeof(string)],
                    "config"
                )
            ]);
        }

        public override BaseCommandResponse Execute(BaseExecutionMethod executionMethod, object executorDetails, string args, bool ignorePermissions = false)
        {
            if (args.Length == 0)
                return new BasicCommandResponse(["You didn't specify a config to load."], CommandResponseType.Private);

            ParsedResult<string> configResult = Api.CommandArgumentParser.Parse<string>(args);
            if (!configResult.successful)
                return new BasicCommandResponse(["You didn't specify a valid config to load."], CommandResponseType.Private);

            Instance.LoadConfig(configResult.result);
            return new StyledCommandResponse("Lobby+", [$"Loaded the '{configResult.result}' lobby config."]);
        }
    }

    public class ReloadLobbyCommand : BaseCommand
    {
        public ReloadLobbyCommand()
        {
            id = "reloadlobby";
            description = "Reloads the current Lobby+ lobby config.";
            args = new([]);
        }

        public override BaseCommandResponse Execute(BaseExecutionMethod executionMethod, object executorDetails, string args, bool ignorePermissions = false)
        {
            Instance.LoadConfig(LobbyPlus.LobbyConfig.Name);
            return new StyledCommandResponse("Lobby+", [$"Reloaded the '{LobbyPlus.LobbyConfig.Name}' lobby config."]);
        }
    }
}