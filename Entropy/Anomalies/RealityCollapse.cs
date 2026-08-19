using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using Reactor.Utilities;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// The meter fills, and something steps out of the game onto the desktop.
/// </summary>
/// <remarks>
/// A real window, from the operating system rather than the game, because the whole
/// point is that it is not in the game. Everything else this mod does can be doubted -
/// it happened on your screen, in a game full of lies, and nobody else saw it. This
/// cannot: it is still there when you alt-tab, and it has your name on it.
/// <para>
/// Fired only by <see cref="Modifiers.EntropyModifier"/> at a full meter, never rolled.
/// </para>
/// </remarks>
public class RealityCollapse : Anomaly
{
    private const uint IconError = 0x00000010;
    private const uint TopMost = 0x00040000;

    public override string Name => "REALITY COLLAPSE";

    public override EntropyTier MinTier => EntropyTier.Critical;

    /// <summary>Only ever fired on purpose, at a full meter.</summary>
    public override bool Scheduled => false;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (target.AmOwner) Open(target.Data?.PlayerName ?? "you");

        yield break;
    }

    /// <summary>
    /// Puts a window on the desktop.
    /// </summary>
    /// <remarks>
    /// On its own thread: a message box does not return until it is dismissed, and on the
    /// game's thread that would freeze everyone else's game too, which would turn one
    /// player's private collapse into everybody's problem.
    /// </remarks>
    private static void Open(string who)
    {
        var thread = new Thread(() =>
        {
            try
            {
                MessageBoxW(
                    IntPtr.Zero,
                    $"{who}.\n\nNone of it was real.",
                    "Entropy",
                    IconError | TopMost);
            }
            catch (Exception exception)
            {
                Logger<EntropyPlugin>.Warning($"Could not open the window: {exception.Message}");
            }
        })
        {
            IsBackground = true,
        };

        thread.Start();
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
