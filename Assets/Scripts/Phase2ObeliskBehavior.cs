using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the behavior of the Phase 2 Obelisk enemy in combat
/// </summary>
public class Phase2ObeliskBehavior : EnemyBehavior
{
    [Header("Skill Probabilities")]
    [Tooltip("Probability of using Malice of Stone skill (deals 70-90 damage to all party members)")]
    [Range(0, 100)]
    public float maliceOfStoneChance = 60f;
    
    [Tooltip("Probability of using Sunder skill (50% chance to instantly kill target)")]
    [Range(0, 100)]
    public float sunderChance = 40f;

    [Header("Animation References")]
    [Tooltip("GameObject that contains the coinflip animation visuals")]
    public GameObject coinflipVisuals;
    
    [Header("Animation Settings")]
    [Tooltip("Vertical offset for positioning the animation above the target")]
    public float verticalOffset = 1.5f;
    
    [Tooltip("Animation playback speed multiplier")]
    [Range(0.1f, 3.0f)]
    public float animationSpeed = 1.0f;
    
    // Add buffer time to ensure animation fully completes
    [Tooltip("Extra time in seconds to wait after animation is expected to complete")]
    [Range(0.1f, 3.0f)]
    public float animationBufferTime = 0.5f;
    
    private bool animationCompleted = false;
    private Animator _coinflipAnimator;
    
    // Get animator component from coinflipVisuals when needed
    private Animator CoinflipAnimator
    {
        get
        {
            if (_coinflipAnimator == null && coinflipVisuals != null)
            {
                _coinflipAnimator = coinflipVisuals.GetComponent<Animator>();
                Debug.Log($"Getting Animator reference: {(_coinflipAnimator != null ? "Found animator" : "Animator is NULL")}");
            }
            return _coinflipAnimator;
        }
    }

    public override IEnumerator ExecuteTurn(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Normalize chance values to ensure they add up to 100%
        float totalChance = maliceOfStoneChance + sunderChance;
        if (totalChance <= 0)
        {
            Debug.LogWarning("All Phase 2 Obelisk skill chances are set to 0, defaulting to Malice of Stone");
            yield return UseMaliceOfStoneSkill(enemy, players, combatUI);
            yield break;
        }
        
        // Calculate normalized probabilities
        float normalizedMaliceOfStone = maliceOfStoneChance / totalChance * 100f;
        float normalizedSunder = sunderChance / totalChance * 100f;
        
        // Roll for skill selection
        float roll = Random.Range(0f, 100f);
        
        if (roll < normalizedMaliceOfStone)
        {
            yield return UseMaliceOfStoneSkill(enemy, players, combatUI);
        }
        else
        {
            yield return UseSunderSkill(enemy, players, combatUI);
        }
    }
    
    private IEnumerator UseMaliceOfStoneSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display generic attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} channels dark energy!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Malice of Stone");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living players
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        
        // Deal 70-90 random damage to all living players
        foreach (var player in livingPlayers)
        {
            // Random damage between 70-90
            float baseDamage = Random.Range(70f, 90f);
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(baseDamage);
            
            player.TakeDamage(finalDamage);
            Debug.Log($"Malice of Stone hit {player.characterName} for {finalDamage} damage");
        }
    }
    
    private IEnumerator UseSunderSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display generic attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} focuses intently!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Sunder");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living players
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        if (livingPlayers.Count == 0) yield break;
        
        // Select a random player
        int randomIndex = Random.Range(0, livingPlayers.Count);
        var target = livingPlayers[randomIndex];
        
        // Position the coinflip visuals above the targeted player
        if (coinflipVisuals != null && target != null)
        {
            // Get the target's position and add vertical offset
            Vector3 targetPosition = target.transform.position;
            targetPosition.y += verticalOffset; // Apply offset to Y axis
            
            // Position the coinflip visuals
            coinflipVisuals.transform.position = targetPosition;
            
            // Show the coinflip visuals
            coinflipVisuals.SetActive(true);
            
            // Set animation speed
            if (CoinflipAnimator != null)
            {
                CoinflipAnimator.speed = animationSpeed;
            }
        }
        
        // Roll for 50% chance of instant kill
        bool playerSurvives = Random.value < 0.5f;
        
        // Dramatic pause before the result
        yield return new WaitForSeconds(1.0f);
        
        // Set the animation boolean parameter based on outcome
        if (CoinflipAnimator != null)
        {
            // Reset animation completion flag
            animationCompleted = false;
            
            // Set the bool parameter - true if player survives, false if player dies
            Debug.Log($"Setting PlayCoinflipSuccess to: {playerSurvives}");
            Debug.Log($"CoinflipAnimator reference check: {(CoinflipAnimator != null ? "Valid" : "NULL")}");
            CoinflipAnimator.SetBool("PlayerCoinflipSuccess", playerSurvives);
            Debug.Log($"PlayerCoinflipSuccess parameter value: {CoinflipAnimator.GetBool("PlayerCoinflipSuccess")}");
            
            // Get the length of the current animation
            AnimatorClipInfo[] clipInfo = CoinflipAnimator.GetCurrentAnimatorClipInfo(0);
            float clipLength = 0;
            
            // Wait a short frame to ensure the animation has started
            yield return null;
            
            // Try to get the clip info again
            clipInfo = CoinflipAnimator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                clipLength = clipInfo[0].clip.length / animationSpeed;
                Debug.Log($"Animation clip length: {clipLength} seconds (with speed adjustment)");
            }
            else
            {
                // Fallback if we couldn't get the clip length
                clipLength = 3.0f / animationSpeed;
                Debug.LogWarning("Couldn't get animation clip length, using fallback duration: " + clipLength);
            }
            
            // Wait for the animation to complete plus buffer time
            yield return new WaitForSeconds(clipLength + animationBufferTime);
        }
        
        if (playerSurvives)
        {
            // Create a miss popup directly instead of using TakeDamage with isMiss flag
            Vector3 popupPosition = target.transform.position + Vector3.up * 0.5f;
            DamagePopup.Create(popupPosition, 0, !target.isEnemy, target.transform, false, true);
            Debug.Log($"Sunder failed against {target.characterName} - target survives!");
            
            if (combatUI != null && combatUI.turnText != null)
            {
                combatUI.DisplayTurnAndActionMessage("The Obelisk Misses");
            }
        }
        else
        {
            // Deal 9999 damage to target (effectively instant kill)
            target.TakeDamage(9999);
            Debug.Log($"Sunder succeeded against {target.characterName} - target obliterated!");
            
            if (combatUI != null && combatUI.turnText != null)
            {
                combatUI.DisplayTurnAndActionMessage($"{target.characterName} is torn in half");
            }
        }
        
        // Hide the coinflip visual after it's done
        if (coinflipVisuals != null)
        {
            coinflipVisuals.SetActive(false);
        }
        
        // Small delay before continuing
        yield return new WaitForSeconds(0.5f);
    }
} 