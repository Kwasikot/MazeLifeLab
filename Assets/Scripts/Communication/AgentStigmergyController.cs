using UnityEngine;

/// <summary>
/// Deposits environmental signals based on EXP-005 communication mode.
/// </summary>
public class AgentStigmergyController : MonoBehaviour
{
    [SerializeField] float trailDepositStrength = 1f;
    [SerializeField] float frontierDepositStrength = 1.5f;
    [SerializeField] float randomNoiseDepositStrength = 0.75f;
    [SerializeField] float randomNoiseChance = 0.15f;

    MazeStigmergyField _field;
    MazeGenerator _truth;
    Experiment005CommunicationMode _mode;
    System.Random _rng;
    int _agentIndex;
    int _deposits;

    public int Deposits => _deposits;

    public void BeginEpisode(
        MazeStigmergyField field,
        Experiment005CommunicationMode mode,
        int agentIndex,
        int mazeSeed,
        MazeGenerator truth)
    {
        _field = field;
        _mode = mode;
        _agentIndex = agentIndex;
        _truth = truth;
        _rng = new System.Random(mazeSeed + 902501 + agentIndex * 3571);
        _deposits = 0;
    }

    public void EndEpisode()
    {
        _field = null;
        _truth = null;
        _rng = null;
        _deposits = 0;
    }

    public void NotifyAtCell(int cellX, int cellY, bool isFrontierCell = false)
    {
        if (_field == null || _truth == null || _mode == Experiment005CommunicationMode.None)
            return;

        switch (_mode)
        {
            case Experiment005CommunicationMode.Trail:
                Deposit(cellX, cellY, trailDepositStrength);
                break;
            case Experiment005CommunicationMode.FrontierHint:
                if (isFrontierCell)
                    Deposit(cellX, cellY, frontierDepositStrength);
                break;
            case Experiment005CommunicationMode.RandomNoise:
                if (_rng.NextDouble() < randomNoiseChance)
                {
                    int[][] directions =
                    {
                        new[] { 0, 1 },
                        new[] { 1, 0 },
                        new[] { 0, -1 },
                        new[] { -1, 0 }
                    };
                    int[] pick = directions[_rng.Next(directions.Length)];
                    int nx = cellX + pick[0];
                    int ny = cellY + pick[1];
                    if (_truth.IsCellInBounds(nx, ny))
                        Deposit(nx, ny, randomNoiseDepositStrength);
                    else
                        Deposit(cellX, cellY, randomNoiseDepositStrength);
                }
                break;
        }
    }

    void Deposit(int cellX, int cellY, float amount)
    {
        _field.Deposit(cellX, cellY, amount, _agentIndex);
        _deposits++;
    }
}
