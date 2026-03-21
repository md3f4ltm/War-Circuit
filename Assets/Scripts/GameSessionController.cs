using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameSessionController : MonoBehaviour
{
    public int startingCoins = 0;
    public int killCoinReward = 8;
    public int healthUpgradeCost = 25;
    public float healthUpgradeAmount = 25f;
    public int attackUpgradeCost = 25;
    public float attackUpgradeAmount = 8f;
    public int fireOrbCost = 40;
    public int freezeOrbCost = 40;
    public float shopOpenDelay = 1.25f;
    public float healthDropChance = 0.1f;
    public string healthPickupTemplateName = "HealthPickupTemplate";

    private int coins;
    private bool shopOpen;
    private string feedbackMessage = string.Empty;
    private float feedbackMessageUntil;
    private WaveSpawner waveSpawner;
    private PlayerController playerController;
    private PlayerOrbController orbController;
    private bool isSubscribed;
    private Coroutine openShopRoutine;

    void Start()
    {
        coins = startingCoins;
        ResolveReferences();
        EnsureSubscribed();
    }

    void OnDestroy()
    {
        if (openShopRoutine != null)
        {
            StopCoroutine(openShopRoutine);
        }

        Unsubscribe();
    }

    void Update()
    {
        ResolveReferences();
        EnsureSubscribed();

        if (!shopOpen || Keyboard.current == null)
        {
            return;
        }

        HandleShopHotkeys();

        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            StartNextWave();
        }
    }

    void ResolveReferences()
    {
        if (waveSpawner == null)
        {
            waveSpawner = GetComponent<WaveSpawner>();
            waveSpawner ??= FindFirstObjectByType<WaveSpawner>();
        }

        if (playerController != null && orbController != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
        {
            return;
        }

        playerController = playerObject.GetComponent<PlayerController>();
        orbController = playerObject.GetComponent<PlayerOrbController>();
    }

    void EnsureSubscribed()
    {
        if (isSubscribed || waveSpawner == null)
        {
            return;
        }

        waveSpawner.EnemyKilled -= HandleEnemyKilled;
        waveSpawner.EnemyKilled += HandleEnemyKilled;
        waveSpawner.WaveCleared -= HandleWaveCleared;
        waveSpawner.WaveCleared += HandleWaveCleared;
        isSubscribed = true;
    }

    void Unsubscribe()
    {
        if (!isSubscribed || waveSpawner == null)
        {
            return;
        }

        waveSpawner.EnemyKilled -= HandleEnemyKilled;
        waveSpawner.WaveCleared -= HandleWaveCleared;
        isSubscribed = false;
    }

    void HandleEnemyKilled(Vector3 deathPosition)
    {
        coins += killCoinReward;
        TrySpawnHealthPickup(deathPosition);
    }

    void HandleWaveCleared(int waveNumber)
    {
        playerController?.SetGameplayLocked(true);
        playerController?.PlayVictoryAnimation();

        if (openShopRoutine != null)
        {
            StopCoroutine(openShopRoutine);
        }

        openShopRoutine = StartCoroutine(OpenShopAfterDelay(waveNumber));
    }

    IEnumerator OpenShopAfterDelay(int waveNumber)
    {
        yield return new WaitForSeconds(shopOpenDelay);
        shopOpen = true;
        ShowFeedback($"Wave {waveNumber} cleared. Spend your coins.");
        openShopRoutine = null;
    }

    void TrySpawnHealthPickup(Vector3 deathPosition)
    {
        if (Random.value > healthDropChance)
        {
            return;
        }

        Vector3 spawnPosition = deathPosition + Vector3.up * 0.75f;
        GameObject template = RuntimeSceneTemplateLibrary.FindSceneTemplate(healthPickupTemplateName);
        GameObject pickup = template != null
            ? Instantiate(template, spawnPosition, Quaternion.identity)
            : CreateFallbackHealthPickup(spawnPosition);

        pickup.name = "HealthPickup";
        pickup.SetActive(true);
    }

    GameObject CreateFallbackHealthPickup(Vector3 position)
    {
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pickup.transform.position = position;

        Renderer renderer = pickup.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.95f, 0.2f, 0.25f);
        }

        pickup.AddComponent<HealthPickup>();
        return pickup;
    }

    void ShowFeedback(string message)
    {
        feedbackMessage = message;
        feedbackMessageUntil = Time.time + 2.2f;
    }

    void SpendCoins(int amount)
    {
        coins -= amount;
    }

    bool CanAfford(int amount)
    {
        return coins >= amount;
    }

    void BuyHealth()
    {
        if (!CanAfford(healthUpgradeCost) || playerController == null)
        {
            return;
        }

        SpendCoins(healthUpgradeCost);
        playerController.IncreaseMaxHealth(healthUpgradeAmount);
        ShowFeedback($"+{healthUpgradeAmount:0} max health");
    }

    void BuyAttack()
    {
        if (!CanAfford(attackUpgradeCost) || playerController == null)
        {
            return;
        }

        SpendCoins(attackUpgradeCost);
        playerController.IncreaseAttackDamage(attackUpgradeAmount);
        ShowFeedback($"+{attackUpgradeAmount:0} attack damage");
    }

    void BuyFireOrb()
    {
        if (!CanAfford(fireOrbCost) || orbController == null)
        {
            return;
        }

        SpendCoins(fireOrbCost);
        orbController.AddFireOrb();
        ShowFeedback("Fire orb added");
    }

    void BuyFreezeOrb()
    {
        if (!CanAfford(freezeOrbCost) || orbController == null)
        {
            return;
        }

        SpendCoins(freezeOrbCost);
        orbController.AddFreezeOrb();
        ShowFeedback("Freeze orb added");
    }

    void StartNextWave()
    {
        if (!shopOpen || waveSpawner == null)
        {
            return;
        }

        shopOpen = false;
        playerController?.SetGameplayLocked(false);
        waveSpawner.StartNextWave();
    }

    void HandleShopHotkeys()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) BuyHealth();
        if (Keyboard.current.digit2Key.wasPressedThisFrame) BuyAttack();
        if (Keyboard.current.digit3Key.wasPressedThisFrame) BuyFireOrb();
        if (Keyboard.current.digit4Key.wasPressedThisFrame) BuyFreezeOrb();
    }

    void DrawShopOption(Rect rect, string hotkey, string title, string description, int cost, System.Action onClick)
    {
        bool canAfford = CanAfford(cost);
        DrawFilledRect(rect, canAfford ? new Color(0.08f, 0.11f, 0.09f, 0.92f) : new Color(0.12f, 0.12f, 0.12f, 0.84f));
        GUI.Box(rect, string.Empty);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperCenter,
            wordWrap = true
        };
        GUIStyle costStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        float titleY = rect.y + 10f;
        float descriptionY = rect.y + 40f;
        float descriptionHeight = 28f;
        float costY = rect.y + rect.height - 46f;
        float buttonY = rect.y + rect.height - 28f;

        GUI.Label(new Rect(rect.x + 10f, rect.y + 10f, 30f, 20f), hotkey);
        GUI.Label(new Rect(rect.x + 10f, titleY, rect.width - 20f, 24f), title, titleStyle);
        GUI.Label(new Rect(rect.x + 16f, descriptionY, rect.width - 32f, descriptionHeight), description, textStyle);
        GUI.Label(new Rect(rect.x + 12f, costY, rect.width - 24f, 18f), $"Cost: {cost}", costStyle);

        GUI.enabled = canAfford;
        if (GUI.Button(new Rect(rect.x + 28f, buttonY, rect.width - 56f, 22f), "Buy"))
        {
            onClick?.Invoke();
        }
        GUI.enabled = true;
    }

    void OnGUI()
    {
        GUIStyle coinStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(Screen.width * 0.5f - 100f, 20f, 200f, 40f), $"Coins: {coins}", coinStyle);

        if (Time.time < feedbackMessageUntil && !string.IsNullOrEmpty(feedbackMessage))
        {
            GUIStyle feedbackStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Box(new Rect(Screen.width * 0.5f - 160f, 68f, 320f, 34f), feedbackMessage, feedbackStyle);
        }

        if (!shopOpen)
        {
            return;
        }

        float panelWidth = Mathf.Min(820f, Screen.width - 50f);
        float panelHeight = Mathf.Min(430f, Screen.height - 90f);
        Rect panelRect = new Rect(Screen.width * 0.5f - panelWidth * 0.5f, Screen.height * 0.5f - panelHeight * 0.5f, panelWidth, panelHeight);
        DrawFilledRect(panelRect, new Color(0.04f, 0.07f, 0.05f, 0.9f));
        GUI.Box(panelRect, "Wave Shop");

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.UpperCenter
        };

        GUI.Label(new Rect(panelRect.x + 20f, panelRect.y + 22f, panelRect.width - 40f, 30f), "Choose upgrades or press 1-4", headerStyle);
        GUI.Label(new Rect(panelRect.x + 20f, panelRect.y + 54f, panelRect.width - 40f, 22f), "Press Enter or Start Wave when you are ready.", hintStyle);

        float gap = 28f;
        float cardWidth = (panelRect.width - gap * 3f) * 0.5f;
        float cardHeight = 126f;
        float leftX = panelRect.x + gap;
        float rightX = leftX + cardWidth + gap;
        float topY = panelRect.y + 106f;
        float bottomY = topY + cardHeight + 18f;

        DrawShopOption(new Rect(leftX, topY, cardWidth, cardHeight), "1", "Health", $"+{healthUpgradeAmount:0} max health and heal.", healthUpgradeCost, BuyHealth);
        DrawShopOption(new Rect(rightX, topY, cardWidth, cardHeight), "2", "Attack", $"+{attackUpgradeAmount:0} sword damage.", attackUpgradeCost, BuyAttack);
        DrawShopOption(new Rect(leftX, bottomY, cardWidth, cardHeight), "3", "Fire Orb", "Press Q to shoot a burning fire orb.", fireOrbCost, BuyFireOrb);
        DrawShopOption(new Rect(rightX, bottomY, cardWidth, cardHeight), "4", "Freeze Orb", "Circles you and slows enemies on contact.", freezeOrbCost, BuyFreezeOrb);

        Rect startButtonRect = new Rect(panelRect.x + panelRect.width * 0.5f - 110f, panelRect.y + panelRect.height - 50f, 220f, 34f);
        DrawFilledRect(startButtonRect, new Color(0.15f, 0.17f, 0.15f, 0.95f));
        if (GUI.Button(startButtonRect, "Start Wave"))
        {
            StartNextWave();
        }
    }

    void DrawFilledRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
