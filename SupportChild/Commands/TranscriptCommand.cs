﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace SupportChild.Commands
{
    public class TranscriptCommand : BaseCommandModule
    {
        [Command("transcript")]
        [Cooldown(1, 5, CooldownBucketType.User)]
        public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
        {
            // Check if the user has permission to use this command.
            if (!Config.HasPermission(command.Member, "transcript"))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "You do not have permission to use this command."
                };
                await command.RespondAsync(error);
                command.Client.Logger.Log(LogLevel.Information, "User tried to use the transcript command but did not have permission.");
                return;
            }

            Database.Ticket ticket;
            string strippedMessage = command.Message.Content.Replace(Config.prefix, "");
            string[] parsedMessage = strippedMessage.Replace("<@!", "").Replace("<@", "").Replace(">", "").Split();

            // If there are no arguments use current channel
            if (parsedMessage.Length < 2)
            {
                if (Database.TryGetOpenTicket(command.Channel.Id, out ticket))
                {
                    try
                    {
                        await Transcriber.ExecuteAsync(command.Channel.Id, ticket.id);
                    }
                    catch (Exception)
                    {
                        DiscordEmbed error = new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Description = "ERROR: Could not save transcript file. Aborting..."
                        };
                        await command.RespondAsync(error);
                        throw;
                    }
                }
                else
                {
                    DiscordEmbed error = new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = "This channel is not a ticket."
                    };
                    await command.RespondAsync(error);
                    return;
                }
            }
            else
            {
                // Check if argument is numerical, if not abort
                if (!uint.TryParse(parsedMessage[1], out uint ticketID))
                {
                    DiscordEmbed error = new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = "Argument must be a number."
                    };
                    await command.RespondAsync(error);
                    return;
                }

                // If the ticket is still open, generate a new fresh transcript
                if (Database.TryGetOpenTicketByID(ticketID, out ticket) && ticket?.creatorID == command.Member.Id)
                {
                    try
                    {
                        await Transcriber.ExecuteAsync(command.Channel.Id, ticket.id);
                    }
                    catch (Exception)
                    {
                        DiscordEmbed error = new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Description = "ERROR: Could not save transcript file. Aborting..."
                        };
                        await command.RespondAsync(error);
                        throw;
                    }

                }
                // If there is no open or closed ticket, send an error. If there is a closed ticket we will simply use the old transcript from when the ticket was closed.
                else if (!Database.TryGetClosedTicket(ticketID, out ticket) || ticket?.creatorID != command.Member.Id && !Database.IsStaff(command.Member.Id))
                {
                    DiscordEmbed error = new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = "Could not find a closed ticket with that number which you opened." + (Config.HasPermission(command.Member, "list") ? "\n(Use the " + Config.prefix + "list command to see all your tickets)" : "")
                    };
                    await command.RespondAsync(error);
                    return;
                }
            }

            // Log it if the log channel exists
            DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
            if (logChannel != null)
            {
                DiscordEmbed embed = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = "Ticket " + ticket.id.ToString("00000") + " transcript generated by " + command.Member.Mention + ".\n",
                    Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + command.Channel.Name }
                };

                using (FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read))
                {
                    DiscordMessageBuilder message = new DiscordMessageBuilder();
                    message.WithEmbed(embed);
                    message.WithFiles(new Dictionary<string, Stream>() { { Transcriber.GetFilename(ticket.id), file } });

                    await logChannel.SendMessageAsync(message);
                }
            }

            try
            {
                // Send transcript privately
                DiscordEmbed directMessageEmbed = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = "Transcript generated, " + command.Member.Mention + "!\n"
                };

                using (FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read))
                {
                    DiscordMessageBuilder directMessage = new DiscordMessageBuilder();
                    directMessage.WithEmbed(directMessageEmbed);
                    directMessage.WithFiles(new Dictionary<string, Stream>() { { Transcriber.GetFilename(ticket.id), file } });

                    await command.Member.SendMessageAsync(directMessage);
                }

                // Respond to message directly
                DiscordEmbed response = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = "Transcript sent, " + command.Member.Mention + "!\n"
                };
                await command.RespondAsync(response);
            }
            catch (UnauthorizedException)
            {
                // Send transcript privately
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Not allowed to send direct message to you, " + command.Member.Mention + ", please check your privacy settings.\n"
                };
                await command.RespondAsync(error);
            }
        }
    }
}
