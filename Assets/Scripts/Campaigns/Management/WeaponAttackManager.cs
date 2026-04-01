using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages weapon attack targeting and execution.
/// When the Weapon Attack button is clicked, enters targeting mode
/// to select an enemy and deal weapon damage.
/// </summary>
public class WeaponAttackManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button weaponAttackButton;
    [SerializeField] private GameObject targetingIndicator;
    [SerializeField] private TextMeshProUGUI targetingText;

    [Header("Attack Info Display")]
    [SerializeField] private TextMeshProUGUI weaponNameText; // Optional: shows weapon name on button

    // State
    private bool isTargeting = false;
    private CharacterData attacker;
    private EnemyData enemyAttacker;
    private bool isEnemyAttacker;
    private Token attackerToken;
    private string currentWeaponName;
    private string currentWeaponDamage;

    // Singleton
    public static WeaponAttackManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Wire up button
        if (weaponAttackButton != null)
        {
            weaponAttackButton.onClick.AddListener(OnWeaponAttackButtonClicked);
        }

        // Hide targeting UI initially
        if (targetingIndicator != null)
        {
            targetingIndicator.SetActive(false);
        }
    }

    void Update()
    {
        if (!isTargeting) return;

        // Right-click to cancel
        if (Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
            return;
        }
    }

    /// <summary>
    /// Called by Token when clicked during targeting mode
    /// </summary>
    public void OnTokenClicked(Token targetToken)
    {
        if (!isTargeting || targetToken == null) return;
        TryAttackToken(targetToken);
    }

    /// <summary>
    /// Called when the Weapon Attack button is clicked
    /// </summary>
    private void OnWeaponAttackButtonClicked()
    {
        //Get the current participant
        if (CombatManager.Instance == null || CombatManager.Instance.GetCombatState() != CombatState.Active)
        {
            Debug.LogWarning("WeaponAttackManager: Combat not active");
            return;
        }
        
        // Check if it's this player's turn
        if (!CombatManager.Instance.IsMyTurn())
        {
            Debug.Log("WeaponAttackManager: Not your turn!");
            return;
        }

        var currentParticipant = CombatManager.Instance.GetCurrentTurnParticipant();
        
        // Try to find the token - on clients it may need to be found by uniqueId
        attackerToken = currentParticipant.token;
        if (attackerToken == null && !string.IsNullOrEmpty(currentParticipant.uniqueId))
        {
            attackerToken = TokenManager.Instance?.GetTokenForCharacter(currentParticipant.uniqueId);
        }
        
        if (attackerToken == null)
        {
            Debug.LogWarning("WeaponAttackManager: No token for current participant");
            return;
        }

        if (attackerToken.getCharacterType() == CharacterType.Enemy)
        {
            enemyAttacker = attackerToken.getEnemyData();
            attacker = null;
            isEnemyAttacker = true;
        }
        else
        {
            attacker = attackerToken.getCharacterData();
            enemyAttacker = null;
            isEnemyAttacker = false;
        }

        if (attacker == null && enemyAttacker == null)
        {
            Debug.LogWarning("WeaponAttackManager: No attacker data found");
            return;
        }

        //Check if already acted this turn
        if (CombatManager.Instance.HasCurrentParticipantActed())
        {
            Debug.Log("WeaponAttackManager: Already acted this turn");
            return;
        }

        //Get weapon info
        if (isEnemyAttacker)
        {
            currentWeaponName = enemyAttacker.weapon;
            currentWeaponDamage = enemyAttacker.weaponDamage;
        }
        else
        {
            currentWeaponName = attacker.weapon;
            currentWeaponDamage = attacker.weaponDamage;
        }

        if (string.IsNullOrEmpty(currentWeaponName) || string.IsNullOrEmpty(currentWeaponDamage))
        {
            Debug.LogWarning("WeaponAttackManager: Attacker has no weapon equipped");
            return;
        }

        //Enter targeting mode
        StartTargeting();
    }

    /// <summary>
    /// Start targeting mode for weapon attack
    /// </summary>
    private void StartTargeting()
    {
        isTargeting = true;

        // Update UI
        if (targetingIndicator != null)
        {
            targetingIndicator.SetActive(true);
        }

        if (targetingText != null)
        {
            targetingText.text = $"Attacking with {currentWeaponName}\nClick on an enemy to attack";
        }
    }

    /// <summary>
    /// Cancel targeting mode
    /// </summary>
    public void CancelTargeting()
    {
        isTargeting = false;
        attacker = null;
        enemyAttacker = null;
        isEnemyAttacker = false;
        attackerToken = null;
        currentWeaponName = null;
        currentWeaponDamage = null;

        if (targetingIndicator != null)
        {
            targetingIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// Try to attack the target token
    /// </summary>
    private void TryAttackToken(Token targetToken)
    {
        // Can't attack yourself
        if (targetToken == attackerToken)
        {
            return;
        }

        // Check if adjacent (melee range)
        if (!IsAdjacent(attackerToken, targetToken))
        {
            if (targetingText != null)
            {
                targetingText.text = $"Too far away!\nMove closer to attack with {currentWeaponName}";
            }
            return;
        }

        // Execute the attack
        ExecuteAttack(targetToken);
    }

    /// <summary>
    /// Check if two tokens are adjacent (within 1 tile of each other)
    /// </summary>
    private bool IsAdjacent(Token token1, Token token2)
    {
        if (token1 == null || token2 == null) return false;

        Tile tile1 = token1.GetCurrentTile();
        Tile tile2 = token2.GetCurrentTile();

        if (tile1 == null || tile2 == null)
        {
            // Fallback: use world positions
            Vector3 pos1 = token1.transform.position;
            Vector3 pos2 = token2.transform.position;
            float distance = Vector2.Distance(new Vector2(pos1.x, pos1.y), new Vector2(pos2.x, pos2.y));
            return distance <= 1.5f; // Within 1.5 units (allowing for diagonal)
        }

        // Get grid positions from tiles
        int x1 = tile1.GridX;
        int y1 = tile1.GridY;
        int x2 = tile2.GridX;
        int y2 = tile2.GridY;

        // Check if within 1 tile (including diagonals)
        int dx = Mathf.Abs(x1 - x2);
        int dy = Mathf.Abs(y1 - y2);

        // Adjacent means within 1 tile horizontally and vertically
        return dx <= 1 && dy <= 1;
    }

    /// <summary>
    /// Execute the weapon attack
    /// </summary>
    private void ExecuteAttack(Token targetToken)
    {
        // Parse and roll weapon damage
        int damage = RollWeaponDamage(currentWeaponDamage);

        // Add strength modifier for melee weapons
        int strMod = GetStrengthModifier(attacker);
        int totalDamage = damage + strMod;

        // Get target's participant index
        int targetIndex = GetParticipantIndex(targetToken);

        bool isClient = Unity.Netcode.NetworkManager.Singleton != null 
            && Unity.Netcode.NetworkManager.Singleton.IsClient 
            && !Unity.Netcode.NetworkManager.Singleton.IsServer;

        // Apply damage - clients send via RPC, host applies directly
        if (CombatManager.Instance != null && targetIndex >= 0)
        {
            if (isClient && PlayerConnectionManager.Instance != null)
            {
                string attackerName = isEnemyAttacker ? (enemyAttacker?.name ?? "Unknown") : (attacker?.charName ?? "Unknown");
                PlayerConnectionManager.Instance.RequestCombatDamageServerRpc(
                    targetIndex, totalDamage, $"{attackerName} attacked with {currentWeaponName}");
            }
            else
            {
                CombatManager.Instance.ApplyDamage(targetIndex, totalDamage);
                CombatManager.Instance.SetCurrentParticipantActed(true);
            }
        }

        // Log the attack
        if (CombatLogger.Instance != null)
        {
            string attackerName = CombatLogger.GetParticipantName(attackerToken);
            string targetName = CombatLogger.GetParticipantName(targetToken);
            GridPosition attackerPos = CombatLogger.GetTokenPosition(attackerToken);
            GridPosition targetPos = CombatLogger.GetTokenPosition(targetToken);
            
            Debug.Log($"CombatLogger: Logging damage - {attackerName} -> {targetName} for {totalDamage} damage");
            CombatLogger.Instance.LogDamage(attackerName, targetName, totalDamage,
                $"attacked with {currentWeaponName}", attackerPos, targetPos);
        }
        else
        {
            Debug.LogWarning("WeaponAttackManager: CombatLogger.Instance is null");
        }

        // Broadcast to chat
        BroadcastAttack(targetToken, damage, strMod, totalDamage);

        // End targeting
        CancelTargeting();
    }

    /// <summary>
    /// Parse weapon damage string (e.g., "1d6", "2d8") and roll dice
    /// </summary>
    private int RollWeaponDamage(string damageExpr)
    {
        if (string.IsNullOrEmpty(damageExpr))
        {
            return 0;
        }

        // Parse format: "XdY" where X is count and Y is sides
        string[] parts = damageExpr.ToLower().Split('d');
        
        if (parts.Length != 2)
        {
            Debug.LogWarning($"WeaponAttackManager: Invalid damage expression '{damageExpr}'");
            return 0;
        }

        if (!int.TryParse(parts[0], out int diceCount))
        {
            diceCount = 1; // Default to 1 die
        }

        if (!int.TryParse(parts[1], out int diceSides))
        {
            Debug.LogWarning($"WeaponAttackManager: Invalid dice sides in '{damageExpr}'");
            return 0;
        }

        // Roll the dice
        int total = 0;
        for (int i = 0; i < diceCount; i++)
        {
            total += Random.Range(1, diceSides + 1);
        }

        return total;
    }

    /// <summary>
    /// Get strength modifier for melee damage
    /// </summary>
    private int GetStrengthModifier(CharacterData character)
    {
        if (isEnemyAttacker)
        {
            return enemyAttacker != null ? (enemyAttacker.strength - 10) / 2 : 0;
        }
        else
        {
            return character != null ? (character.strength - 10) / 2 : 0;
        }
    }

    /// <summary>
    /// Broadcast the attack to chat
    /// </summary>
    private void BroadcastAttack(Token target, int diceRoll, int strMod, int totalDamage)
    {
        if (ChatNetwork.Instance == null) return;

        string attackerName = isEnemyAttacker ? (enemyAttacker?.name ?? "Unknown") : (attacker?.charName ?? "Unknown");
        string targetName = GetTokenName(target);
        string modText = strMod >= 0 ? $"+{strMod}" : strMod.ToString();

        string message = $"attacked {targetName} with {currentWeaponName}! ({currentWeaponDamage}{modText}: {totalDamage} damage)";
        ChatNetwork.Instance.SendMessage(message, attackerName);
    }

    /// <summary>
    /// Get the participant index for a token in combat
    /// </summary>
    private int GetParticipantIndex(Token token)
    {
        if (CombatManager.Instance == null) return -1;

        var initiativeOrder = CombatManager.Instance.GetInitiativeOrder();
        if (initiativeOrder == null) return -1;

        for (int i = 0; i < initiativeOrder.Count; i++)
        {
            if (initiativeOrder[i].token == token)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the current character (from combat turn, selected token, or player assignment)
    /// </summary>
    private CharacterData GetCurrentCharacter()
    {
        // First try: combat turn participant
        if (CombatManager.Instance != null && CombatManager.Instance.GetCombatState() == CombatState.Active)
        {
            var currentParticipant = CombatManager.Instance.GetCurrentTurnParticipant();
            if (currentParticipant.token != null && currentParticipant.token.getCharacterData() != null)
            {
                return currentParticipant.token.getCharacterData();
            }
            
            // If combat is active but token is null/destroyed, try to find the token by uniqueId from spawned tokens
            if (!string.IsNullOrEmpty(currentParticipant.uniqueId))
            {
                var tokens = TokenManager.Instance?.GetSpawnedTokens();
                if (tokens != null)
                {
                    foreach (var token in tokens)
                    {
                        if (token == null) continue;
                        
                        // Match by character ID or enemy name
                        if (token.getCharacterData() != null && 
                            token.getCharacterData().id == currentParticipant.uniqueId)
                        {
                            return token.getCharacterData();
                        }
                        if (token.getEnemyData() != null && 
                            $"enemy_{token.getEnemyData().name}" == currentParticipant.uniqueId)
                        {
                            return token.getCharacterData(); // Enemies don't have CharacterData
                        }
                    }
                }
            }
        }

        // Second try: currently selected token
        if (TokenManager.Instance != null && TokenManager.Instance.GetSelectedToken() != null)
        {
            var selectedToken = TokenManager.Instance.GetSelectedToken();
            if (selectedToken.getCharacterData() != null)
            {
                return selectedToken.getCharacterData();
            }
        }

        // Third try: player assignment
        if (PlayerAssignmentHelper.Instance != null)
        {
            return PlayerAssignmentHelper.Instance.GetMyCharacter();
        }

        return null;
    }

    /// <summary>
    /// Get the current token
    /// </summary>
    private Token GetCurrentToken()
    {
        // First try: combat turn participant
        if (CombatManager.Instance != null && CombatManager.Instance.GetCombatState() == CombatState.Active)
        {
            var currentParticipant = CombatManager.Instance.GetCurrentTurnParticipant();
            if (currentParticipant.token != null)
            {
                return currentParticipant.token;
            }
            
            // If combat is active but token is null/destroyed, try to find the token by uniqueId from spawned tokens
            if (!string.IsNullOrEmpty(currentParticipant.uniqueId))
            {
                var tokens = TokenManager.Instance?.GetSpawnedTokens();
                if (tokens != null)
                {
                    foreach (var token in tokens)
                    {
                        if (token == null) continue;
                        
                        // Match by character ID or enemy name
                        if (token.getCharacterData() != null && 
                            token.getCharacterData().id == currentParticipant.uniqueId)
                        {
                            return token;
                        }
                        if (token.getEnemyData() != null && 
                            $"enemy_{token.getEnemyData().name}" == currentParticipant.uniqueId)
                        {
                            return token;
                        }
                    }
                }
            }
        }

        // Second try: currently selected token
        if (TokenManager.Instance != null && TokenManager.Instance.GetSelectedToken() != null)
        {
            return TokenManager.Instance.GetSelectedToken();
        }

        return null;
    }

    /// <summary>
    /// Get name of a token
    /// </summary>
    private string GetTokenName(Token token)
    {
        if (token == null) return "Unknown";

        if (token.getCharacterType() == CharacterType.Enemy)
        {
            return token.getEnemyData()?.name ?? "Enemy";
        }
        else
        {
            return token.getCharacterData()?.charName ?? "Character";
        }
    }

    /// <summary>
    /// Check if currently targeting
    /// </summary>
    public bool IsTargeting()
    {
        return isTargeting;
    }

    /// <summary>
    /// Check if the given token is the attacker (for clicking on self to cancel)
    /// </summary>
    public bool IsAttacker(Token token)
    {
        return token != null && token == attackerToken;
    }

    /// <summary>
    /// Update the weapon attack button state based on combat turn
    /// </summary>
    public void UpdateWeaponButtonState()
    {
        if (weaponAttackButton != null)
        {
            bool combatActive = CombatManager.Instance != null && CombatManager.Instance.IsCombatActive();
            bool canAct = !combatActive || (!CombatManager.Instance.HasCurrentParticipantActed() && CombatManager.Instance.IsMyTurn());
            weaponAttackButton.interactable = canAct;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (weaponAttackButton != null)
        {
            weaponAttackButton.onClick.RemoveListener(OnWeaponAttackButtonClicked);
        }
    }
}
