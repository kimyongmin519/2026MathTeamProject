using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.Players
{
    public class BoostGaugeUI : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Image gaugeFill;
        [SerializeField] private Color chargingColor = new Color(0.1f, 0.75f, 1f, 1f);
        [SerializeField] private Color readyColor = new Color(1f, 0.8f, 0.05f, 1f);
        [SerializeField] private Color boostingColor = new Color(0.85f, 0.2f, 1f, 1f);

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            if (gaugeFill != null)
            {
                gaugeFill.type = Image.Type.Filled;
                gaugeFill.fillMethod = Image.FillMethod.Horizontal;
                gaugeFill.fillOrigin = 0;
                gaugeFill.fillAmount = 0f;
            }
        }

        private void Update()
        {
            if (playerController == null || gaugeFill == null)
            {
                return;
            }

            gaugeFill.fillAmount = playerController.IsBoosting
                ? playerController.BoostTime01
                : playerController.BoostGauge01;

            if (playerController.IsBoosting)
            {
                gaugeFill.color = Color.Lerp(boostingColor, Color.white, Pulse());
            }
            else if (playerController.IsBoostReady)
            {
                gaugeFill.color = Color.Lerp(readyColor, Color.white, Pulse());
            }
            else
            {
                gaugeFill.color = chargingColor;
            }
        }

        private float Pulse()
        {
            return (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.25f;
        }
    }
}
