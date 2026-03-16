using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 2.2f, 0);
    public Vector2 size = new Vector2(80, 12);
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color healthColor = Color.green;
    public Color borderColor = Color.black;

    private EnemyController enemy;
    private Camera mainCamera;

    void Start()
    {
        enemy = GetComponent<EnemyController>();
        mainCamera = Camera.main;
    }

    void OnGUI()
    {
        if (enemy == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 worldPos = transform.position + offset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0) return;

        float guiY = Screen.height - screenPos.y;

        float healthPercent = enemy.GetHealthPercent();
        if (healthPercent <= 0) return;

        Rect bgRect = new Rect(screenPos.x - size.x / 2, guiY - size.y / 2, size.x, size.y);
        GUI.color = borderColor;
        GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

        Rect innerBgRect = new Rect(bgRect.x + 1, bgRect.y + 1, bgRect.width - 2, bgRect.height - 2);
        GUI.color = backgroundColor;
        GUI.DrawTexture(innerBgRect, Texture2D.whiteTexture);

        Rect healthRect = new Rect(innerBgRect.x, innerBgRect.y, innerBgRect.width * healthPercent, innerBgRect.height);
        GUI.color = Color.Lerp(Color.red, Color.green, healthPercent);
        GUI.DrawTexture(healthRect, Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}
