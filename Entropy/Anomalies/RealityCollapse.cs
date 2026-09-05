using System.Collections;
using System.Runtime.InteropServices;
using Reactor.Utilities;
using Random = System.Random;

namespace Entropy.Anomalies;

// Shows a desktop message at full entropy, once per round.
public class RealityCollapse : Anomaly
{
    private const uint IconError = 0x00000010;
    private const uint TopMost = 0x00040000;

    public override string Name => "REALITY COLLAPSE";

    public override float MinEntropy => 75f;

    public override bool Scheduled => false;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (target.AmOwner) Open(target.Data?.PlayerName ?? "you");

        yield break;
    }

    // MessageBox blocks until dismissed, so keep it off Unity's main thread.
    private static void Open(string playerName)
    {
        var thread = new Thread(() =>
        {
            try
            {
                MessageBoxW(
                    IntPtr.Zero,
                    $"{playerName}.\n\nNone of it was real.",
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
