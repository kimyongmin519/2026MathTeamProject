using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.Players
{
    public class BoostGaugeUI : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Vector2 gaugeSize = new Vector2(420f, 28f);
        [SerializeField] private float bottomOffset = 55f;
        [SerializeField] private Color chargingColor = new Color(0.1f, 0.75f, 1f, 1f);
        [SerializeField] private Color readyColor = new Color(1f, 0.8f, 0.05f, 1f);
        [SerializeField] private Color boostingColor = new Color(0.85f, 0.2f, 1f, 1f);

        private Image _fillImage;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }

            CreateGauge();
        }

        private void Update()
        {
            if (playerController == null || _fillImage == null)
            {
                return;
            }

            _fillImage.fillAmount = playerController.IsBoosting
                ? playerController.BoostTime01
                : playerController.BoostGauge01;

            if (playerController.IsBoosting)
            {
                _fillImage.color = Color.Lerp(boostingColor, Color.white, Pulse());
            }
            else if (playerController.IsBoostReady)
            {
                _fillImage.color = Color.Lerp(readyColor, Color.white, Pulse());
            }
            else
            {
                _fillImage.color = chargingColor;
            }
        }

        private void CreateGauge()
        {
            GameObject canvasObject = new GameObject("Boost Gauge Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage("Background", canvasObject.transform);
            background.color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
            SetRect(background.rectTransform, gaugeSize + new Vector2(8f, 8f));

            Image fill = CreateImage("Fill", background.transform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            fill.color = chargingColor;
            SetRect(fill.rectTransform, gaugeSize);
            fill.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            fill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            fill.rectTransform.anchoredPosition = Vector2.zero;
            _fillImage = fill;
        }

        private Image CreateImage(string objectName, Transform parent)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            return imageObject.GetComponent<Image>();
        }

        private void SetRect(RectTransform rectTransform, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, bottomOffset);
            rectTransform.sizeDelta = size;
        }

        private float Pulse()
        {
            return (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.25f;
        }
    }
}
