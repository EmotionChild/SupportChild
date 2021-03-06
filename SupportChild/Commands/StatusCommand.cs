using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SupportChild.Commands
{
    public class StatusCommand : BaseCommandModule
    {
        [Command("status")]
        [Cooldown(1, 5, CooldownBucketType.User)]
        public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
        {
            // Check if the user has permission to use this command.
            if (!Config.HasPermission(command.Member, "status"))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "You do not have permission to use this command."
                };
                await command.RespondAsync(error);
                command.Client.Logger.Log(LogLevel.Information, "User tried to use the status command but did not have permission.");
                return;
            }

            long openTickets = Database.GetNumberOfTickets();
            long closedTickets = Database.GetNumberOfClosedTickets();

            DiscordEmbed botInfo = new DiscordEmbedBuilder()
                .WithAuthor("EmotionChild/SupportChild @ GitHub", "https://github.com/EmotionChild/SupportChild", "https://cdn.emotionchild.com/Ellie.png")
                .WithTitle("Bot information")
                .WithColor(DiscordColor.Cyan)
                .AddField("Version:", SupportChild.GetVersion(), false)
                .AddField("Open tickets:", openTickets + "", true)
                .AddField("Closed tickets (1.1.0+ tickets only):", closedTickets + " ", true);
            await command.RespondAsync(botInfo);
        }
    }
}