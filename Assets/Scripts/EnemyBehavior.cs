using System.Collections;
using UnityEngine;

/// <summary>
/// Interface for all enemy behavior scripts to implement
/// </summary>
public interface IEnemyBehavior
{
    /// <summary>
    /// Execute the enemy's turn
    /// </summary>
    /// <param name="enemy">The enemy's CombatStats</param>
    /// <param name="players">List of player CombatStats</param>
    /// <param name="combatUI">Reference to the CombatUI</param>
    /// <returns>Wait time until the turn is complete</returns>
    IEnumerator ExecuteTurn(CombatStats enemy, System.Collections.Generic.List<CombatStats> players, CombatUI combatUI);
}

/// <summary>
/// Base class for enemy behaviors
/// </summary>
public abstract class EnemyBehavior : MonoBehaviour, IEnemyBehavior
{
    public abstract IEnumerator ExecuteTurn(CombatStats enemy, System.Collections.Generic.List<CombatStats> players, CombatUI combatUI);
    
    /// <summary>
    /// Helper method to wait for the action display to complete
    /// </summary>
    protected IEnumerator WaitForActionDisplay()
    {
        // Wait a tiny amount to ensure the action label coroutine has started
        yield return null;
        
        // Wait for the game to resume (after action display is done)
        while (Time.timeScale == 0)
            yield return null;
    }
} 