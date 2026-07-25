using System;
using System.IO;
using System.Linq;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Rendering;

public static class AnsiAnimation
{
	public static void PlayTypewriterHeader(int colDelayMs = 25)
	{
		Console.Clear();
		var bannerLines = new[]
		{
			"  ██████╗  ██╗  ██╗  ██████╗  ██████╗  ██╗  ██╗ ████████╗   ██╗    ██████╗   ██████╗ ",
			"  ██╔══██╗ ██║  ██║ ██╔════╝ ██╔═══██╗ ██║  ██║ ╚══██╔══╝   ██║   ╚════██╗ ██╔════╝ ",
			"  ██║  ██║ ██║  ██║ ██║  ███╗██║   ██║ ██║  ██║    ██║      ╚═╝    █████╔╝ ███████╗ ",
			"  ██║  ██║ ██║  ██║ ██║   ██║██║   ██║ ██║  ██║    ██║            ██╔═══╝  ██╔═══██╗",
			"  ██████╔╝ ╚█████╔╝ ╚██████╔╝╚██████╔╝ ╚█████╔╝    ██║            ███████╗ ╚██████╔╝",
			"  ╚═════╝   ╚════╝   ╚═════╝  ╚═════╝   ╚════╝     ╚═╝            ╚══════╝  ╚═════╝ "
		};

		int maxCols = bannerLines.Max(l => l.Length);

		if (colDelayMs <= 0)
		{
			AnsiConsole.Write(new FigletText("DUGOUT '26").Color(Color.Red));
			return;
		}

		Console.ForegroundColor = ConsoleColor.Red;

		for (int col = 1; col <= maxCols; col++)
		{
			try
			{
				Console.SetCursorPosition(0, 0);
			}
			catch
			{
				// Ignore set cursor failure in non-interactive / redirected streams
			}

			for (int row = 0; row < bannerLines.Length; row++)
			{
				var lineText = bannerLines[row];
				var visibleLen = Math.Min(col, lineText.Length);
				var visiblePart = lineText.Substring(0, visibleLen);
				Console.WriteLine(visiblePart.PadRight(maxCols));
			}

			Thread.Sleep(colDelayMs);
		}

		Console.ResetColor();
		Console.WriteLine();

		// Blinking DOS/Retro cursor a couple times
		for (int blink = 0; blink < 3; blink++)
		{
			try
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("  █");
				Thread.Sleep(250);
				Console.Write("\b \b");
				Thread.Sleep(250);
			}
			catch
			{
				// Ignore if non-interactive console
			}
		}

		Console.ResetColor();
		Console.WriteLine();
	}

	public static void SlowRevealRenderable(IRenderable renderable, int lineDelayMs = 60)
	{
		if (lineDelayMs <= 0)
		{
			AnsiConsole.Write(renderable);
			return;
		}

		using var writer = new StringWriter();
		var captureConsole = AnsiConsole.Create(new AnsiConsoleSettings
		{
			Ansi = AnsiSupport.Yes,
			ColorSystem = ColorSystemSupport.TrueColor,
			Out = new AnsiConsoleOutput(writer)
		});

		captureConsole.Write(renderable);

		var lines = writer.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
		foreach (var line in lines)
		{
			Console.WriteLine(line);
			if (lineDelayMs > 0)
			{
				Thread.Sleep(lineDelayMs);
			}
		}
	}

	public static void PlayBatterSwinging(int frameDelayMs = 180)
	{
		string[][] frames = new string[][]
		{
			// Frame 0: Stepping into the box
			new string[]
			{
				"[red]       ┌───────────────────────────────────────────────┐[/]",
				"[red]       │           BOTTOM OF THE 9TH - FULL COUNT      │[/]",
				"[red]       └───────────────────────────────────────────────┘[/]",
				"",
				"       [grey]PITCHER[/]                      [yellow]BATTER[/]",
				"         [cyan]o[/]                           [green]o[/]",
				"        [cyan]/|\\ [/]                        [green]/|\\=[/][gold1]||[/]",
				"        [cyan]/ \\ [/]                        [green]/ \\ [/]",
				"   ─────────────────────────────────────────────────────────────",
				"   [dim]Batter steps into the box... Crowd cheers![/]"
			},
			// Frame 1: Pitcher Windup
			new string[]
			{
				"[red]       ┌───────────────────────────────────────────────┐[/]",
				"[red]       │           BOTTOM OF THE 9TH - FULL COUNT      │[/]",
				"[red]       └───────────────────────────────────────────────┘[/]",
				"",
				"       [grey]PITCHER[/]                      [yellow]BATTER[/]",
				"        [cyan]o[/]                            [green]o[/]",
				"       [cyan]/|\\_[/]                       [green]/|\\=[/][gold1]||[/]",
				"       [cyan]/ | [/]                        [green]/ \\ [/]",
				"   ─────────────────────────────────────────────────────────────",
				"   [yellow]Here comes the windup...[/]"
			},
			// Frame 2: The Pitch!
			new string[]
			{
				"[red]       ┌───────────────────────────────────────────────┐[/]",
				"[red]       │           BOTTOM OF THE 9TH - FULL COUNT      │[/]",
				"[red]       └───────────────────────────────────────────────┘[/]",
				"",
				"       [grey]PITCHER[/]      [white]o[/]               [yellow]BATTER[/]",
				"        [cyan]\\o/[/]                         [green]o[/]",
				"         [cyan]| [/]                         [green]/|\\=[/][gold1]||[/]",
				"        [cyan]/ \\[/]                         [green]/ \\ [/]",
				"   ─────────────────────────────────────────────────────────────",
				"   [bold white]THE PITCH IS INCOMING![/]"
			},
			// Frame 3: Fastball in the zone!
			new string[]
			{
				"[red]       ┌───────────────────────────────────────────────┐[/]",
				"[red]       │           BOTTOM OF THE 9TH - FULL COUNT      │[/]",
				"[red]       └───────────────────────────────────────────────┘[/]",
				"",
				"       [grey]PITCHER[/]             [white]o[/]        [yellow]BATTER[/]",
				"        [cyan]\\o/[/]                         [green]o[/]",
				"         [cyan]| [/]                         [green]/|\\=[/][gold1]||[/]",
				"        [cyan]/ \\[/]                         [green]/ \\ [/]",
				"   ─────────────────────────────────────────────────────────────",
				"   [bold yellow]FASTBALL IN THE ZONE![/]"
			},
			// Frame 4: SWING AND CRACK!
			new string[]
			{
				"[red]       ┌───────────────────────────────────────────────┐[/]",
				"[red]       │           BOTTOM OF THE 9TH - FULL COUNT      │[/]",
				"[red]       └───────────────────────────────────────────────┘[/]",
				"",
				"       [grey]PITCHER[/]               [bold yellow]💥 CRACK! 💥[/]",
				"        [cyan]\\o/[/]                         [bold green]\\o/[/][gold1]═══[/]",
				"         [cyan]| [/]                         [bold green] | [/]",
				"        [cyan]/ \\[/]                         [bold green]/ \\ [/]",
				"   ─────────────────────────────────────────────────────────────",
				"   [bold yellow]CRACK! HIGH FLY BALL TO DEEP LEFT FIELD![/]"
			},
			// Frame 5: Flying High
			new string[]
			{
				"[red]       ┌───────────────────────────────────────────────┐[/]",
				"[red]       │           HIGH FLY BALL GOING...              │[/]",
				"[red]       └───────────────────────────────────────────────┘[/]",
				"",
				"                  [bold white]⚾[/]",
				"       [grey]PITCHER[/]                             [yellow]BATTER[/]",
				"        [cyan]o[/]                                [green]\\o/[/]",
				"       [cyan]/|\\[/]                                [green]|[/]",
				"       [cyan]/ \\[/]                               [green]/ \\[/]",
				"   ─────────────────────────────────────────────────────────────",
				"   [bold magenta]IT'S GOING... GOING...[/]"
			},
			// Frame 6: Way back!
			new string[]
			{
				"[red]       ┌───────────────────────────────────────────────┐[/]",
				"[red]       │           WAY BACK NEAR THE FENCE!            │[/]",
				"[red]       └───────────────────────────────────────────────┘[/]",
				"",
				"                               [bold white]⚾[/]",
				"       [grey]PITCHER[/]                             [yellow]BATTER[/]",
				"        [cyan]o[/]                                [green]\\o/[/]",
				"       [cyan]/|\\[/]                                [green]|[/]",
				"       [cyan]/ \\[/]                               [green]/ \\[/]",
				"   ─────────────────────────────────────────────────────────────",
				"   [bold magenta]OUTFIELD RETREATS TO THE WALL...[/]"
			},
			// Frame 7: OVER THE FENCE! HOME RUN!
			new string[]
			{
				"[bold gold1]  ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ [/]",
				"[bold gold1]  ★  H O M E   R U N !  D U G O U T   M A N A G E R   ' 2 6   ★ [/]",
				"[bold gold1]  ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ ★ [/]",
				"",
				"                                      [bold white]⚾[/] [green]TOUCH 'EM ALL![/]",
				"       [grey]PITCHER[/]                             [bold yellow]\\o/[/]",
				"        [cyan]o[/]                                [bold yellow] | [/]",
				"       [cyan]/|\\[/]                               [bold yellow]/ \\ [/]",
				"   ─────────────────────────────────────────────────────────────",
				"   [bold green]IT IS GONE! A TOWERING HOME RUN![/]"
			}
		};

		foreach (var frame in frames)
		{
			AnsiConsole.Clear();
			foreach (var line in frame)
			{
				AnsiConsole.MarkupLine(line);
			}

			if (frameDelayMs > 0)
			{
				Thread.Sleep(frameDelayMs);
			}
		}

		if (frameDelayMs > 0)
		{
			Thread.Sleep(400);
		}
	}
}
