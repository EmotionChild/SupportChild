using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportChild.Commands;

public class StatusCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("status", "Shows bot status and information.")]
	public async Task OnExecute(InteractionContext command)
	{
		long openTickets = Database.GetNumberOfTickets();
		long closedTickets = Database.GetNumberOfClosedTickets();

		DiscordEmbed botInfo = new DiscordEmbedBuilder()
			.WithAuthor("EmotionChild/supportchild @ GitHub", "https://github.com/EmotionChild/SupportChild", "https://cdn.discordapp.com/attachments/765441543100170271/914327948667011132/Ellie_Concept_2_transparent_ver.png")
			.WithTitle("Bot information")
			.WithColor(DiscordColor.Cyan)
			.AddField("Version:", SupportChild.GetVersion())
			.AddField("Open tickets:", openTickets + "", true)
			.AddField("Closed tickets:", closedTickets + " ", true);
		await command.CreateResponseAsync(botInfo);
	}
}
